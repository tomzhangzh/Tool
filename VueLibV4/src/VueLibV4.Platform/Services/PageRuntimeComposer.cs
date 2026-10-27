using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Services.Dependency;

namespace VueLibV4.Platform.Services;

/// <summary>
/// 页面运行时装配结果：控制器只负责「取参 → 调服务 → 渲染」，不再自行装配任何 JObject。
/// </summary>
public sealed class PageRuntimeResult
{
    /// <summary>是否装配成功；false 时 <see cref="Error"/> 为可直接回显的中文原因。</summary>
    public bool Ok { get; init; }

    /// <summary>失败原因（页面实例不存在 / 未绑定模板等）。</summary>
    public string? Error { get; init; }

    /// <summary>外壳 Razor 视图路径（模板 ViewPath，空则回退默认 CRUD 外壳）。</summary>
    public string ViewPath { get; init; } = string.Empty;

    /// <summary>视图模型：规格驱动模板传 <see cref="DynWebPage"/> 实例，共享模型模板传 <see cref="DynSharedModel"/>。</summary>
    public object? ViewModel { get; init; }

    /// <summary>页面标题（不含站点后缀，由控制器拼装 ViewData）。</summary>
    public string Title { get; init; } = string.Empty;

    internal static PageRuntimeResult Fail(string error) => new() { Ok = false, Error = error };
}

/// <summary>
/// 页面运行时装配器：把「读实例 → 读模板 → 推导参数 → 加载三屏 PageSetting → 组装共享模型」
/// 从控制器下沉为平台层服务。无 MVC 依赖，可脱离 HttpContext 单测。
/// </summary>
public interface IPageRuntimeComposer : IScopeDependency
{
    /// <summary>按页面实例 Id 装配运行时视图模型；实例/模板缺失时返回 <see cref="PageRuntimeResult.Ok"/>=false。</summary>
    PageRuntimeResult Compose(int pageId);
}

/// <inheritdoc cref="IPageRuntimeComposer"/>
public class PageRuntimeComposer : IPageRuntimeComposer
{
    /// <summary>模板未配置 ViewPath 时的默认外壳视图。</summary>
    public const string DefaultViewPath = "~/Views/DynTemplates/CrudBasic.cshtml";

    private readonly IDynWebPageService _webPages;
    private readonly IDynTemplateService _templates;
    private readonly IPageSettingService _settings;

    public PageRuntimeComposer(
        IDynWebPageService webPages,
        IDynTemplateService templates,
        IPageSettingService settings)
    {
        _webPages = webPages;
        _templates = templates;
        _settings = settings;
    }

    public PageRuntimeResult Compose(int pageId)
    {
        var page = _webPages.GetById(pageId);
        if (page == null) return PageRuntimeResult.Fail("页面实例不存在：" + pageId);

        var template = page.TemplateId != null ? _templates.GetById(page.TemplateId.Value) : null;
        if (template == null) return PageRuntimeResult.Fail("页面实例未绑定模板（TemplateId 为空）");

        var templateConfig = ParseObject(template.ConfigJson);
        var viewPath = string.IsNullOrWhiteSpace(template.ViewPath) ? DefaultViewPath : template.ViewPath;

        // 规格/积木驱动模板：壳视图自行从 SpecJson/ParamsJson 读实例规格并拼装列表 model，
        // 这里只透传 DynWebPage 实例（能力位由模板 ConfigJson 声明，见 TemplateCapabilities）。
        if (TemplateCapabilities.IsSpecDriven(template.Code, templateConfig))
            return new PageRuntimeResult { Ok = true, ViewPath = viewPath, ViewModel = page, Title = page.Name };

        var rawParams = TryParseDict(page.ParamsJson);
        var effective = BuildEffectiveParams(rawParams);
        // 模板 ConfigJson 顶层 mPassThrough（透传属性，向下传给外壳 / createapp）
        var passThrough = templateConfig["mPassThrough"] as JObject ?? new JObject();
        // 模板默认配置（外壳可选用）
        var defaultConfig = ParseObject(template.DefaultJson);

        var f = LoadSetting(TryGetInt(effective, "FilterPageSettingId"));
        var l = LoadSetting(TryGetInt(effective, "ListPageSettingId"));
        var d = LoadSetting(TryGetInt(effective, "DetailPageSettingId"));

        var model = new DynSharedModel
        {
            DynWebPageId = page.Id,
            DynTemplateId = template.Id,
            PageTitle = page.Name,
            TemplateCode = template.Code,
            RawParams = rawParams,
            EffectiveParams = effective,
            PassThrough = passThrough,
            TemplateConfig = defaultConfig,
            FilterConfig = f?.Config,
            FilterDefaultJson = f?.DefaultJson,
            FilterRenderMode = f?.RenderMode,
            FilterPartialPath = f?.PartialPath,
            ListConfig = l?.Config,
            ListDefaultJson = l?.DefaultJson,
            ListRenderMode = l?.RenderMode,
            ListPartialPath = l?.PartialPath,
            DetailConfig = d?.Config,
            DetailDefaultJson = d?.DefaultJson,
            DetailRenderMode = d?.RenderMode,
            DetailPartialPath = d?.PartialPath
        };

        return new PageRuntimeResult { Ok = true, ViewPath = viewPath, ViewModel = model, Title = page.Name };
    }

