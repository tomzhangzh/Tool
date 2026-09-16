using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>桌面解决方案：一个解决方案可包含多个 DynProject（不同数据库）（强类型 Model）</summary>
[Area("Platform")]
[Route("api/platform/desktopsolution")]
[ApiController]
public class DesktopSolutionController : ControllerBase
{
    private readonly DbFactory _dbs;
    public DesktopSolutionController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All()
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(db.Queryable<DesktopSolution>()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortNo)
            .ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(FirstByIdOrCode(db, id));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DesktopSolution data)
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
        db.Deleteable<DesktopSolution>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }

    private DesktopSolution FirstByIdOrCode(SqlSugarClient db, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id))
            return db.Queryable<DesktopSolution>().First(s => s.Id == id);
        return db.Queryable<DesktopSolution>().First(s => s.Code == key);
    }
}
