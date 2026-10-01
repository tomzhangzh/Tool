using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using VueLibV4.Platform.Models;
using VueLibV4.Services.Dependency;
using VueLibV4.Services.Extensions;

namespace VueLibV4.Platform.Services;

/// <summary>
/// 页面生成器 Service（M4 v2）：把「选表 → 读 SQLite schema → 按规则生成三屏组件树」的业务规则从 Controller 下沉到此。
///
/// ========== 三屏生成规则总览 ==========
/// 一条记录对应「筛选区(Filter) / 列表区(List) / 详情区(Detail)」三份 PageSetting + 1 条 DynWebPage 绑定三屏。
///   1. Filter（筛选区）：
///      - 只生成条件输入控件，【不】内置「查询 / 重置」按钮（按钮统一放外层 List 页面，职责分离）。
///      - DefaultJson = 筛选默认条件模板：{ 列: { op: 操作符, value: 默认值 } }。
///      - 主键 / 时间列 / 长文本(JSON/text) 不进筛选区。
///   2. List（列表区）：
///      - ConfigJson.columns 为列定义（Back 渲染模式时列表走后端 Razor 局部视图，仅列定义仍由前端读取）。
///      - DefaultJson = { pageInfo, sort } 分页与默认排序。
///   3. Detail（详情区）：
///      - 主键 → 隐藏域（新增空→insert / 编辑有值→update）。
///      - 选中字段 >10 个：外层为「div 两列网格」布局；≤10 个：外层 DynGridContainer（同为 div+grid 两列）。
///      - label 统一右对齐（labelposition=right）。
///      - 长文本(textarea)/JSON(json) 控件默认占两列（col-span-2，内容较宽）。
///      - 数据库 NOT NULL 且无默认值 → 自动追加 required 校验。
///      - 审计字段（CreateTime/UpdateTime）不进详情区。
///
/// ========== 字段控件映射规则（优先级从高到低） ==========
///   1. 字段名含 Icon        → DynElEmojiPicker（Emoji 图标选择）
///   2. 字段名以 Json 结尾    → DynJsonDesigner（JSON 配置设计器）
///   3. 布尔（Is/Has 开头）  → DynElSwitch（开关）
///   4. 长文本（Description/Remark/Content/Text结尾/Ext）→ DynElTextarea（多行文本）
///   5. 数字类型            → DynElInputNumber
///   6. 用户指定字典/外键    → DynElSelect（下拉，数据源=字典或关联表）
///   7. 其它文本            → DynElInput
///
/// 依赖注入：接口继承 IScopeDependency，启动扫描自动注册为 Scoped，无需手工 AddScoped。
/// </summary>
public interface IPageGenService : IScopeDependency
{
    /// <summary>
    /// 执行页面生成；返回 (是否成功, 提示信息, 生成结果)。失败时 Data 为 null。
    /// 生成产物：三屏 PageSetting + 筛选列表主页（filterlist-crud 积木模板）+
    /// 编辑弹窗实例（detail-modal 模板）+ 页面扩展视图骨架（真实 cshtml，回填 ExtViewPath）。
    /// </summary>
    Task<(bool Ok, string Msg, PageGenResult? Data)> GenerateAsync(JObject req, CancellationToken ct = default);

    /// <summary>
    /// 读取表字段元数据 + 按同一套规则推断「推荐控件」，供页面生成向导【第二步：配置字段】预填充控件下拉。
    /// 与 Generate 共用 BuildColumns 推断逻辑，保证「第二步预览」与「最终生成」控件推断一致。
    /// </summary>
    List<GenFieldMeta> GetTableFieldMeta(string table, string? project = null);
}

