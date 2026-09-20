using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SqlSugar;
using VueLibV4.Web.Core;
using VueLibV4.Web.Models;

namespace VueLibV4.Web.Areas.Business.Controllers;

/// <summary>
/// 页面生成器（M4）：选项目/选表/勾选字段 → 生成筛选/列表/详情三份组件配置树，
/// 落库 3 条 PageSetting + 1 条 DynWebPage（绑定三屏与 filter-list-detail 模板），返回运行 URL。
/// 路由：api/business/pagegen
/// </summary>
[Area("Business")]
[Route("api/business/pagegen")]
[ApiController]
public class PageGenController : ControllerBase
{
    private readonly DbFactory _dbs;
    public PageGenController(DbFactory dbs) { _dbs = dbs; }

    public class GenField
    {
        public string Name { get; set; } = "";
        public string Label { get; set; } = "";
        public string DataType { get; set; } = "TEXT";
        /// <summary>input/textarea/number/switch/date/select</summary>
        public string Control { get; set; } = "input";
        public bool InFilter { get; set; }
        public bool InList { get; set; }
        public bool InDetail { get; set; }
        /// <summary>筛选操作符：like/eq/gt/ge/lt/le</summary>
        public string Op { get; set; } = "like";
        public int? Width { get; set; }
        public bool Sortable { get; set; }
        public string DictType { get; set; }
    }

    [HttpPost("generate")]
    public ApiResult Generate([FromBody] JObject req)
    {
        var project = req["project"]?.ToString();
        var table = req["table"]?.ToString();
        var code = (req["code"]?.ToString() ?? "").Trim();
        var name = (req["name"]?.ToString() ?? "").Trim();
        var pk = (req["pk"]?.ToString() ?? "Id").Trim();
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少表名");
        if (string.IsNullOrWhiteSpace(code)) return ApiResult.Fail("缺少页面编码");
        if (string.IsNullOrWhiteSpace(name)) name = code;

        var fields = req["fields"]?.ToObject<List<GenField>>() ?? new List<GenField>();
        if (fields.Count == 0) return ApiResult.Fail("请至少选择一个字段");

        using var db = _dbs.PlatformDb();

        // 项目 Code → Id
        int? projectId = null;
        if (!string.IsNullOrWhiteSpace(project))
        {
            var proj = int.TryParse(project, out var pid)
                ? db.Queryable<DynProject>().First(x => x.Id == pid)
                : db.Queryable<DynProject>().First(x => x.Code == project);
            projectId = proj?.Id;
        }

        var filterCfg = BuildFilterCfg(code, fields);
        var listCfg = BuildListCfg(code, project, table, fields);
        var detailCfg = BuildDetailCfg(code, pk, fields);

        // 幂等：同 code 重新生成时更新既有 PageSetting
        var filterId = UpsertSetting(db, code + "_filter", name + " - 筛选区", "Filter", projectId, table, filterCfg);
        var listId = UpsertSetting(db, code + "_list", name + " - 列表区", "List", projectId, table, listCfg);
        var detailId = UpsertSetting(db, code + "_detail", name + " - 详情区", "Detail", projectId, table, detailCfg);

        var tpl = db.Queryable<DynTemplate>().First(t => t.Code == "filter-list-detail");
        var existPage = db.Queryable<DynWebPage>().First(p => p.Code == code);
        var page = new DynWebPage
        {
            Code = code,
            Name = name,
            ProjectId = projectId,
            TemplateId = tpl?.Id,
            FilterPageSettingId = filterId,
            ListPageSettingId = listId,
            DetailPageSettingId = detailId,
            PageJson = null,
            ConfigJson = new JObject { ["gridId"] = "grid_" + code }.ToString(Newtonsoft.Json.Formatting.None),
            Url = "/Business/Templates/Run?code=" + Uri.EscapeDataString(code),
            IsActive = true
        };
        if (existPage != null)
        {
            page.Id = existPage.Id;
            page.CreateTime = existPage.CreateTime;
            db.Updateable(page).ExecuteCommand();
        }
        else
        {
            page.Id = db.Insertable(page).ExecuteReturnIdentity();
        }

        return ApiResult.Ok(new
        {
            page.Id,
            code,
            name,
            url = page.Url,
            settingIds = new { filter = filterId, list = listId, detail = detailId }
        }, "页面生成成功");
    }

