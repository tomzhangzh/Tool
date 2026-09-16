using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Business.Controllers;

/// <summary>业务数据字典：存放在各业务库自身的 BusinessDict 表中</summary>
[Area("Business")]
[Route("api/business/dict")]
[ApiController]
public class BusinessDictController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public BusinessDictController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string dictType = null)
    {
        using var db = _dbs.BusinessDb();
        var rows = string.IsNullOrEmpty(dictType)
            ? _svc.Query(db, "BusinessDict", orderBy: "[DictType] asc,[SortNo] asc")
            : _svc.Query(db, "BusinessDict", "[DictType]=@t", new { t = dictType }, "[SortNo] asc");
        return ApiResult.Ok(rows);
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.BusinessDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "BusinessDict", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "BusinessDict", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.BusinessDb();
        return ApiResult.Ok(_svc.Delete(db, "BusinessDict", keys), "删除成功");
    }
}
