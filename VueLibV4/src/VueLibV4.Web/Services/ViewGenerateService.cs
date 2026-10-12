using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Services.Dependency;

namespace VueLibV4.Web.Services;

/// <summary>
/// 视图生成服务：把一条 PageSetting 的组件配置树（ConfigJson：component/options/
/// modelname/childrenctrls）生成为【可手工修改的真实 cshtml】，例如：
/// <code>
/// &lt;dyn-grid-container :parentmodelinfo="form" comoptions="@co0"&gt;
///   &lt;dyn-el-input modelname="Code" labeloptions="@lo1" comoptions="@co1"&gt;&lt;/dyn-el-input&gt;
/// &lt;/dyn-grid-container&gt;
/// </code>
/// JSON 不手工转义：组件 JSON 写在 @{ } 内的 C# 逐字字符串变量里，属性写成
/// <c>comoptions="@co1"</c>，由 Razor 渲染时自动 HTML 编码（双引号 → &amp;quot;）。
/// 组件标签与运行时注册名保持同一规则：dyn- + kebab 组件名
/// （DynGridContainer → dyn-grid-container，DynElInput → dyn-el-input）。
/// 默认输出路径（相对 Web ContentRoot）：
///   ViewsAutoGenerate/{Project}/{TableName}/{SettingType}_{Id}.cshtml
/// 自动注册：实现 IScopeDependency，AddVueLibPlatform() 扫描程序集即可发现。
/// </summary>
public interface IViewGenerateService : IScopeDependency
{
    /// <summary>
    /// 按 pageSettingId 读 PageSetting 并生成 cshtml 到 outputPath；
    /// outputPath 为空时使用默认路径 ViewsAutoGenerate/{Project}/{TableName}/{SettingType}_{Id}.cshtml。
    /// 每次调用都重新生成并【覆盖】（文件是可手改的派生产物，按钮二次生成前需确认覆盖）。
    /// 仅组件树型设置（Filter/Detail，根节点含 component）可生成；
    /// List 的 {columns:[...]} 不是组件树，抛 InvalidOperationException。
    /// </summary>
    Task<ViewGenerateResult> GenerateAsync(int pageSettingId, string? outputPath = null, CancellationToken ct = default);

    /// <summary>
    /// 纯函数：组件树 JObject → 完整 cshtml 文本（不做 IO，便于单测/预览）。
    /// settingType 决定嵌入宿主的模型变量名（Filter=filterModel，Detail=form）。
    /// </summary>
    string RenderView(JObject? config, string settingType, int settingId, string? settingName);

    /// <summary>计算默认输出物理路径（相对 ContentRoot 的 ViewsAutoGenerate/...）。</summary>
    string ResolveDefaultPhysicalPath(PageSetting setting, string? projectCode);
}

/// <summary>视图生成结果。</summary>
public class ViewGenerateResult
{
    public int SettingId { get; set; }
    public string SettingType { get; set; } = "";
    public string? TableName { get; set; }
    public string ProjectCode { get; set; } = "";
    /// <summary>生成内容标签数（根节点计入）</summary>
    public int TagCount { get; set; }
    /// <summary>Razor 虚拟路径 ~/ViewsAutoGenerate/...（输出在 ContentRoot 外时回物理路径）</summary>
    public string ViewPath { get; set; } = "";
    /// <summary>实际写入的物理路径</summary>
    public string PhysicalPath { get; set; } = "";
}

/// <inheritdoc />
public class ViewGenerateService : IViewGenerateService
{
    private readonly IWebHostEnvironment _env;
    private readonly IPageSettingService _settings;
    private readonly IDynProjectService _projects;

    public ViewGenerateService(IWebHostEnvironment env, IPageSettingService settings, IDynProjectService projects)
    {
        _env = env;
        _settings = settings;
        _projects = projects;
    }

