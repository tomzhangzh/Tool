using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>平台数据字典（业务字典放各业务库的 BusinessDict 中）（强类型 Model）</summary>
[Area("Platform")]
[Route("api/platform/dyndict")]
[ApiController]
public class DynDictController : ControllerBase
{
    private readonly DbFactory _dbs;
    public DynDictController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All(string dictType = null)
    {
        using var db = _dbs.PlatformDb();
        var q = db.Queryable<DynDict>()
            .Where(d => d.IsActive)
            .OrderBy(d => d.DictType)
            .OrderBy(d => d.SortNo);
        if (!string.IsNullOrEmpty(dictType))
            q = q.Where(d => d.DictType == dictType);
        return ApiResult.Ok(q.ToList());
    }

    [HttpGet("types")]
    public ApiResult Types()
    {
        using var db = _dbs.PlatformDb();
        var rows = db.Queryable<DynDict>().Where(d => d.IsActive).ToList();
        return ApiResult.Ok(rows.GroupBy(r => r.DictType)
            .Select(g => new { type = g.Key, items = g.ToList() }));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynDict data)
    {
        using var db = _dbs.PlatformDb();
        if (data.Id <= 0)
        {
            db.Insertable(data).ExecuteCommand();
            return ApiResult.Ok(new { data.Id }, "新增成功");
        }
        db.Updateable(data).ExecuteCommand();
        return ApiResult.Ok(new { data.Id }, "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        var id = keys["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        db.Deleteable<DynDict>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }
}
