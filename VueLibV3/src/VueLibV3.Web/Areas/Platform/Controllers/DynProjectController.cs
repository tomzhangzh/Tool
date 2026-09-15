using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV3.Web.Core;

namespace VueLibV3.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态项目：属于某个 DesktopSolution。
/// 一个解决方案可有多个项目，项目本质是"不同的数据库"（各自持有连接串）。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynproject")]
[ApiController]
public class DynProjectController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DynProjectController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string solutionId = null)
    {
        using var db = _dbs.PlatformDb();
        if (string.IsNullOrEmpty(solutionId))
            return ApiResult.Ok(_svc.Query(db, "DynProject", orderBy: "[SortNo] asc"));
        return ApiResult.Ok(_svc.Query(db, "DynProject", "[SolutionId]=@sid", new { sid = solutionId }, "[SortNo] asc"));
    }

    [HttpGet("get")]
    public ApiResult Get(string id) { using var db = _dbs.PlatformDb(); return ApiResult.Ok(_svc.FirstByIdOrCode(db, "DynProject", id)); }

    /// <summary>测试项目连接串是否可用</summary>
    [HttpPost("test")]
    public ApiResult Test([FromBody] JObject data)
    {
        try
        {
            using var db = _dbs.ProjectDb(data["ConnectionString"]?.ToString());
            db.Ado.CheckConnection();
            return ApiResult.Ok(true, "连接成功");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("连接失败: " + ex.Message);
        }
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.PlatformDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "DynProject", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "DynProject", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Delete(db, "DynProject", keys), "删除成功");
    }
}
