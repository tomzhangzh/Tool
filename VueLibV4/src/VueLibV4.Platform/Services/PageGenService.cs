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
/// 职责划分：字段推断在 PageFieldInference，三屏 JSON 组装在 PagePageBuilder，本类只做校验入参 → 调推断 → 调组装 → 事务落库 / 预览返回。
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

    /// <summary>
    /// 类模式：列出可选的 C# 类（仅 VueLibV4 自有程序集），供向导第一步「按类生成」下拉。
    /// </summary>
    List<GenClassOption> ListClasses(string? keyword = null);

    /// <summary>
    /// 类模式：读取 C# 类字段元数据并推断控件（与表模式共用 BuildColumns）。
    /// 标签优先取 [Display(Name)]/[Description]，为空的才英文拆词。
    /// </summary>
    List<GenFieldMeta> GetClassFieldMeta(string className);

    /// <summary>
    /// 只构造、不落库：复用与 Generate 完全相同的规则，返回三屏配置 JSON（筛选/列表/详情），
    /// 供向导【预览抽屉】用 dyn-dynamic-com 真实渲染（下拉数据源、字典、外键、中文 label 与最终生成一致）。
    /// </summary>
    (JObject FilterCfg, JObject ListCfg, JObject DetailCfg) Preview(JObject req);

    /// <summary>返回支持三屏生成的模板列表（SupportGen=1），供向导下拉选择。</summary>
    List<GenTemplateOption> ListGenTemplates();

    /// <summary>
    /// 自然语言生成三屏 PageSetting（不落库）。
    /// 入参 req：{ prompt: "用户描述", tableName?: "表名", project?: "项目" }
    /// 返回 { filter, list, detail, reasoning } ，供前端预览。
    /// </summary>
    Task<(bool Ok, string Msg, JObject? Filter, JObject? List, JObject? Detail, string? Reasoning)> GenerateByAiAsync(JObject req, CancellationToken ct = default);
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

/// <summary>三屏生成器可选模板项。</summary>
public class GenTemplateOption
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Icon { get; set; }
    public string? Description { get; set; }
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

/// <summary>类模式候选源（供向导第一步下拉选择）。</summary>
public class GenClassOption
{
    /// <summary>完整类型名（反射用，也是 className 入参）</summary>
    public string FullName { get; set; } = "";
    public string Name { get; set; } = "";
    public string Namespace { get; set; } = "";
    /// <summary>可用于页面的属性数（已排除导航属性/集合）</summary>
    public int PropCount { get; set; }
}

/// <summary>页面生成编排者（见 IPageGenService 顶部注释）：校验入参 → 调推断 → 调组装 → 落库 / 预览。</summary>
public class PageGenService : IPageGenService
{
    private readonly PageFieldInference _inference;
    private readonly PagePageBuilder _builder;
    private readonly IDynTemplateService _templates;
    private readonly IDynWebPageService _pages;
    private readonly IPageSettingService _settings;
    private readonly IDynPageExtService? _pageExt;
    private readonly IDynSchemaLabelService? _schemaLabels;
    private readonly IAiService? _ai;
    private readonly IComponentMetaService? _components;

    public PageGenService(
        PageFieldInference inference,
        PagePageBuilder builder,
        IDynTemplateService templates,
        IDynWebPageService pages,
        IPageSettingService settings,
        IDynPageExtService? pageExt = null,
        IDynSchemaLabelService? schemaLabels = null,
        IAiService? ai = null,
        IComponentMetaService? components = null)
    {
        _inference = inference;
        _builder = builder;
        _templates = templates;
        _pages = pages;
        _settings = settings;
        _pageExt = pageExt;
        _schemaLabels = schemaLabels;
        _ai = ai;
        _components = components;
    }

