using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>桌面解决方案：一个解决方案可包含多个 DynProject（不同数据库）（强类型 Model + 强类型服务）</summary>
[Area("Platform")]
[Route("api/platform/desktopsolution")]
[ApiController]
public class DesktopSolutionController : ControllerBase
{
    private readonly IDesktopSolutionService _svc;
    public DesktopSolutionController(IDesktopSolutionService svc) { _svc = svc; }

    [HttpGet("all")]
    public ApiResult All()
    {
        return ApiResult.Ok(_svc.Query(s => s.IsActive)
            .OrderBy(s => s.SortNo)
            .ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        return ApiResult.Ok(FirstByIdOrCode(id));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DesktopSolution data)
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

    private DesktopSolution FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(s => s.Code == key).First();
    }
}