    /// <summary>TableName 自动补齐 dyndata 免 model 接口 Url（手动填写优先）。</summary>
    private static Dictionary<string, object> BuildEffectiveParams(Dictionary<string, object> raw)
    {
        var result = new Dictionary<string, object>(raw);
        if (HasValue(raw, "TableName"))
        {
            // 唯一数据端点：表名随 body 发送（{table,data} / {table,keys}），数据域由 project 坐标决定
            if (!HasValue(result, "ListUrl")) result["ListUrl"] = DynDataEndpoints.Search;
            if (!HasValue(result, "AddUrl")) result["AddUrl"] = DynDataEndpoints.Save;
            if (!HasValue(result, "EditUrl")) result["EditUrl"] = DynDataEndpoints.Save;
            if (!HasValue(result, "DeleteUrl")) result["DeleteUrl"] = DynDataEndpoints.Delete;
        }
        return result;
    }

    private static bool HasValue(Dictionary<string, object> d, string key)
        => d.TryGetValue(key, out var v) && v != null && !string.IsNullOrWhiteSpace(v.ToString());

    /// <summary>从参数字典安全读取 int?（值缺失、空或不可解析时返回 null）。</summary>
    private static int? TryGetInt(Dictionary<string, object> d, string key)
    {
        if (d.TryGetValue(key, out var v) && v != null && int.TryParse(v.ToString(), out var i)) return i;
        return null;
    }

    private static Dictionary<string, object> TryParseDict(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>();
        try { return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>(); }
        catch { return new Dictionary<string, object>(); }
    }

    private static JObject ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new JObject();
        try { return JObject.Parse(json); }
        catch { return new JObject(); }
    }

    /// <summary>读取 PageSetting 引用包：Config(UI渲染树) + DefaultJson(model骨架) + RenderMode + PartialPath。</summary>
    private SettingBundle? LoadSetting(int? settingId)
    {
        if (settingId == null || settingId <= 0) return null;
        var s = _settings.GetById(settingId.Value);
        if (s == null) return null;
        return new SettingBundle
        {
            Config = ParseObjectOrNull(s.ConfigJson),
            DefaultJson = s.DefaultJson,
            RenderMode = s.RenderMode,
            PartialPath = s.PartialPath
        };
    }

    private static JObject? ParseObjectOrNull(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JObject.Parse(json); }
        catch { return null; }
    }

    /// <summary>PageSetting 引用包：Config(UI渲染树) + DefaultJson(model骨架) + RenderMode + PartialPath。</summary>
    private sealed class SettingBundle
    {
        public JObject? Config { get; init; }
        public string? DefaultJson { get; init; }
        public string? RenderMode { get; init; }
        public string? PartialPath { get; init; }
    }
}
