using Newtonsoft.Json.Linq;

namespace VueLibV4.Platform.Models;

/// <summary>
/// 模板能力位：决定运行时渲染路径（规格/积木驱动 vs. 共享模型驱动）。
/// 权威来源是模板自己的 ConfigJson —— 顶层写 <c>"specDriven": true|false</c> 即可声明，
/// 新增模板无需修改任何控制器代码；未声明时按下方内置基线判定，保证既有 14 个模板行为不变。
/// </summary>
public static class TemplateCapabilities
{
    /// <summary>ConfigJson 顶层的能力位键名（全小写，与配置键契约一致）。</summary>
    public const string SpecDrivenKey = "specDriven";

    /// <summary>
    /// 内置基线：外壳视图以 <c>@model DynWebPage</c> 接收实例规格的模板 Code（自读 SpecJson 拼装）。
    /// 仅用于「ConfigJson 尚未声明能力位」的存量数据；声明后以声明为准。
    /// </summary>
    private static readonly HashSet<string> BaselineSpecDriven = new(StringComparer.OrdinalIgnoreCase)
    {
        "triscreen-blocks",
        "filterlist-crud",
        "tree-detail",
        "tree-detail-v2",
        "tree-master-detail",
        "list-master-detail",
        "filter-list-open-window",
        "filter-list-open-windowV2",
        "filter-list-drawer-leftV2",
        "detail-modal",
        "detail-modal-v2",
        "filter-list-tabbed",
        "tab-host",
        "tab-container",
        "tabs-basic"
    };

    /// <summary>
    /// 是否规格驱动渲染：ConfigJson 顶层 <c>specDriven</c> 优先（布尔或可解析字符串），
    /// 未声明时回退内置基线；Code 为空恒为 false。
    /// </summary>
    public static bool IsSpecDriven(string? code, JObject? templateConfig)
    {
        var declared = templateConfig?[SpecDrivenKey];
        if (declared != null && declared.Type != JTokenType.Null)
        {
            if (declared.Type == JTokenType.Boolean) return declared.Value<bool>();
            var text = declared.ToString().Trim();
            if (bool.TryParse(text, out var b)) return b;
            if (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (text == "0" || text.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        }
        return !string.IsNullOrWhiteSpace(code) && BaselineSpecDriven.Contains(code.Trim());
    }
}
