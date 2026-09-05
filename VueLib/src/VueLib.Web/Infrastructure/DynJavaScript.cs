using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace VueLib.Web.Infrastructure;

/// <summary>
/// 服务端"JavaScript 动作"基类（参考 backend/TUI.Core/Models/BaseJavaScript.cs 的思想，适配 dyn-lib 动作体系）。
/// Controller 中调用 this.ExecJS(new SetWindowJavaScript { ... }) 注册动作，
/// 由公共布局渲染为 <c>&lt;div style="display:none" dyn-init-{action}='{json}'&gt;&lt;/div&gt;</c>，
/// dyn-lib 在页面初始化（initActions 扫描 dyn-init-*）时自动执行。
/// </summary>
public abstract class DynJavaScript
{
    /// <summary>动作名（对应 dyn-{event}-{action} 中的 action，如 setwindow / alert）</summary>
    public abstract string Action { get; }

    /// <summary>触发事件，默认 init（页面初始化自动执行）；可改为 click / change 等</summary>
    public virtual string Event { get; } = "init";

    /// <summary>动作参数（序列化为属性值 JSON）</summary>
    public virtual object? Options => null;

    /// <summary>渲染为隐藏 div（display:none，不占布局）</summary>
    public string Render()
    {
        var json = Options == null ? "" : JsonConvert.SerializeObject(Options);
        var safe = json.Replace("'", "&#39;").Replace("\r", "").Replace("\n", "");
        return $"<div style=\"display:none\" dyn-{Event}-{Action.ToLowerInvariant()}='{safe}'></div>";
    }
}

/// <summary>消息提示：dyn-init-showmessage='{"message":"保存成功","type":"success","title":"提示"}'</summary>
public class FlashMessageJavaScript : DynJavaScript
{
    public string Message { get; set; } = "";
    public string Type { get; set; } = "success";   // success / error / warning / info
    public string Title { get; set; } = "提示";
    public override string Action => "showmessage";
    public override object? Options => new { message = Message, type = Type, title = Title };
}

/// <summary>弹窗警示（ElementPlus ElMessageBox / NutUI Dialog）：dyn-init-alert='{...}'</summary>
public class AlertJavaScript : DynJavaScript
{
    public string Message { get; set; } = "";
    public string Title { get; set; } = "提示";
    public string Type { get; set; } = "warning";  // success / warning / error / info
    public override string Action => "alert";
    public override object? Options => new { message = Message, title = Title, type = Type };
}

/// <summary>设置所在窗口标题/尺寸/全屏/关闭：dyn-init-setwindow='{...}'</summary>
public class SetWindowJavaScript : DynJavaScript
{
    public string? Title { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool? Fullscreen { get; set; }
    public bool? Minimize { get; set; }
    public bool? Close { get; set; }
    public override string Action => "setwindow";
    public override object? Options => new
    {
        title = Title, width = Width, height = Height,
        fullscreen = Fullscreen, minimize = Minimize, close = Close
    };
}

/// <summary>关闭最近的模态框/窗口：dyn-init-close='{}'</summary>
public class CloseDialogJavaScript : DynJavaScript
{
    public override string Action => "close";
    public override object? Options => new { };
}

/// <summary>重新加载容器：dyn-init-reload='{"selector":"#xxx"}'（不传 selector 则刷新最近 dyn-init 容器）</summary>
public class ReloadJavaScript : DynJavaScript
{
    public string? Selector { get; set; }
    public override string Action => "reload";
    public override object? Options => string.IsNullOrWhiteSpace(Selector) ? new { } : new { selector = Selector };
}

/// <summary>页面跳转：dyn-init-redirect='{"url":"/xxx"}'</summary>
public class RedirectJavaScript : DynJavaScript
{
    public string Url { get; set; } = "/";
    public override string Action => "redirect";
    public override object? Options => new { url = Url };
}

/// <summary>直接执行 JS：dyn-init-evaljs='{"script":"alert(1)"}'</summary>
public class EvalJavaScript : DynJavaScript
{
    public string Script { get; set; } = "";
    public override string Action => "evaljs";
    public override object? Options => new { script = Script };
}

/// <summary>更新指定容器（沿祖先找 data-url）：dyn-init-updateel='{"url":"/x","params":{}}'</summary>
public class UpdateElJavaScript : DynJavaScript
{
    public string Url { get; set; } = "";
    public object? Params { get; set; }
    public override string Action => "updateel";
    public override object? Options => new Dictionary<string, object?> { ["url"] = Url, ["params"] = Params };
}

/// <summary>Controller 扩展：注册 dyn 动作到当前请求（渲染时由布局输出）</summary>
public static class DynControllerExtensions
{
    public const string ViewDataKey = "DynActions";

    /// <summary>
    /// 注册一个或多个 dyn 动作，页面渲染（布局/分部视图）时输出为隐藏 div 并由 dyn-lib 自动执行。
    /// 示例：this.ExecJS(new FlashMessageJavaScript { Message = "保存成功" }, new SetWindowJavaScript { Title = "新标题", Fullscreen = true });
    /// </summary>
    public static void ExecJS(this Controller controller, params DynJavaScript[] actions)
    {
        if (actions == null || actions.Length == 0) return;
        var list = controller.ViewData[ViewDataKey] as List<DynJavaScript> ?? new List<DynJavaScript>();
        list.AddRange(actions);
        controller.ViewData[ViewDataKey] = list;
    }

    /// <summary>渲染已注册的 dyn 动作（布局/视图末尾调用 @Html.RenderDynActions()）</summary>
    public static IHtmlContent RenderDynActions(this IHtmlHelper html)
    {
        var list = html.ViewContext.ViewData[ViewDataKey] as List<DynJavaScript>;
        if (list == null || list.Count == 0) return new Microsoft.AspNetCore.Html.HtmlString("");
        var sb = new System.Text.StringBuilder();
        foreach (var a in list) sb.Append(a.Render());
        return new Microsoft.AspNetCore.Html.HtmlString(sb.ToString());
    }
}
