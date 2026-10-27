using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态网页模板导出与扩展视图骨架：把 PageJson 转成手写 dyn-xxx 标签模板，或生成扩展视图文件。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynpage")]
[ApiController]
public class DynPageTemplateController : ControllerBase
{
    private readonly IDynWebPageService _svc;
    private readonly IDynPageExtService? _pageExt;

    public DynPageTemplateController(
        IDynWebPageService svc,
        IDynPageExtService? pageExt = null)
    {
        _svc = svc;
        _pageExt = pageExt;
    }

    /// <summary>
    /// 导出手写模板：把 PageSet 的 ConfigJson 转成嵌套的 dyn-xxx 标签格式。
    /// 入参 {id}，返回 { template: "..." }。
    /// </summary>
    [HttpPost("export-template")]
    public ApiResult ExportTemplate([FromBody] JObject body)
    {
        var id = body["id"]?.Value<int>() ?? body["Id"]?.Value<int>() ?? 0;
        var page = id > 0 ? _svc.GetById(id) : null;
        if (page == null) return ApiResult.Fail("页面不存在");
        if (string.IsNullOrWhiteSpace(page.PageJson)) return ApiResult.Fail("页面没有配置");

        try
        {
            var cfg = JObject.Parse(page.PageJson);
            var template = BuildTemplate(cfg, 0);
            return ApiResult.Ok(new { template = template }, "导出成功");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("导出失败: " + ex.Message);
        }
    }

    /// <summary>递归把 JSON 配置转成手写模板标签</summary>
    private string BuildTemplate(JObject cfg, int indent)
    {
        var pad = new string(' ', indent * 4);
        var component = cfg["component"]?.ToString() ?? "dyn-el-container";
        var modelname = cfg["modelname"]?.ToString();
        var options = cfg["options"] as JObject;
        var comoptions = options?["comoptions"] as JObject;
        var labeloptions = options?["labeloptions"] as JObject;
        var children = cfg["childrenctrls"] as JArray;

        var sb = new StringBuilder();
        sb.Append(pad).Append("<").Append(component);
        if (!string.IsNullOrEmpty(modelname)) sb.Append(" modelname=\"").Append(modelname).Append("\"");

        // labeloptions → 独立属性
        if (labeloptions != null)
        {
            var label = labeloptions["label"]?.ToString();
            if (!string.IsNullOrEmpty(label)) sb.Append(" label=\"").Append(label).Append("\"");
            var required = labeloptions["required"]?.Value<bool>() ?? false;
            if (required) sb.Append(" required");
        }

        // comoptions 常用属性 → 独立属性
        if (comoptions != null)
        {
            AppendComOption(sb, comoptions, "placeholder", "placeholder");
            AppendComOption(sb, comoptions, "type", "type");
            AppendComOption(sb, comoptions, "size", "size");
            AppendComOption(sb, comoptions, "disabled", "disabled");
            AppendComOption(sb, comoptions, "clearable", "clearable");
            AppendComOption(sb, comoptions, "readonly", "readonly");
            AppendComOption(sb, comoptions, "min", "min");
            AppendComOption(sb, comoptions, "max", "max");
            AppendComOption(sb, comoptions, "multiple", "multiple");
            AppendComOption(sb, comoptions, "filterable", "filterable");
            AppendComOption(sb, comoptions, "show-word-limit", "show-word-limit");
            AppendComOption(sb, comoptions, "maxlength", "maxlength");
            // 剩余的特殊值用 :prop 绑定
            // options 数组用 :options
            var optionsArr = comoptions["options"] as JArray;
            if (optionsArr != null && optionsArr.HasValues)
            {
                sb.Append(" :options='").Append(optionsArr.ToString(Newtonsoft.Json.Formatting.None)).Append("'");
            }
        }

        if (children != null && children.Count > 0)
        {
            sb.AppendLine(">");
            foreach (var child in children)
            {
                sb.Append(BuildTemplate((JObject)child, indent + 1));
            }
            sb.Append(pad).AppendLine("</" + component + ">");
        }
        else
        {
            sb.AppendLine(" />");
        }
        return sb.ToString();
    }

    /// <summary>把 comoptions 里的属性转成独立标签属性</summary>
    private void AppendComOption(StringBuilder sb, JObject comoptions, string key, string attrName)
    {
        var token = comoptions[key];
        if (token == null || token.Type == JTokenType.Null) return;

        if (token.Type == JTokenType.Boolean)
        {
            if (token.Value<bool>()) sb.Append(" ").Append(attrName);
        }
        else if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
        {
            sb.Append(" ").Append(attrName).Append("=\"").Append(token.ToString()).Append("\"");
        }
        else
        {
            var val = token.ToString();
            if (!string.IsNullOrEmpty(val)) sb.Append(" ").Append(attrName).Append("=\"").Append(val).Append("\"");
        }
    }

    /// <summary>
    /// 为已保存的页面实例生成（或定位）扩展视图骨架文件，并把路径回填到 ExtViewPath。
    /// 文件已存在时不覆盖（保护手工定制）。入参 {id}。
    /// </summary>
    [HttpPost("ext-skeleton")]
    public async Task<ApiResult> ExtSkeleton([FromBody] JObject body, CancellationToken ct)
    {
        var id = body["id"]?.Value<int>() ?? body["Id"]?.Value<int>() ?? 0;
        var page = id > 0 ? _svc.GetById(id) : null;
        if (page == null) return ApiResult.Fail("页面不存在，请先保存页面");
        if (string.IsNullOrWhiteSpace(page.Code)) return ApiResult.Fail("页面 Code 为空，无法生成扩展视图");
        if (_pageExt == null) return ApiResult.Fail("扩展视图服务未注册");
        var path = await _pageExt.EnsureSkeletonAsync(page.Code, page.Name, ct);
        if (!string.Equals(page.ExtViewPath, path, StringComparison.OrdinalIgnoreCase))
        {
            page.ExtViewPath = path;
            _svc.Update(page);
        }
        return ApiResult.Ok(new { viewPath = path }, "扩展视图骨架已就绪");
    }
}
