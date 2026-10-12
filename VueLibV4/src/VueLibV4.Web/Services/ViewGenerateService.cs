using System.Globalization;
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
/// @{
///     var n0 = new DynNodeViewOptions {
///         comoptions = new { direction = "vertical", size = "small" }.ToJson()
///     };
/// }
/// &lt;dyn-grid-container :parentmodelinfo="form" comoptions="@n0.comoptions"&gt;
///   &lt;dyn-el-input modelname="Code" labeloptions="@n1.labeloptions"&gt;&lt;/dyn-el-input&gt;
/// &lt;/dyn-grid-container&gt;
/// </code>
/// 选项不手写 JSON 转义：@{ } 内用 <see cref="VueLibV4.Web.DynNodeViewOptions"/>
/// + 匿名对象 <c>.ToJson()</c> 生成 JSON 字符串（IDE 有补全、编译期检查键名），
/// 属性写成 <c>comoptions="@n0.comoptions"</c>，由 Razor 渲染时自动 HTML 编码。
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
    /// <summary>字符串型选项：配置键名 → DynNodeViewOptions 属性名（evalCondition 是配置契约里唯一的驼峰键）。</summary>
    private static readonly (string ConfigKey, string Member)[] StringOptions =
    {
        ("compassthrough", "compassthrough"),
        ("evalCondition", "evalcondition")
    };

    public string RenderView(JObject? config, string settingType, int settingId, string? settingName)
    {
        if (config?["component"]?.ToString() is not { Length: > 0 })
            throw new InvalidOperationException("ConfigJson 根节点缺少 component，不是组件树型配置。");
        var hostVar = HostModelVar(settingType);

        var inits = new List<string>();   // @{} 内的 DynNodeViewOptions 初始化
        var body = new StringBuilder();
        var seq = 0;

        void RenderNode(JObject node, int depth, bool isRoot)
        {
            var idx = seq++;
            var varName = "n" + idx;
            var pad = new string(' ', depth * 2);
            var tag = ToTagName(node["component"]!.ToString());
            body.Append(pad).Append('<').Append(tag);

            if (isRoot)
                body.Append(" :parentmodelinfo=\"").Append(hostVar).Append('"');

            var modelName = node["modelname"]?.ToString();
            if (!string.IsNullOrEmpty(modelName))
                body.Append(" modelname=\"").Append(WebUtility.HtmlEncode(modelName)).Append('"');

            var assigned = new List<string>(); // 已赋值的成员名，决定输出哪些属性
            var initLines = new List<string>();
            var options = node["options"] as JObject;
            if (options != null)
            {
                foreach (var g in JsonOptionGroups)
                {
                    if (options[g] is JObject jo && jo.Count > 0)
                    {
                        initLines.Add($"        {g} = {RenderOptionsExpr(jo)},");
                        assigned.Add(g);
                    }
                }
                foreach (var (configKey, member) in StringOptions)
                {
                    var s = options[configKey]?.ToString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        initLines.Add($"        {member} = {ToVerbatimLiteral(s)},");
                        assigned.Add(member);
                    }
                }
            }

            if (initLines.Count > 0)
            {
                inits.Add($"    var {varName} = new DynNodeViewOptions {{");
                inits.AddRange(initLines);
                inits.Add("    };");
                foreach (var m in assigned)
                    body.Append(' ').Append(m).Append("=\"@").Append(varName).Append('.').Append(m).Append('"');
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
        sb.AppendLine("@using VueLibV4.Web");
        sb.AppendLine("@using VueLibV4.Services.Extensions");
        sb.AppendLine("@* =============================================================================");
        sb.AppendLine($"   自动生成视图：PageSetting #{settingId}（{settingType} / {settingName}）");
        sb.AppendLine("   生成器：VueLibV4.Web.Services.ViewGenerateService。可自由修改本文件，");
        sb.AppendLine("   修改后即时生效（Razor 运行时编译）；再次点“生成视图”会【覆盖】本文件。");
        sb.AppendLine($"   宿主模型变量：{hostVar}（由嵌入的 {settingType} Block 提供，勿改名）。");
        sb.AppendLine("   选项写法：@{ } 内 new DynNodeViewOptions { comoptions = new { ... }.ToJson() }，");
        sb.AppendLine("   属性写 comoptions=\"@n0.comoptions\"，Razor 自动 HTML 转义；");
        sb.AppendLine("   也可直接改成 Vue 绑定，如 :comoptions=\"{ placeholder: 'x' }\"。");
        sb.AppendLine("   ============================================================================ *@");
        sb.AppendLine("@{");
        foreach (var l in inits) sb.AppendLine(l);
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

    // ------------------------------------------------------------------
    //  JToken → C# 初始化表达式（让生成的选项是“代码”而不是“字符串里的 JSON”）
    // ------------------------------------------------------------------

    /// <summary>
    /// 选项组 → C# 表达式：优先 <c>new { k = v, ... }.ToJson()</c>（IDE 可补全/查错）；
    /// 仅当键名不是合法 C# 标识符时回退为 C#11 原始字符串 """..."""（JSON 本体）。
    /// </summary>
    private static string RenderOptionsExpr(JToken token)
    {
        if (token is JObject jo && TryRenderObject(jo, out var expr))
            return expr + ".ToJson()";
        return ToRawStringLiteral(token.ToString(Formatting.None));
    }

    private static bool TryRenderObject(JObject jo, out string expr)
    {
        // 键值对保留原始键名；含 C# 关键字键（如 class）时改用 Dictionary 初始化器
        // （["class"]=...），避免在 Razor @{} 块里出现 @class —— @ 是 Razor 保留切换符。
        var pairs = new List<(string Key, string Val)>();
        var needDictionary = false;
        foreach (var p in jo.Properties())
        {
            if (!IsSimpleIdentifier(p.Name))
            {
                expr = null!;
                return false;
            }
            if (!TryRenderValue(p.Value, out var val))
            {
                expr = null!;
                return false;
            }
            if (CSharpKeywords.Contains(p.Name)) needDictionary = true;
            pairs.Add((p.Name, val));
        }
        if (pairs.Count == 0) { expr = "new { }"; return true; }
        if (needDictionary)
        {
            expr = "new Dictionary<string, object?> { "
                + string.Join(", ", pairs.Select(kv => $"[\"{kv.Key}\"] = {kv.Val}")) + " }";
        }
        else
        {
            expr = "new { " + string.Join(", ", pairs.Select(kv => $"{kv.Key} = {kv.Val}")) + " }";
        }
        return true;
    }

    private static bool TryRenderValue(JToken? token, out string expr)
    {
        switch (token)
        {
            case null:
            case JValue { Type: JTokenType.Null }:
                expr = "null";
                return true;
            case JObject jo:
                return TryRenderObject(jo, out expr);
            case JArray ja:
                var items = new List<string>();
                foreach (var it in ja)
                {
                    if (!TryRenderValue(it, out var v))
                    {
                        expr = null!;
                        return false;
                    }
                    items.Add(v);
                }
                expr = items.Count == 0
                    ? "new object?[0]"
                    : "new object?[] { " + string.Join(", ", items) + " }";
                return true;
            case JValue jv:
                switch (jv.Type)
                {
                    case JTokenType.Boolean:
                        expr = jv.Value<bool>() ? "true" : "false";
                        return true;
                    case JTokenType.Integer:
                        expr = ((IFormattable)jv.Value!).ToString(null, CultureInfo.InvariantCulture);
                        return true;
                    case JTokenType.Float:
                        expr = ((IFormattable)jv.Value!).ToString(null, CultureInfo.InvariantCulture);
                        return true;
                    case JTokenType.String:
                        expr = ToVerbatimLiteral(jv.Value<string>() ?? "");
                        return true;
                    case JTokenType.Date:
                        expr = ToVerbatimLiteral(jv.Value<DateTime>().ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture));
                        return true;
                    case JTokenType.Guid:
                        expr = ToVerbatimLiteral(jv.Value<Guid>().ToString());
                        return true;
                    default:
                        expr = null!;
                        return false; // 未知类型让上层整组回退原始字符串
                }
            default:
                expr = null!;
                return false;
        }
    }

    /// <summary>
    /// 字符串 → C# 字面量：普通内容用常规 "...";（转义 \ 和 " 及制表符/换行），
    /// 含引号/反斜杠/换行等时用逐字字符串 @"..."（双引号翻倍），保证任意内容都安全。
    /// </summary>
    private static string ToVerbatimLiteral(string s)
    {
        var needVerbatim = s.IndexOfAny(new[] { '"', '\\', '\r', '\n', '\t', '\0' }) >= 0;
        if (!needVerbatim)
            return "\"" + s + "\"";
        return "@\"" + s.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// C#11 原始字符串字面量：定界引号数取内容中最长连续引号 +2（至少 3 个），
    /// 保证首尾恰好是引号时也不会误判定界符；JSON 首尾空白由浏览器 JSON.parse 容忍。
    /// </summary>
    private static string ToRawStringLiteral(string s)
    {
        var maxRun = 0;
        var run = 0;
        foreach (var c in s)
        {
            if (c == '"') { run++; if (run > maxRun) maxRun = run; }
            else run = 0;
        }
        var q = new string('"', Math.Max(3, maxRun + 2));
        return q + s + q;
    }

    private static bool IsSimpleIdentifier(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        if (!(char.IsLetter(s[0]) || s[0] == '_')) return false;
        foreach (var c in s)
            if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
        return true;
    }

    private static readonly HashSet<string> CSharpKeywords = new()
    {
        "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const","continue",
        "decimal","default","delegate","do","double","else","enum","event","explicit","extern","false","finally",
        "fixed","float","for","foreach","goto","if","implicit","in","int","interface","internal","is","lock",
        "long","namespace","new","null","object","operator","out","override","params","private","protected",
        "public","readonly","ref","return","sbyte","sealed","short","sizeof","stackalloc","static","string",
        "struct","switch","this","throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort",
        "using","virtual","void","volatile","while"
    };

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
