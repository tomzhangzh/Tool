using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 页面图表控制器：ECharts 示例数据源。
/// 访问示例：/Platform/Chart/Data
/// </summary>
[Area("Platform")]
public class PageChartController : Controller
{
    /// <summary>
    /// ECharts Demo 数据源：返回一组示例图表数据，演示 DynEChart 组件接口传数据。
    /// </summary>
    [HttpGet("/Platform/Chart/Data")]
    public IActionResult EChartData()
    {
        var data = new
        {
            xData = new[] { "华东", "华南", "华北", "西南", "华中", "东北" },
            seriesData = new[] { 1280, 1560, 980, 720, 1120, 640 }
        };
        return Json(new { code = 0, msg = "ok", data });
    }
}
