using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态网页：真正的动态页面（强类型 Model，外键 int Id）。
/// 选择一个 DynTemplate，结合用户配置参数（ConfigJson）实例化 PageJson，即可运行。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynwebpage")]
[ApiController]
public class DynWebPageController : ControllerBase
{
    private readonly DbFactory _dbs;
    public DynWebPageController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All(string projectId = null)
    {
        using var db = _dbs.PlatformDb();
        var q = db.Queryable<DynWebPage>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.CreateTime, SqlSugar.OrderByType.Desc);
        if (!string.IsNullOrEmpty(projectId))
        {
            if (int.TryParse(projectId, out var pid))
                q = q.Where(p => p.ProjectId == pid);
            else
            {
                var proj = db.Queryable<DynProject>().First(x => x.Code == projectId);
                if (proj != null) q = q.Where(p => p.ProjectId == proj.Id);
                else return ApiResult.Ok(new List<DynWebPage>());
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

    /// <summary>
    /// 运行时渲染数据：解析 PageJson；
    /// 若页面未实例化（PageJson 为空）则回退到模板 TemplateJson —— 选择模板+配置即完成页面。
    /// </summary>
    [HttpGet("render")]
    public ApiResult Render(string id, string code = null)
    {
        using var db = _dbs.PlatformDb();
        var row = string.IsNullOrEmpty(id)
            ? db.Queryable<DynWebPage>().First(p => p.Code == code)
            : FirstByIdOrCode(db, id);
        if (row == null) return ApiResult.Fail("页面不存在");
        var result = new JObject { ["id"] = row.Id, ["name"] = row.Name, ["code"] = row.Code };

        JObject config = null;
        if (!string.IsNullOrWhiteSpace(row.PageJson))
        {
            try { config = JObject.Parse(row.PageJson); } catch { config = null; }
        }
        if (config == null && row.TemplateId > 0)
        {
            var tpl = db.Queryable<DynTemplate>().First(t => t.Id == row.TemplateId);
            if (tpl != null)
            {
                try { config = JObject.Parse(tpl.TemplateJson ?? "{}"); } catch { config = null; }
            }
        }

        result["config"] = config ?? new JObject();
        try { result["configjson"] = JObject.Parse(row.ConfigJson ?? "{}"); }
        catch { result["configjson"] = new JObject(); }
        return ApiResult.Ok(result);
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynWebPage data)
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
        db.Deleteable<DynWebPage>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }

    private DynWebPage FirstByIdOrCode(SqlSugarClient db, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id))
            return db.Queryable<DynWebPage>().First(p => p.Id == id);
        return db.Queryable<DynWebPage>().First(p => p.Code == key);
    }
}