/// <summary>页面生成成功后的返回数据（Controller 转为 ApiResult.Ok 返回前端）</summary>
public class PageGenResult
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string RenderMode { get; set; } = "Front";
    public string Url { get; set; } = "";
    public int? FilterId { get; set; }
    public int? ListId { get; set; }
    public int? DetailId { get; set; }
    /// <summary>编辑弹窗页面实例 Id（detail-modal 模板，主页 ModalPageId 引用）</summary>
    public int? ModalPageId { get; set; }
    /// <summary>布局壳页面实例 Id（请求 layout="list-master-detail" 时额外生成，复用同批三屏设置）</summary>
    public int? ShellPageId { get; set; }
    /// <summary>布局壳页面 Url（ShellPageId 有值时）</summary>
    public string? ShellUrl { get; set; }
    /// <summary>页面扩展视图路径（真实 cshtml 骨架；生成失败或已手工指定时可能为空）</summary>
    public string? ExtViewPath { get; set; }
}

/// <summary>
/// 向导【第二步：配置字段】所需的字段元信息 + 推荐控件。
/// Control 与 SuggestComponent 同源：SuggestComponent 是展示用的组件名（DynElInput 等），
/// Control 是用于最终组装的控件枚举（input/textarea/json/number/switch/select/emoji），
/// 前端修改控件下拉后把 Control 传回 Generate，二者通过同一套映射对应。
/// </summary>
public class GenFieldMeta
{
    public string FieldName { get; set; } = "";
    public string Label { get; set; } = "";
    public string DbType { get; set; } = "TEXT";
    public bool IsNullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    /// <summary>控件枚举（input/textarea/json/number/switch/select/emoji/date）</summary>
    public string Control { get; set; } = "input";
    /// <summary>推荐组件名（DynElInput / DynElEmojiPicker / DynJsonDesigner 等）</summary>
    public string SuggestComponent { get; set; } = "DynElInput";
    /// <summary>是否默认进筛选区（与 Generate 的分类规则一致，避免第二步勾选与最终生成不一致）</summary>
    public bool InFilter { get; set; }
    /// <summary>是否默认进列表区</summary>
    public bool InList { get; set; } = true;
    /// <summary>是否默认进详情区</summary>
    public bool InDetail { get; set; } = true;
}

/// <summary>页面生成规则实现（见 IPageGenService 顶部注释）。</summary>
public class PageGenService : IPageGenService
{
    private readonly IDynProjectService _projects;
    private readonly IDynTemplateService _templates;
    private readonly IDynWebPageService _pages;
    private readonly IPageSettingService _settings;
    private readonly IConfiguration _config;
    private readonly IDynPageExtService? _pageExt;
    private readonly IDynSchemaLabelService? _schemaLabels;

    public PageGenService(
        IDynProjectService projects,
        IDynTemplateService templates,
        IDynWebPageService pages,
        IPageSettingService settings,
        IConfiguration config,
        IDynPageExtService? pageExt = null,
        IDynSchemaLabelService? schemaLabels = null)
    {
        _projects = projects;
        _templates = templates;
        _pages = pages;
        _settings = settings;
        _config = config;
        _pageExt = pageExt;
        _schemaLabels = schemaLabels;
    }

    /// <summary>
    /// 把显示名字典应用到推断列：表字段层 → 通用列名层（服务内部回退）→ 保留 NiceLabel 英文拆词。
    /// project 为坐标字符串/Id（"__platform__" / "Platform" / "2" / 业务项目 code）。
    /// </summary>
    private void ApplySchemaLabels(List<GenColumn> columns, string table, string? project)
    {
        if (_schemaLabels == null || columns.Count == 0) return;
        var pid = ResolveProjectId(project ?? "");
        if (pid == null) // 与生成主流程一致：未指定/特殊坐标时回退 Platform 工程，再退首个工程
        {
            try { pid = _projects.Query(x => x.Code == "Platform").First()?.Id; } catch { }
            if (pid == null) { try { pid = _projects.Query(x => true).First()?.Id; } catch { } }
        }
        if (pid == null) return; // 解析不到项目（无任何工程）时退回英文，不猜
        var labels = _schemaLabels.ResolveLabels(pid.Value, table, columns.Select(c => c.Name));
        if (labels.Count == 0) return;
        foreach (var c in columns)
            if (labels.TryGetValue(c.Name, out var lb) && !string.IsNullOrWhiteSpace(lb)) c.Label = lb;
    }

