using Microsoft.AspNetCore.Mvc;
using VueLib.Web.Dtos;
using VueLib.Web.Models;
using VueLib.Web.Services;

namespace VueLib.Web.Controllers;

/// <summary>
/// 低代码平台 API - 组件元数据、页面配置
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LowCodeController : ControllerBase
{
    private readonly ComponentMetaService _metaService;
    private readonly PageSettingService _pageService;
    private readonly ILogger<LowCodeController> _logger;

    public LowCodeController(
        ComponentMetaService metaService,
        PageSettingService pageService,
        ILogger<LowCodeController> logger)
    {
        _metaService = metaService;
        _pageService = pageService;
        _logger = logger;
    }

    /// <summary>获取所有可用组件元数据（设计器组件面板用）</summary>
    [HttpGet("components")]
    public async Task<ApiResponse<List<ComponentMeta>>> GetComponents([FromQuery] int? type)
    {
        try
        {
            var list = await _metaService.GetEnabledListAsync(type);
            return ApiResponse<List<ComponentMeta>>.Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组件元数据失败");
            return ApiResponse<List<ComponentMeta>>.Fail(ex.Message);
        }
    }

    /// <summary>获取页面列表</summary>
    [HttpGet("pages")]
    public async Task<ApiResponse<List<PageSetting>>> GetPages([FromQuery] string? category)
    {
        try
        {
            var list = await _pageService.GetListAsync(category);
            return ApiResponse<List<PageSetting>>.Ok(list);
        }
        catch (Exception ex)
        {
            return ApiResponse<List<PageSetting>>.Fail(ex.Message);
        }
    }

    /// <summary>分页查询页面列表（支持筛选）</summary>
    [HttpGet("pages/paged")]
    public async Task<IActionResult> GetPagesPaged([FromQuery] int page = 1, [FromQuery] int limit = 10,
        [FromQuery] string? pageCode = null, [FromQuery] string? pageName = null,
        [FromQuery] string? platform = null, [FromQuery] string? category = null)
    {
        try
        {
            var (list, total) = await _pageService.GetPagedListAsync(page, limit, pageCode, pageName, platform, category);
            return Ok(new { success = true, data = list, count = total });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message, data = new List<PageSetting>(), count = 0 });
        }
    }

    /// <summary>根据页面编码获取页面配置</summary>
    [HttpGet("page/{code}")]
    public async Task<ApiResponse<PageSetting>> GetPage(string code)
    {
        try
        {
            var page = await _pageService.GetByCodeAsync(code);
            if (page == null) return ApiResponse<PageSetting>.Fail($"页面 [{code}] 不存在");
            return ApiResponse<PageSetting>.Ok(page);
        }
        catch (Exception ex)
        {
            return ApiResponse<PageSetting>.Fail(ex.Message);
        }
    }

    /// <summary>保存页面配置（新增或更新）</summary>
    [HttpPost("page")]
    public async Task<ApiResponse<int>> SavePage([FromBody] PageSetting page)
    {
        try
        {
            var id = await _pageService.SaveAsync(page);
            return ApiResponse<int>.Ok(id, "保存成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存页面失败");
            return ApiResponse<int>.Fail(ex.Message);
        }
    }

    /// <summary>删除页面</summary>
    [HttpDelete("page/{id}")]
    public async Task<ApiResponse<bool>> DeletePage(int id)
    {
        try
        {
            var ok = await _pageService.DeleteAsync(id);
            return ApiResponse<bool>.Ok(ok);
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Fail(ex.Message);
        }
    }

    #region 组件管理 API

    /// <summary>获取所有组件（管理后台用，含禁用）</summary>
    [HttpGet("components/all")]
    public async Task<ApiResponse<List<ComponentMeta>>> GetAllComponents()
    {
        try
        {
            var list = await _metaService.GetAllAsync();
            return ApiResponse<List<ComponentMeta>>.Ok(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有组件失败");
            return ApiResponse<List<ComponentMeta>>.Fail(ex.Message);
        }
    }

    /// <summary>根据 ID 获取组件详情</summary>
    [HttpGet("component/{id}")]
    public async Task<ApiResponse<ComponentMeta>> GetComponent(int id)
    {
        try
        {
            var meta = await _metaService.GetByIdAsync(id);
            if (meta == null) return ApiResponse<ComponentMeta>.Fail($"组件 ID={id} 不存在");
            return ApiResponse<ComponentMeta>.Ok(meta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取组件详情失败");
            return ApiResponse<ComponentMeta>.Fail(ex.Message);
        }
    }

    /// <summary>保存组件（新增或更新）</summary>
    [HttpPost("component")]
    public async Task<ApiResponse<int>> SaveComponent([FromBody] ComponentMeta meta)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(meta.ComponentName))
                return ApiResponse<int>.Fail("组件名称不能为空");
            if (string.IsNullOrWhiteSpace(meta.LoadUrl))
                return ApiResponse<int>.Fail("加载地址不能为空");

            var id = await _metaService.SaveAsync(meta);
            return ApiResponse<int>.Ok(id, "保存成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存组件失败");
            return ApiResponse<int>.Fail(ex.Message);
        }
    }

    /// <summary>删除组件</summary>
    [HttpDelete("component/{id}")]
    public async Task<ApiResponse<bool>> DeleteComponent(int id)
    {
        try
        {
            var ok = await _metaService.DeleteAsync(id);
            return ApiResponse<bool>.Ok(ok);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除组件失败");
            return ApiResponse<bool>.Fail(ex.Message);
        }
    }

    #endregion
}
