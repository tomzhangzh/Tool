using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面模板：预定义模板（学生管理CRUD / 左树右列表 / 主页 / 空白页等）（强类型 Model + 强类型服务）。
/// TemplateJson 为可实例化的 DynCom 配置树，ConfigJson 为模板参数 schema。
/// </summary>
[Area("Platform")]
[Route("api/platform/dyntemplate")]
[ApiController]
public class DynTemplateController : ControllerBase
{
    private readonly IDynTemplateService _svc;
    public DynTemplateController(IDynTemplateService svc) { _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string category = null)
    {
        var q = _svc.Query(t => t.IsActive)
            .OrderBy(t => t.SortNo);
        if (!string.IsNullOrEmpty(category))
            q = q.Where(t => t.Category == category);
        return ApiResult.Ok(q.ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        return ApiResult.Ok(FirstByIdOrCode(id));
    }

    /// <summary>取模板解析后的配置树</summary>
    [HttpGet("config")]
    public ApiResult Config(string id)
    {
        var row = FirstByIdOrCode(id);
        if (row == null) return ApiResult.Fail("模板不存在");
        try { return ApiResult.Ok(JObject.Parse(row.TemplateJson ?? "{}")); }
        catch { return ApiResult.Fail("模板JSON格式错误"); }
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynTemplate data)
    {
        if (data.Id <= 0)
        {
            _svc.Insert(data);
            return ApiResult.Ok(new { data.Id }, "新增成功");
        }
        _svc.Update(data);
        return ApiResult.Ok(new { data.Id }, "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        var id = keys["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        _svc.DeleteById(id);
        return ApiResult.Ok(true, "删除成功");
    }

    private DynTemplate FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(t => t.Code == key).First();
    }
}
