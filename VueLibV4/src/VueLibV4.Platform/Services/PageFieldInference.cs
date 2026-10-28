using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using VueLibV4.Services.Dependency;

namespace VueLibV4.Platform.Services;

/// <summary>
/// 字段推断：把数据表列元数据（类型/外键/默认值/注释）推断成控件类型、操作符、三屏归属与中文标签。
/// 只读 schema、不写库：显示名字典经 <see cref="IDynSchemaLabelService"/> 只读解析（表字段层 → 通用列名层 → 英文拆词）。
/// 控件映射优先级见 <see cref="IPageGenService"/> 顶部注释（icon → json → bool → textarea → number → select → input）。
/// </summary>
public sealed class PageFieldInference : IScopeDependency
{
    private readonly IDynProjectService _projects;
    private readonly IConfiguration _config;
    private readonly IDynSchemaLabelService? _schemaLabels;

    public PageFieldInference(
        IDynProjectService projects,
        IConfiguration config,
        IDynSchemaLabelService? schemaLabels = null)
    {
        _projects = projects;
        _config = config;
        _schemaLabels = schemaLabels;
    }

    /// <summary>
    /// 读表 schema 并按同一套规则推断字段：解析字典映射 → 读列 → 分类 → 应用显示名 → 应用前端勾选覆盖。
    /// 读不到 schema 时返回 null，由调用方按各自契约给出提示或空配置。
    /// </summary>
    internal List<GenColumn>? PrepareFields(string table, string? project, JObject req)
    {
        // 用户指定的字典映射：{ 列名: dictType } → 该列生成下拉(数据源=字典)
        var dictTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (req["dictTypes"] is JObject dts)
            foreach (var kv in dts) dictTypes[kv.Key.Trim()] = kv.Value?.ToString() ?? "";

        var schema = req["className"]?.ToString() is { Length: > 0 } cn
            ? ClassModelReader.ReadSchema(cn)      // 类模式：反射 C# 类得到列元数据
            : ReadTableSchema(table);              // 表模式：读数据库 schema
        if (schema.Count == 0) return null;

        // 按 schema 智能分类（控件/操作符/进哪些屏）
        var columns = BuildColumns(schema, dictTypes);

        // 显示名字典：表字段层 → 通用列名层 → NiceLabel 英文。必须在"用户覆盖"之前应用。
        // 类模式的标签以 [Display(Name)]/[Description] 为准，不叠加表显示名字典。
        if (schema.Count > 0 && (req["className"]?.ToString() ?? "").Length == 0)
            ApplySchemaLabels(columns, table, project);
        return SelectFields(columns, req);
    }

    /// <summary>
    /// 字段选择：req["fields"] 为对象数组（{ name, label, control, op, inFilter, inList, inDetail, ... }）。
    /// 只取 name 参与筛选；并用前端显式选择（控件/标签/操作符/屏归属）覆盖智能分类，尊重用户勾选。
    /// </summary>
    internal List<GenColumn> SelectFields(List<GenColumn> columns, JObject req)
    {
        var fieldNames = new List<string>();
        var fieldOverrides = new Dictionary<string, JObject>(StringComparer.OrdinalIgnoreCase);
        if (req["fields"] is JArray fa)
        {
            foreach (var t in fa)
            {
                if (t is JObject jo)
                {
                    var n = (jo["name"]?.ToString() ?? "").Trim();
                    if (n.Length > 0) { fieldNames.Add(n); fieldOverrides[n] = jo; }
                }
                else
                {
                    var s = t?.ToString()?.Trim() ?? "";
                    if (s.Length > 0) fieldNames.Add(s); // 兼容传纯字段名数组
                }
            }
        }
        var fields = columns.Where(c => fieldNames.Count == 0 || fieldNames.Contains(c.Name)).ToList();
        // 前端覆盖智能分类（仅覆盖明确传入的值）
        if (fieldOverrides.Count > 0)
        {
            foreach (var f in fields)
            {
                if (!fieldOverrides.TryGetValue(f.Name, out var ov)) continue;
                var ctrl = ov["control"]?.ToString(); if (!string.IsNullOrWhiteSpace(ctrl)) f.Control = ctrl;
                var lbl = ov["label"]?.ToString();   if (!string.IsNullOrWhiteSpace(lbl)) f.Label = lbl;
                var op = ov["op"]?.ToString();       if (!string.IsNullOrWhiteSpace(op)) f.Op = op;
                if (ov["inFilter"]?.Type == JTokenType.Boolean) f.InFilter = (bool)ov["inFilter"]!;
                if (ov["inList"]?.Type == JTokenType.Boolean) f.InList = (bool)ov["inList"]!;
                if (ov["inDetail"]?.Type == JTokenType.Boolean) f.InDetail = (bool)ov["inDetail"]!;
            }
        }
        return fields;
    }

