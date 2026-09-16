using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Newtonsoft.Json.Linq;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面模板：预定义模板（学生管理CRUD / 左树右列表 / 主页 / 空白页等）（强类型 Model）。
/// TemplateJson 为可实例化的 DynCom 配置树，ConfigJson 为模板参数 schema。
/// </summary>
[Area("Platform")]
[Route("api/platform/dyntemplate")]
[ApiController]
public class DynTemplateController : ControllerBase
{
    private readonly DbFactory _dbs;
    public DynTemplateController(DbFactory dbs) { _dbs = dbs; }

    [HttpGet("all")]
    public ApiResult All(string category = null)
    {
        using var db = _dbs.PlatformDb();
        var q = db.Queryable<DynTemplate>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortNo);
        if (!string.IsNullOrEmpty(category))
            q = q.Where(t => t.Category == category);
        return ApiResult.Ok(q.ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        using var db = _dbs.PlatformDb();
        return ApiResult.Ok(FirstByIdOrCode(db, id));
    }

    /// <summary>取模板解析后的配置树</summary>
    [HttpGet("config")]
    public ApiResult Config(string id)
    {
        using var db = _dbs.PlatformDb();
        var row = FirstByIdOrCode(db, id);
        if (row == null) return ApiResult.Fail("模板不存在");
        try { return ApiResult.Ok(JObject.Parse(row.TemplateJson ?? "{}")); }
        catch { return ApiResult.Fail("模板JSON格式错误"); }
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynTemplate data)
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
        db.Deleteable<DynTemplate>(id).ExecuteCommand();
        return ApiResult.Ok(true, "删除成功");
    }

    private DynTemplate FirstByIdOrCode(SqlSugarClient db, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id))
            return db.Queryable<DynTemplate>().First(t => t.Id == id);
        return db.Queryable<DynTemplate>().First(t => t.Code == key);
    }
}
