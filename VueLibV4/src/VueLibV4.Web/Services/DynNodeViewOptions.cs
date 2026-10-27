namespace VueLibV4.Web;

/// <summary>
/// 裸标签节点（生成视图 / 手写 cshtml 中的 <c>dyn-*</c> 标签）选项承载类。
/// 各 JSON 选项组属性为【JSON 字符串】，推荐用
/// <see cref="VueLibV4.Services.Extensions.JsonExtensions.ToJson"/> 从匿名对象生成，
/// 有 IDE 补全与编译期检查，不用手写转义：
/// <code>
/// @using VueLibV4.Web
/// @using VueLibV4.Services.Extensions
/// @{
///     var n0 = new DynNodeViewOptions {
///         comoptions   = new { size = "small", clearable = true }.ToJson(),
///         labeloptions = new { label = "编码", required = true, show = true }.ToJson(),
///         itemoptions  = new { @class = "col-span-2" }.ToJson()
///     };
/// }
/// &lt;dyn-el-input modelname="Code" comoptions="@n0.comoptions" labeloptions="@n0.labeloptions" /&gt;
/// </code>
/// 属性名与运行时配置契约一致（全小写无分隔）；属性以 <c>comoptions="@n0.comoptions"</c>
/// 形式输出时由 Razor 自动 HTML 编码，前端 _BaseLayout 会把字符串 JSON.parse 回对象。
/// </summary>
public sealed class DynNodeViewOptions
{
    /// <summary>透传给底层 UI 组件的参数（el-input 的 size/placeholder/clearable…），JSON 对象字符串。</summary>
    public string? comoptions { get; set; }

    /// <summary>标签壳选项：label/required/show/labelposition/labelwidth，JSON 对象字符串。</summary>
    public string? labeloptions { get; set; }

    /// <summary>节点外层项选项：style/class，JSON 对象字符串。</summary>
    public string? itemoptions { get; set; }

    /// <summary>包裹层选项，JSON 对象字符串。</summary>
    public string? wrapperoptions { get; set; }

    /// <summary>父→子属性穿透白名单（逗号分隔的配置点路径，如 comoptions.size）。</summary>
    public string? compassthrough { get; set; }

    /// <summary>联动脚本（JS 函数体，返回 {shell,labeloptions,comoptions} 补丁）；配置契约键名同为 evalcondition。</summary>
    public string? evalcondition { get; set; }

    /// <summary>
    /// 值绑定表达式（只读）。配置后 modelinfo 由表达式求值，不再走 modelname 路径读写；
    /// 适用于展示型组件（badge/text/echart）或计算字段。表达式作用域：model=表单对象, $self=旧值, row=行数据。
    /// 例："{{ model.price * model.qty }}"
    /// </summary>
    public string? modelexpr { get; set; }

    /// <summary>
    /// 值写回表达式（可选）。modelexpr 模式下组件赋值时执行，$value 为新值。
    /// 例："model.total = $value"
    /// </summary>
    public string? modelsetexpr { get; set; }

    /// <summary>
    /// 属性关联绑定（JSON 对象字符串）。键=属性路径（如 comoptions.disabled），值=表达式或字面量。
    /// 运行时求值后合并到对应 options，优先级：静态配置 &lt; bindings &lt; evalcondition。
    /// 例：{"labeloptions.show":"{{ model.type === 'A' }}","comoptions.disabled":"{{ model.status === 'submitted' }}"}
    /// </summary>
    public string? bindings { get; set; }
}