    /// <summary>执行生成（Controller 只做路由与 ApiResult 封装，业务规则全在此）。</summary>
    public async Task<(bool Ok, string Msg, PageGenResult? Data)> GenerateAsync(JObject req, CancellationToken ct = default)
    {
        var project = req["project"]?.ToString();
        var table = (req["table"]?.ToString() ?? "").Trim();
        var className = (req["className"]?.ToString() ?? "").Trim();
        // 输入源：className 非空即类模式（元数据来自 C# 类，表名可留空）
        var isClassMode = className.Length > 0;
        var code = (req["code"]?.ToString() ?? "").Trim();
        var name = (req["name"]?.ToString() ?? "").Trim();
        var pk = (req["pk"]?.ToString() ?? "Id").Trim();
        // 渲染方式：Front=全 DynCom；Back=列表走后端 Razor 局部视图（筛选/详情仍走 DynCom，最灵活）
        var renderMode = (req["renderMode"]?.ToString() ?? "Front").Trim();
        if (renderMode != "Back") renderMode = "Front";
        // 布局列数：筛选区每行 3/4 列（默认4）；详情区 1/2 列（默认2）
        int filterCols = req["filterCols"]?.Type == JTokenType.Integer ? (int)req["filterCols"]! : 4;
        int detailCols = req["detailCols"]?.Type == JTokenType.Integer ? (int)req["detailCols"]! : 2;
        if (string.IsNullOrWhiteSpace(table) && !isClassMode) return (false, "缺少表名（或改用 className 类模式）", null);
        if (string.IsNullOrWhiteSpace(table)) table = ClassModelReader.SimpleName(className);   // 类模式未给表名：用类名兜底
        if (string.IsNullOrWhiteSpace(code)) return (false, "缺少页面编码", null);
        if (string.IsNullOrWhiteSpace(name)) name = code;

        // 字段推断：读 schema → 智能分类（控件/操作符/进哪些屏）→ 显示名字典（表层→通用层→英文拆词）→ 前端勾选覆盖
        var fields = _inference.PrepareFields(table, project, req);
        if (fields == null || fields.Count == 0)
        {
            return (false, isClassMode
                ? $"类 {className} 不存在或不含可用于页面的属性"
                : $"表 {table} 不存在或无法读取 schema", null);
        }
        if (fields.Count == 0) return (false, "请至少选择一个字段", null);

        // 工程归属：外键/字典下拉走 /api/dyncommon/options?projectId 需要工程；未指定则回退平台工程
        int? projectId = _inference.ResolveProjectId(project);

        // 回写（默认开启，前端 rememberLabels 可关）：只写表字段层，回写最终选用字段的最终 label（含用户手改）。
        // 护栏在服务层：projectId<=0/表名为空一律跳过，通用层永不被写。
        var rememberLabels = req["rememberLabels"]?.Type != JTokenType.Boolean || (bool)req["rememberLabels"]!;
        if (rememberLabels && projectId != null && !isClassMode)
        {
            try
            {
                // 只回写"非英文默认拆词"的 label：避免把 NiceLabel 产出的英文垃圾冻进表层
                // （字典命中/用户手改的中文才值得沉淀；英文默认值下次还能重新推断）
                var toSave = fields
                    .Where(f => !string.IsNullOrWhiteSpace(f.Label) && f.Label != PageFieldInference.NiceLabel(f.Name))
                    .ToDictionary(f => f.Name, f => f.Label!);
                _schemaLabels?.UpsertTableLabels(projectId.Value, table, toSave);
            }
            catch (Exception ex) { System.Console.WriteLine("[PageGen] 字典回写跳过：" + ex.Message); }
        }

        // ---------- 生成三屏（ConfigJson=UI 渲染树 / DefaultJson=model 骨架） ----------
        var filterCfg = _builder.BuildFilterCfg(fields, projectId, table, filterCols);
        var filterDefault = _builder.BuildFilterDefaultJson(fields);
        var listCfg = _builder.BuildListCfg(fields);
        var listDefault = _builder.BuildListDefaultJson();
        var detailCfg = _builder.BuildDetailCfg(pk, fields, projectId, table, detailCols);
        var detailDefault = _builder.BuildDetailDefaultJson(pk, fields);

        // ---------- 落库三屏（幂等：同 code 更新） ----------
        var filterId = UpsertSetting(code + "_filter", name + " - 筛选区", "Filter", renderMode, null, table, projectId, filterCfg, filterDefault);
        var listId = UpsertSetting(code + "_list", name + " - 列表区", "List", renderMode,
            renderMode == "Back" ? "~/Views/DynTemplates/Partials/ListBack.cshtml" : null,
            table, projectId, listCfg, listDefault);
        var detailId = UpsertSetting(code + "_detail", name + " - 详情区", "Detail", "Front", null, table, projectId, detailCfg, detailDefault);

        // DynWebPage：主页绑定用户选择的三屏模板（SupportGen=1）。
        // 默认 filter-list-open-windowV2；detail 弹窗统一用 detail-modal-v2。
        var tplCode = (req["tplCode"]?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(tplCode)) tplCode = "filter-list-open-windowV2";
        var tpl = _templates.Query(t => t.Code == tplCode && t.SupportGen && t.IsActive).First();
        if (tpl == null) tpl = _templates.Query(t => t.Code == "filter-list-open-windowV2" && t.SupportGen).First();
        if (tpl == null) tpl = _templates.Query(t => t.Code == "filterlist-crud").First();
        var modalTpl = _templates.Query(t => t.Code == "detail-modal-v2").First()
                       ?? _templates.Query(t => t.Code == "detail-modal").First();

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
                    ["list"] = new JObject { ["settingId"] = listId, ["model"] = new JObject() },
                    ["detail"] = new JObject { ["settingId"] = detailId, ["model"] = new JObject() }
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

        modalPage.Url = "/Platform/Detail/Modal?id=" + modalPage.Id;
        _pages.Update(modalPage);
        page.Url = "/Platform/Shell/Runtime?id=" + page.Id;
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
            var spec = _builder.BuildShellSpec(table, pk, project, filterId, listId, detailId);

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
            shellPage.Url = "/Platform/Shell/Runtime?id=" + shellPage.Id;
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

    /// <summary>返回支持三屏生成的模板列表（SupportGen=1），按 SortNo 排序。</summary>
    public List<GenTemplateOption> ListGenTemplates()
    {
        return _templates.Query(t => t.SupportGen && t.IsActive)
            .OrderBy(t => t.SortNo)
            .Select(t => new GenTemplateOption
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Icon = t.Icon,
                Description = t.Description
            }).ToList();
    }

