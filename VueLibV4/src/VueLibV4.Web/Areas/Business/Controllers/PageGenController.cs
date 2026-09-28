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

    /// <summary>生成三屏页面（Filter/List/Detail 三份 PageSetting + 1 条 DynWebPage）。</summary>
    [HttpPost("generate")]
    public ApiResult Generate([FromBody] JObject req)
    {
        var r = _pageGen.Generate(req);
        return r.Ok ? ApiResult.Ok(r.Data, r.Msg) : ApiResult.Fail(r.Msg);
    }

    /// <summary>
    /// 读取表字段元数据 + 按规则推断推荐控件，供向导【第二步：配置字段】预填充控件下拉。
    /// 用法：GET /api/business/pagegen/fields?table=Order
    /// </summary>
    [HttpGet("fields")]
    public ApiResult Fields(string table)
    {
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少表名");
        return ApiResult.Ok(_pageGen.GetTableFieldMeta(table));
    }
}
