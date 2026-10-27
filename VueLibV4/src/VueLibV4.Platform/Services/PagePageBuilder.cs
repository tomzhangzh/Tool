using System.Text;
using Newtonsoft.Json.Linq;
using VueLibV4.Services.Dependency;

namespace VueLibV4.Platform.Services;

/// <summary>
/// 三屏 JSON 组装：把已推断好的字段元数据组装成 Filter / List / Detail 三份 ConfigJson 与 DefaultJson，
/// 以及布局壳页面的 SpecJson。纯 JSON 组装，不读写数据库。
/// 节点结构（options/validators/childrenctrls/slots/extendinfo）与设计器、运行时保持同构。
/// </summary>
public sealed class PagePageBuilder : IScopeDependency
{
    /// <summary>
    /// 组件节点基座：统一补全 options(comoptions/labeloptions/itemoptions/compassthrough) 与
    /// validators/childrenctrls/slots/extendinfo 空骨架，保证生成的 ConfigJson 与设计器/运行时同构。
    /// compassthrough：容器把 comoptions.size、labeloptions 布局三项向下透传（见 _ContainerLayout/_BaseLayout）。
    /// </summary>
    private static JObject BaseNode(string component, string modelName, JObject comoptions, string label = null, bool showLabel = false)
    {
        return new JObject
        {
            ["component"] = component,
            ["modelname"] = modelName,
            ["options"] = new JObject
            {
                ["comoptions"] = comoptions,
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

    /// <summary>
    /// 字段控件节点。
    ///  - Filter 模式：modelname=列.value（绑定 {列:{op,value}} 结构）。
    ///  - Detail 模式：modelname=列（绑定 form 对象）。
    /// 控件 → 组件映射：emoji→DynElEmojiPicker / json→DynJsonDesigner / textarea→DynElTextarea /
    ///                  number→DynElInputNumber / switch→DynElSwitch / select→DynElSelect / 其他→DynElInput。
    /// </summary>
    private static JObject FieldNode(GenColumn f, bool filterMode, int? projectId, string table)
    {
        var comp = PageFieldInference.ControlToComponent(f.Control);
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
            case "DynElSwitch":
                // 库里布尔/标志位多为 int(0/1)，el-switch 默认认 true/false，必须显式绑定值
                co["activeValue"] = 1; co["inactiveValue"] = 0; break;
            case "DynElSelect":
                // 数据源优先级：字典 > 外键表 > 当前表 DISTINCT（SQL 自查询，可手改）
                if (!string.IsNullOrWhiteSpace(f.DictType)) { co["sourceType"] = "dict"; co["dictType"] = f.DictType; }
                else if (!string.IsNullOrWhiteSpace(f.FkTable)) { co["sourceType"] = "table"; co["table"] = f.FkTable; co["valueField"] = "Id"; co["textField"] = "Name"; co["projectId"] = projectId; }
                else { co["sourceType"] = "sql"; co["sql"] = $"SELECT DISTINCT [{f.Name}] AS [Value], [{f.Name}] AS [Text] FROM [{table}] WHERE [{f.Name}] IS NOT NULL AND [{f.Name}] <> ''"; co["projectId"] = projectId; co["filterable"] = true; co["allowCreate"] = true; }
                co["placeholder"] = "请选择"; co["clearable"] = true; break;
            default:
                co["placeholder"] = "请输入" + f.Label; co["clearable"] = true; break;
        }
        return BaseNode(comp, modelName, co, f.Label, true);
    }

    /// <summary>
    /// 生成筛选区(Filter)组件树。
    /// 规则：只生成条件输入控件，不内置「查询/重置」按钮（按钮统一放外层 List 页面，职责分离）。
    /// filterCols：每行几列（3 或 4，默认 4），控制网格列数。
    /// </summary>
    internal JObject BuildFilterCfg(List<GenColumn> fields, int? projectId, string table, int filterCols = 4)
    {
        if (filterCols != 3 && filterCols != 4) filterCols = 4;
        var children = new JArray();
        foreach (var f in fields.Where(x => x.InFilter)) children.Add(FieldNode(f, true, projectId, table));

        var root = BaseNode("DynGridContainer", "", new JObject
        {
            ["direction"] = "vertical", ["size"] = "small"
        });
        root["options"]!["labeloptions"] = new JObject { ["required"] = false, ["show"] = false, ["labelposition"] = "right", ["labelwidth"] = "120px" };
        root["options"]!["itemoptions"]!["class"] = $"grid grid-cols-{filterCols} gap-x-3 gap-y-2 items-center";
        root["childrenctrls"] = children;
        return root;
    }

    /// <summary>
    /// 筛选区默认条件（DefaultJson）：{ 列: { op, value:null } }，页面打开时作为初始查询条件。
    /// 查询时收集当前输入；重置时恢复为此默认值。
    /// </summary>
    internal JObject BuildFilterDefaultJson(List<GenColumn> fields)
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
    internal JObject BuildListCfg(List<GenColumn> fields)
    {
        var columns = new JArray();
        columns.Add(new JObject { ["field"] = "Id", ["label"] = "Id", ["width"] = 90 });
        foreach (var f in fields.Where(x => x.InList))
        {
            if (f.Name == "CreateTime" || f.Name == "UpdateTime") continue; // 时间列由下方显式追加，避免重复
            var col = new JObject { ["field"] = f.Name, ["label"] = f.Label };
            col["width"] = f.CellTag ? 100 : (f.Control == "textarea" ? 220 : 160);
            if (f.CellTag)
            {
                col["cellType"] = "tag";
                // int(0/1) 标志位映射成 是/否 tag
                col["options"] = new JArray
                {
                    new JObject { ["value"] = 1, ["label"] = "是", ["type"] = "success" },
                    new JObject { ["value"] = 0, ["label"] = "否", ["type"] = "info" }
                };
            }
            columns.Add(col);
        }
        columns.Add(new JObject { ["field"] = "CreateTime", ["label"] = "创建时间", ["width"] = 190 });
        return new JObject { ["columns"] = columns };
    }

    /// <summary>列表区默认分页与排序（DefaultJson）。</summary>
    internal JObject BuildListDefaultJson()
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
    internal JObject BuildDetailCfg(string pk, List<GenColumn> fields, int? projectId, string table, int detailCols = 2)
    {
        if (detailCols != 1 && detailCols != 2) detailCols = 2;
        var children = new JArray();
        var detailFields = fields.Where(x => x.InDetail).ToList();

        // 主键隐藏域（新增空→insert；编辑有值→update）
        var hidden = BaseNode("DynElInput", pk, new JObject { ["placeholder"] = "" }, "", false);
        hidden["options"]!["itemoptions"]!["style"] = new JObject { ["display"] = "none" };
        children.Add(hidden);

        foreach (var f in detailFields)
        {
            var node = FieldNode(f, false, projectId, table);
            // 长文本 / JSON 内容较宽：两列布局时占满两列；一列布局时本身就占满，无需跨列
            if (detailCols == 2 && f.Control is "textarea" or "json")
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
        root["options"]!["itemoptions"]!["class"] = $"grid grid-cols-{detailCols} gap-x-3 gap-y-2 items-start";
        root["childrenctrls"] = children;
        return root;
    }

    /// <summary>详情区默认 model（DefaultJson）：数字默认 0、开关默认 1、其余 null。</summary>
    internal JObject BuildDetailDefaultJson(string pk, List<GenColumn> fields)
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

    /// <summary>
    /// 布局壳（list-master-detail）的 SpecJson：复用同批三屏 setting，slots 按 filter/master/detail 绑定。
    /// project 显式请求坐标优先；为空则留空，壳视图回退页面 ProjectId。
    /// </summary>
    internal JObject BuildShellSpec(string table, string pk, string? project, int filterId, int listId, int detailId)
    {
        return new JObject
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
    }

    // ======================================================================
    //  AI 三屏配置：提示词约束与结果结构校验（纯字符串/JSON 处理，不访问数据库）
    // ======================================================================

    /// <summary>
    /// 构建 AI 系统提示词：组件清单 + 配置结构规则 + 输出格式约束。
    /// 只给组件名+用途+一个完整默认配置示例，让 AI 学会结构即可，避免 prompt 过长。
    /// </summary>
    internal string BuildAiSystemPrompt(List<CompSchemaItem> schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是 VueLibV4 低代码平台的页面配置生成器。根据用户的自然语言描述，输出三屏（筛选区/列表区/详情区）的组件配置 JSON。");
        sb.AppendLine();
        sb.AppendLine("【组件配置结构】每个组件节点格式如下：");
        sb.AppendLine("{");
        sb.AppendLine("  \"component\": \"DynElInput\",          // 组件名，必须从下方清单选择");
        sb.AppendLine("  \"modelname\": \"UserName\",             // 绑定的模型字段名（无绑定为空字符串）");
        sb.AppendLine("  \"options\": {");
        sb.AppendLine("    \"comoptions\": { \"placeholder\": \"请输入\" },  // 组件自身属性");
        sb.AppendLine("    \"labeloptions\": { \"label\": \"用户名\", \"required\": false, \"show\": true },");
        sb.AppendLine("    \"itemoptions\": { \"style\": {}, \"class\": \"\" }");
        sb.AppendLine("  },");
        sb.AppendLine("  \"validators\": [],                      // 校验规则数组");
        sb.AppendLine("  \"childrenctrls\": [],                   // 子组件数组（容器类组件才有）");
        sb.AppendLine("  \"slots\": {},");
        sb.AppendLine("  \"extendinfo\": {}");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("【三屏输出结构】");
        sb.AppendLine("{");
        sb.AppendLine("  \"filter\": { ...DynGridContainer 包裹筛选控件... },");
        sb.AppendLine("  \"list\":   { \"columns\": [...], ... },  // 列表区用 columns 数组定义列");
        sb.AppendLine("  \"detail\": { ...DynGridContainer 包裹详情控件... }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("【列表区 columns 列定义格式】");
        sb.AppendLine("{ \"field\": \"UserName\", \"label\": \"用户名\", \"width\": 120, \"minWidth\": 100 }");
        sb.AppendLine();
        sb.AppendLine("【规则】");
        sb.AppendLine("1. component 必须从下方组件清单中选择，禁止编造不存在的组件名。");
        sb.AppendLine("2. 筛选区只放输入类控件（DynElInput/DynElSelect/DynElDatePicker 等），不放按钮。");
        sb.AppendLine("3. 详情区用 DynGridContainer 作为根容器包裹所有字段。");
        sb.AppendLine("4. 主键字段（通常是 Id）详情区可省略或放隐藏域。");
        sb.AppendLine("5. 只输出 JSON，不要任何解释文字、不要 markdown 代码块标记。");
        sb.AppendLine();
        sb.AppendLine("【可用组件清单】");
        foreach (var c in schema)
        {
            sb.AppendLine($"- {c.Name}（{c.Label}）[{c.Category}]");
        }
        sb.AppendLine();
        sb.AppendLine("【组件默认配置示例】以下是几个常用组件的完整默认配置，可参考其结构：");
        var examples = schema.Where(c =>
            c.Name is "DynElInput" or "DynElSelect" or "DynElDatePicker" or "DynElSwitch" or "DynElTextarea" or "DynGridContainer").ToList();
        foreach (var c in examples)
        {
            if (!string.IsNullOrEmpty(c.DefaultConfig))
            {
                sb.AppendLine($"--- {c.Name} 默认配置 ---");
                sb.AppendLine(c.DefaultConfig);
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    /// <summary>构建用户提示词：自然语言描述 + 可选表结构</summary>
    internal string BuildAiUserPrompt(string prompt, string? tableName, string? project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("用户需求：");
        sb.AppendLine(prompt);
        if (!string.IsNullOrEmpty(tableName))
        {
            sb.AppendLine();
            sb.AppendLine($"目标表：{tableName}");
            if (!string.IsNullOrEmpty(project))
                sb.AppendLine($"项目：{project}");
            sb.AppendLine("请根据表名推断常见字段（如 Id, Name, CreateTime 等），并合理分配到筛选区/列表区/详情区。");
        }
        sb.AppendLine();
        sb.AppendLine("请输出三屏配置 JSON。");
        return sb.ToString();
    }

    /// <summary>递归校验配置树：component 名必须在合法集合内，非法则删除该节点</summary>
    internal void ValidateConfigTree(JObject? node, HashSet<string> validNames)
    {
        if (node == null) return;
        var comp = node["component"]?.ToString();
        if (!string.IsNullOrEmpty(comp) && !validNames.Contains(comp))
        {
            // 非法组件名：移除，避免渲染崩溃
            node["component"] = "DynElInput";
        }
        if (node["childrenctrls"] is JArray children)
        {
            foreach (var child in children.ToList())
            {
                ValidateConfigTree(child as JObject, validNames);
            }
        }
    }
}

/// <summary>AI 提示词中引用的组件清单项（名称/标签/分类/默认配置）。</summary>
internal sealed class CompSchemaItem
{
    public string Name { get; set; } = "";
    public string Label { get; set; } = "";
    public string Category { get; set; } = "";
    public string? DefaultConfig { get; set; }
}
