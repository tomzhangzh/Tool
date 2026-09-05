using Microsoft.AspNetCore.Mvc;
using System.Data;
using VueLib.Web.Services;

namespace VueLib.Web.Areas.DynCommon.Controllers;

/// <summary>SQL 下拉选项请求</summary>
public class ProjectOptionsRequest
{
    public int ProjectId { get; set; }
    public string? Sql { get; set; }
    public string? ValueField { get; set; }
    public string? TextField { get; set; }
}

/// <summary>
/// 工程级公共接口（Area: DynCommon）。
/// 无需专用 Model：按工程连接执行配置好的 SQL 返回下拉列表等通用数据。
/// </summary>
[Area("DynCommon")]
public class ProjectController : Controller
{
    private readonly DynProjectService _svc;

    public ProjectController(DynProjectService svc) => _svc = svc;

    /// <summary>
    /// 执行 SQL 返回下拉选项 [{ value, text }]。
    /// POST /DynCommon/Project/Options  body: { projectId, sql, valueField, textField }
    /// GET  /DynCommon/Project/Options?projectId=&amp;sql=&amp;valueField=&amp;textField=（便于浏览器直接验证）
    /// 约定：不传 valueField/textField 时取结果集第 1、2 列；value 去重。
    /// </summary>
    [HttpPost("/DynCommon/Project/Options")]
    public IActionResult Options([FromBody] ProjectOptionsRequest? req)
    {
        if (req == null || req.ProjectId <= 0) return Json(new { success = false, message = "缺少 projectId" });
        return ExecuteOptions(req.ProjectId, req.Sql, req.ValueField, req.TextField);
    }

    [HttpGet("/DynCommon/Project/Options")]
    public IActionResult OptionsGet(int projectId, string sql, string? valueField = null, string? textField = null)
    {
        return ExecuteOptions(projectId, sql, valueField, textField);
    }

    private IActionResult ExecuteOptions(int projectId, string? sql, string? valueField, string? textField)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return Json(new { success = false, message = "sql 不能为空" });

        var project = _svc.GetProject(projectId);
        if (project == null) return Json(new { success = false, message = "工程不存在" });

        try
        {
            using var db = _svc.CreateProjectClient(project);
            var dt = db.Ado.GetDataTable(sql);
            if (dt == null || dt.Columns.Count == 0)
                return Json(new { success = false, message = "查询无结果集" });

            var vf = string.IsNullOrWhiteSpace(valueField) ? dt.Columns[0].ColumnName : valueField!.Trim();
            var tf = string.IsNullOrWhiteSpace(textField) ? (dt.Columns.Count > 1 ? dt.Columns[1].ColumnName : vf) : textField!.Trim();

            var seen = new HashSet<string?>();
            var list = new List<object>();
            foreach (DataRow row in dt.Rows)
            {
                var v = row[vf]?.ToString();
                if (string.IsNullOrEmpty(v) || !seen.Add(v)) continue;
                list.Add(new { value = v, text = row[tf]?.ToString() ?? v });
            }
            return Json(new { success = true, data = list });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "SQL 执行失败：" + ex.Message });
        }
    }
}
