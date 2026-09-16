using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>桌面快捷方式：关联解决方案，可指向网页/动作/外部URL（强类型 Model，外键 int Id）</summary>
[Area("Platform")]
[Route("api/platform/desktopshortcut")]
[ApiController]
public class DesktopShortcutController : ControllerBase
{
    private readonly DbFactory _dbs;
    public DesktopShortcutController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All(string solutionId = null)
    {
        using var db = _dbs.PlatformDb();
        var q = db.Queryable<DesktopShortcut>()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortNo);
        if (!string.IsNullOrEmpty(solutionId))
        {
            // 兼容：solutionId 可为数字 Id 或解决方案 Code
            if (int.TryParse(solutionId, out var sid))
                q = q.Where(s => s.SolutionId == sid);
            else
            {
                var sol = db.Queryable<DesktopSolution>().First(s => s.Code == solutionId);
                if (sol != null) q = q.Where(s => s.SolutionId == sol.Id);
                else return ApiResult.Ok(new List<DesktopShortcut>());
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

    [HttpPost("save")]
    public ApiResult Save([FromBody] DesktopShortcut data)
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
        db.Deleteable<DesktopShortcut>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }

    private DesktopShortcut FirstByIdOrCode(SqlSugarClient db, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id))
            return db.Queryable<DesktopShortcut>().First(s => s.Id == id);
        return db.Queryable<DesktopShortcut>().First(s => s.Code == key);
    }
}
