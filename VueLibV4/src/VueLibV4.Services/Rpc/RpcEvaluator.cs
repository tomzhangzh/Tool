using System.Collections;
using System.Reflection;
using DynamicExpresso;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Services.Data;

namespace VueLibV4.Services.Rpc;

/// <summary>
/// 能力桥执行引擎（Scoped）：
/// 1) Eval：用 DynamicExpresso 执行前端传来的 C# 表达式/语句，注入 DI 容器中的全部 IDependency 服务
///    以及三个内置变量 Crud（DynamicCrudService）/ Biz（业务库）/ Platform（平台库）；
/// 2) Invoke：按服务名+方法名反射调用 DI 服务的公共方法。
/// 结果统一物化为内存对象（IQueryable/IEnumerable 立即 ToList，释放数据库连接后再 JSON 序列化）。
/// </summary>
public class RpcEvaluator
{
    /// <summary>eval 代码文本最大长度，防止超长提交</summary>
    public const int MaxCodeLength = 20000;

    private readonly IServiceProvider _sp;
    private readonly RpcServiceCatalog _catalog;
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _crud;

    /// <summary>最近一次执行结果是否被行数上限截断（控制器据此写审计日志）</summary>
    public bool LastResultTruncated { get; private set; }

    public RpcEvaluator(IServiceProvider sp, RpcServiceCatalog catalog, DbFactory dbs, DynamicCrudService crud)
    {
        _sp = sp;
        _catalog = catalog;
        _dbs = dbs;
        _crud = crud;
    }

    // ============================== Eval（自由代码） ==============================

    /// <summary>
    /// 执行一段 C# 代码并返回结果。
    /// 代码示例：DesktopShortcutService.ListBySolution(null).Where(x =&gt; x.IsActive == true).ToList()
    /// </summary>
    public async Task<object> EvalAsync(string code, IDictionary<string, object> args, int timeoutSec, int maxRows)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("代码不能为空");
        if (code.Length > MaxCodeLength) throw new ArgumentException($"代码长度不能超过 {MaxCodeLength} 字符");
        LastResultTruncated = false;

        // Biz/Platform 为本次执行临时打开的库客户端：结果物化后立即释放
        using var biz = _dbs.BusinessDb();
        using var platform = _dbs.PlatformDb();

        var interpreter = CreateInterpreter(biz, platform, args);

