using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV3.Web.Core;

namespace VueLibV3.Web.Areas.Platform.Controllers;

/// <summary>桌面快捷方式：关联解决方案，可指向网页/动作/外部URL</summary>
[Area("Platform")]
[Route("api/platform/desktopshortcut")]
[ApiController]
public class DesktopShortcutController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DesktopShortcutController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string solutionId = null)
    {
        using var db = _dbs.PlatformDb();
        if (string.IsNullOrEmpty(solutionId))
            return ApiResult.Ok(_svc.Query(db, "DesktopShortcut", orderBy: "[SortNo] asc"));
        return ApiResult.Ok(_svc.Query(db, "DesktopShortcut", "[SolutionId]=@sid", new { sid = solutionId }, "[SortNo] asc"));
    }

    [HttpGet("get")]
    public ApiResult Get(string id) { using var db = _dbs.PlatformDb(); return ApiResult.Ok(_svc.FirstByIdOrCode(db, "DesktopShortcut", id)); }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.PlatformDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "DesktopShortcut", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "DesktopShortcut", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Delete(db, "DesktopShortcut", keys), "删除成功");
    }
}
