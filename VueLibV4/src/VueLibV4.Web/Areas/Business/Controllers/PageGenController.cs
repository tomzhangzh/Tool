using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Areas.Business.Controllers;

/// <summary>
/// 页面生成器 Controller（M4 v2）：只负责路由与 ApiResult 封装。
/// 三屏生成规则（筛选/列表/详情布局、字段控件映射、默认条件、必填校验等）全部在
/// <see cref="IPageGenService"/>（VueLibV4.Platform.Services.PageGenService）中实现，便于集中维护与生成文档。
/// 路由：api/business/pagegen
/// </summary>
[Area("Business")]
[Route("api/business/pagegen")]
[ApiController]
public class PageGenController : ControllerBase
{
    private readonly IPageGenService _pageGen;

    public PageGenController(IPageGenService pageGen) => _pageGen = pageGen;

    /// <summary>
    /// 生成三屏页面：三屏 PageSetting + 筛选列表主页（filterlist-crud）+
    /// 编辑弹窗实例（detail-modal）+ 页面扩展视图骨架（真实 cshtml）。
    /// </summary>
    [HttpPost("generate")]
    public async Task<ApiResult> Generate([FromBody] JObject req, CancellationToken ct)
    {
        var r = await _pageGen.GenerateAsync(req, ct);
        return r.Ok ? ApiResult.Ok(r.Data, r.Msg) : ApiResult.Fail(r.Msg);
    }

    /// <summary>
    /// 读取表字段元数据 + 按规则推断推荐控件，供向导【第二步：配置字段】预填充控件下拉。
    /// 用法：GET /api/business/pagegen/fields?table=Order&project=__platform__
    /// </summary>
    [HttpGet("fields")]
    public ApiResult Fields(string table, string? project = null)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少表名");
        return ApiResult.Ok(_pageGen.GetTableFieldMeta(table, project));
    }

    /// <summary>
    /// 预览（不落库）：复用 Generate 同一套规则，返回筛选/列表/详情三份配置 JSON，
    /// 前端用 dyn-dynamic-com 真实渲染预览抽屉。
    /// </summary>
    [HttpPost("preview")]
    public ApiResult Preview([FromBody] JObject req)
    {
        var r = _pageGen.Preview(req);
        return ApiResult.Ok(new { filter = r.FilterCfg, list = r.ListCfg, detail = r.DetailCfg });
    }

    /// <summary>返回支持三屏生成的模板列表（SupportGen=1），供向导下拉选择。</summary>
    [HttpGet("templates")]
    public ApiResult Templates()
    {
        return ApiResult.Ok(_pageGen.ListGenTemplates());
    }
}
