using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态积木（Block）元数据：可 DB 动态注册的独立功能单元（filter/list/detail…）。
/// ParamConfigJson/ParamDefaultJson 为接线参数 UI 包；Commands/Events 为命令/事件契约。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynblock")]
[ApiController]
public class DynBlockController : ControllerBase
{
    private readonly IDynBlockService _svc;
    public DynBlockController(IDynBlockService svc) { _svc = svc; }

    /// <summary>全部启用积木（可按 role=filter/list/detail/tree 过滤）</summary>
    [HttpGet("all")]
    public ApiResult All(string role = null)
    {
        var q = _svc.Query(b => b.IsActive);
        if (!string.IsNullOrEmpty(role)) q = q.Where(b => b.ImplementsRole == role);
        return ApiResult.Ok(q.OrderBy(b => b.SortNo).ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        return ApiResult.Ok(FirstByIdOrCode(id));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynBlock data)
    {
        if (string.IsNullOrWhiteSpace(data.Code)) return ApiResult.Fail("Code 不能为空");
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

    private DynBlock FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(b => b.Code == key).First();
    }
}
