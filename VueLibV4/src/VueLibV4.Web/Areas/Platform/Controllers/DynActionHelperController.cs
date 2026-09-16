using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动作助手：所有动作（含脚本）必须入库（强类型 Model），支持 script / url / api / chain 四类。
/// 前端运行时从 /all 动态加载并注册（动态注册），也可从 /script 拉取生成脚本。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynactionhelper")]
[ApiController]
public class DynActionHelperController : ControllerBase
{
    private readonly DbFactory _dbs;
    public DynActionHelperController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All()
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(db.Queryable<DynActionHelper>()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortNo)
            .ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(FirstByIdOrCode(db, id));
    }

    /// <summary>生成前端注册脚本（文本），便于按需注入</summary>
    [HttpGet("script")]
    public IActionResult Script()
    {
        using var db = _dbs.PlatformDb();
        var rows = db.Queryable<DynActionHelper>()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortNo)
            .ToList();
        var sb = new System.Text.StringBuilder("// DynActionHelper 动态注册脚本（由数据库生成）\n");
        foreach (var r in rows)
        {
            sb.Append("DynActionHelper.register({")
              .Append("code:").Append(JsonEncode(r.Code))
              .Append(",name:").Append(JsonEncode(r.Name))
              .Append(",actionType:").Append(JsonEncode(r.ActionType))
              .Append(",script:").Append(JsonEncode(r.Script))
              .AppendLine("});");
        }
        return Content(sb.ToString(), "text/plain; charset=utf-8");
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynActionHelper data)
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
        db.Deleteable<DynActionHelper>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }

    private DynActionHelper FirstByIdOrCode(SqlSugarClient db, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id))
            return db.Queryable<DynActionHelper>().First(a => a.Id == id);
        return db.Queryable<DynActionHelper>().First(a => a.Code == key);
    }

    private static string JsonEncode(string s)
        => Newtonsoft.Json.JsonConvert.ToString(s ?? "");
}
