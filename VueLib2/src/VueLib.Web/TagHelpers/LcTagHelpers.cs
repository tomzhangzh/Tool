using Microsoft.AspNetCore.Razor.TagHelpers;
using SqlSugar;
using System.Text;
using VueLib.Web.Models;

namespace VueLib.Web.TagHelpers;

/// <summary>
/// &lt;lc-page code="demo-form" /&gt; 在任意 Razor 页面中嵌入一个低代码页面。
/// 渲染为挂载容器 + 引导脚本（复用 VueLib 运行时）。
/// </summary>
[HtmlTargetElement("lc-page")]
public class LcPageTagHelper : TagHelper
{
    private readonly ISqlSugarClient _sugar;

    public LcPageTagHelper(ISqlSugarClient sugar) => _sugar = sugar;

    public string Code { get; set; } = "";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        var uid = "lcp_" + Guid.NewGuid().ToString("N")[..8];
        output.Attributes.SetAttribute("id", uid);
        output.Attributes.SetAttribute("data-lc-page", Code);

        var platform = _sugar.AsTenant().GetConnectionScope("platform");
        await platform.Queryable<LcPage>().AnyAsync(p => p.Code == Code);

        var sb = new StringBuilder();
        sb.Append("<div id=\"").Append(uid).Append("\" class=\"lc-embedded-page\" data-lc-page=\"").Append(Code).AppendLine("\"></div>");
        sb.AppendLine("<script>");
        sb.AppendLine("(function(){");
        sb.AppendLine("  function boot(){");
        sb.AppendLine("    if (!window.VueLib) { setTimeout(boot, 200); return; }");
        sb.Append("    VueLib.runtime.bootEmbeddedPage(document.getElementById('").Append(uid).Append("'), '").Append(Code).AppendLine("');");
        sb.AppendLine("  }");
        sb.AppendLine("  boot();");
        sb.AppendLine("})();");
        sb.AppendLine("</script>");
        output.Content.SetHtmlContent(sb.ToString());
    }
}

/// <summary>
/// &lt;lc-component name="ElementButton" /&gt; 渲染一个低代码组件实例（无 model 绑定场景）。
/// 演示 TagHelper 可自定义属性注入组件选项。
/// </summary>
[HtmlTargetElement("lc-component")]
public class LcComponentTagHelper : TagHelper
{
    public string Name { get; set; } = "";
    public string? Modelname { get; set; }
    public string? Text { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        var uid = "lcc_" + Guid.NewGuid().ToString("N")[..8];
        output.Attributes.SetAttribute("id", uid);
        output.Attributes.SetAttribute("data-lc-component", Name);

        var config = new
        {
            component = Name,
            modelname = Modelname,
            options = new
            {
                comoptions = new { text = Text ?? "" },
                comlisteners = new { },
                labeloptions = new { label = "", required = false, show = true },
                itemoptions = new { style = new { }, @class = "" },
                wrapperoptions = new { }
            },
            validators = new object[0],
            childrenctrls = new object[0],
            slots = new { },
            extendinfo = new { }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var sb = new StringBuilder();
        sb.Append("<div id=\"").Append(uid).Append("\" class=\"lc-embedded-component\" data-lc-component=\"").Append(Name).AppendLine("\"></div>");
        sb.AppendLine("<script>");
        sb.AppendLine("(function(){");
        sb.AppendLine("  function boot(){");
        sb.AppendLine("    if (!window.VueLib) { setTimeout(boot, 200); return; }");
        sb.Append("    VueLib.runtime.bootEmbeddedComponent(document.getElementById('").Append(uid).Append("'), ").Append(json).AppendLine(");");
        sb.AppendLine("  }");
        sb.AppendLine("  boot();");
        sb.AppendLine("})();");
        sb.AppendLine("</script>");
        output.Content.SetHtmlContent(sb.ToString());
    }
}
