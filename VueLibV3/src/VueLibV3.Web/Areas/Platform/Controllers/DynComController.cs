using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV3.Web.Core;

namespace VueLibV3.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态组件（DynCom）库：
/// 设计时与运行时共用一套代码；组件定义存库（ConfigJson），
/// 若代码（dyn-com.js）中已定义同名组件，则优先使用代码中的实现。
/// </summary>
[Area("Platform")]
[Route("api/platform/dyncom")]
[ApiController]
public class DynComController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DynComController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string category = null)
    {
        using var db = _dbs.PlatformDb();
        var rows = string.IsNullOrEmpty(category)
            ? _svc.Query(db, "DynCom", orderBy: "[Category] asc,[SortNo] asc")
            : _svc.Query(db, "DynCom", "[Category]=@c", new { c = category }, "[SortNo] asc");
        return ApiResult.Ok(rows);
    }

    [HttpGet("get")]
    public ApiResult Get(string id) { using var db = _dbs.PlatformDb(); return ApiResult.Ok(_svc.FirstByIdOrCode(db, "DynCom", id)); }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.PlatformDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "DynCom", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "DynCom", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Delete(db, "DynCom", keys), "删除成功");
    }
}