    /// <summary>
    /// 把显示名字典应用到推断列：表字段层 → 通用列名层（服务内部回退）→ 保留 NiceLabel 英文拆词。
    /// project 为坐标字符串/Id（"__platform__" / "Platform" / "2" / 业务项目 code）。
    /// </summary>
    internal void ApplySchemaLabels(List<GenColumn> columns, string table, string? project)
    {
        if (_schemaLabels == null || columns.Count == 0) return;
        var pid = ResolveProjectId(project);
        if (pid == null) return; // 解析不到项目（无任何工程）时退回英文，不猜
        var labels = _schemaLabels.ResolveLabels(pid.Value, table, columns.Select(c => c.Name));
        if (labels.Count == 0) return;
        foreach (var c in columns)
            if (labels.TryGetValue(c.Name, out var lb) && !string.IsNullOrWhiteSpace(lb)) c.Label = lb;
    }

    /// <summary>
    /// 把前端项目标识解析为 DynProject.Id（支持传 Id 或 Code）；未指定/特殊坐标时回退 Platform 工程，再退首个工程。
    /// 外键/字典下拉走 /api/dyncommon/options?projectId 需要工程归属。
    /// </summary>
    internal int? ResolveProjectId(string? project)
    {
        int? pid = null;
        if (!string.IsNullOrWhiteSpace(project))
        {
            var proj = int.TryParse(project, out var id)
                ? _projects.GetById(id)
                : _projects.Query(x => x.Code == project).First();
            pid = proj?.Id;
        }
        if (pid == null) { try { pid = _projects.Query(x => x.Code == "Platform").First()?.Id; } catch { } }
        if (pid == null) { try { pid = _projects.Query(x => true).First()?.Id; } catch { } }
        return pid;
    }

    // ======================================================================
    //  Schema 读取
    // ======================================================================

    /// <summary>按表名读取 schema：先在业务库(BusinessDb)找，再回退平台库(PlatformDb)。</summary>
    internal List<ColumnMeta> ReadTableSchema(string table)
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

    /// <summary>在单个 SQLite 库中读取表列信息 + DEFAULT + 外键推断。</summary>
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

    /// <summary>从 CREATE SQL 中解析每列的 DEFAULT 表达式（用于「有默认值则不强校验必填」）。</summary>
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

    // ======================================================================
    //  列分类（控件 / 操作符 / 进哪些屏）
    // ======================================================================

