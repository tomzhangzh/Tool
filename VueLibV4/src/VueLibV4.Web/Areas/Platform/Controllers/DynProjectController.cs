using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Services.Data;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 动态项目：属于某个 DesktopSolution（强类型 Model，外键 int Id）。
/// 一个解决方案可有多个项目，项目本质是"不同的数据库"（各自持有连接串）。
/// 平台库 CRUD 走 IDynProjectService；test 端点需按用户提交的连接串临时连库，保留 DbFactory 基础设施。
/// </summary>
[Area("Platform")]
[Route("api/platform/dynproject")]
[ApiController]
public class DynProjectController : ControllerBase
{
    private readonly IDynProjectService _svc;
    private readonly IDesktopSolutionService _solutions;
    private readonly DbFactory _dbs;

    public DynProjectController(IDynProjectService svc, IDesktopSolutionService solutions, DbFactory dbs)
    {
        _svc = svc;
        _solutions = solutions;
        _dbs = dbs;
    }

    [HttpGet("all")]
    public ApiResult All(string solutionId = null)
    {
        // 兼容：solutionId 可为数字 Id 或解决方案 Code
        if (!string.IsNullOrEmpty(solutionId) && !int.TryParse(solutionId, out _))
        {
            var sol = _solutions.List(s => s.Code == solutionId).FirstOrDefault();
            if (sol == null) return ApiResult.Ok(new List<DynProject>());
            solutionId = sol.Id.ToString();
        }
        int? sid = int.TryParse(solutionId, out var v) ? v : null;

        var rows = _svc.Query(p => p.IsActive && (sid == null || p.SolutionId == sid))
            .OrderBy(p => p.SortNo)
            .ToList();
        return ApiResult.Ok(rows);
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
    {
        return ApiResult.Ok(FirstByIdOrCode(id));
    }

    /// <summary>测试项目连接串是否可用</summary>
    [HttpPost("test")]
    public ApiResult Test([FromBody] JObject data)
    {
        try
        {
            using var db = _dbs.ProjectDb(data["ConnectionString"]?.ToString());
            db.Ado.CheckConnection();
            return ApiResult.Ok(true, "连接成功");
        }
        catch (Exception ex)
        {
            return ApiResult.Fail("连接失败: " + ex.Message);
        }
    }

    [HttpPost("save")]
    public ApiResult Save([FromBody] DynProject data)
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

    private DynProject FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _svc.GetById(id);
        return _svc.Query(p => p.Code == key).First();
    }
}
