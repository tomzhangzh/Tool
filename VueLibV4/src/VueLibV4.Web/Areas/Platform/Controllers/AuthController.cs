using Microsoft.AspNetCore.Mvc;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 认证相关页面：登录、修改密码
/// </summary>
[Area("Platform")]
public class AuthController : Controller
{
    /// <summary>登录页（全局过滤器的唯一匿名页面入口）</summary>
    [HttpGet("/Platform/Page/Login")]
    [AllowAnonymousPermission]
    public IActionResult Login()
    {
        return View("~/Areas/Platform/Views/Page/Login.cshtml");
    }

    /// <summary>修改密码页</summary>
    [HttpGet("/Platform/Page/ChangePassword")]
    public IActionResult ChangePassword()
    {
        return View("~/Areas/Platform/Views/Page/ChangePassword.cshtml");
    }
}
