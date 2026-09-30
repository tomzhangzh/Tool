using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Services.Rpc;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Controllers;

/// <summary>
/// 能力桥：让前端 JS 直接调用后端 C# 服务 / 执行 C# 表达式。
///
///   GET  /api/rpc/services                          服务与方法目录（设计器浏览面板用）
///   POST /api/rpc/invoke/{service}/{method}         按名调用 DI 服务的公共方法，body 为参数 JSON 数组
///   POST /api/rpc/eval                              执行 C# 代码，body：{ code, args, timeoutSec }
///
/// 前端用法：
///   const data = await dyn.service('DesktopShortcutService').ListBySolution(null);
///   const rows = await dyn.eval(`DesktopShortcutService.List().Where(x =&gt; x.IsActive == true).ToList()`);
///
/// 配置（appsettings → Dyn:Rpc）：
///   Enabled        总开关（默认 true；平台接入鉴权前，仅建议内网开发环境开放）
///   EvalEnabled    eval 自由代码开关（默认 true；生产环境建议关闭，只保留 invoke）
///   MaxRows        返回集合行数硬上限（默认 10000）
///   TimeoutSeconds 单次执行超时秒数（默认 30）
/// </summary>
[Route("api/rpc")]
[ApiController]
public class RpcController : ControllerBase
{
    private readonly RpcServiceCatalog _catalog;
    private readonly RpcEvaluator _evaluator;
    private readonly IConfiguration _config;
    private readonly ILogger<RpcController> _logger;

    public RpcController(RpcServiceCatalog catalog, RpcEvaluator evaluator,
        IConfiguration config, ILogger<RpcController> logger)
    {
        _catalog = catalog;
        _evaluator = evaluator;
        _config = config;
        _logger = logger;
    }

    // ---------------- 服务目录 ----------------

    [HttpGet("services")]
    public ApiResult Services()
    {
        if (!RpcEnabled) return ApiResult.Fail("RPC 能力桥未开启（Dyn:Rpc:Enabled=false）", 403);
        return ApiResult.Ok(_catalog.Describe());
    }

    // ---------------- 指定方法调用 ----------------

    [HttpPost("invoke/{service}/{method}")]
    public async Task<ApiResult> Invoke(string service, string method, [FromBody] JArray args)
    {
        if (!RpcEnabled) return ApiResult.Fail("RPC 能力桥未开启（Dyn:Rpc:Enabled=false）", 403);

        var sw = Stopwatch.StartNew();
        try
        {
            var data = await _evaluator.InvokeAsync(service, method, args, TimeoutSeconds, MaxRows);
            sw.Stop();
            _logger.LogInformation(
                "[Rpc] invoke {Service}.{Method} args={ArgsCount} truncated={Truncated} {Ms}ms",
                service, method, args?.Count ?? 0, _evaluator.LastResultTruncated, sw.ElapsedMilliseconds);
            return ApiResult.Ok(data);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[Rpc] invoke 失败 {Service}.{Method} {Ms}ms：{Msg}",
                service, method, sw.ElapsedMilliseconds, ex.Message);
            return ApiResult.Fail(ToErrorMessage(ex));
        }
    }

    // ---------------- 自由代码执行 ----------------

    [HttpPost("eval")]
    public async Task<ApiResult> Eval([FromBody] JObject body)
    {
        if (!RpcEnabled) return ApiResult.Fail("RPC 能力桥未开启（Dyn:Rpc:Enabled=false）", 403);
        if (!EvalEnabled) return ApiResult.Fail("eval 自由代码未开启（Dyn:Rpc:EvalEnabled=false）", 403);

        var code = body?["code"]?.ToString();
        if (string.IsNullOrWhiteSpace(code)) return ApiResult.Fail("缺少 code（要执行的 C# 代码）");
        var timeoutSec = body["timeoutSec"]?.Type == JTokenType.Integer
            ? body["timeoutSec"].Value<int>()
            : TimeoutSeconds;

        var args = NormalizeArgs(body["args"]);
        var sw = Stopwatch.StartNew();
        try
        {
            var data = await _evaluator.EvalAsync(code, args, timeoutSec, MaxRows);
            sw.Stop();
            _logger.LogInformation(
                "[Rpc]eval args={ArgsCount} truncated={Truncated} {Ms}ms code={Code}",
                args.Count, _evaluator.LastResultTruncated, sw.ElapsedMilliseconds,
                code.Length > 200 ? code[..200] + "..." : code);
            return ApiResult.Ok(data);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "[Rpc]eval 失败 {Ms}ms：{Msg}；code={Code}",
                sw.ElapsedMilliseconds, ex.Message, code.Length > 200 ? code[..200] + "..." : code);
            return ApiResult.Fail(ToErrorMessage(ex));
        }
    }

    // ---------------- 配置与辅助 ----------------

    private bool RpcEnabled => ReadBool("Dyn:Rpc:Enabled", true);
    private bool EvalEnabled => ReadBool("Dyn:Rpc:EvalEnabled", true);
    private int MaxRows => Math.Clamp(ReadInt("Dyn:Rpc:MaxRows", 10000), 1, 1_000_000);
    private int TimeoutSeconds => Math.Clamp(ReadInt("Dyn:Rpc:TimeoutSeconds", 30), 1, 600);

    private bool ReadBool(string key, bool def)
    {
        var v = _config[key];
        if (string.IsNullOrWhiteSpace(v)) return def;
        return v.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || v.Trim() == "1";
    }

    private int ReadInt(string key, int def)
        => int.TryParse(_config[key], out var v) && v > 0 ? v : def;

    /// <summary>反射调用异常取最内层真实信息，前端直接可读</summary>
    private static string ToErrorMessage(Exception ex)
    {
        var msg = ex.Message;
        if (ex is ArgumentException or InvalidOperationException or TimeoutException) return msg;
        // DynamicExpresso 解析异常 / TargetInvocationException 等同样直接返回 Message
        return msg;
    }

    /// <summary>
    /// 把前端传入的 args JSON 归一化为 CLR 值字典：
    /// 整数优先按 int 传递（避免 long 与实体 int 属性比较时表达式树类型不一致）。
    /// </summary>
    private static Dictionary<string, object> NormalizeArgs(JToken token)
    {
        var dict = new Dictionary<string, object>(StringComparer.Ordinal);
        if (token is not JObject jo) return dict;
        foreach (var kv in jo)
        {
            if (string.IsNullOrWhiteSpace(kv.Key)) continue;
            dict[kv.Key] = Unwrap(kv.Value);
        }
        return dict;
    }

    private static object Unwrap(JToken t)
    {
        if (t == null) return null;
        switch (t.Type)
        {
            case JTokenType.Null:
            case JTokenType.Undefined:
                return null;
            case JTokenType.Object:
            {
                var d = new Dictionary<string, object>();
                foreach (var kv in (JObject)t) d[kv.Key] = Unwrap(kv.Value);
                return d;
            }
            case JTokenType.Array:
                return ((JArray)t).Select(Unwrap).ToList();
            case JTokenType.Integer:
            {
                var l = t.Value<long>();
                return l is >= int.MinValue and <= int.MaxValue ? (object)(int)l : l;
            }
            case JTokenType.Float:
                return t.Value<double>();
            case JTokenType.Boolean:
                return t.Value<bool>();
            case JTokenType.Date:
                return t.Value<DateTime>();
            default:
                return ((JValue)t).Value;
        }
    }
}
