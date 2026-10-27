using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态网页元数据 CRUD：真正的动态页面（强类型 Model + 强类型服务，外键 int Id）。
/// 选择一个 DynTemplate，结合用户配置参数（ParamsJson：模板自身参数 + blocks 槽位分组）实例化 PageJson，即可运行。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynpage")]
[ApiController]
public class DynPageController : ControllerBase
{
    private readonly IDynWebPageService _svc;
    private readonly IDynProjectService _projects;

    public DynPageController(
        IDynWebPageService svc,
        IDynProjectService projects)
    {
        _svc = svc;
        _projects = projects;
    }

    [HttpGet("all")]
    public ApiResult All(string projectId = null)
    {
        // 兼容：projectId 可为数字 Id 或项目 Code
        if (!string.IsNullOrEmpty(projectId) && !int.TryParse(projectId, out _))
        {
            var proj = _projects.List(x => x.Code == projectId).FirstOrDefault();
            if (proj == null) return ApiResult.Ok(new List<DynWebPage>());
            projectId = proj.Id.ToString();
        }
        int? pid = int.TryParse(projectId, out var v) ? v : null;

        var rows = _svc.Query(p => p.IsActive && (pid == null || p.ProjectId == pid))
            .OrderBy(p => p.CreateTime, OrderByType.Desc)
            .ToList();
        return ApiResult.Ok(rows);
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        return ApiResult.Ok(FirstByIdOrCode(id));
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynWebPage data)
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

    private DynWebPage FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(p => p.Code == key).First();
    }
}
