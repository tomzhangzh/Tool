import sqlite3, json
db = r'E:\Tom\Tool\VueLibV4\src\VueLibV4\bin\Debug\net8.0\App_Data\Platform.db'
db = r'E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\bin\Debug\net8.0\App_Data\Platform.db'
conn = sqlite3.connect(db)
cur = conn.cursor()

def prop(modelname, label, kind, default, extra=None):
    """单个属性控件：自带labeloptions，直接渲染"""
    child_opts = {"default": default}
    if extra: child_opts.update(extra)
    comp_map = {
        "input": "DynElInput",
        "switch": "DynElSwitch",
        "number": "DynElInputNumber",
        "select": "DynElSelect",
    }
    return {
        "component": comp_map.get(kind, "DynElInput"),
        "modelname": modelname,
        "options": {
            "comoptions": child_opts,
            "labeloptions": {"label": label, "show": True, "required": False, "labelwidth": "80px"},
            "itemoptions": {"style": {"marginBottom": "12px"}}
        },
        "childrenctrls": []
    }

def container(items):
    return {
        "component": "DynElContainer",
        "options": {
            "comoptions": {"direction": "vertical"},
            "itemoptions": {"style": {"padding": "8px"}}
        },
        "childrenctrls": items
    }

# 通用容器配置（label对齐、label宽度、size）
common_container_props = [
    prop("options.labeloptions.labelposition", "Label对齐", "select", "right", {
        "optionValues": [
            {"label": "左对齐", "value": "left"},
            {"label": "右对齐", "value": "right"},
            {"label": "顶部", "value": "top"},
        ]
    }),
    prop("options.labeloptions.labelwidth", "Label宽度", "input", "100px"),
    prop("options.comoptions.size", "子控件尺寸", "select", "default", {
        "optionValues": [
            {"label": "默认", "value": "default"},
            {"label": "大号", "value": "large"},
            {"label": "小号", "value": "small"},
        ]
    }),
]

# 表单控件属性
input_props = [
    prop("options.comoptions.placeholder", "占位符", "input", ""),
    prop("options.comoptions.clearable", "可清除", "switch", True),
    prop("options.comoptions.disabled", "禁用", "switch", False),
    prop("options.comoptions.maxlength", "最大长度", "number", 200),
]

textarea_props = [
    prop("options.comoptions.placeholder", "占位符", "input", ""),
    prop("options.comoptions.rows", "行数", "number", 3),
]

select_props = [
    prop("options.comoptions.placeholder", "占位符", "input", "请选择"),
    prop("options.comoptions.clearable", "可清除", "switch", True),
    prop("options.comoptions.multiple", "多选", "switch", False),
]

switch_props = [
    prop("options.comoptions.activeText", "开启文字", "input", "开启"),
    prop("options.comoptions.inactiveText", "关闭文字", "input", "关闭"),
]

number_props = [
    prop("options.comoptions.placeholder", "占位符", "input", ""),
    prop("options.comoptions.min", "最小值", "number", 0),
    prop("options.comoptions.max", "最大值", "number", 100),
]

date_props = [
    prop("options.comoptions.placeholder", "占位符", "input", "请选择日期"),
    prop("options.comoptions.type", "类型", "select", "date", {
        "optionValues": [
            {"label": "日期", "value": "date"},
            {"label": "日期时间", "value": "datetime"},
            {"label": "周", "value": "week"},
            {"label": "月份", "value": "month"},
        ]
    }),
]

button_props = [
    prop("options.comoptions.type", "按钮类型", "select", "primary", {
        "optionValues": [
            {"label": "主要", "value": "primary"},
            {"label": "成功", "value": "success"},
            {"label": "警告", "value": "warning"},
            {"label": "危险", "value": "danger"},
        ]
    }),
    prop("options.comoptions.plain", "朴素", "switch", False),
]

grid_props = [
    prop("options.comoptions.gridTemplateColumns", "列数", "select", "1fr 1fr", {
        "optionValues": [
            {"label": "1列", "value": "1fr"},
            {"label": "2列", "value": "1fr 1fr"},
            {"label": "3列", "value": "1fr 1fr 1fr"},
            {"label": "4列", "value": "1fr 1fr 1fr 1fr"},
        ]
    }),
    prop("options.comoptions.gap", "间距", "input", "16px"),
] + common_container_props

form_props = common_container_props + [
    prop("options.comoptions.inline", "行内表单", "switch", False),
]

configs = {
    "DynElInput": container(input_props),
    "DynElTextarea": container(textarea_props),
    "DynElSelect": container(select_props),
    "DynElSwitch": container(switch_props),
    "DynElInputNumber": container(number_props),
    "DynElDatePicker": container(date_props),
    "DynElButton": container(button_props),
    "DynGridContainer": container(grid_props),
    "DynElContainer": container(common_container_props),
    "DynForm": container(form_props),
}

for name, cfg in configs.items():
    json_str = json.dumps(cfg, ensure_ascii=False)
    cur.execute("UPDATE ComponentMeta SET PropertyConfigJson = ? WHERE ComponentName = ?", (json_str, name))
    print(f"{name}: {cur.rowcount} row(s)")

conn.commit()
conn.close()
print('Done')
