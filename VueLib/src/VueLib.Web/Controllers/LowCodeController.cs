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
}
