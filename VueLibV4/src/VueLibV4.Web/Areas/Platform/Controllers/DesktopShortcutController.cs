using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;
using VueLibV4.Services.Data.Sugar;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 桌面快捷方式：关联解决方案，可指向网页/动作/外部URL（强类型 Model，外键 int Id）。
/// 强类型服务样板：IDesktopShortcutService 由启动扫描自动注入（IScopeDependency），
/// CRUD/分页全部来自 ISugarService&lt;T&gt; 基类，控制器不再直接接触 DbFactory/SqlSugarClient。
/// </summary>
[Area("Platform")]
[Route("api/platform/desktopshortcut")]
[ApiController]
public class DesktopShortcutController : ControllerBase
{
    private readonly IDesktopShortcutService _shortcuts;
    private readonly IDesktopSolutionService _solutions;

    public DesktopShortcutController(IDesktopShortcutService shortcuts, IDesktopSolutionService solutions)
    {
        _shortcuts = shortcuts;
        _solutions = solutions;
    }

    [HttpGet("all")]
    public ApiResult All(string solutionId = null)
    {
        // 兼容：solutionId 可为数字 Id 或解决方案 Code
        if (!string.IsNullOrEmpty(solutionId) && !int.TryParse(solutionId, out _))
        {
            var sol = _solutions.List(s => s.Code == solutionId).FirstOrDefault();
            if (sol == null) return ApiResult.Ok(new List<DesktopShortcut>());
            solutionId = sol.Id.ToString();
        }

        int? sid = int.TryParse(solutionId, out var v) ? v : null;
        var rows = _shortcuts
            .Query(s => s.IsActive && (sid == null || s.SolutionId == sid))
            .OrderBy(s => s.SortNo)
            .ToList();
        return ApiResult.Ok(rows);
    }

    [HttpGet("get")]
    public ApiResult Get(string id)
        => ApiResult.Ok(FirstByIdOrCode(id));

    [HttpPost("save")]
    public ApiResult Save([FromBody] DesktopShortcut data)
    {
        if (data.Id <= 0)
        {
            _shortcuts.Insert(data);   // 自增 Id 回填
            return ApiResult.Ok(new { data.Id }, "新增成功");
        }
        _shortcuts.Update(data);
        return ApiResult.Ok(new { data.Id }, "保存成功");
    }

    [HttpPost("delete")]
    public ApiResult Delete([FromBody] JObject keys)
    {
        var id = keys["Id"]?.Value<int>() ?? 0;
        if (id <= 0) return ApiResult.Fail("缺少 Id");
        _shortcuts.DeleteById(id);
        return ApiResult.Ok(true, "删除成功");
    }

    private DesktopShortcut FirstByIdOrCode(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (int.TryParse(key, out var id)) return _shortcuts.GetById(id);
        return _shortcuts.Query(s => s.Code == key).First();
    }
}
