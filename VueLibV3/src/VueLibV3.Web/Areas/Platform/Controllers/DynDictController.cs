using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV3.Web.Core;

namespace VueLibV3.Web.Areas.Platform.Controllers;

/// <summary>平台数据字典（业务字典放各业务库的 BusinessDict 中）</summary>
[Area("Platform")]
[Route("api/platform/dyndict")]
[ApiController]
public class DynDictController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DynDictController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string dictType = null)
    {
        using var db = _dbs.PlatformDb();
        if (string.IsNullOrEmpty(dictType))
            return ApiResult.Ok(_svc.Query(db, "DynDict", orderBy: "[DictType] asc,[SortNo] asc"));
        return ApiResult.Ok(_svc.Query(db, "DynDict", "[DictType]=@t", new { t = dictType }, "[SortNo] asc"));
    }

    [HttpGet("types")]
    public ApiResult Types()
    {
        using var db = _dbs.PlatformDb();
        var rows = _svc.Query(db, "DynDict");
        return ApiResult.Ok(rows.GroupBy(r => r["DictType"]?.ToString())
            .Select(g => new { type = g.Key, items = g.ToList() }));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.PlatformDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "DynDict", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "DynDict", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Delete(db, "DynDict", keys), "删除成功");
    }
}
