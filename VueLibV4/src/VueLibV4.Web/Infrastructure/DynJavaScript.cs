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

/// <summary>刷新指定 gridId 的 DynTable（三屏模板保存成功后刷新列表）</summary>
public class GridReloadJavaScript : DynJavaScript
{
    public string GridId { get; set; } = "";
    public override string Action => "grid";
    public override object? Options => new { gridId = GridId };
}

/// <summary>Controller / View 扩展：注册并渲染服务端 dyn 动作</summary>
public static class DynControllerExtensions
{
    /// <summary>本请求动作（ViewData）：随当前响应一起渲染</summary>
    public const string ViewDataKey = "DynActions";

    /// <summary>跨请求动作（TempData）：下一请求渲染时执行一次</summary>
    public const string TempDataKey = "DynActionsNext";

    public static void ExecJS(this Controller controller, params DynJavaScript[] actions)
    {
        if (actions == null || actions.Length == 0) return;
        var list = controller.ViewData[ViewDataKey] as List<DynJavaScript> ?? new List<DynJavaScript>();
        list.AddRange(actions);
        controller.ViewData[ViewDataKey] = list;
    }

    /// <summary>
    /// 注册"下一个请求"才执行的动作（存 TempData），用于传统 POST → RedirectToAction 场景：
    /// <code>
    /// ExecJSForNext(new FlashMessageJavaScript { Message = "保存成功" });
    /// return RedirectToAction("Index");
    /// </code>
    /// 实现说明：注册时立即渲染为隐藏 div 字符串再存入 TempData，
    /// 避免抽象类型 List&lt;DynJavaScript&gt; 在 Cookie TempData 中反序列化失败；
    /// 由 RenderDynActions 读出后立即 Remove（一次性消费）。
    /// 注意：Cookie TempData 体积有限，不要一次塞过多/过大的动作（大量动作请走 JSON 动作链）。
    /// </summary>
    public static void ExecJSForNext(this Controller controller, params DynJavaScript[] actions)
    {
        if (actions == null || actions.Length == 0) return;
        var sb = new System.Text.StringBuilder();
        foreach (var a in actions) sb.Append(a.Render());
        var pending = controller.TempData[TempDataKey] as string;
        controller.TempData[TempDataKey] = string.Concat(pending, sb.ToString());
    }

    public static IHtmlContent RenderDynActions(this IHtmlHelper html)
    {
        var sb = new System.Text.StringBuilder();

        // 1) 本请求动作（ViewData）
        if (html.ViewContext.ViewData[ViewDataKey] is List<DynJavaScript> list)
            foreach (var a in list) sb.Append(a.Render());

        // 2) 上一请求遗留的跨请求动作（TempData）：渲染后立即清除，保证只执行一次
        // TempData 未启用（无 provider）时为 null，此时跳过即可，不能让整页渲染失败
        var tempData = html.ViewContext.TempData;
        if (tempData != null && tempData[TempDataKey] is string pending && pending.Length > 0)
        {
            sb.Append(pending);
            tempData.Remove(TempDataKey);
        }

        return sb.Length == 0 ? new HtmlString("") : new HtmlString(sb.ToString());
    }
}
