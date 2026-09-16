using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态组件（DynCom）库（强类型 Model）：
/// 设计时与运行时共用一套代码；组件定义存库（ConfigJson），
/// 若代码（dyn-com.js）中已定义同名组件，则优先使用代码中的实现。
/// </summary>
[Area("Platform")]
[Route("api/platform/dyncom")]
[ApiController]
public class DynComController : ControllerBase
{
    private readonly DbFactory _dbs;
    public DynComController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All(string category = null)
    {
        using var db = _dbs.PlatformDb();
        var q = db.Queryable<DynCom>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Category)
            .OrderBy(c => c.SortNo);
        if (!string.IsNullOrEmpty(category))
            q = q.Where(c => c.Category == category);
        return ApiResult.Ok(q.ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(FirstByIdOrCode(db, id));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynCom data)
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
        db.Deleteable<DynCom>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }

    private DynCom FirstByIdOrCode(SqlSugarClient db, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id))
            return db.Queryable<DynCom>().First(c => c.Id == id);
        return db.Queryable<DynCom>().First(c => c.Code == key);
    }
}