    // ---------------- 配置树构建 ----------------

    private static JObject BaseNode(string component, string modelName, JObject comOptions, string label = null, bool showLabel = false)
    {
        return new JObject
        {
            ["component"] = component,
            ["modelname"] = modelName,
            ["options"] = new JObject
            {
                ["comoptions"] = comOptions,
                ["comlisteners"] = new JObject(),
                ["labeloptions"] = new JObject { ["label"] = label ?? "", ["required"] = false, ["show"] = showLabel },
                ["itemoptions"] = new JObject { ["style"] = new JObject(), ["class"] = "" }
            },
            ["validators"] = new JArray(),
            ["childrenctrls"] = new JArray(),
            ["slots"] = new JObject(),
            ["extendinfo"] = new JObject()
        };
    }

    private static JObject FieldNode(GenField f)
    {
        var comp = f.Control switch
        {
            "textarea" => "DynElTextarea",
            "number" => "DynElInputNumber",
            "switch" => "DynElSwitch",
            "date" => "DynElDatePicker",
            "select" => "DynElSelect",
            _ => "DynElInput"
        };
        var co = new JObject();
        if (comp == "DynElTextarea") { co["rows"] = 3; co["placeholder"] = "请输入" + f.Label; }
        else if (comp == "DynElInputNumber") { co["step"] = 1; }
        else if (comp == "DynElDatePicker") { co["type"] = "date"; co["valueFormat"] = "YYYY-MM-DD"; }
        else if (comp == "DynElSelect")
        {
            if (!string.IsNullOrWhiteSpace(f.DictType)) { co["sourceType"] = "dict"; co["dictType"] = f.DictType; }
            else { co["sourceType"] = "static"; co["optionValuesText"] = ""; co["placeholder"] = "请选择"; co["clearable"] = true; }
        }
        else { co["placeholder"] = "请输入" + f.Label; co["clearable"] = true; }

        var node = BaseNode(comp, f.Name, co, f.Label, true);
        return node;
    }

    private static JObject BuildFilterCfg(string code, List<GenField> fields)
    {
        var children = new JArray();
        foreach (var f in fields.Where(x => x.InFilter)) children.Add(FieldNode(f));

        // 查询 / 重置 按钮行（dyn-click 管道：grid 动作按 gridId 刷新；reset=true 先清空筛选字段）
        var btnRow = BaseNode("DynElContainer", "", new JObject { ["direction"] = "horizontal" });
        btnRow["options"]!["itemoptions"]!["style"] = new JObject { ["display"] = "flex", ["gap"] = "8px", ["marginTop"] = "8px" };
        var btnQuery = BaseNode("DynElButton", "", new JObject
        {
            ["text"] = "查询", ["type"] = "primary",
            ["dyn-click"] = "grid('grid_" + code + "')"
        });
        var btnReset = BaseNode("DynElButton", "", new JObject
        {
            ["text"] = "重置",
            ["dyn-click"] = "grid({\"gridId\":\"grid_" + code + "\",\"reset\":true})"
        });
        btnRow["childrenctrls"] = new JArray(btnQuery, btnReset);
        children.Add(btnRow);

        var root = BaseNode("DynElContainer", "", new JObject { ["direction"] = "vertical" });
        root["options"]!["itemoptions"]!["style"] = new JObject();
        root["childrenctrls"] = children;
        return root;
    }

