using Microsoft.AspNetCore.Mvc;
using VueLibV4.Web.Dtos;
using VueLibV4.Web.Services;

namespace VueLibV4.Web.Controllers;

/// <summary>
/// 组件定义 API（只读拉取，走 GET）—— 供前端 vueLoadCom 动态加载组件。
/// 双定义源：Razor View 代码优先，DB 回退。
/// GET /api/component/list              已启用组件清单
/// GET /api/component/define/{name}      组件完整定义（template+script+style）
/// POST /api/component/defines          批量定义
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ComponentController : ControllerBase
{
    private readonly ComponentService _componentService;
    private readonly ILogger<ComponentController> _logger;

    public ComponentController(ComponentService componentService, ILogger<ComponentController> logger)
    {
        _componentService = componentService;
        _logger = logger;
    }

    /// <summary>统一信封（与 V1 vue-loader.js 约定一致：success/message/data）</summary>
    private static object Envelope(object data, string message = "ok", bool success = true)
        => new { success, message, data };

    [HttpGet("list")]
    public IActionResult GetList()
    {
        try
        {
            var list = _componentService.GetEnabledListAsync();
            return Ok(Envelope(list, $"共 {list.Count} 个组件"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组件清单失败");
            return Ok(Envelope(null, "获取组件清单失败: " + ex.Message, false));
        }
    }

    /// <summary>根据组件名获取完整定义（templateContent / scriptContent / styleContent）</summary>
    [HttpGet("define/{componentName}")]
    public IActionResult GetDefine(string componentName)
    {
        try
        {
            var define = _componentService.GetDefineByName(componentName);
            if (define == null)
                return Ok(Envelope((object)null, $"组件 [{componentName}] 不存在或未启用", false));
            return Ok(Envelope(define));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组件定义失败: {ComponentName}", componentName);
            return Ok(Envelope((object)null, "获取组件定义失败: " + ex.Message, false));
        }
    }

    [HttpPost("defines")]
    public IActionResult GetDefines([FromBody] string[] componentNames)
    {
        try
        {
            var defines = _componentService.GetDefinesByNames(componentNames ?? Array.Empty<string>());
            return Ok(Envelope(defines));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取组件定义失败");
            return Ok(Envelope((object)null, "批量获取组件定义失败: " + ex.Message, false));
        }
    }
}
