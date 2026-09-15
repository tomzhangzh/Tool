using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV3.Web.Core;

namespace VueLibV3.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面模板：预定义模板（学生管理CRUD / 左树右列表 / 主页 / 空白页等）。
/// TemplateJson 为可实例化的 DynCom 配置树，ConfigJson 为模板参数 schema。
/// </summary>
[Area("Platform")]
[Route("api/platform/dyntemplate")]
[ApiController]
public class DynTemplateController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DynTemplateController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string category = null)
    {
        using var db = _dbs.PlatformDb();
        var rows = string.IsNullOrEmpty(category)
            ? _svc.Query(db, "DynTemplate", orderBy: "[SortNo] asc")
            : _svc.Query(db, "DynTemplate", "[Category]=@c", new { c = category }, "[SortNo] asc");
        return ApiResult.Ok(rows);
    }

    [HttpGet("get")]
    public ApiResult Get(string id) { using var db = _dbs.PlatformDb(); return ApiResult.Ok(_svc.FirstByIdOrCode(db, "DynTemplate", id)); }

    /// <summary>取模板解析后的配置树</summary>
    [HttpGet("config")]
    public ApiResult Config(string id)
    {
        using var db = _dbs.PlatformDb();
        var row = _svc.FirstByIdOrCode(db, "DynTemplate", id);
        if (row == null) return ApiResult.Fail("模板不存在");
        var templateJson = row["TemplateJson"]?.ToString();
        try { return ApiResult.Ok(JObject.Parse(templateJson ?? "{}")); }
        catch { return ApiResult.Fail("模板JSON格式错误"); }
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.PlatformDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "DynTemplate", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "DynTemplate", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Delete(db, "DynTemplate", keys), "删除成功");
    }
}