    /// <summary>执行生成（Controller 只做路由与 ApiResult 封装，业务规则全在此）。</summary>
    public async Task<(bool Ok, string Msg, PageGenResult? Data)> GenerateAsync(JObject req, CancellationToken ct = default)
    {
        var project = req["project"]?.ToString();
        var table = (req["table"]?.ToString() ?? "").Trim();
        var code = (req["code"]?.ToString() ?? "").Trim();
        var name = (req["name"]?.ToString() ?? "").Trim();
        var pk = (req["pk"]?.ToString() ?? "Id").Trim();
        // 渲染方式：Front=全 DynCom；Back=列表走后端 Razor 局部视图（筛选/详情仍走 DynCom，最灵活）
        var renderMode = (req["renderMode"]?.ToString() ?? "Front").Trim();
        if (renderMode != "Back") renderMode = "Front";
        if (string.IsNullOrWhiteSpace(table)) return (false, "缺少表名", null);
        if (string.IsNullOrWhiteSpace(code)) return (false, "缺少页面编码", null);
        if (string.IsNullOrWhiteSpace(name)) name = code;

        // 用户指定的字典映射：{ 列名: dictType } → 该列生成下拉(数据源=字典)
        var dictTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (req["dictTypes"] is JObject dts)
            foreach (var kv in dts) dictTypes[kv.Key.Trim()] = kv.Value?.ToString() ?? "";

        var schema = ReadTableSchema(table);
        if (schema.Count == 0) return (false, $"表 {table} 不存在或无法读取 schema", null);

        // 按 schema 智能分类（控件/操作符/进哪些屏）
        var columns = BuildColumns(schema, dictTypes);

        // 显示名字典：表字段层 → 通用列名层 → NiceLabel 英文。必须在"用户覆盖"之前应用，
        // 下方 fieldOverrides 会用向导里用户确认/手改过的 label 覆盖这里。
        ApplySchemaLabels(columns, table, project);

        // 字段选择：req["fields"] 为对象数组（{ name, label, control, op, inFilter, inList, inDetail, ... }）。
        // 只取 name 参与筛选；并用前端显式选择（控件/标签/操作符/屏归属）覆盖智能分类，尊重用户勾选。
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
        if (fields.Count == 0) return (false, "请至少选择一个字段", null);
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

        // 工程归属：外键/字典下拉走 /api/dyncommon/options?projectId 需要工程；未指定则回退平台工程
        int? projectId = ResolveProjectId(project);
        if (projectId == null)
        {
            try { projectId = _projects.Query(x => x.Code == "Platform").First()?.Id; } catch { }
            if (projectId == null) { try { projectId = _projects.Query(x => true).First()?.Id; } catch { } }
        }

        // 回写（默认开启，前端 rememberLabels 可关）：只写表字段层，回写最终选用字段的最终 label（含用户手改）。
        // 护栏在服务层：projectId<=0/表名为空一律跳过，通用层永不被写。
        var rememberLabels = req["rememberLabels"]?.Type != JTokenType.Boolean || (bool)req["rememberLabels"]!;
        if (rememberLabels && projectId != null)
        {
            try
            {
                // 只回写"非英文默认拆词"的 label：避免把 NiceLabel 产出的英文垃圾冻进表层
                // （字典命中/用户手改的中文才值得沉淀；英文默认值下次还能重新推断）
                var toSave = fields
                    .Where(f => !string.IsNullOrWhiteSpace(f.Label) && f.Label != NiceLabel(f.Name))
                    .ToDictionary(f => f.Name, f => f.Label!);
                _schemaLabels?.UpsertTableLabels(projectId.Value, table, toSave);
            }
            catch (Exception ex) { System.Console.WriteLine("[PageGen] 字典回写跳过：" + ex.Message); }
        }

        // ---------- 生成三屏（ConfigJson=UI 渲染树 / DefaultJson=model 骨架） ----------
        var filterCfg = BuildFilterCfg(fields, projectId);
        var filterDefault = BuildFilterDefaultJson(fields);
        var listCfg = BuildListCfg(fields);
        var listDefault = BuildListDefaultJson();
        var detailCfg = BuildDetailCfg(pk, fields, projectId);
        var detailDefault = BuildDetailDefaultJson(pk, fields);

        // ---------- 落库三屏（幂等：同 code 更新） ----------
        var filterId = UpsertSetting(code + "_filter", name + " - 筛选区", "Filter", renderMode, null, table, projectId, filterCfg, filterDefault);
        var listId = UpsertSetting(code + "_list", name + " - 列表区", "List", renderMode,
            renderMode == "Back" ? "~/Views/DynTemplates/Partials/ListBack.cshtml" : null,
            table, projectId, listCfg, listDefault);
        var detailId = UpsertSetting(code + "_detail", name + " - 详情区", "Detail", "Front", null, table, projectId, detailCfg, detailDefault);

        // DynWebPage：主页绑定 filterlist-crud 积木模板（筛选/列表 Block + detail-modal 弹窗实例）。
        // ParamsJson = 模板顶层参数 + blocks 槽位（各槽位 URL 留空，由 Block/模板自动补 dyndata 端点）。
        var tpl = _templates.Query(t => t.Code == "filterlist-crud").First()
                  ?? _templates.Query(t => t.Code == "crud-basic").First();
        var modalTpl = _templates.Query(t => t.Code == "detail-modal").First();

        // 编辑弹窗实例（幂等：{code}-modal）：DetailModal.cshtml 读顶层 TableName/KeyField + blocks.detail
        var existModal = _pages.Query(p => p.Code == code + "-modal").First();
        var modalPage = existModal ?? new DynWebPage { Code = code + "-modal", CreateTime = DateTime.Now };
        modalPage.Name = name + " - 编辑弹窗";
        modalPage.ProjectId = projectId;
        modalPage.TemplateId = modalTpl?.Id;
        modalPage.ParamsJson = new JObject
        {
            ["TableName"] = table,
            ["KeyField"] = pk,
            ["blocks"] = new JObject
            {
                ["detail"] = new JObject
                {
                    ["settingId"] = detailId,
                    // addUrl/editUrl/deleteUrl 留空 → DetailApp/模板走业务 dyndata 缺省端点
                    ["model"] = new JObject()
                }
            }
        }.ToJson();
        modalPage.IsActive = true;
        if (existModal == null) _pages.Insert(modalPage); else _pages.Update(modalPage);

        var existPage = _pages.Query(p => p.Code == code).First();
        var page = new DynWebPage
        {
            Code = code,
            Name = name,
            ProjectId = projectId,
            TemplateId = tpl?.Id,
            PageJson = null,
            ParamsJson = new JObject
            {
                ["TableName"] = table,
                ["KeyField"] = pk,
                ["ChildFkField"] = "",
                ["ModalPageId"] = modalPage.Id,
                ["blocks"] = new JObject
                {
                    ["filter"] = new JObject { ["settingId"] = filterId, ["model"] = new JObject() },
                    ["list"] = new JObject { ["settingId"] = listId, ["model"] = new JObject() }
                }
            }.ToJson(),
            IsActive = true
        };
        if (existPage != null)
        {
            page.Id = existPage.Id;
            page.CreateTime = existPage.CreateTime;
            // 重新生成不覆盖用户已手工指定的扩展视图；未配置过则生成骨架（真实 cshtml，文件已存在不重写）
            page.ExtViewPath = existPage.ExtViewPath;
            _pages.Update(page);
        }
        else
        {
            _pages.Insert(page); // 自增 Id 回填
        }

        // 页面扩展视图骨架（首次生成）：失败不阻断主流程，仅在提示中说明
        string? extViewPath = page.ExtViewPath;
        string? extWarn = null;
        if (string.IsNullOrWhiteSpace(extViewPath) && _pageExt != null)
        {
            try
            {
                extViewPath = await _pageExt.EnsureSkeletonAsync(code, name, ct);
                page.ExtViewPath = extViewPath;
                _pages.Update(page);
            }
            catch (Exception ex)
            {
                extWarn = "；扩展视图骨架生成失败：" + ex.Message;
            }
        }

        modalPage.Url = "/Platform/Page/DetailModal?id=" + modalPage.Id;
        _pages.Update(modalPage);
        page.Url = "/Platform/Page/DynWebPage?id=" + page.Id;
        _pages.Update(page);

        // ---------- 可选：额外产出一个布局壳页面（list-master-detail），复用同批三屏设置 ----------
        int? shellPageId = null;
        string? shellUrl = null;
        var layout = (req["layout"]?.ToString() ?? "").Trim();
        if (string.Equals(layout, "list-master-detail", StringComparison.OrdinalIgnoreCase)
            || string.Equals(layout, "shell", StringComparison.OrdinalIgnoreCase))
        {
            var shellTpl = _templates.Query(t => t.Code == "list-master-detail").First();
            if (shellTpl == null) return (false, "布局壳模板 list-master-detail 不存在（平台库未初始化？）", null);

            // provide.project：显式请求坐标优先；为空则留空，壳视图回退页面 ProjectId
            var spec = new JObject
            {
                ["layout"] = "list-master-detail",
                ["layoutProps"] = new JObject { ["leftWidth"] = 50, ["childFkField"] = "" },
                ["provide"] = new JObject
                {
                    ["table"] = table,
                    ["keyField"] = pk,
                    ["project"] = project ?? ""
                },
                ["slots"] = new JObject
                {
                    ["filter"] = new JObject { ["block"] = "filter", ["settingId"] = filterId },
                    ["master"] = new JObject { ["block"] = "list", ["settingId"] = listId },
                    ["detail"] = new JObject { ["block"] = "detail", ["settingId"] = detailId }
                }
            };

            var shellCode = code + "-shell";
            var existShell = _pages.Query(p => p.Code == shellCode).First();
            var shellPage = existShell ?? new DynWebPage { Code = shellCode, CreateTime = DateTime.Now };
            shellPage.Name = name + "（布局壳）";
            shellPage.ProjectId = projectId;
            shellPage.TemplateId = shellTpl.Id;
            shellPage.PageJson = null;
            shellPage.ParamsJson = null; // 壳页面：规格只在 SpecJson，不与老参数混用
            shellPage.SpecJson = spec.ToJson();
            shellPage.IsActive = true;
            if (existShell == null) _pages.Insert(shellPage); else _pages.Update(shellPage);
            shellPage.Url = "/Platform/Page/DynWebPage?id=" + shellPage.Id;
            _pages.Update(shellPage);
            shellPageId = shellPage.Id;
            shellUrl = shellPage.Url;
        }

        var msg = "页面生成成功" + (shellPageId != null ? "（含布局壳页面）" : "") + extWarn;
        return (true, msg, new PageGenResult
        {
            Id = page.Id,
            Code = code,
            Name = name,
            RenderMode = renderMode,
            Url = page.Url,
            FilterId = filterId,
            ListId = listId,
            DetailId = detailId,
            ModalPageId = modalPage.Id,
            ShellPageId = shellPageId,
            ShellUrl = shellUrl,
            ExtViewPath = extViewPath
        });
    }