    public async Task<ViewGenerateResult> GenerateAsync(int pageSettingId, string? outputPath = null, CancellationToken ct = default)
    {
        var setting = _settings.GetById(pageSettingId)
            ?? throw new InvalidOperationException($"PageSetting 不存在：Id={pageSettingId}");
        if (string.IsNullOrWhiteSpace(setting.ConfigJson) || !TryParseTree(setting.ConfigJson, out var config))
            throw new InvalidOperationException($"PageSetting #{pageSettingId} 的 ConfigJson 为空或不是组件树（根节点需含 component）。");

        var markup = RenderView(config, setting.SettingType, setting.Id, setting.Name);

        var projectCode = ResolveProjectCode(setting.ProjectId);
        var physical = !string.IsNullOrWhiteSpace(outputPath)
            ? Path.GetFullPath(outputPath)
            : ResolveDefaultPhysicalPath(setting, projectCode);

        Directory.CreateDirectory(Path.GetDirectoryName(physical)!);
        // 确定性产物，每次覆盖；UTF-8 无 BOM（避免 Razor/前端解析 BOM 干扰）
        await File.WriteAllTextAsync(physical, markup, new UTF8Encoding(false), ct);

        var rel = Path.GetRelativePath(_env.ContentRootPath, physical).Replace('\\', '/');
        var viewPath = rel.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(rel)
            ? physical
            : "~/" + rel;

        return new ViewGenerateResult
        {
            SettingId = setting.Id,
            SettingType = setting.SettingType,
            TableName = setting.TableName,
            ProjectCode = projectCode,
            TagCount = CountTags(config!),
            ViewPath = viewPath,
            PhysicalPath = physical
        };
    }

    // ------------------------------------------------------------------
    //  组件树 → cshtml
    // ------------------------------------------------------------------

    /// <summary>组件树根节点必须带 component；List 的 {columns:[...]} 在此被拦下。</summary>
    private static bool TryParseTree(string json, out JObject? tree)
    {
        tree = null;
        try
        {
            if (JToken.Parse(json) is JObject jo && jo["component"]?.ToString() is { Length: > 0 })
            {
                tree = jo;
                return true;
            }
        }
        catch { /* 非法 JSON 按非组件树处理 */ }
        return false;
    }

    /// <summary>设置类型 → 嵌入宿主 Block 的 Vue 模型变量名（与 FilterApp/DetailApp 模板一致）。</summary>
    private static string HostModelVar(string settingType) => (settingType ?? "").Trim() switch
    {
        "Filter" => "filterModel",
        "Detail" => "form",
        _ => throw new InvalidOperationException(
            $"暂不支持为 SettingType='{settingType}' 生成视图：仅组件树型设置（Filter/Detail）可生成，List 列配置不是组件树。")
    };

    /// <summary>序列化为属性值的 JSON 选项组（节点 options 下，值为对象）。</summary>
    private static readonly string[] JsonOptionGroups = { "comoptions", "labeloptions", "itemoptions", "wrapperoptions" };
    /// <summary>字符串型选项（直接以普通属性输出）。</summary>
    private static readonly string[] StringOptions = { "compassthrough", "evalCondition" };

    public string RenderView(JObject? config, string settingType, int settingId, string? settingName)
    {
        if (config?["component"]?.ToString() is not { Length: > 0 })
            throw new InvalidOperationException("ConfigJson 根节点缺少 component，不是组件树型配置。");
        var hostVar = HostModelVar(settingType);

        var vars = new List<string>();   // @{} 内的变量声明
        var body = new StringBuilder();
        var seq = 0;

        void RenderNode(JObject node, int depth, bool isRoot)
        {
            var idx = seq++;
            var pad = new string(' ', depth * 2);
            var tag = ToTagName(node["component"]!.ToString());
            body.Append(pad).Append('<').Append(tag);

            if (isRoot)
                body.Append(" :parentmodelinfo=\"").Append(hostVar).Append('"');

            var modelName = node["modelname"]?.ToString();
            if (!string.IsNullOrEmpty(modelName))
                body.Append(" modelname=\"").Append(WebUtility.HtmlEncode(modelName)).Append('"');

            var options = node["options"] as JObject;
            if (options != null)
            {
                foreach (var g in JsonOptionGroups)
                {
                    if (options[g] is JObject jo && jo.Count > 0)
                    {
                        var varName = OptionVarPrefix(g) + idx; // comoptions→co0、labeloptions→lo0…
                        vars.Add($"    var {varName} = {ToVerbatim(jo.ToString(Formatting.None))};");
                        body.Append(' ').Append(g).Append("=\"@").Append(varName).Append('"');
                    }
                }
                foreach (var g in StringOptions)
                {
                    var s = options[g]?.ToString();
                    if (!string.IsNullOrEmpty(s))
                        body.Append(' ').Append(g).Append("=\"").Append(WebUtility.HtmlEncode(s)).Append('"');
                }
            }

            var children = node["childrenctrls"] as JArray;
            if (children is not { Count: > 0 })
            {
                body.Append("></").Append(tag).Append(">\n");
                return;
            }

            body.Append(">\n");
            foreach (var child in children)
                if (child is JObject co && co["component"] != null)
                    RenderNode(co, depth + 1, false);
            body.Append(pad).Append("</").Append(tag).Append(">\n");
        }

        RenderNode(config, 1, true);

        var sb = new StringBuilder();
        sb.AppendLine("@* =============================================================================");
        sb.AppendLine($"   自动生成视图：PageSetting #{settingId}（{settingType} / {settingName}）");
        sb.AppendLine("   生成器：VueLibV4.Web.Services.ViewGenerateService。可自由修改本文件，");
        sb.AppendLine("   修改后即时生效（Razor 运行时编译）；再次点“生成视图”会【覆盖】本文件。");
        sb.AppendLine($"   宿主模型变量：{hostVar}（由嵌入的 {settingType} Block 提供，勿改名）。");
        sb.AppendLine("   JSON 写法：@{ } 内 C# 逐字字符串（双引号写两个），属性 comoptions=\"@co0\" 由 Razor 自动转义；");
        sb.AppendLine("   也可直接改成 Vue 绑定，如 :comoptions=\"{ placeholder: 'x' }\"。");
        sb.AppendLine("   ============================================================================ *@");
        sb.AppendLine("@{");
        foreach (var v in vars) sb.AppendLine(v);
        sb.AppendLine("}");
        sb.Append(body);
        return sb.ToString();
    }