        // DynamicExpresso 不响应取消令牌，用 WhenAny 赛跑保证超时即返回（计算线程随后自行结束）
        var timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSec));
        var work = Task.Run(() => interpreter.Eval(code));
        var finished = await Task.WhenAny(work, Task.Delay(timeout));
        if (finished != work)
            throw new TimeoutException($"RPC 代码执行超过 {timeoutSec} 秒，已终止");
        if (work.IsFaulted)
            throw work.Exception?.GetBaseException() ?? new Exception("代码执行失败");
        var result = work.Result;

        using var unwrapCts = new CancellationTokenSource(timeout);
        result = await UnwrapTaskAsync(result, unwrapCts.Token);
        return Materialize(result, maxRows);
    }

    private Interpreter CreateInterpreter(SqlSugarClient biz, SqlSugarClient platform, IDictionary<string, object> args)
    {
        var interpreter = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions);

        // LINQ 扩展方法：Enumerable（内存集合上的 lambda 链，如 List().Where(x => ...).ToList()）
        interpreter.Reference(typeof(Enumerable));
        interpreter.Reference(typeof(Queryable));

        // 内置变量
        interpreter.SetVariable("Crud", _crud);
        interpreter.SetVariable("Biz", biz);
        interpreter.SetVariable("Platform", platform);

        // DI 容器中的全部 IDependency 服务（按实现类短名注入，如 DesktopShortcutService）
        foreach (var entry in _catalog.Entries)
        {
            object svc;
            try { svc = _sp.GetService(entry.ServiceType); }
            catch { continue; }
            if (svc == null) continue;
            // 保留名不允许服务覆盖
            if (RpcServiceCatalog.ReservedNames.Contains(entry.Name, StringComparer.OrdinalIgnoreCase)) continue;
            interpreter.SetVariable(entry.Name, svc);
        }

        // 强类型实体查询门面：变量名=实体类名（如 DesktopShortcut），
        // 支持 DesktopShortcut.Where("x => x.IsActive == active") 这类字符串 lambda（编译为表达式树下推 SQL）。
        // 避开保留名与服务名；在 args 之前注册，前端显式参数同名时以参数为准。
        var fluentOpenType = typeof(DynQueryFluent<>);
        foreach (var kv in _catalog.Entities)
        {
            if (RpcServiceCatalog.ReservedNames.Contains(kv.Key, StringComparer.OrdinalIgnoreCase)) continue;
            if (_catalog.Find(kv.Key) != null) continue;
            var facade = Activator.CreateInstance(fluentOpenType.MakeGenericType(kv.Value), interpreter, platform);
            if (facade != null) interpreter.SetVariable(kv.Key, facade);
        }

        // 前端传入的参数（与代码分离，杜绝注入）
        if (args != null)
        {
            foreach (var kv in args)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key))
                    interpreter.SetVariable(kv.Key, kv.Value);
            }
        }

        return interpreter;
    }

    // ============================== Invoke（指定方法） ==============================

    /// <summary>按服务名调用其公共方法，args 为按参数顺序排列的 JSON 数组</summary>
    public async Task<object> InvokeAsync(string serviceName, string methodName, JArray args, int timeoutSec, int maxRows)
    {
        LastResultTruncated = false;
        var entry = _catalog.Find(serviceName)
            ?? throw new ArgumentException($"未找到 RPC 服务：{serviceName}（可用服务见 GET /api/rpc/services）");
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("缺少方法名");

        var svc = _sp.GetService(entry.ServiceType)
            ?? throw new InvalidOperationException($"服务 {serviceName} 无法从容器解析");

        var candidates = RpcServiceCatalog.GetCallableMethods(entry.ImplType)
            .Where(m => m.Name.Equals(methodName.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.GetParameters().Count(p => !p.IsOptional))
            .ToList();
        if (candidates.Count == 0)
            throw new ArgumentException($"服务 {serviceName} 没有可调用的方法 {methodName}");

        var argCount = args?.Count ?? 0;
        Exception lastError = null;

        foreach (var method in candidates)
        {
            var ps = method.GetParameters();
            // 跳过带 out/ref 的重载；参数数量需落在 [必选参数数, 总参数数] 区间
            if (ps.Any(p => p.IsOut || p.ParameterType.IsByRef)) continue;
            var required = ps.Count(p => !p.IsOptional);
            if (argCount < required || argCount > ps.Length) continue;

            object[] callArgs;
            try
            {
                callArgs = BindArguments(ps, args);
            }
            catch (Exception ex)
            {
                lastError = ex;
                continue; // 参数类型不匹配，尝试下一个重载
            }

            // 超时赛跑：保证超时即返回（目标方法若不响应取消，其线程随后自行结束）
            var timeout = TimeSpan.FromSeconds(Math.Max(1, timeoutSec));
            var work = Task.Run(() => method.Invoke(svc, callArgs));
            var finished = await Task.WhenAny(work, Task.Delay(timeout));
            if (finished != work)
                throw new TimeoutException($"RPC 方法 {serviceName}.{methodName} 执行超过 {timeoutSec} 秒");
            if (work.IsFaulted)
                throw work.Exception?.GetBaseException() ?? new Exception("方法执行失败");
            object result = work.Result;

            using var unwrapCts = new CancellationTokenSource(timeout);
            result = await UnwrapTaskAsync(result, unwrapCts.Token);
            return Materialize(result, maxRows);
        }

        throw new ArgumentException(
            $"找不到与参数匹配的重载 {serviceName}.{methodName}（传了 {argCount} 个参数）" +
            (lastError != null ? "：" + lastError.Message : ""));
    }

    /// <summary>把 JSON 数组按方法参数类型逐个转换</summary>
    private static object[] BindArguments(ParameterInfo[] ps, JArray args)
    {
        var callArgs = new object[ps.Length];
        for (var i = 0; i < ps.Length; i++)
        {
            var pi = ps[i];
            var token = args != null && i < args.Count ? args[i] : null;

            if (pi.ParameterType == typeof(CancellationToken))
            {
                callArgs[i] = CancellationToken.None;
                continue;
            }

            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
            {
                var underlying = Nullable.GetUnderlyingType(pi.ParameterType);
                if (pi.ParameterType.IsValueType && underlying == null && !pi.IsOptional)
                    callArgs[i] = Activator.CreateInstance(pi.ParameterType);
                else
                    callArgs[i] = pi.IsOptional ? pi.DefaultValue : null;
                continue;
            }

            callArgs[i] = token.ToObject(pi.ParameterType)
                ?? (pi.IsOptional ? pi.DefaultValue : null);
        }
        return callArgs;
    }

    // ============================== 通用辅助 ==============================

    /// <summary>Task/Task&lt;T&gt; 结果解包（同步等待，受同一取消令牌约束）</summary>
    private static async Task<object> UnwrapTaskAsync(object result, CancellationToken ct)
    {
        if (result is Task task)
        {
            await Task.Run(() =>
            {
                // 不做无限等待：外层 CancellationTokenSource 到期会抛 OperationCanceledException
                while (!task.IsCompleted)
                {
                    if (!task.Wait(200))
                        ct.ThrowIfCancellationRequested();
                }
                if (task.IsFaulted)
                    throw task.Exception?.GetBaseException() ?? new Exception("任务执行失败");
            }, ct);
            var resultProp = task.GetType().GetProperty("Result");
            return resultProp?.GetValue(task);
        }
        return result;
    }

    /// <summary>
    /// 物化延迟结果：IQueryable/延迟迭代器必须在数据库连接释放前执行；
    /// 同时对返回行数做硬上限保护（截断时置 LastResultTruncated，由控制器写审计日志）。
    /// 字符串/字典/字节数组保持原样；集合统一物化为 JSON 友好的数组。
    /// </summary>
    private object Materialize(object result, int maxRows)
    {
        if (result == null || result is string || result is IDictionary || result is byte[]) return result;

        if (result is IEnumerable enumerable)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                if (list.Count >= maxRows) { LastResultTruncated = true; break; }
                list.Add(item);
            }
            return list;
        }

        return result;
    }
}
