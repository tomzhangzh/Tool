using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace VueLib.Web.Controllers;

/// <summary>
/// dyn-lib 动作注册表 + 通用事件委托 Demo
/// 验证 dyn-{event}-{action} 属性驱动：postdata / reload / updateEl / dyn-init-load 等
/// </summary>
public class DynDemoController : Controller
{
    /// <summary>Demo 主页面</summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// postdata 动作后端：接收 Model，返回 JSON（dyn-lib 会合并进 Model 并响应式刷新）
    /// </summary>
    [HttpPost]
    public IActionResult Submit([FromBody] JsonElement model)
    {
        int count = 0;
        if (model.ValueKind == JsonValueKind.Object && model.TryGetProperty("count", out var c) && c.ValueKind == JsonValueKind.Number)
        {
            count = c.GetInt32();
        }
        return Json(new
        {
            success = true,
            message = "提交成功",
            count = count + 1,
            msg = "已提交于 " + DateTime.Now.ToString("HH:mm:ss"),
            server = "DynDemo/Submit"
        });
    }

    /// <summary>
    /// reload 动作数据源：返回一段含 dyn-init app 的分部视图 HTML（验证 reload 时把 Vue model POST 回来）
    /// </summary>
    [HttpPost]
    public IActionResult Partial([FromBody] JsonElement model)
    {
        string recv = "{}";
        if (model.ValueKind == JsonValueKind.Object)
        {
            recv = model.ToString();
            if (recv.Length > 120) recv = recv.Substring(0, 120) + "...";
        }
        string html = "<div dyn-init='{\"count\":0,\"note\":\"n\"}'>"
                    + "<div style=\"padding:8px;border:1px dashed #409eff;border-radius:4px;\">"
                    + "<div>分部视图 app · 服务端时间：<b>" + DateTime.Now.ToString("HH:mm:ss") + "</b></div>"
                    + "<div>model：count={{ model.count }}，note=\"{{ model.note }}\"</div>"
                    + "<button dyn-click-postdata='{\"url\":\"/DynDemo/Submit\"}' style=\"height:26px;padding:0 10px;background:#409eff;color:#fff;border:none;border-radius:3px;cursor:pointer;\">内部 +1</button>"
                    + "<div style=\"margin-top:6px;font-size:12px;color:#909399;\">reload 收到参数：<code>" + WebUtility.HtmlEncode(recv) + "</code></div>"
                    + "</div>"
                    + "</div>";
        return Content(html, "text/html");
    }

    /// <summary>
    /// dyn-init-load 动作数据源：div 初始化完毕立即请求此端点，后端 HTML 填充该 div。
    /// 返回内容含一个 input，用于验证 reload 时容器内 form 序列化进请求参数。
    /// </summary>
    [HttpPost]
    public IActionResult LoadPart([FromBody] JsonElement model)
    {
        string recv = "{}";
        if (model.ValueKind == JsonValueKind.Object)
        {
            recv = model.ToString();
            if (recv.Length > 120) recv = recv.Substring(0, 120) + "...";
        }
        string html = "<div class=\"load-body\" style=\"padding:8px;border:1px solid #67c23a;border-radius:4px;\">"
                    + "<div>dyn-init-load 填充内容 · 服务端时间：<b>" + DateTime.Now.ToString("HH:mm:ss") + "</b></div>"
                    + "<div style=\"margin:6px 0;\">容器内表单输入（reload 会序列化为参数）：<input name=\"search\" value=\"abc\" style=\"height:24px;padding:0 6px;border:1px solid #dcdfe6;border-radius:3px;\" /></div>"
                    + "<div style=\"font-size:12px;color:#909399;\">reload 收到参数：<code>" + WebUtility.HtmlEncode(recv) + "</code></div>"
                    + "</div>";
        return Content(html, "text/html");
    }
}