    // ======================================================================
    //  字段元数据 + 推荐控件（向导第二步）
    // ======================================================================

    /// <summary>
    /// 读取表字段元数据，并按与 Generate 相同的 BuildColumns 规则推断推荐控件。
    /// 供页面生成向导【第二步：配置字段】预填充控件下拉框；用户可手动覆盖后回传 Generate。
    /// </summary>
    public List<GenFieldMeta> GetTableFieldMeta(string table, string? project = null)
    {
        var schema = ReadTableSchema(table);
        if (schema.Count == 0) return new List<GenFieldMeta>();
        // 字典映射此处不涉及（向导第二步用户还没配置字典），先用空字典走智能分类
        var columns = BuildColumns(schema, new Dictionary<string, string>());
        // 显示名字典（表层→通用层→英文）：让向导第二步直接预填中文名
        ApplySchemaLabels(columns, table, project);
        return columns.Select(c => new GenFieldMeta
        {
            FieldName = c.Name,
            Label = c.Label,
            DbType = c.DataType,
            IsNullable = !c.NotNull,
            IsPrimaryKey = c.IsPk,
            IsForeignKey = c.FkTable != null,
            Control = c.Control,
            SuggestComponent = ControlToComponent(c.Control),
            InFilter = c.InFilter,
            InList = c.InList,
            InDetail = c.InDetail
        }).ToList();
    }

