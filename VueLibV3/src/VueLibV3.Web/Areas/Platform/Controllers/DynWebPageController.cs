using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV3.Web.Core;

namespace VueLibV3.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态网页：真正的动态页面。
/// 选择一个 DynTemplate，结合用户配置参数（ConfigJson）实例化 PageJson，即可运行。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynwebpage")]
[ApiController]
public class DynWebPageController : ControllerBase
{
    private readonly DbFactory _dbs;
    private readonly DynamicCrudService _svc;
    public DynWebPageController(DbFactory dbs, DynamicCrudService svc) { _dbs = dbs; _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string projectId = null)
    {
        using var db = _dbs.PlatformDb();
        var rows = string.IsNullOrEmpty(projectId)
            ? _svc.Query(db, "DynWebPage", orderBy: "[CreateTime] desc")
            : _svc.Query(db, "DynWebPage", "[ProjectId]=@p", new { p = projectId }, "[CreateTime] desc");
        return ApiResult.Ok(rows);
    }

    [HttpGet("get")]
    public ApiResult Get(string id) { using var db = _dbs.PlatformDb(); return ApiResult.Ok(_svc.FirstByIdOrCode(db, "DynWebPage", id)); }

    /// <summary>
    /// 运行时渲染数据：解析 PageJson；
    /// 若页面未实例化（PageJson 为空）则回退到模板 TemplateJson —— 选择模板+配置即完成页面。
    /// </summary>
    [HttpGet("render")]
    public ApiResult Render(string id, string code = null)
    {
        using var db = _dbs.PlatformDb();
        var row = string.IsNullOrEmpty(id)
            ? _svc.First(db, "DynWebPage", "[Code]=@c", new { c = code })
            : _svc.FirstByIdOrCode(db, "DynWebPage", id);
        if (row == null) return ApiResult.Fail("页面不存在");
        var result = new JObject { ["id"] = row["Id"], ["name"] = row["Name"], ["code"] = row["Code"] };

        JObject config = null;
        var pageJson = row["PageJson"]?.ToString();
        if (!string.IsNullOrWhiteSpace(pageJson))
        {
            try { config = JObject.Parse(pageJson); } catch { config = null; }
        }
        if (config == null && row["TemplateId"] != null)
        {
            var tpl = _svc.First(db, "DynTemplate", "[Code]=@tc", new { tc = row["TemplateId"]?.ToString() });
            if (tpl != null)
            {
                try { config = JObject.Parse(tpl["TemplateJson"]?.ToString() ?? "{}"); } catch { config = null; }
            }
        }

        result["config"] = config ?? new JObject();
        try { result["configjson"] = JObject.Parse(row["ConfigJson"]?.ToString() ?? "{}"); }
        catch { result["configjson"] = new JObject(); }
        return ApiResult.Ok(result);
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] JObject data)
    {
        using var db = _dbs.PlatformDb();
        var id = data["Id"]?.ToString();
        if (string.IsNullOrEmpty(id)) return ApiResult.Ok(_svc.Insert(db, "DynWebPage", data), "新增成功");
        return ApiResult.Ok(_svc.Update(db, "DynWebPage", data), "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(_svc.Delete(db, "DynWebPage", keys), "删除成功");
    }
}
