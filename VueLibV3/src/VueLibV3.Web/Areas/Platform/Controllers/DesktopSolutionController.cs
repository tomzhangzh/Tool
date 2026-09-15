using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV3.Web.Core;

namespace VueLibV3.Web.Areas.Platform.Controllers;

/// <summary>桌面解决方案：一个解决方案可包含多个 DynProject（不同数据库）</summary>
[Area("Platform")]
[Route("api/platform/desktopsolution")]
[ApiController]
public class DesktopSolutionController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DesktopSolutionController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All() { using var db = _dbs.PlatformDb(); return ApiResult.Ok(_svc.Query(db, "DesktopSolution", orderBy: "[SortNo] asc")); }

    [HttpGet("get")]
    public ApiResult Get(string id) { using var db = _dbs.PlatformDb(); return ApiResult.Ok(_svc.FirstByIdOrCode(db, "DesktopSolution", id)); }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.PlatformDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "DesktopSolution", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "DesktopSolution", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Delete(db, "DesktopSolution", keys), "删除成功");
    }
}
