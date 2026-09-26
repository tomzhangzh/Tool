using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态组件（DynCom）库（强类型 Model + 强类型服务）：
/// 设计时与运行时共用一套代码；组件定义存库（ConfigJson），
/// 若代码（dyn-com.js）中已定义同名组件，则优先使用代码中的实现。
/// </summary>
[Area("Platform")]
[Route("api/platform/dyncom")]
[ApiController]
public class DynComController : ControllerBase
{
    private readonly IDynComService _svc;
    public DynComController(IDynComService svc) { _svc = svc; }

    [HttpGet("all")]
    public ApiResult All(string category = null)
    {
        var q = _svc.Query(c => c.IsActive)
            .OrderBy(c => c.Category)
            .OrderBy(c => c.SortNo);
        if (!string.IsNullOrEmpty(category))
            q = q.Where(c => c.Category == category);
        return ApiResult.Ok(q.ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        return ApiResult.Ok(FirstByIdOrCode(id));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynCom data)
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

    private DynCom FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(c => c.Code == key).First();
    }
}
