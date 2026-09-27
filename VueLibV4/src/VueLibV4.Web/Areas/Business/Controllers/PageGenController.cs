using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using VueLibV4.Services.Extensions;
using VueLibV4.Web.Core;
using VueLibV4.Platform.Models;
using VueLibV4.Platform.Services;

namespace VueLibV4.Web.Areas.Business.Controllers;

/// <summary>
/// 页面生成器（M4 v2）：选表 → 读 SQLite schema（列/类型/NOT NULL/主键/DEFAULT）→
/// 按"三屏双层配置+双渲染"新模式生成 筛选(Filter)/列表(List)/详情(Detail) 三份 PageSetting，
/// 落库 3 条 PageSetting（含 ConfigJson=UI 渲染树 + DefaultJson=model 骨架 + RenderMode/PartialPath）
/// + 1 条 DynWebPage（绑定三屏，ConfigJson=模板参数 TableName/DetailPageSettingId，URL 由外壳自动补齐）。
/// 路由：api/business/pagegen
/// </summary>
[Area("Business")]
[Route("api/business/pagegen")]
[ApiController]
public class PageGenController : ControllerBase
{
    private readonly IDynProjectService _projects;
    private readonly IDynTemplateService _templates;
    private readonly IDynWebPageService _pages;
    private readonly IPageSettingService _settings;
    private readonly IConfiguration _config;

    public PageGenController(
        IDynProjectService projects,
        IDynTemplateService templates,
        IDynWebPageService pages,
        IPageSettingService settings,
        IConfiguration config)
    {
        _projects = projects;
        _templates = templates;
        _pages = pages;
        _settings = settings;
        _config = config;
    }

    /// <summary>表列元数据（从 CREATE TABLE 解析）</summary>
    public class ColumnMeta
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "TEXT";
        public bool NotNull { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool HasDefault { get; set; }
        /// <summary>外键目标表（列名 XxxId 且非主键时推断）；无则空</summary>
        public string? FkTable { get; set; }
        public string? DefaultValue { get; set; }
    }

    [HttpPost("generate")]
    public ApiResult Generate([FromBody] JObject req)
    {
        var project = req["project"]?.ToString();
        var table = (req["table"]?.ToString() ?? "").Trim();
        var code = (req["code"]?.ToString() ?? "").Trim();
        var name = (req["name"]?.ToString() ?? "").Trim();
        var pk = (req["pk"]?.ToString() ?? "Id").Trim();
        // 渲染方式：Front=全 DynCom；Back=列表走后端 Razor 局部视图（筛选/详情仍走 DynCom，最灵活）
        var renderMode = (req["renderMode"]?.ToString() ?? "Front").Trim();
        if (renderMode != "Back") renderMode = "Front";
        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少表名");
        if (string.IsNullOrWhiteSpace(code)) return ApiResult.Fail("缺少页面编码");
        if (string.IsNullOrWhiteSpace(name)) name = code;

        // 用户指定的字典映射：{ 列名: dictType }
        var dictTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (req["dictTypes"] is JObject dts)
            foreach (var kv in dts) dictTypes[kv.Key.Trim()] = kv.Value?.ToString() ?? "";

        var schema = ReadTableSchema(table);
        if (schema.Count == 0) return ApiResult.Fail($"表 {table} 不存在或无法读取 schema");

        var columns = BuildColumns(schema, dictTypes);

        // 字段选择：req["fields"] 可选（勾选）；缺省按列智能分类
        var fieldNames = new List<string>();
        if (req["fields"] is JArray fa)
            foreach (var t in fa) fieldNames.Add(t?.ToString() ?? "");
        var fields = columns.Where(c => fieldNames.Count == 0 || fieldNames.Contains(c.Name)).ToList();
        if (fields.Count == 0) return ApiResult.Fail("请至少选择一个字段");

        // 工程归属：外键/字典下拉走 /api/dyncommon/options?projectId 需要工程；未指定则回退平台工程
        int? projectId = ResolveProjectId(project);
        if (projectId == null)
        {
            try { projectId = _projects.Query(x => x.Code == "Platform").First()?.Id; } catch { }
            if (projectId == null) { try { projectId = _projects.Query(x => true).First()?.Id; } catch { } }
        }

        // ---------- 生成三屏（ConfigJson=UI / DefaultJson=model 骨架） ----------
        var filterCfg = BuildFilterCfg(code, fields, projectId);
        var filterDefault = BuildFilterDefaultJson(fields);
        var listCfg = BuildListCfg(table, fields);
        var listDefault = BuildListDefaultJson(fields);
        var detailCfg = BuildDetailCfg(pk, fields, projectId);
        var detailDefault = BuildDetailDefaultJson(pk, fields);

        // 落库三屏（幂等：同 code 更新）
        var filterId = UpsertSetting(code + "_filter", name + " - 筛选区", "Filter", renderMode, null, table, projectId, filterCfg, filterDefault);
        var listId = UpsertSetting(code + "_list", name + " - 列表区", "List", renderMode,
            renderMode == "Back" ? "~/Views/DynTemplates/Partials/ListBack.cshtml" : null,
            table, projectId, listCfg, listDefault);
        var detailId = UpsertSetting(code + "_detail", name + " - 详情区", "Detail", "Front", null, table, projectId, detailCfg, detailDefault);

        // DynWebPage：ConfigJson=模板参数（TableName + DetailPageSettingId；ListUrl/AddUrl/EditUrl/DeleteUrl 由外壳自动补齐）
        var tpl = _templates.Query(t => t.Code == "crud-basic").First();
        var existPage = _pages.Query(p => p.Code == code).First();
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
            ConfigJson = new JObject
            {
                ["TableName"] = table,
                ["DetailPageSettingId"] = detailId
            }.ToJson(),
            IsActive = true
        };
        if (existPage != null) { page.Id = existPage.Id; page.CreateTime = existPage.CreateTime; _pages.Update(page); }
        else _pages.Insert(page); // 自增 Id 回填