    /// <summary>
    /// 按 schema + 用户字典映射，智能决定每个字段的控件、操作符与三屏归属。
    /// 控件映射优先级见 IPageGenService 顶部注释（icon → json → bool → textarea → number → select → input）。
    /// </summary>
    internal List<GenColumn> BuildColumns(List<ColumnMeta> schema, Dictionary<string, string> dictTypes)
    {
        var list = new List<GenColumn>();
        foreach (var c in schema)
        {
            var name = c.Name;
            // 类模式：优先用 [Display(Name)]/[Description] 注解标签；表模式 Label 为空，仍走英文拆词
            var label = string.IsNullOrWhiteSpace(c.Label) ? NiceLabel(name) : c.Label;
            var isPk = c.IsPrimaryKey || string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase);
            var type = c.Type.ToUpperInvariant();
            var isBool = type.Contains("INT") && (name.StartsWith("Is", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Has", StringComparison.OrdinalIgnoreCase));
            // JSON 字段：字段名以 Json 结尾（ConfigJson/PageJson/ExtJson 等）→ 使用 JSON 设计器组件
            var isJson = name.EndsWith("Json", StringComparison.OrdinalIgnoreCase);
            // 长文本：description / remark / content / text 结尾 / ext
            var isLongText = !isJson && (name.Contains("Description", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Remark", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Content", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("Text", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Ext", StringComparison.OrdinalIgnoreCase));
            var isTime = name.Contains("Time", StringComparison.OrdinalIgnoreCase) || type.Contains("DATE");

            string control = "input";
            if (name.Contains("Icon", StringComparison.OrdinalIgnoreCase) && !isPk) control = "emoji";
            else if (isJson) control = "json";
            else if (isBool) control = "switch";
            else if (isLongText) control = "textarea";
            else if (!type.Contains("INT") && !type.Contains("NUM") && !type.Contains("REAL") && !type.Contains("DEC")) control = "input";
            else control = "number";

            // 用户指定字典 / 外键关系 → 覆盖为下拉(select)
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
            // 进哪些屏：筛选区只收简单可比较控件；列表区不收长文本/JSON；详情区排除主键与审计时间
            gc.InFilter = !isPk && !isTime && (control is "input" or "select" or "switch" || isBool);
            gc.InList = !isPk && !isLongText && control != "json" && name != "ExtJson";
            gc.InDetail = !isPk && name != "CreateTime" && name != "UpdateTime";
            gc.CellTag = isBool;
            list.Add(gc);
        }
        return list;
    }

    /// <summary>列名 → 显示标签（大写下划线拆分：OrderNo → Order No）。</summary>
    internal static string NiceLabel(string name)
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

    /// <summary>控件枚举 → 组件名（向导第二步预填与最终生成共用同一套映射，保证一致）。</summary>
    internal static string ControlToComponent(string control) => control switch
    {
        "emoji" => "DynElEmojiPicker",
        "json" => "DynJsonDesigner",
        "textarea" => "DynElTextarea",
        "number" => "DynElInputNumber",
        "switch" => "DynElSwitch",
        "select" => "DynElSelect",
        _ => "DynElInput"
    };

}

/// <summary>表列元数据（从 CREATE TABLE 解析）</summary>
internal sealed class ColumnMeta
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "TEXT";
    public bool NotNull { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool HasDefault { get; set; }
    /// <summary>外键目标表（列名 XxxId 且非主键时推断）；无则空</summary>
    public string? FkTable { get; set; }
    public string? DefaultValue { get; set; }
    /// <summary>显式标签（类模式来自 [Display(Name)]/[Description]）；为空则由 Name 拆词生成</summary>
    public string Label { get; set; } = "";
}

/// <summary>参与生成的列（已按规则分类好的字段元数据）。</summary>
internal sealed class GenColumn
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string DataType { get; set; } = "TEXT";
    public bool NotNull { get; set; }
    public bool HasDefault { get; set; }
    public bool IsPk { get; set; }
    /// <summary>控件类型：input/textarea/json/number/switch/select/emoji</summary>
    public string Control { get; set; } = "input";
    public string Op { get; set; } = "like";   // like/eq/gt/ge/lt/le
    public bool InFilter { get; set; }
    public bool InList { get; set; }
    public bool InDetail { get; set; }
    public bool CellTag { get; set; }
    public string? FkTable { get; set; }
    public string? DictType { get; set; }
}
