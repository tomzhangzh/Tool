#nullable enable
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace VueLibV4.Web.Infrastructure;

/// <summary>
/// 服务端 "JavaScript 动作" 基类。
/// Controller 中 this.ExecJS(new XxxJavaScript { ... }) 注册动作，
/// 由公共 _Layout 渲染为隐藏 div：<div style="display:none" dyn-init-{action}='{json}'></div>，
/// 前端动作系统在 init 扫描时自动执行。
/// </summary>
public abstract class DynJavaScript
{
    public abstract string Action { get; }
    public virtual string Event { get; } = "init";
    public virtual object? Options => null;

    public string Render()
    {
        var json = Options == null ? "" : JsonConvert.SerializeObject(Options);
        var safe = json.Replace("'", "&#39;").Replace("\r", "").Replace("\n", "");
        return $"<div style=\"display:none\" dyn-{Event}-{Action.ToLowerInvariant()}='{safe}'></div>";
    }
}

public class FlashMessageJavaScript : DynJavaScript
{
    public string Message { get; set; } = "";
    public string Type { get; set; } = "success";
    public string Title { get; set; } = "提示";
    public override string Action => "showmessage";
    public override object? Options => new { message = Message, type = Type, title = Title };
}

public class ReloadJavaScript : DynJavaScript
{
    public string? Selector { get; set; }
    public override string Action => "reload";
    public override object? Options => string.IsNullOrWhiteSpace(Selector) ? new { } : new { selector = Selector };
}

public class EvalJavaScript : DynJavaScript
{
    public string Script { get; set; } = "";
    public override string Action => "evaljs";
    public override object? Options => new { script = Script };
}

public class RedirectJavaScript : DynJavaScript
{
    public string Url { get; set; } = "/";
    public override string Action => "redirect";
    public override object? Options => new { url = Url };
}

public class CloseDialogJavaScript : DynJavaScript
{
    public override string Action => "close";
    public override object? Options => new { };
}

/// <summary>Controller / View 扩展：注册并渲染服务端 dyn 动作</summary>
public static class DynControllerExtensions
{
    public const string ViewDataKey = "DynActions";

    public static void ExecJS(this Controller controller, params DynJavaScript[] actions)
    {
        if (actions == null || actions.Length == 0) return;
        var list = controller.ViewData[ViewDataKey] as List<DynJavaScript> ?? new List<DynJavaScript>();
        list.AddRange(actions);
        controller.ViewData[ViewDataKey] = list;
    }

    public static IHtmlContent RenderDynActions(this IHtmlHelper html)
    {
        var list = html.ViewContext.ViewData[ViewDataKey] as List<DynJavaScript>;
        if (list == null || list.Count == 0) return new HtmlString("");
        var sb = new System.Text.StringBuilder();
        foreach (var a in list) sb.Append(a.Render());
        return new HtmlString(sb.ToString());
    }
}