    /// <summary>
    /// 读取表字段元数据，并按与 Generate 相同的 BuildColumns 规则推断推荐控件。
    /// 供页面生成向导【第二步：配置字段】预填充控件下拉框；用户可手动覆盖后回传 Generate。
    /// </summary>
    public List<GenFieldMeta> GetTableFieldMeta(string table, string? project = null)
    {
        var schema = _inference.ReadTableSchema(table);
        if (schema.Count == 0) return new List<GenFieldMeta>();
        // 字典映射此处不涉及（向导第二步用户还没配置字典），先用空字典走智能分类
        var columns = _inference.BuildColumns(schema, new Dictionary<string, string>());
        // 显示名字典（表层→通用层→英文）：让向导第二步直接预填中文名
        _inference.ApplySchemaLabels(columns, table, project);
        return columns.Select(c => new GenFieldMeta
        {
            FieldName = c.Name,
            Label = c.Label,
            DbType = c.DataType,
            IsNullable = !c.NotNull,
            IsPrimaryKey = c.IsPk,
            IsForeignKey = c.FkTable != null,
            Control = c.Control,
            SuggestComponent = PageFieldInference.ControlToComponent(c.Control),
            InFilter = c.InFilter,
            InList = c.InList,
            InDetail = c.InDetail
        }).ToList();
    }

    // ======================================================================
    //  类模式输入源（C# 类 / DTO / record → 与表模式同构的字段元数据）
    // ======================================================================

    /// <summary>列出可选的 C# 类（仅 VueLibV4 自有程序集，排除导航属性后无可用属性的类）。</summary>
    public List<GenClassOption> ListClasses(string? keyword = null)
    {
        return ClassModelReader.ListClasses(keyword)
            .Select(o => new GenClassOption
            {
                FullName = o.FullName,
                Name = o.Name,
                Namespace = o.Namespace,
                PropCount = o.PropCount
            }).ToList();
    }

    /// <summary>读取类字段元数据并推断控件；标签优先 [Display(Name)]/[Description]。</summary>
    public List<GenFieldMeta> GetClassFieldMeta(string className)
    {
        var schema = ClassModelReader.ReadSchema(className);
        if (schema.Count == 0) return new List<GenFieldMeta>();
        // 与表模式共用智能分类；类模式没有字典映射与表显示名字典
        var columns = _inference.BuildColumns(schema, new Dictionary<string, string>());
        return columns.Select(c => new GenFieldMeta
        {
            FieldName = c.Name,
            Label = c.Label,
            DbType = c.DataType,
            IsNullable = !c.NotNull,
            IsPrimaryKey = c.IsPk,
            IsForeignKey = c.FkTable != null,
            Control = c.Control,
            SuggestComponent = PageFieldInference.ControlToComponent(c.Control),
            InFilter = c.InFilter,
            InList = c.InList,
            InDetail = c.InDetail
        }).ToList();
    }