    private static int CountTags(JObject node)
    {
        var n = 1;
        if (node["childrenctrls"] is JArray children)
            foreach (var c in children)
                if (c is JObject co && co["component"] != null) n += CountTags(co);
        return n;
    }

    /// <summary>
    /// 组件名 → 运行时标签名，与前端注册名同一套解析规则：
    /// Dyn 前缀组件（注册名 DynGridContainer）→ dyn-grid-container（Vue 还原为 PascalCase 注册名）；
    /// 非 Dyn 前缀组件（注册名 JsonDesigner）→ json-designer（直接 kebab 注册名，勿加 dyn-）。
    /// </summary>
    private static string ToTagName(string component)
    {
        var kebab = Regex.Replace(component, "([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();
        if (component.StartsWith("Dyn", StringComparison.Ordinal) && component.Length > 3)
            return "dyn-" + Regex.Replace(component[3..], "([a-z0-9])([A-Z])", "$1-$2").ToLowerInvariant();
        return kebab;
    }

    /// <summary>选项组 → cshtml 变量前缀：comoptions=co / labeloptions=lo / itemoptions=io / wrapperoptions=wo。</summary>
    private static string OptionVarPrefix(string group) => group switch
    {
        "comoptions" => "co",
        "labeloptions" => "lo",
        "itemoptions" => "io",
        "wrapperoptions" => "wo",
        _ => "op"
    };

    /// <summary>紧凑 JSON → Razor 安全的 C# 逐字字符串：@"..."（内部双引号翻倍）。</summary>
    private static string ToVerbatim(string compactJson)
        => "@\"" + compactJson.Replace("\"", "\"\"") + "\"";

    // ------------------------------------------------------------------
    //  其它
    // ------------------------------------------------------------------

    public string ResolveDefaultPhysicalPath(PageSetting setting, string? projectCode)
    {
        var project = SanitizeSegment(string.IsNullOrWhiteSpace(projectCode) ? "Default" : projectCode);
        var table = SanitizeSegment(string.IsNullOrWhiteSpace(setting.TableName) ? "General" : setting.TableName);
        var type = SanitizeSegment(string.IsNullOrWhiteSpace(setting.SettingType) ? "Setting" : setting.SettingType);
        var rel = Path.Combine("ViewsAutoGenerate", project, table, $"{type}_{setting.Id}.cshtml");
        return Path.GetFullPath(Path.Combine(_env.ContentRootPath, rel));
    }

    private string? ResolveProjectCode(int? projectId)
    {
        if (projectId is null or 0) return null;
        try { return _projects.GetById(projectId.Value)?.Code; }
        catch { return null; }
    }

    /// <summary>路径片段只保留 字母数字-下划线-短横点，其余替换为 _，杜绝路径穿越。</summary>
    private static string SanitizeSegment(string segment)
    {
        var s = Regex.Replace(segment.Trim(), @"[^A-Za-z0-9._-]", "_");
        return string.IsNullOrWhiteSpace(s) ? "_" : s;
    }
}
