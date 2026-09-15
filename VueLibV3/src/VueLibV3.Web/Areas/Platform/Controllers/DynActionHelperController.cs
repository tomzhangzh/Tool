using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV3.Web.Core;

namespace VueLibV3.Web.Areas.Platform.Controllers;

/// <summary>
/// 动作助手：所有动作（含脚本）必须入库，支持 script / url / api / chain 四类。
/// 前端运行时从 /all 动态加载并注册（动态注册），也可从 /script 拉取生成脚本。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynactionhelper")]
[ApiController]
public class DynActionHelperController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DynActionHelperController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All()
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Query(db, "DynActionHelper", "[IsActive]=@a", new { a = "1" }, "[SortNo] asc"));
    }

    [HttpGet("get")]
    public ApiResult Get(string id) { using var db = _dbs.PlatformDb(); return ApiResult.Ok(_svc.FirstByIdOrCode(db, "DynActionHelper", id)); }

    /// <summary>生成前端注册脚本（文本），便于按需注入</summary>
    [HttpGet("script")]
    public IActionResult Script()
    {
        using var db = _dbs.PlatformDb();
        var rows = _svc.Query(db, "DynActionHelper", "[IsActive]=@a", new { a = "1" });
        var sb = new System.Text.StringBuilder("// DynActionHelper 动态注册脚本（由数据库生成）\n");
        foreach (var r in rows)
        {
            var code = r["Code"]?.ToString();
            var name = r["Name"]?.ToString();
            var type = r["ActionType"]?.ToString();
            var script = r["Script"]?.ToString() ?? "";
            sb.Append("DynActionHelper.register({")
              .Append("code:").Append(JsonEncode(code))
              .Append(",name:").Append(JsonEncode(name))
              .Append(",actionType:").Append(JsonEncode(type))
              .Append(",script:").Append(JsonEncode(script))
              .AppendLine("});");
        }
        return Content(sb.ToString(), "text/plain; charset=utf-8");
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.PlatformDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "DynActionHelper", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "DynActionHelper", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Delete(db, "DynActionHelper", keys), "删除成功");
    }

    private static string JsonEncode(string s)
        => Newtonsoft.Json.JsonConvert.ToString(s ?? "");
}