    // ======================================================================
    //  Schema 读取
    // ======================================================================

    /// <summary>表列元数据（从 CREATE TABLE 解析）</summary>
    private class ColumnMeta
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

    /// <summary>按表名读取 schema：先在业务库(BusinessDb)找，再回退平台库(PlatformDb)。</summary>
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

    /// <summary>参与生成的列（已按规则分类好的字段元数据）。</summary>
    private class GenColumn
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

    /// <summary>
    /// 按 schema + 用户字典映射，智能决定每个字段的控件、操作符与三屏归属。
    /// 控件映射优先级见 IPageGenService 顶部注释（icon → json → bool → textarea → number → select → input）。
    /// </summary>
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

    // ======================================================================
    //  配置树构建（DynCom 组件 JSON 树）
    // ======================================================================

    /// <summary>
    /// 组件节点基座：统一补全 options(comoptions/labeloptions/itemoptions/compassthrough) 与
    /// validators/childrenctrls/slots/extendinfo 空骨架，保证生成的 ConfigJson 与设计器/运行时同构。
    /// compassthrough：容器把 comoptions.size、labeloptions 布局三项向下透传（见 _ContainerLayout/_BaseLayout）。
    /// </summary>
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

    /// <summary>控件枚举 → 组件名（向导第二步与最终生成共用同一套映射，保证一致）。</summary>
    private static string ControlToComponent(string control) => control switch
    {
        "emoji" => "DynElEmojiPicker",
        "json" => "DynJsonDesigner",
        "textarea" => "DynElTextarea",
        "number" => "DynElInputNumber",
        "switch" => "DynElSwitch",
        "select" => "DynElSelect",
        _ => "DynElInput"
    };

