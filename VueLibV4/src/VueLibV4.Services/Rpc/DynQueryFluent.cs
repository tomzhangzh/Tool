using System.Linq.Expressions;
using DynamicExpresso;
using SqlSugar;

namespace VueLibV4.Services.Rpc;

/// <summary>
/// 能力桥专用的强类型查询门面（eval 中以实体名为变量，如 DesktopShortcut）。
///
/// 为什么需要它：DynamicExpresso 只能把 lambda 绑定成委托（Func），无法为自定义方法
/// 生成 Expression&lt;Func&lt;T,...&gt;&gt; 表达式树（Queryable 的 LINQ 方法是引擎内置特例），
/// 而 SqlSugar 的 Where/OrderBy/Select 只接受 Expression 或 SQL 字符串。
/// 因此门面让用户以【字符串 lambda】书写条件，内部用同一个 DynamicExpresso 解释器
/// 编译成真正的表达式树再交给 SqlSugar —— 过滤/排序仍在数据库侧执行，不会全表加载到内存。
///
/// 用法（eval）：
/// <code>
/// DesktopShortcut.Where("x => x.IsActive == active").OrderBy("x => x.SortNo").ToList()
/// DesktopShortcut.WhereIF(flag, "x => x.Name.Contains(name)").Take(10).ToList()
/// DesktopShortcut.SelectFields("Name, SortNo").ToList()
/// </code>
/// 字符串 lambda 中可直接引用 args 注入的参数（如上例的 active、name），与外层 eval 共享变量。
/// </summary>
public class DynQueryFluent<T> where T : class, new()
{
    private readonly Interpreter _interpreter;
    private ISugarQueryable<T> _q;

    public DynQueryFluent(Interpreter interpreter, ISqlSugarClient db)
    {
        _interpreter = interpreter;
        _q = db.Queryable<T>();
    }

    /// <summary>条件过滤："x => x.IsActive == true"，表达式树下推 SQL</summary>
    public DynQueryFluent<T> Where(string predicate)
    {
        if (!string.IsNullOrWhiteSpace(predicate))
            _q = _q.Where(CompilePredicate(predicate));
        return this;
    }

    /// <summary>条件为 true 时才追加过滤（动态拼条件用）</summary>
    public DynQueryFluent<T> WhereIF(bool condition, string predicate)
        => condition ? Where(predicate) : this;

    /// <summary>升序："x => x.SortNo"</summary>
    public DynQueryFluent<T> OrderBy(string keySelector)
    {
        if (!string.IsNullOrWhiteSpace(keySelector))
            _q = _q.OrderBy(CompileKey(keySelector), OrderByType.Asc);
        return this;
    }

    /// <summary>降序："x => x.Id"</summary>
    public DynQueryFluent<T> OrderByDescending(string keySelector)
    {
        if (!string.IsNullOrWhiteSpace(keySelector))
            _q = _q.OrderBy(CompileKey(keySelector), OrderByType.Desc);
        return this;
    }

    /// <summary>跳过 n 行（与 Take 配合分页，需先 OrderBy）</summary>
    public DynQueryFluent<T> Skip(int count)
    {
        if (count > 0) _q = _q.Skip(count);
        return this;
    }

    /// <summary>取前 n 行</summary>
    public DynQueryFluent<T> Take(int count)
    {
        if (count > 0) _q = _q.Take(count);
        return this;
    }

    /// <summary>只取指定列（SQL 字段名/逗号分隔，支持 AS 别名），返回动态对象数组</summary>
    public List<dynamic> SelectFields(string fields)
        => string.IsNullOrWhiteSpace(fields)
            ? _q.ToList().Cast<dynamic>().ToList()
            : _q.Select<dynamic>(fields).ToList();

    public List<T> ToList() => _q.ToList();

    /// <summary>取第一行，无数据时 SqlSugar 返回 null</summary>
    public T First() => _q.First();

    public int Count() => _q.Count();

    public bool Any() => _q.Any();

    // ---- lambda 字符串编译（与 eval 共享解释器/变量） ----

    private Expression<Func<T, bool>> CompilePredicate(string code)
    {
        var (body, names) = SplitLambda(code);
        return _interpreter.ParseAsExpression<Func<T, bool>>(body, names);
    }

    private Expression<Func<T, object>> CompileKey(string code)
    {
        var (body, names) = SplitLambda(code);
        return _interpreter.ParseAsExpression<Func<T, object>>(body, names);
    }

    /// <summary>
    /// 把 "x => x.IsActive" / "(x) => x.SortNo" 拆成（方法体, 参数名）。
    /// DynamicExpresso 的 ParseAsExpression 要求只传方法体、参数名单独声明；
    /// 没有 =&gt; 时把整串当作方法体，参数名默认 x。
    /// </summary>
    private static (string Body, string[] Names) SplitLambda(string code)
    {
        var arrow = code.IndexOf("=>", StringComparison.Ordinal);
        if (arrow < 0) return (code.Trim(), new[] { "x" });

        var body = code.Substring(arrow + 2).Trim();
        var head = code.Substring(0, arrow).Trim().TrimStart('(').TrimEnd(')').Trim();
        if (head.Length == 0) return (body, new[] { "x" });

        // 去掉可能的参数类型前缀（"T x" → "x"）；无类型时整段就是参数名
        var first = head.Split(',')[0].Trim();
        var parts = first.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var name = parts.Length > 1 ? parts[^1] : first;
        return (body, new[] { string.IsNullOrWhiteSpace(name) ? "x" : name });
    }
}
