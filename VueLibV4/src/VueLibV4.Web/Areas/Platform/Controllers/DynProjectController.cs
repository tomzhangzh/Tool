using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态项目：属于某个 DesktopSolution（强类型 Model，外键 int Id）。
/// 一个解决方案可有多个项目，项目本质是"不同的数据库"（各自持有连接串）。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynproject")]
[ApiController]
public class DynProjectController : ControllerBase
{
    private readonly DbFactory _dbs;
    public DynProjectController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All(string solutionId = null)
    {
        using var db = _dbs.PlatformDb();
        var q = db.Queryable<DynProject>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortNo);
        if (!string.IsNullOrEmpty(solutionId))
        {
            if (int.TryParse(solutionId, out var sid))
                q = q.Where(p => p.SolutionId == sid);
            else
            {
                var sol = db.Queryable<DesktopSolution>().First(s => s.Code == solutionId);
                if (sol != null) q = q.Where(p => p.SolutionId == sol.Id);
                else return ApiResult.Ok(new List<DynProject>());
            }
        }
        return ApiResult.Ok(q.ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(FirstByIdOrCode(db, id));
    }

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
    public ApiResult Save([FromBody] DynProject data)
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
        db.Deleteable<DynProject>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }

    private DynProject FirstByIdOrCode(SqlSugarClient db, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id))
            return db.Queryable<DynProject>().First(p => p.Id == id);
        return db.Queryable<DynProject>().First(p => p.Code == key);
    }
}
