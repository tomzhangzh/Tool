using Microsoft.AspNetCore.Mvc;
using VueLib.Web.Dtos;
using VueLib.Web.Services;

namespace VueLib.Web.Controllers;

/// <summary>
/// 组件定义 API - 供前端 vueLoadCom 动态加载组件
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

    /// <summary>
    /// 获取所有已启用组件清单（前端启动时调用，用于注册路由和全局组件）
    /// GET /api/component/list
    /// </summary>
    [HttpGet("list")]
    public async Task<ApiResponse<List<ComponentListItemDto>>> GetList()
    {
        try
        {
            var list = await _componentService.GetEnabledListAsync();
            return ApiResponse<List<ComponentListItemDto>>.Ok(list, $"共 {list.Count} 个组件");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组件清单失败");
            return ApiResponse<List<ComponentListItemDto>>.Fail("获取组件清单失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 根据组件名称获取完整定义（template + script + style）
    /// GET /api/component/define/{componentName}
    /// </summary>
    [HttpGet("define/{componentName}")]
    public async Task<ApiResponse<ComponentDefineDto>> GetDefine(string componentName)
    {
        try
        {
            var define = await _componentService.GetDefineByNameAsync(componentName);
            if (define == null)
            {
                return ApiResponse<ComponentDefineDto>.Fail($"组件 [{componentName}] 不存在或未启用");
            }
            return ApiResponse<ComponentDefineDto>.Ok(define);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组件定义失败: {ComponentName}", componentName);
            return ApiResponse<ComponentDefineDto>.Fail("获取组件定义失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 批量获取组件定义（前端可一次性预加载多个组件）
    /// POST /api/component/defines
    /// Body: ["CompA", "CompB"]
    /// </summary>
    [HttpPost("defines")]
    public async Task<ApiResponse<List<ComponentDefineDto>>> GetDefines([FromBody] string[] componentNames)
    {
        try
        {
            var defines = await _componentService.GetDefinesByNamesAsync(componentNames);
            return ApiResponse<List<ComponentDefineDto>>.Ok(defines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取组件定义失败");
            return ApiResponse<List<ComponentDefineDto>>.Fail("批量获取组件定义失败: " + ex.Message);
        }
    }

    /// <summary>
    /// 获取所有页面组件（用于构建 Vue Router 路由表）
    /// GET /api/component/pages
    /// </summary>
    [HttpGet("pages")]
    public async Task<ApiResponse<List<ComponentListItemDto>>> GetPages()
    {
        try
        {
            var pages = await _componentService.GetPageComponentsAsync();
            return ApiResponse<List<ComponentListItemDto>>.Ok(pages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取页面组件失败");
            return ApiResponse<List<ComponentListItemDto>>.Fail("获取页面组件失败: " + ex.Message);
        }
    }
}
