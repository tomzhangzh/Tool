using System.Text.Json;
using System.Text.Json.Serialization;
using SqlSugar;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>Demo 页面/组件工厂：程序化生成低代码配置树（等价于设计器保存的 JSON）</summary>
public class DemoPageFactory
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>生成平台种子：手工组件（版本演示 / 组合组件）+ 演示页面</summary>
    public async Task SeedAsync(ISqlSugarClient platform)
    {
        // ---- 版本演示组件：两个手工版本 ----
        if (!await platform.Queryable<LcComponent>().AnyAsync(c => c.Name == "VersionDemo"))
        {
            await platform.Insertable(new List<LcComponent>
            {
                new()
                {
                    Name = "VersionDemo", DisplayName = "版本演示(v1)", Category = "Demo", Icon = "Coin",
                    Version = 1, IsCurrent = false, SourceType = "demo",
                    ComponentText = VersionDemoText(1)
                },
                new()
                {
                    Name = "VersionDemo", DisplayName = "版本演示(v2)", Category = "Demo", Icon = "Coin",
                    Version = 2, IsCurrent = true, SourceType = "demo",
                    ComponentText = VersionDemoText(2)
                }
            }).ExecuteCommandAsync();
        }

        // ---- 组合组件：CustomerCard（开放属性 + 开放插槽 body）----
        if (!await platform.Queryable<LcComponent>().AnyAsync(c => c.Name == "CustomerCard"))
        {
            await platform.Insertable(new LcComponent
            {
                Name = "CustomerCard", DisplayName = "客户卡片(组合)", Category = "Composite", Icon = "CreditCard",
                Version = 1, IsCurrent = true, SourceType = "composite",
                CompositeConfigJson = CustomerCardComposite(),
                ConfigSchemaJson = CustomerCardSchema()
            }).ExecuteCommandAsync();
        }

        // ---- 演示页面 ----
        await EnsurePageAsync(platform, "demo-form", "表单+校验+多源下拉", FormDemo());
        await EnsurePageAsync(platform, "demo-grid3", "三屏管理 Filter-Grid-Detail", Grid3Demo());
        await EnsurePageAsync(platform, "demo-window", "窗口 Element/Lay + URL内容", WindowDemo());
        await EnsurePageAsync(platform, "demo-fragment", "透明片段 Fragment", FragmentDemo());
        await EnsurePageAsync(platform, "demo-flex", "Flexbox + Tailwind 容器", FlexDemo());
        await EnsurePageAsync(platform, "demo-composite", "组合组件+开放插槽", CompositeDemo());
        await EnsurePageAsync(platform, "demo-actions", "动作链 setVar/delay/notify/triggerEvent", ActionsDemo());
        await EnsurePageAsync(platform, "demo-version", "组件版本锁定", VersionDemoPage());
    }

    private static async Task EnsurePageAsync(ISqlSugarClient platform, string code, string name, (string Config, string Model) page)
    {
        if (await platform.Queryable<LcPage>().AnyAsync(p => p.Code == code)) return;
        await platform.Insertable(new LcPage
        {
            Code = code, Name = name,
            ConfigJson = page.Config,
            DefaultModelJson = page.Model
        }).ExecuteCommandAsync();
    }

    private static string Serialize(object o) => JsonSerializer.Serialize(o, JsonOpts);

    // ================= 节点构建器 =================
    /// <summary>组件节点。opts 回调参数是 options 字典（comoptions/comlisteners/labeloptions/...）</summary>
    private static Dictionary<string, object?> Com(string component, string? modelname = null,
        Action<Dictionary<string, object?>>? opts = null,
        Action<List<Dictionary<string, object?>>>? children = null,
        Dictionary<string, object?>? slots = null,
        List<object>? validators = null)
    {
        var node = new Dictionary<string, object?>
        {
            ["component"] = component,
            ["modelname"] = modelname,
            ["options"] = new Dictionary<string, object?>
            {
                ["comoptions"] = new Dictionary<string, object?>(),
                ["comlisteners"] = new Dictionary<string, object?>(),
                ["labeloptions"] = new Dictionary<string, object?> { ["label"] = "", ["required"] = false, ["show"] = true },
                ["itemoptions"] = new Dictionary<string, object?> { ["style"] = new Dictionary<string, object?>(), ["class"] = "" },
                ["wrapperoptions"] = new Dictionary<string, object?>()
            },
            ["validators"] = validators ?? new List<object>(),
            ["childrenctrls"] = new List<Dictionary<string, object?>>(),
            ["slots"] = slots ?? new Dictionary<string, object?>(),
            ["extendinfo"] = new Dictionary<string, object?>()
        };
        opts?.Invoke((Dictionary<string, object?>)node["options"]!);
        children?.Invoke((List<Dictionary<string, object?>>)node["childrenctrls"]!);
        return node;
    }

    /// <summary>在节点上写 comoptions</summary>
    private static void Opt(Dictionary<string, object?> node, string key, object? value)
        => ((Dictionary<string, object?>)((Dictionary<string, object?>)node["options"]!)["comoptions"]!)[key] = value;

    /// <summary>在 options 字典上写 comoptions（Com 的 opts 回调内使用）</summary>
    private static void Cmt(Dictionary<string, object?> options, string key, object? value)
        => ((Dictionary<string, object?>)options["comoptions"]!)[key] = value;

    private static void Flex(Dictionary<string, object?> options, string direction = "col",
        string justify = "start", string align = "stretch", int gap = 8, string className = "")
    {
        var f = new Dictionary<string, object?>
        {
            ["enabled"] = true, ["direction"] = direction, ["justify"] = justify,
            ["align"] = align, ["wrap"] = "wrap", ["gap"] = gap, ["className"] = className
        };
        Cmt(options, "flexbox", f);
    }

    private static void Label(Dictionary<string, object?> node, string label, bool required = false)
    {
        var lo = (Dictionary<string, object?>)((Dictionary<string, object?>)node["options"]!)["labeloptions"]!;
        lo["label"] = label; lo["required"] = required;
    }

    private static void Class(Dictionary<string, object?> node, string cls)
        => ((Dictionary<string, object?>)((Dictionary<string, object?>)node["options"]!)["itemoptions"]!)["class"] = cls;

    private static List<object> Valid(string type, string message, object? value = null)
        => new() { new Dictionary<string, object?> { ["type"] = type, ["message"] = message, ["value"] = value } };

    private static Dictionary<string, object?> Slot(string label, bool open, params Dictionary<string, object?>[] children)
        => new() { ["label"] = label, ["open"] = open, ["childrenctrls"] = children.ToList() };

    private static Dictionary<string, object?> Chain(params object[] steps)
        => new() { ["chain"] = steps };

    private static Dictionary<string, object?> Step(string action, Dictionary<string, object?> options)
        => new() { ["action"] = action, ["options"] = options };

    // ================= Demo 页面 =================
    private static (string Config, string Model) FormDemo()
    {
        var root = Com("DivContainer", null, o => Flex(o, "col", className: "max-w-2xl mx-auto p-6"));

        var name = Com("ElementInput", "name");
        Opt(name, "placeholder", "请输入客户名称");
        Label(name, "客户名称", true);
        name["validators"] = Valid("required", "客户名称不能为空");

        var selectDict = Com("ElementSelect", "category");
        Label(selectDict, "商品分类(Dict字典)");
        Opt(selectDict, "optionSource", new Dictionary<string, object?>
        {
            ["kind"] = "dict", ["typeCode"] = "product_category"
        });

        var selectSql = Com("ElementSelect", "company");
        Label(selectSql, "公司(SQL查询业务库)");
        Opt(selectSql, "optionSource", new Dictionary<string, object?>
        {
            ["kind"] = "sql",
            ["sql"] = "SELECT Company AS Label, Company AS Value FROM Customers ORDER BY Company",
            ["labelField"] = "Label", ["valueField"] = "Value"
        });

        var selectAjax = Com("ElementSelect", "ownerId");
        Label(selectAjax, "负责人(Ajax)");
        Opt(selectAjax, "optionSource", new Dictionary<string, object?>
        {
            ["kind"] = "ajax", ["url"] = "/api/demo/customers", ["labelField"] = "label", ["valueField"] = "value"
        });

        var qty = Com("ElementInputNumber", "qty");
        Label(qty, "数量", true);
        qty["validators"] = Valid("min", "数量不能小于1", 1);

        var check = Com("ElementCheckboxGroup", "tags");
        Label(check, "标签(Checkbox)");
        Opt(check, "optionValues", "VIP,大客户,新客户");

        var radio = Com("ElementRadioGroup", "level");
        Label(radio, "级别(Radio)");
        Opt(radio, "optionValues", "A级,B级,C级");

        var date = Com("ElementDatePicker", "birthday");
        Label(date, "生日(Date)");

        var sw = Com("ElementSwitch", "active");
        Label(sw, "启用(Switch)");

        var remark = Com("ElementTextArea", "remark");
        Label(remark, "备注");

        var btnOk = Com("ElementButton", null);
        Opt(btnOk, "text", "提交并触发事件");
        Opt(btnOk, "type", "primary");
        btnOk["extendinfo"] = new Dictionary<string, object?>
        {
            ["actions"] = Chain(
                Step("notify", new Dictionary<string, object?> { ["type"] = "success", ["message"] = "表单已提交！" }),
                Step("setVar", new Dictionary<string, object?> { ["path"] = "formState.submitted", ["value"] = true }),
                Step("triggerEvent", new Dictionary<string, object?>
                {
                    ["name"] = "demo:submitted",
                    ["payload"] = new Dictionary<string, object?> { ["name"] = "{{name}}" }
                }))
        };

        var btnReset = Com("ElementButton", null);
        Opt(btnReset, "text", "重置表单");
        btnReset["extendinfo"] = new Dictionary<string, object?>
        {
            ["actions"] = Chain(
                Step("setVar", new Dictionary<string, object?> { ["path"] = "resetSignal", ["value"] = 1 }),
                Step("notify", new Dictionary<string, object?> { ["type"] = "info", ["message"] = "已触发重置信号" }))
        };

        ((List<Dictionary<string, object?>>)root["childrenctrls"]!).AddRange(new[]
        {
            Com("ElementDivider", null, o => Cmt(o, "text", "基础表单（ElementUI + LabelWrapper 校验）")),
            name, selectDict, selectSql, selectAjax, qty, check, radio, date, sw, remark,
            Com("Row", null, children: c =>
            {
                c.Add(Com("Col", null, o => Cmt(o, "span", 12), children: cc => cc.Add(btnOk)));
                c.Add(Com("Col", null, o => Cmt(o, "span", 12), children: cc => cc.Add(btnReset)));
            })
        });

        var model = new Dictionary<string, object?>
        {
            ["name"] = "", ["category"] = "", ["company"] = "", ["ownerId"] = "", ["qty"] = 1,
            ["tags"] = new[] { "VIP" }, ["level"] = "A级", ["birthday"] = null, ["active"] = true,
            ["remark"] = "", ["formState"] = new Dictionary<string, object?> { ["submitted"] = false },
            ["resetSignal"] = 0
        };
        return (Serialize(root), Serialize(model));
    }

    private static (string Config, string Model) Grid3Demo()
    {
        var root = Com("Grid3");
        Opt(root, "featureUrl", "/api/demo");
        Opt(root, "filterUrl", "/partial/filter");
        Opt(root, "listUrl", "/partial/list");
        Opt(root, "detailUrl", "/partial/detail");
        Opt(root, "title", "商品管理");
        Opt(root, "showToolbar", true);
        var model = new Dictionary<string, object?>
        {
            ["filter"] = new Dictionary<string, object?> { ["keyword"] = "", ["category"] = "" },
            ["page"] = new Dictionary<string, object?> { ["pageIndex"] = 1, ["pageSize"] = 5 }
        };
        return (Serialize(root), Serialize(model));
    }

    private static (string Config, string Model) WindowDemo()
    {
        var root = Com("DivContainer", null, o => Flex(o, "row", gap: 12, className: "p-6"));

        var btnEl = Com("ElementButton", null);
        Opt(btnEl, "text", "打开 Element Dialog (URL内容)");
        Opt(btnEl, "type", "primary");
        btnEl["extendinfo"] = new Dictionary<string, object?>
        {
            ["actions"] = Chain(Step("openwindow", new Dictionary<string, object?>
            {
                ["mode"] = "element", ["title"] = "Element 弹窗", ["width"] = "600px",
                ["url"] = "/api/demo/window-content",
                ["params"] = new Dictionary<string, object?> { ["from"] = "window-demo" }
            }))
        };

        var btnLay = Com("ElementButton", null);
        Opt(btnLay, "text", "打开 LayUI 风格窗口(可拖动)");
        Opt(btnLay, "type", "warning");
        btnLay["extendinfo"] = new Dictionary<string, object?>
        {
            ["actions"] = Chain(Step("openwindow", new Dictionary<string, object?>
            {
                ["mode"] = "lay", ["title"] = "LayUI 窗口", ["width"] = "520px", ["height"] = "360px",
                ["url"] = "/api/demo/window-content",
                ["params"] = new Dictionary<string, object?> { ["from"] = "lay-window" }
            }))
        };

        var info = Com("ElementTextArea", "log");
        Label(info, "窗口操作日志");

        ((List<Dictionary<string, object?>>)root["childrenctrls"]!).AddRange(new[] { btnEl, btnLay, info });
        var model = new Dictionary<string, object?> { ["log"] = "" };
        return (Serialize(root), Serialize(model));
    }

    private static (string Config, string Model) FragmentDemo()
    {
        var root = Com("DivContainer", null, o => Flex(o, "col", gap: 8, className: "max-w-xl p-6"));

        var frag = Com("Fragment", null, children: c =>
        {
            c.Add(Com("ElementInput", "fragA", o => Cmt(o, "placeholder", "Fragment 内的输入框 A(无外层div)")));
            c.Add(Com("ElementInput", "fragB", o => Cmt(o, "placeholder", "Fragment 内的输入框 B")));
        });

        ((List<Dictionary<string, object?>>)root["childrenctrls"]!).AddRange(new[]
        {
            Com("ElementDivider", null, o => Cmt(o, "text", "Fragment 透明容器：运行时不产生外层 DOM")),
            frag
        });
        var model = new Dictionary<string, object?> { ["fragA"] = "", ["fragB"] = "" };
        return (Serialize(root), Serialize(model));
    }

    private static (string Config, string Model) FlexDemo()
    {
        var root = Com("DivContainer", null, o => Flex(o, "col", gap: 16, className: "p-6"));

        var rowBetween = Com("DivContainer", null,
            o => Flex(o, "row", justify: "between", gap: 8, className: "border border-dashed border-gray-300 p-3 rounded"),
            children: c =>
            {
                c.Add(Com("ElementInput", null, o => Cmt(o, "placeholder", "左1")));
                c.Add(Com("ElementButton", null, o => Cmt(o, "text", "中按钮")));
                c.Add(Com("ElementInput", null, o => Cmt(o, "placeholder", "右2")));
            });

        var colCenter = Com("DivContainer", null,
            o => Flex(o, "col", align: "center", gap: 8, className: "border border-dashed border-gray-300 p-3 rounded"),
            children: c =>
            {
                c.Add(Com("ElementButton", null, o => Cmt(o, "text", "按钮1")));
                c.Add(Com("ElementButton", null, o => Cmt(o, "text", "按钮2")));
            });

        var half = Com("DivContainer", null,
            o => Flex(o, "row", gap: 4, className: "border border-dashed border-gray-300 p-3 rounded"),
            children: c =>
            {
                var a = Com("ElementInput", null, o => Cmt(o, "placeholder", "w-1/2"));
                Class(a, "w-1/2");
                var b = Com("ElementInput", null, o => Cmt(o, "placeholder", "w-1/2"));
                Class(b, "w-1/2");
                c.Add(a); c.Add(b);
            });

        ((List<Dictionary<string, object?>>)root["childrenctrls"]!).AddRange(new[]
        {
            Com("ElementDivider", null, o => Cmt(o, "text", "Flexbox 容器（Tailwind 类生成，类似 flexbox generator）")),
            rowBetween, colCenter, half
        });
        return (Serialize(root), Serialize(new Dictionary<string, object?>()));
    }

    private static (string Config, string Model) CompositeDemo()
    {
        var root = Com("DivContainer", null, o => Flex(o, "col", gap: 12, className: "max-w-xl p-6"));

        var card = Com("CustomerCard");
        Opt(card, "title", "VIP 客户");
        Opt(card, "showPhone", true);
        card["childrenctrls"] = new List<Dictionary<string, object?>>
        {
            Com("ElementTextArea", "cardNote", o => Cmt(o, "placeholder", "开放插槽 body 中的内容：备注"))
        };

        ((List<Dictionary<string, object?>>)root["childrenctrls"]!).AddRange(new[]
        {
            Com("ElementDivider", null, o => Cmt(o, "text", "组合组件 CustomerCard（开放属性 + 开放插槽）")),
            card
        });
        var model = new Dictionary<string, object?>
        {
            ["customerName"] = "张三", ["customerPhone"] = "13800138000", ["customerCompany"] = "字节跳动", ["cardNote"] = ""
        };
        return (Serialize(root), Serialize(model));
    }

    private static (string Config, string Model) ActionsDemo()
    {
        var root = Com("DivContainer", null, o => Flex(o, "col", gap: 12, className: "max-w-2xl p-6"));

        var btn1 = Com("ElementButton", null);
        Opt(btn1, "text", "演示 setVar + delay + notify");
        btn1["extendinfo"] = new Dictionary<string, object?>
        {
            ["actions"] = Chain(
                Step("setVar", new Dictionary<string, object?> { ["path"] = "counter", ["value"] = 1 }),
                Step("delay", new Dictionary<string, object?> { ["ms"] = 1000 }),
                Step("notify", new Dictionary<string, object?> { ["type"] = "success", ["message"] = "counter 已设为 1（延时1秒后提示）" }))
        };

        var btn2 = Com("ElementButton", null);
        Opt(btn2, "text", "演示 triggerEvent");
        Opt(btn2, "type", "warning");
        btn2["extendinfo"] = new Dictionary<string, object?>
        {
            ["actions"] = Chain(Step("triggerEvent", new Dictionary<string, object?>
            {
                ["name"] = "demo:custom-event",
                ["payload"] = new Dictionary<string, object?> { ["msg"] = "hello {{counter}}" }
            }))
        };

        var log = Com("ElementTextArea", "actionLog");
        Label(log, "事件日志(自动监听 demo:custom-event)");
        log["extendinfo"] = new Dictionary<string, object?>
        {
            ["listen"] = new object[] { "demo:custom-event" }
        };

        ((List<Dictionary<string, object?>>)root["childrenctrls"]!).AddRange(new[]
        {
            Com("ElementDivider", null, o => Cmt(o, "text", "动作链演示：setVar / delay / notify / triggerEvent（控制台看 [DynChain] 日志）")),
            btn1, btn2, log
        });
        var model = new Dictionary<string, object?> { ["counter"] = 0, ["actionLog"] = "" };
        return (Serialize(root), Serialize(model));
    }

    private static (string Config, string Model) VersionDemoPage()
    {
        var root = Com("DivContainer", null, o => Flex(o, "col", gap: 12, className: "max-w-xl p-6"));

        var v1 = Com("VersionDemo", "vdata");

        ((List<Dictionary<string, object?>>)root["childrenctrls"]!).AddRange(new[]
        {
            Com("ElementDivider", null, o => Cmt(o, "text", "版本锁定演示：页面锁定 VersionDemo v1（当前已发布 v2）")),
            v1
        });

        // 组件版本锁定表（设计器保存页面时也会写入这里）
        root["extendinfo"] = new Dictionary<string, object?>
        {
            ["componentVersions"] = new Dictionary<string, int> { ["VersionDemo"] = 1 }
        };

        var model = new Dictionary<string, object?> { ["vdata"] = "页面模型数据" };
        return (Serialize(root), Serialize(model));
    }

    // ================= 组合组件定义 =================
    private static string CustomerCardComposite()
    {
        var inner = Com("DivContainer", null,
            o => Flex(o, "col", gap: 8, className: "border border-gray-200 rounded-lg p-4 shadow-sm"),
            children: c =>
            {
                c.Add(Com("ElementInput", "customerName", o => Cmt(o, "placeholder", "客户名称")));
                c.Add(Com("ElementInput", "customerPhone", o => Cmt(o, "placeholder", "联系电话")));
                c.Add(Com("ElementInput", "customerCompany", o => Cmt(o, "placeholder", "公司")));
            },
            slots: new Dictionary<string, object?>
            {
                ["body"] = Slot("body 开放插槽", true)
            });

        inner["extendinfo"] = new Dictionary<string, object?>
        {
            ["compositeMeta"] = new Dictionary<string, object?>
            {
                ["openProps"] = new object[] { "title", "showPhone" },
                ["openSlots"] = new object[] { "body" }
            }
        };
        return Serialize(inner);
    }

    private static string CustomerCardSchema()
        => Serialize(new
        {
            openProps = new[]
            {
                new { key = "title", label = "标题", type = "text", group = "comoptions" },
                new { key = "showPhone", label = "显示电话", type = "switch", group = "comoptions" }
            },
            openSlots = new[] { new { key = "body", label = "body 插槽" } }
        });

    // ================= 版本演示组件文本 =================
    private static string VersionDemoText(int v)
    {
        var badge = v == 1 ? "v1(旧版, 已锁定)" : "v2(当前版本)";
        var border = v == 1 ? "border-blue-300 bg-blue-50" : "border-green-300 bg-green-50";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<template>");
        sb.AppendLine("  <div class=\"border rounded p-4 " + border + "\">");
        sb.AppendLine("    <div class=\"font-bold mb-2\">VersionDemo <span>" + badge + "</span></div>");
        sb.AppendLine("    <div class=\"text-sm text-gray-600\">当前渲染的是 <b>" + badge + "</b>。v2 版本新增绿色样式与“当前版本”标识。</div>");
        sb.AppendLine("    <el-input v-model=\"modelinfo\" placeholder=\"绑定模型: vdata\" class=\"mt-2\"></el-input>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</template>");
        sb.AppendLine("<script tag='comconfig'>");
        sb.AppendLine("var comConfig = {");
        sb.AppendLine("  props: { jsonconfig: { type: Object, required: true }, parentmodelinfo: { type: Object, required: false } },");
        sb.AppendLine("  setup(props, context) {");
        sb.AppendLine("    const { ref, computed } = Vue;");
        sb.AppendLine("    const innerValue = ref(null);");
        sb.AppendLine("    const modelinfo = computed({");
        sb.AppendLine("      get() { return props.parentmodelinfo ? props.parentmodelinfo.vdata : innerValue.value; },");
        sb.AppendLine("      set(val) { if (props.parentmodelinfo) props.parentmodelinfo.vdata = val; else innerValue.value = val; }");
        sb.AppendLine("    });");
        sb.AppendLine("    return { modelinfo };");
        sb.AppendLine("  }");
        sb.AppendLine("};");
        sb.AppendLine("</script>");
        return sb.ToString();
    }
}