    /// <summary>
    /// 字段控件节点。
    ///  - Filter 模式：modelname=列.value（绑定 {列:{op,value}} 结构）。
    ///  - Detail 模式：modelname=列（绑定 form 对象）。
    /// 控件 → 组件映射：emoji→DynElEmojiPicker / json→DynJsonDesigner / textarea→DynElTextarea /
    ///                  number→DynElInputNumber / switch→DynElSwitch / select→DynElSelect / 其他→DynElInput。
    /// </summary>
    private static JObject FieldNode(GenColumn f, bool filterMode, int? projectId)
    {
        var comp = ControlToComponent(f.Control);
        var modelName = filterMode ? f.Name + ".value" : f.Name;
        var co = new JObject();
        co["size"] = "small";
        switch (comp)
        {
            case "DynElEmojiPicker":
                co["placeholder"] = "搜索 Emoji（中/英文）"; co["clearable"] = true; break;
            case "DynJsonDesigner":
                co["rows"] = 8; co["placeholder"] = "{}"; co["title"] = f.Label + " 配置设计器"; break;
            case "DynElTextarea":
                co["rows"] = 3; co["placeholder"] = "请输入" + f.Label; break;
            case "DynElInputNumber":
                co["step"] = 1; break;
            case "DynElSelect":
                // 数据源：字典 / 外键表(projectId 用于拉取下拉) / 静态占位
                if (!string.IsNullOrWhiteSpace(f.DictType)) { co["sourceType"] = "dict"; co["dictType"] = f.DictType; }
                else if (!string.IsNullOrWhiteSpace(f.FkTable)) { co["sourceType"] = "table"; co["table"] = f.FkTable; co["valueField"] = "Id"; co["textField"] = "Name"; co["projectId"] = projectId; }
                else { co["sourceType"] = "static"; co["optionValuesText"] = ""; }
                co["placeholder"] = "请选择"; co["clearable"] = true; break;
            default:
                co["placeholder"] = "请输入" + f.Label; co["clearable"] = true; break;
        }
        return BaseNode(comp, modelName, co, f.Label, true);
    }