    /// <summary>
    /// 预览构造（不落库、不回写 label）：与 Generate 走同一套推断与组装，保证预览即最终生成的样子。
    /// </summary>
    public (JObject FilterCfg, JObject ListCfg, JObject DetailCfg) Preview(JObject req)
    {
        var project = req["project"]?.ToString();
        var table = (req["table"]?.ToString() ?? "").Trim();
        var className = (req["className"]?.ToString() ?? "").Trim();
        var pk = (req["pk"]?.ToString() ?? "Id").Trim();

        var empty = new JObject();
        if (string.IsNullOrWhiteSpace(table) && className.Length == 0) return (empty, empty, empty);
        if (string.IsNullOrWhiteSpace(table)) table = ClassModelReader.SimpleName(className);   // 类模式兜底表名
        var fields = _inference.PrepareFields(table, project, req);
        if (fields == null || fields.Count == 0) return (empty, empty, empty);

        int? projectId = _inference.ResolveProjectId(project);

        int filterCols = req["filterCols"]?.Type == JTokenType.Integer ? (int)req["filterCols"]! : 4;
        int detailCols = req["detailCols"]?.Type == JTokenType.Integer ? (int)req["detailCols"]! : 2;

        var filterCfg = _builder.BuildFilterCfg(fields, projectId, table, filterCols);
        var listCfg = _builder.BuildListCfg(fields);
        var detailCfg = _builder.BuildDetailCfg(pk, fields, projectId, table, detailCols);
        return (filterCfg, listCfg, detailCfg);
    }

    // ======================================================================
    //  落库
    // ======================================================================

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

    // ======================================================================
    //  AI 生成
    // ======================================================================

    public async Task<(bool Ok, string Msg, JObject? Filter, JObject? List, JObject? Detail, string? Reasoning)> GenerateByAiAsync(JObject req, CancellationToken ct = default)
    {
        if (_ai == null) return (false, "AI 服务未注入", null, null, null, null);

        var prompt = req["prompt"]?.ToString()?.Trim();
        if (string.IsNullOrEmpty(prompt)) return (false, "缺少 prompt 参数", null, null, null, null);

        var tableName = req["tableName"]?.ToString();
        var project = req["project"]?.ToString();

        // 1. 构建组件 schema（已启用组件清单 + 默认配置）
        var compSchema = BuildComponentSchema();

        // 2. 系统提示词
        var systemPrompt = _builder.BuildAiSystemPrompt(compSchema);

        // 3. 用户提示词
        var userPrompt = _builder.BuildAiUserPrompt(prompt, tableName, project);

        try
        {
            var (json, reasoning) = await _ai.ChatJsonAsync(systemPrompt, userPrompt, ct);

            // 4. 提取三屏配置
            var filter = json["filter"] as JObject;
            var list = json["list"] as JObject;
            var detail = json["detail"] as JObject;

            // 5. 校验：组件名必须存在
            var validNames = new HashSet<string>(compSchema.Select(c => c.Name));
            _builder.ValidateConfigTree(filter, validNames);
            _builder.ValidateConfigTree(list, validNames);
            _builder.ValidateConfigTree(detail, validNames);

            return (true, "生成成功", filter, list, detail, reasoning);
        }
        catch (Exception ex)
        {
            return (false, $"AI 生成失败: {ex.Message}", null, null, null, null);
        }
    }

    /// <summary>构建组件 schema：取所有已启用组件的名称/标签/分类/默认配置</summary>
    private List<CompSchemaItem> BuildComponentSchema()
    {
        var list = new List<CompSchemaItem>();
        if (_components == null) return list;
        var rows = _components.Query(c => c.IsActive).OrderBy(c => c.Category).ToList();
        foreach (var r in rows)
        {
            list.Add(new CompSchemaItem
            {
                Name = r.ComponentName,
                Label = r.Label,
                Category = r.Category,
                DefaultConfig = r.DefaultConfigJson
            });
        }
        return list;
    }
}
