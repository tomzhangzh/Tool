using Microsoft.AspNetCore.Mvc;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// Demo 页面：所有功能演示页
/// </summary>
[Area("Platform")]
public class DemoController : Controller
{
    private const string Demo = "~/Areas/Platform/Views/Demo/{0}.cshtml";
    private const string Page = "~/Areas/Platform/Views/Page/{0}.cshtml";

    /// <summary>TabsBlock 演示：主表单 + 子表标签页联动</summary>
    [HttpGet("/Platform/Page/TabsBlockDemo")]
    public IActionResult TabsBlockDemo()
    {
        ViewData["Title"] = "TabsBlock 演示 - VueLibV4";
        ViewData["DemoEdit"] = Request.Query["demo"] == "edit";

        // 学生详情表单配置
        var nameInput = new { component = "DynElInput", modelname = "Name",
            options = new { labeloptions = new { label = "学生姓名" }, comoptions = new {} } };
        var noInput = new { component = "DynElInput", modelname = "StudentNo",
            options = new { labeloptions = new { label = "学号" }, comoptions = new {} } };
        var gradeInput = new { component = "DynElInput", modelname = "Grade",
            options = new { labeloptions = new { label = "年级" }, comoptions = new {} } };

        var detailCfg = new {
            component = "DynGridContainer",
            options = new { comoptions = new {}, itemoptions = new { style = new { gap = "8px" }, @class = "" } },
            childrenctrls = new[] { nameInput, noInput, gradeInput }
        };

        var detailBlkConfig = Newtonsoft.Json.JsonConvert.SerializeObject(new {
            table = "Student", keyField = "Id",
            detailConfig = detailCfg,
            detailDefault = new { Name = "", StudentNo = "", Grade = "" }
        });

        var tabsBlkConfig = Newtonsoft.Json.JsonConvert.SerializeObject(new {
            detailBlkConfig = detailBlkConfig,
            idField = "Id",
            subTabs = new[] {
                new { key = "course", label = "相关课程", fkField = "StudentId" },
                new { key = "score",  label = "成绩",     fkField = "StudentId" }
            }
        });

        ViewData["TabsBlkConfig"] = tabsBlkConfig;
        ViewData["ExtId"] = "tabsDemo-" + Guid.NewGuid().ToString("N");
        return View(string.Format(Page, "TabsBlockDemo"));
    }

    [HttpGet("/Platform/Page/KanbanDemo")]
    public IActionResult KanbanDemo()
    {
        ViewData["Title"] = "Kanban 看板 - VueLibV4";
        return View(string.Format(Page, "KanbanDemo"));
    }

    [HttpGet("/Platform/Page/WidgetDemo")]
    public IActionResult WidgetDemo()
    {
        ViewData["Title"] = "WidgetApp 仪表盘演示";
        var widgets = new object[]
        {
            new { id = "w1", title = "用户统计", url = "", colSpan = 12, rowSpan = 2, isGroup = false },
            new { id = "w2", title = "订单概览", url = "", colSpan = 12, rowSpan = 2, isGroup = false },
            new { id = "w3", title = "数据分组", colSpan = 24, rowSpan = 4, isGroup = true, children = new object[]
            {
                new { id = "w3a", title = "子项A", url = "", colSpan = 12, rowSpan = 2, isGroup = false },
                new { id = "w3b", title = "子项B", url = "", colSpan = 12, rowSpan = 2, isGroup = false }
            }}
        };
        var blkConfig = Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            allowAddGroup = true,
            allowAddWidget = true,
            allowResize = true,
            allowSort = true,
            columns = 24,
            pageId = "widget-demo",
            widgets = widgets
        });
        ViewData["BlkConfig"] = blkConfig;
        ViewData["ExtId"] = "wd-" + Guid.NewGuid().ToString("N");
        return View(string.Format(Page, "WidgetDemo"));
    }

    [HttpGet("/Platform/Page/ListViewDemo")]
    public IActionResult ListViewDemo()
    {
        ViewData["Title"] = "ListViewApp 卡片网格列表演示";
        ViewData["ListSettingId"] = 133;
        ViewData["TableName"] = "SysMenu";
        ViewData["KeyField"] = "Id";
        ViewData["Project"] = "__platform__";
        ViewData["ExecId"] = "lv-demo-" + Guid.NewGuid().ToString("N")[..8];
        ViewData["ExtId"] = "lv-" + Guid.NewGuid().ToString("N")[..12];
        return View(string.Format(Page, "ListViewDemo"));
    }

    [HttpGet("/Platform/Page/Demo/ActionHelper")]
    public IActionResult DemoActionHelper()
    {
        ViewData["Title"] = "动作助手 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "ActionHelper"));
    }

    [HttpGet("/Platform/Page/Demo/Combination")]
    public IActionResult DemoCombination()
    {
        ViewData["Title"] = "组合组件 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Combination"));
    }

    [HttpGet("/Platform/Page/Demo/Grid")]
    public IActionResult DemoGrid()
    {
        ViewData["Title"] = "栅格布局 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Grid"));
    }

    [HttpGet("/Platform/Page/Demo/Crud")]
    public IActionResult DemoCrud()
    {
        ViewData["Title"] = "免模型 CRUD Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Crud"));
    }

    [HttpGet("/Platform/Page/Demo/Gallery")]
    public IActionResult DemoGallery()
    {
        ViewData["Title"] = "组件画廊 - VueLibV4";
        return View(string.Format(Demo, "Gallery"));
    }

    [HttpGet("/Platform/Page/Demo/TemplateImport")]
    public IActionResult DemoTemplateImport()
    {
        ViewData["Title"] = "模板导入 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "TemplateImport"));
    }

    [HttpGet("/Platform/Page/Demo/CodeMirror")]
    public IActionResult DemoCodeMirror()
    {
        ViewData["Title"] = "CodeMirror Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "CodeMirror"));
    }

    /// <summary>能力桥 RPC Demo：dyn.service 调用后端 DI 服务 / dyn.eval 执行 C# 取数（含调用规范）</summary>
    [HttpGet("/Platform/Page/Demo/Rpc")]
    public IActionResult DemoRpc()
    {
        ViewData["Title"] = "能力桥 RPC Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View(string.Format(Demo, "Rpc"));
    }

    /// <summary>统一参数上下文 Demo：多来源合并/就近优先、片段继承、Tabs 共享层 commit（快照型 vs 跟随型）</summary>
    [HttpGet("/Platform/Page/Demo/TabsParams")]
    public IActionResult DemoTabsParams()
    {
        ViewData["Title"] = "统一参数上下文 Tabs 传参 Demo - VueLibV4";
        return View(string.Format(Demo, "TabsParams"));
    }

    /// <summary>下拉联动 Demo：DynElSelect 通过 SQL 模板 {{model.city}} 实现级联刷新</summary>
    [HttpGet("/Platform/Page/Demo/CascadeSelect")]
    public IActionResult DemoCascadeSelect()
    {
        ViewData["Title"] = "下拉联动 Demo - VueLibV4";
        return View(string.Format(Demo, "CascadeSelect"));
    }

    /// <summary>Mac 风格桌面 Demo（仿 macOS / portfolio.zxh.me 拟物风格，独立 _LayoutMac 布局）</summary>
    [HttpGet("/Platform/Page/MacDesktopDemo")]
    public IActionResult DemoMac()
    {
        ViewData["Title"] = "Mac 桌面 Demo - VueLibV4";
        ViewData["ApiBase"] = "/api";
        return View("~/Views/DynTemplates/MacDesktopDemo.cshtml");
    }
}