    private static JObject BuildListCfg(string code, string project, string table, List<GenField> fields)
    {
        var filterFields = new JArray();
        foreach (var f in fields.Where(x => x.InFilter))
            filterFields.Add(new JObject { ["field"] = f.Name, ["op"] = string.IsNullOrWhiteSpace(f.Op) ? "eq" : f.Op });

        var columns = new JArray();
        foreach (var f in fields.Where(x => x.InList))
        {
            var col = new JObject { ["prop"] = f.Name, ["label"] = f.Label, ["sortable"] = f.Sortable };
            if (f.Width != null) col["width"] = f.Width;
            columns.Add(col);
        }

        var co = new JObject
        {
            ["gridId"] = "grid_" + code,
            ["table"] = table,
            ["pager"] = true,
            ["size"] = 10,
            ["filterFromScope"] = true,
            ["filterFields"] = filterFields,
            ["columns"] = columns,
            ["actionsWidth"] = 120,
            ["actions"] = new JArray
            {
                new JObject
                {
                    ["label"] = "编辑", ["type"] = "primary", ["action"] = "open",
                    ["options"] = new JObject
                    {
                        ["mode"] = "fragment",
                        ["title"] = "编辑",
                        ["width"] = "760px",
                        ["height"] = "85vh",
                        ["url"] = "/Business/Templates/Detail?code=" + Uri.EscapeDataString(code)
                    }
                }
            }
        };
        if (!string.IsNullOrWhiteSpace(project)) co["project"] = project;

        var root = BaseNode("DynTable", "", co);
        return root;
    }

    private static JObject BuildDetailCfg(string code, string pk, List<GenField> fields)
    {
        var children = new JArray();

        // 主键隐藏域（新增时为空 → 后端 insert；编辑时有值 → update）
        var hidden = BaseNode("DynElInput", pk, new JObject { ["placeholder"] = "" }, "", false);
        hidden["options"]!["itemoptions"]!["style"] = new JObject { ["display"] = "none" };
        children.Add(hidden);

        foreach (var f in fields.Where(x => x.InDetail)) children.Add(FieldNode(f));

        var saveUrl = "/Business/Templates/Save?code=" + Uri.EscapeDataString(code);
        var btnRow = BaseNode("DynElContainer", "", new JObject { ["direction"] = "horizontal" });
        btnRow["options"]!["itemoptions"]!["style"] = new JObject { ["display"] = "flex", ["gap"] = "8px", ["justifyContent"] = "flex-end", ["marginTop"] = "12px" };
        var btnCancel = BaseNode("DynElButton", "", new JObject { ["text"] = "取消", ["dyn-click"] = "close" });
        var btnSave = BaseNode("DynElButton", "", new JObject
        {
            ["text"] = "保存", ["type"] = "primary",
            ["dyn-click"] = "submit",
            ["data-dyn-url"] = saveUrl,
            ["data-dyn-target"] = "#detailFragment"
        });
        btnRow["childrenctrls"] = new JArray(btnCancel, btnSave);
        children.Add(btnRow);

        var root = BaseNode("DynElContainer", "", new JObject { ["direction"] = "vertical" });
        root["childrenctrls"] = children;
        return root;
    }

    // ---------------- 落库 ----------------

    private static int UpsertSetting(SqlSugarClient db, string settingCode, string settingName,
        string type, int? projectId, string table, JObject cfg)
    {
        var json = cfg.ToString(Newtonsoft.Json.Formatting.None);
        var exist = db.Queryable<PageSetting>().First(x => x.Code == settingCode);
        if (exist != null)
        {
            exist.Name = settingName;
            exist.SettingType = type;
            exist.ProjectId = projectId;
            exist.TableName = table;
            exist.ConfigJson = json;
            db.Updateable(exist).ExecuteCommand();
            return exist.Id;
        }
        var row = new PageSetting
        {
            Code = settingCode,
            Name = settingName,
            SettingType = type,
            ProjectId = projectId,
            TableName = table,
            ConfigJson = json,
            IsActive = true
        };
        // 显式取回自增 Id（SQLite 下 ExecuteCommand 不保证回填实体）
        return db.Insertable(row).ExecuteReturnIdentity();
    }
}
