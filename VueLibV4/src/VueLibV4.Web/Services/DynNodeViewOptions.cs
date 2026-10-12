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

    /// <summary>联动脚本（JS 函数体，返回 {shell,labeloptions,comoptions} 补丁），注意配置契约键名为 evalCondition。</summary>
    public string? evalcondition { get; set; }
}
