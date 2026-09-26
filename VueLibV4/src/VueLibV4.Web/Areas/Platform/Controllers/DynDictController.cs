using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>平台数据字典（业务字典放各业务库的 BusinessDict 中）（强类型 Model + 强类型服务）</summary>
[Area("Platform")]
[Route("api/platform/dyndict")]
[ApiController]
public class DynDictController : ControllerBase
{
    private readonly IDynDictService _svc;
    public DynDictController(IDynDictService svc) { _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string dictType = null)
    {
        var q = _svc.Query(d => d.IsActive)
            .OrderBy(d => d.DictType)
            .OrderBy(d => d.SortNo);
        if (!string.IsNullOrEmpty(dictType))
            q = q.Where(d => d.DictType == dictType);
        return ApiResult.Ok(q.ToList());
    }

    [HttpGet("types")]
    public ApiResult Types()
    {
        var rows = _svc.List(d => d.IsActive);
        return ApiResult.Ok(rows.GroupBy(r => r.DictType)
            .Select(g => new { type = g.Key, items = g.ToList() }));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynDict data)
    {
        if (data.Id <= 0)
        {
            _svc.Insert(data);
            return ApiResult.Ok(new { data.Id }, "新增成功");
        }
        _svc.Update(data);
        return ApiResult.Ok(new { data.Id }, "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        var id = keys["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        _svc.DeleteById(id);
        return ApiResult.Ok(true, "删除成功");
    }
}