        page.Url = "/Platform/Page/DynWebPage?id=" + page.Id;
        _pages.Update(page);

        return ApiResult.Ok(new
        {
            page.Id,
            code,
            name,
            renderMode,
            url = page.Url,
            settingIds = new { filter = filterId, list = listId, detail = detailId }
        }, "页面生成成功");
    }

    // ---------------- Schema 读取 ----------------

    private List<ColumnMeta> ReadTableSchema(string table)
    {
        var dbFiles = new[] { "BusinessDb", "PlatformDb" };
        foreach (var key in dbFiles)
        {
            var cs = _config.GetConnectionString(key);
            if (string.IsNullOrWhiteSpace(cs)) continue;
            var list = TryReadSchema(cs, table);
            if (list != null) return list;
        }
        return new List<ColumnMeta>();
    }

    private List<ColumnMeta>? TryReadSchema(string cs, string table)
    {
        var builder = new SqliteConnectionStringBuilder(cs);
        var ds = builder.DataSource;
        if (!Path.IsPathRooted(ds)) builder.DataSource = Path.Combine(AppContext.BaseDirectory, ds.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            using var conn = new SqliteConnection(builder.ConnectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name=$t";
            cmd.Parameters.AddWithValue("$t", table);
            var createSql = cmd.ExecuteScalar() as string;
            if (string.IsNullOrWhiteSpace(createSql)) return null;

            var list = new List<ColumnMeta>();
            cmd.CommandText = "PRAGMA table_info(\"" + table.Replace("\"", "\"\"") + "\")";
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    list.Add(new ColumnMeta
                    {
                        Name = rd.GetString(1),
                        Type = rd.GetString(2),
                        NotNull = rd.GetInt32(3) != 0,
                        IsPrimaryKey = rd.GetInt32(5) != 0
                    });
                }
            }
            // 从 CREATE SQL 解析 DEFAULT 与主键约束
            ParseDefaults(createSql, list);
            // 收集库中全部表名，供外键目标匹配
            var allTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var tc = conn.CreateCommand())
            {
                tc.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
                using (var tr = tc.ExecuteReader()) while (tr.Read()) allTables.Add(tr.GetString(0));
            }
            // 外键推断：XxxId 且非主键 → 匹配目标表（精确 → 复数 → 前缀/后缀；ParentId 关联自身表）
            foreach (var col in list)
            {
                if (col.IsPrimaryKey || col.Name == "Id") continue;
                if (col.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && col.Name.Length > 2)
                {
                    var baseName = col.Name.Substring(0, col.Name.Length - 2);
                    string? hit = null;
                    if (allTables.Contains(baseName)) hit = baseName;
                    else if (allTables.Contains(baseName + "s")) hit = baseName + "s";
                    else hit = allTables.FirstOrDefault(t =>
                        t.EndsWith(baseName, StringComparison.OrdinalIgnoreCase) || t.StartsWith(baseName, StringComparison.OrdinalIgnoreCase));
                    // 语义映射：常见列后缀 → 已知目标表（XxxPageSettingId → PageSetting 等）
                    if (hit == null)
                    {
                        var low = col.Name.ToLowerInvariant();
                        if (low.EndsWith("pagesettingid") && allTables.Contains("PageSetting")) hit = "PageSetting";
                        else if (low.EndsWith("settingid") && allTables.Contains("PageSetting")) hit = "PageSetting";
                        else if (low.EndsWith("templateid") && allTables.Contains("DynTemplate")) hit = "DynTemplate";
                        else if (low.EndsWith("projectid") && allTables.Contains("DynProject")) hit = "DynProject";
                        else if (low.EndsWith("dictid") && allTables.Contains("DynDict")) hit = "DynDict";
                        else if (low.EndsWith("menuid") && allTables.Contains("SysMenu")) hit = "SysMenu";
                        else if (low.EndsWith("actionhelperid") && allTables.Contains("DynActionHelper")) hit = "DynActionHelper";
                    }
                    if (hit == null && string.Equals(baseName, "Parent", StringComparison.OrdinalIgnoreCase) && allTables.Contains(table)) hit = table;
                    if (hit != null) col.FkTable = hit;
                }
            }
            return list;
        }
        catch { return null; }
    }

    private static void ParseDefaults(string createSql, List<ColumnMeta> list)
    {
        foreach (var col in list)
        {
            // 定位列定义行：列名（"Name"或 Name）后到逗号
            var pat = new System.Text.RegularExpressions.Regex(
                $"(?is)[\"\\[]?{System.Text.RegularExpressions.Regex.Escape(col.Name)}[\"\\]]?\\s+\\S+[^,]*");
            var m = pat.Match(createSql);
            if (!m.Success) continue;
            var def = m.Value;
            col.HasDefault = System.Text.RegularExpressions.Regex.IsMatch(def, @"(?i)\bDEFAULT\b");
            var dm = System.Text.RegularExpressions.Regex.Match(def, @"(?i)DEFAULT\s+(\([^)]*\)|\S+)");
            if (dm.Success) col.DefaultValue = dm.Groups[1].Value.Trim('(', ')', '\'', '"', ' ');
        }
    }

    private static bool TableExists(SqliteConnection conn, string table)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$t";
        cmd.Parameters.AddWithValue("$t", table);
        return cmd.ExecuteScalar() != null;
    }

    // ---------------- 列分类 ----------------

    private class GenColumn
    {
        public string Name { get; set; } = "";
        public string Label { get; set; } = "";
        public string DataType { get; set; } = "TEXT";
        public bool NotNull { get; set; }
        public bool HasDefault { get; set; }
        public bool IsPk { get; set; }
        public string Control { get; set; } = "input"; // input/textarea/number/switch/select
        public string Op { get; set; } = "like";       // like/eq
        public bool InFilter { get; set; }
        public bool InList { get; set; }
        public bool InDetail { get; set; }
        public bool CellTag { get; set; }
        public string? FkTable { get; set; }
        public string? DictType { get; set; }
    }

    private List<GenColumn> BuildColumns(List<ColumnMeta> schema, Dictionary<string, string> dictTypes)
    {
        var list = new List<GenColumn>();
        foreach (var c in schema)
        {
            var name = c.Name;
            var label = NiceLabel(name);
            var isPk = c.IsPrimaryKey || string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase);
            var type = c.Type.ToUpperInvariant();
            var isBool = type.Contains("INT") && (name.StartsWith("Is", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Has", StringComparison.OrdinalIgnoreCase));
            var isLongText = name.EndsWith("Json", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Content", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Remark", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Ext", StringComparison.OrdinalIgnoreCase);
            var isTime = name.Contains("Time", StringComparison.OrdinalIgnoreCase) || type.Contains("DATE");

            string control = "input";
            if (name.Contains("Icon", StringComparison.OrdinalIgnoreCase) && !isPk) control = "emoji";
            else if (isBool) control = "switch";
            else if (isLongText) control = "textarea";
            else if (!type.Contains("INT") && !type.Contains("NUM") && !type.Contains("REAL") && !type.Contains("DEC")) control = "input";
            else control = "number";

            // 字典/外键 → select
            string? dictType = null;
            string? fkTable = null;
            if (dictTypes.TryGetValue(name, out var dt) && !string.IsNullOrWhiteSpace(dt)) { dictType = dt; control = "select"; }
            else if (!isPk && c.FkTable != null) { fkTable = c.FkTable; control = "select"; }

            var gc = new GenColumn
            {
                Name = name, Label = label, DataType = type, NotNull = c.NotNull, HasDefault = c.HasDefault, IsPk = isPk,
                Control = control, FkTable = fkTable, DictType = dictType
            };
            // 筛选操作符：文本 like，数字/布尔/时间 eq
            gc.Op = control is "input" or "textarea" || type.Contains("TEXT") && !isBool ? "like" : "eq";
            // 进哪些屏
            gc.InFilter = !isPk && !isTime && (control is "input" or "select" or "switch" || isBool);
            gc.InList = !isPk && !isLongText && name != "ExtJson";
            gc.InDetail = !isPk && name != "CreateTime" && name != "UpdateTime";
            gc.CellTag = isBool;
            list.Add(gc);
        }
        return list;
    }

    private static string NiceLabel(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var ch = name[i];
            if (i > 0 && char.IsUpper(ch) && char.IsLower(name[i - 1])) sb.Append(' ');
            sb.Append(ch);
        }
        return sb.ToString();
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
                ["labeloptions"] = new JObject { ["label"] = label ?? "", ["required"] = false, ["show"] = showLabel },
                ["itemoptions"] = new JObject { ["style"] = new JObject(), ["class"] = "" },
                ["compassthrough"] = "comoptions.size,labeloptions.labelposition,labeloptions.labelwidth"
            },
            ["validators"] = new JArray(),
            ["childrenctrls"] = new JArray(),
            ["slots"] = new JObject(),
            ["extendinfo"] = new JObject()
        };
    }

    /// <summary>字段控件节点（Filter 用 modelname=列.value 绑定 {列:{op,value}}；Detail 用 modelname=列 绑定 form）</summary>
    private static JObject FieldNode(GenColumn f, bool filterMode, int? projectId)
    {
        var comp = f.Control switch
        {
            "emoji" => "DynElEmojiPicker",
            "textarea" => "DynElTextarea",
            "number" => "DynElInputNumber",
            "switch" => "DynElSwitch",
            "select" => "DynElSelect",
            _ => "DynElInput"
        };
        var modelName = filterMode ? f.Name + ".value" : f.Name;
        var co = new JObject();
        co["size"] = "small";
        switch (comp)
        {
            case "DynElEmojiPicker":
                co["placeholder"] = "搜索 Emoji（中/英文）"; co["clearable"] = true; break;
            case "DynElTextarea":
                co["rows"] = 3; co["placeholder"] = "请输入" + f.Label; break;
            case "DynElInputNumber":
                co["step"] = 1; break;
            case "DynElSelect":
                if (!string.IsNullOrWhiteSpace(f.DictType)) { co["sourceType"] = "dict"; co["dictType"] = f.DictType; }
                else if (!string.IsNullOrWhiteSpace(f.FkTable)) { co["sourceType"] = "table"; co["table"] = f.FkTable; co["valueField"] = "Id"; co["textField"] = "Name"; co["projectId"] = projectId; }
                else { co["sourceType"] = "static"; co["optionValuesText"] = ""; }
                co["placeholder"] = "请选择"; co["clearable"] = true; break;
            default:
                co["placeholder"] = "请输入" + f.Label; co["clearable"] = true; break;
        }
        return BaseNode(comp, modelName, co, f.Label, true);
    }

    private static JObject BuildFilterCfg(string code, List<GenColumn> fields, int? projectId)
    {
        var children = new JArray();
        foreach (var f in fields.Where(x => x.InFilter)) children.Add(FieldNode(f, true, projectId));

        var btnRow = BaseNode("DynElContainer", "", new JObject { ["direction"] = "horizontal" });
        btnRow["options"]!["itemoptions"]!["style"] = new JObject { ["display"] = "flex", ["gap"] = "8px", ["marginTop"] = "8px" };
        btnRow["childrenctrls"] = new JArray(
            BaseNode("DynElButton", "", new JObject { ["text"] = "查询", ["type"] = "primary", ["size"] = "small", ["dyn-click"] = "query" }),
            BaseNode("DynElButton", "", new JObject { ["text"] = "重置", ["size"] = "small", ["dyn-click"] = "reset" })
        );
        children.Add(btnRow);

        var root = BaseNode("DynGridContainer", "", new JObject
        {
            ["direction"] = "vertical", ["size"] = "small"
        });
        root["options"]!["labeloptions"] = new JObject { ["required"] = false, ["show"] = false, ["labelposition"] = "right", ["labelwidth"] = "120px" };
        root["options"]!["itemoptions"]!["class"] = "grid grid-cols-4 gap-x-3 gap-y-2 items-center";
        root["options"]!["comInnerInfo"] = new JObject { ["labelWidth"] = "100px" };
        root["childrenctrls"] = children;
        return root;
    }

    private static JObject BuildFilterDefaultJson(List<GenColumn> fields)
    {
        var o = new JObject();
        foreach (var f in fields.Where(x => x.InFilter))
            o[f.Name] = new JObject { ["op"] = f.Op, ["value"] = null };
        return o;
    }

    private static JObject BuildListCfg(string table, List<GenColumn> fields)
    {
        var columns = new JArray();
        columns.Add(new JObject { ["field"] = "Id", ["label"] = "Id", ["width"] = 90 });
        foreach (var f in fields.Where(x => x.InList))
        {
            if (f.Name == "CreateTime" || f.Name == "UpdateTime") continue; // 时间列由下方显式追加，避免重复
            var col = new JObject { ["field"] = f.Name, ["label"] = f.Label };
            col["width"] = f.CellTag ? 100 : (f.Control == "textarea" ? 220 : 160);
            if (f.CellTag) col["cellType"] = "tag";
            columns.Add(col);
        }
        columns.Add(new JObject { ["field"] = "CreateTime", ["label"] = "创建时间", ["width"] = 190 });
        return new JObject { ["columns"] = columns };
    }

    private static JObject BuildListDefaultJson(List<GenColumn> fields)
    {
        return new JObject
        {
            ["pageInfo"] = new JObject { ["PageIndex"] = 1, ["PageSize"] = 20, ["TotalCount"] = 0 },
            ["sort"] = new JObject { ["field"] = "CreateTime", ["order"] = "desc" }
        };
    }

    private static JObject BuildDetailCfg(string pk, List<GenColumn> fields, int? projectId)
    {
        var children = new JArray();

        // 主键隐藏域（新增空→insert；编辑有值→update）
        var hidden = BaseNode("DynElInput", pk, new JObject { ["placeholder"] = "" }, "", false);
        hidden["options"]!["itemoptions"]!["style"] = new JObject { ["display"] = "none" };
        children.Add(hidden);

        foreach (var f in fields.Where(x => x.InDetail))
        {
            var node = FieldNode(f, false, projectId);
            if (f.NotNull && !f.HasDefault)
            {
                node["options"]!["labeloptions"]!["required"] = true;
                ((JArray)node["validators"]!).Add(new JObject { ["type"] = "required", ["message"] = f.Label + "不能为空" });
            }
            children.Add(node);
        }

        var root = BaseNode("DynGridContainer", "", new JObject { ["direction"] = "vertical", ["size"] = "small" });
        root["options"]!["labeloptions"] = new JObject { ["required"] = false, ["show"] = false, ["labelposition"] = "right", ["labelwidth"] = "120px" };
        root["options"]!["itemoptions"]!["class"] = "grid grid-cols-2 gap-x-3 gap-y-2 items-start";
        root["options"]!["comInnerInfo"] = new JObject { ["labelWidth"] = "110px" };
        root["childrenctrls"] = children;
        return root;
    }

    private static JObject BuildDetailDefaultJson(string pk, List<GenColumn> fields)
    {
        var o = new JObject();
        o[pk] = null;
        foreach (var f in fields.Where(x => x.InDetail))
        {
            o[f.Name] = f.Control switch
            {
                "number" => 0,
                "switch" => 1,
                _ => null
            };
        }
        return o;
    }

    // ---------------- 落库 ----------------

    private int? ResolveProjectId(string project)
    {
        if (string.IsNullOrWhiteSpace(project)) return null;
        var proj = int.TryParse(project, out var pid)
            ? _projects.GetById(pid)
            : _projects.Query(x => x.Code == project).First();
        return proj?.Id;
    }

    private int UpsertSetting(string settingCode, string settingName, string type,
        string renderMode, string? partialPath, string table, int? projectId,
        JObject cfg, JObject defaultJson)
    {
        var json = cfg.ToJson();
        var defJson = defaultJson.ToJson();
        var exist = _settings.Query(x => x.Code == settingCode).First();
        if (exist != null)
        {
            exist.Name = settingName;
            exist.SettingType = type;
            exist.RenderMode = renderMode;
            exist.PartialPath = partialPath;
            exist.ProjectId = projectId;
            exist.TableName = table;
            exist.ConfigJson = json;
            exist.DefaultJson = defJson;
            _settings.Update(exist);
            return exist.Id;
        }
        var row = new PageSetting
        {
            Code = settingCode,
            Name = settingName,
            SettingType = type,
            RenderMode = renderMode,
            PartialPath = partialPath,
            ProjectId = projectId,
            TableName = table,
            ConfigJson = json,
            DefaultJson = defJson,
            IsActive = true
        };
        _settings.Insert(row);
        return row.Id;
    }
}