    /// <summary>
    /// 生成筛选区(Filter)组件树。
    /// 规则：只生成条件输入控件，不内置「查询/重置」按钮（按钮统一放外层 List 页面，职责分离）。
    /// </summary>
    private static JObject BuildFilterCfg(List<GenColumn> fields, int? projectId)
    {
        var children = new JArray();
        foreach (var f in fields.Where(x => x.InFilter)) children.Add(FieldNode(f, true, projectId));

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

    /// <summary>
    /// 筛选区默认条件（DefaultJson）：{ 列: { op, value:null } }，页面打开时作为初始查询条件。
    /// 查询时收集当前输入；重置时恢复为此默认值。
    /// </summary>
    private static JObject BuildFilterDefaultJson(List<GenColumn> fields)
    {
        var o = new JObject();
        foreach (var f in fields.Where(x => x.InFilter))
            o[f.Name] = new JObject { ["op"] = f.Op, ["value"] = null };
        return o;
    }

    /// <summary>
    /// 生成列表区(List)列定义（ConfigJson.columns）。
    /// 时间列由下方显式追加，避免重复；布尔列渲染为 tag；长文本列加宽。
    /// </summary>
    private static JObject BuildListCfg(List<GenColumn> fields)
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

    /// <summary>列表区默认分页与排序（DefaultJson）。</summary>
    private static JObject BuildListDefaultJson()
    {
        return new JObject
        {
            ["pageInfo"] = new JObject { ["PageIndex"] = 1, ["PageSize"] = 20, ["TotalCount"] = 0 },
            ["sort"] = new JObject { ["field"] = "CreateTime", ["order"] = "desc" }
        };
    }

    /// <summary>
    /// 生成详情区(Detail)组件树。
    /// 规则：
    ///  - 主键 → 隐藏域（新增空→insert / 编辑有值→update）。
    ///  - 选中字段 >10：外层为「div 两列网格」布局（DynGridContainer 底层渲染 div + grid）；≤10 同样两列。
    ///  - label 统一右对齐（labelposition=right）。
    ///  - 长文本(textarea)/JSON(json) 控件默认占两列（col-span-2，内容较宽）。
    ///  - 数据库 NOT NULL 且无默认值 → 自动追加 required 校验。
    ///  - 审计字段（CreateTime/UpdateTime）已在分类阶段排除，不进详情区。
    /// </summary>
    private static JObject BuildDetailCfg(string pk, List<GenColumn> fields, int? projectId)
    {
        var children = new JArray();
        var detailFields = fields.Where(x => x.InDetail).ToList();
        // 字段数量 >10 判定为「多字段」场景（两列布局 + 长内容跨列）
        var many = detailFields.Count > 10;

        // 主键隐藏域（新增空→insert；编辑有值→update）
        var hidden = BaseNode("DynElInput", pk, new JObject { ["placeholder"] = "" }, "", false);
        hidden["options"]!["itemoptions"]!["style"] = new JObject { ["display"] = "none" };
        children.Add(hidden);

        foreach (var f in detailFields)
        {
            var node = FieldNode(f, false, projectId);
            // 长文本 / JSON 内容较宽，默认占满两列
            if (f.Control is "textarea" or "json")
                node["options"]!["itemoptions"]!["class"] = "col-span-2";
            // 数据库 NOT NULL 且无默认值 → 必填校验
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

    /// <summary>详情区默认 model（DefaultJson）：数字默认 0、开关默认 1、其余 null。</summary>
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

    // ======================================================================
    //  落库
    // ======================================================================

    /// <summary>把前端项目标识解析为 DynProject.Id（支持传 Id 或 Code）。</summary>
    private int? ResolveProjectId(string project)
    {
        if (string.IsNullOrWhiteSpace(project)) return null;
        var proj = int.TryParse(project, out var pid)
            ? _projects.GetById(pid)
            : _projects.Query(x => x.Code == project).First();
        return proj?.Id;
    }

    /// <summary>
    /// 幂等落库一条 PageSetting（同 Code 则更新，否则插入）。
    /// 同步回填 ProjectId / TableName（页面归属项目 + 关联业务表，供按项目/按表反查）。
    /// ConfigJson=UI 渲染树；DefaultJson=model 骨架。
    /// </summary>
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
