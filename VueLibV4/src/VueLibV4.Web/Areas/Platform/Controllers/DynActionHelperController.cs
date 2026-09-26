using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动作助手：所有动作（含脚本）必须入库（强类型 Model + 强类型服务），支持 script / url / api / chain 四类。
/// 前端运行时从 /all 动态加载并注册（动态注册），也可从 /script 拉取生成脚本。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynactionhelper")]
[ApiController]
public class DynActionHelperController : ControllerBase
{
    private readonly IDynActionHelperService _svc;
    public DynActionHelperController(IDynActionHelperService svc) { _svc = svc; }

    [HttpGet("all")]
    public ApiResult All()
    {
        return ApiResult.Ok(_svc.Query(a => a.IsActive)
            .OrderBy(a => a.SortNo)
            .ToList());
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        return ApiResult.Ok(FirstByIdOrCode(id));
    }

    /// <summary>生成前端注册脚本（文本），便于按需注入</summary>
    [HttpGet("script")]
    public IActionResult Script()
    {
        var rows = _svc.Query(a => a.IsActive)
            .OrderBy(a => a.SortNo)
            .ToList();
        var sb = new System.Text.StringBuilder("// DynActionHelper 动态注册脚本（由数据库生成）\n");
        foreach (var r in rows)
        {
            sb.Append("DynActionHelper.register({")
              .Append("code:").Append(JsonEncode(r.Code))
              .Append(",name:").Append(JsonEncode(r.Name))
              .Append(",actionType:").Append(JsonEncode(r.ActionType))
              .Append(",script:").Append(JsonEncode(r.Script))
              .AppendLine("});");
        }
        return Content(sb.ToString(), "text/plain; charset=utf-8");
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynActionHelper data)
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

    private DynActionHelper FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(a => a.Code == key).First();
    }

    private static string JsonEncode(string s)
        => Newtonsoft.Json.JsonConvert.ToString(s ?? "");
}
