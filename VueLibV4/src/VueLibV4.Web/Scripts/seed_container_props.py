import sqlite3, json
db = r'E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\bin\Debug\net8.0\App_Data\Platform.db'
conn = sqlite3.connect(db)
cur = conn.cursor()

def fi(modelname, label, kind, default, extra=None):
    """FormItem + 控件"""
    child_opts = {"kind": kind, "default": default}
    if extra: child_opts.update(extra)
    comp_map = {
        "input": "DynElInput",
        "switch": "DynElSwitch",
        "number": "DynElInputNumber",
        "select": "DynElSelect",
    }
    return {
        "component": "DynElFormItem",
        "modelname": modelname,
        "options": {"comoptions": {},
                    "labeloptions": {"label": label, "show": True, "labelwidth": "80px"},
                    "itemoptions": {"style": {"marginBottom": "8px"}}},
        "childrenctrls": [{
            "component": comp_map.get(kind, "DynElInput"),
            "options": {"comoptions": child_opts, "itemoptions": {"style": {}}},
            "childrenctrls": []
        }]
    }

def container(items):
    return {
        "component": "DynElContainer",
        "options": {"itemoptions": {"style": {"padding": "0 8px"}}},
        "childrenctrls": items
    }

# 通用容器配置（label对齐、label宽度、size）
common_container_items = [
    fi("options.labeloptions.labelposition", "Label对齐", "select", "right", {
        "optionValues": [
            {"label": "左对齐", "value": "left"},
            {"label": "右对齐", "value": "right"},
            {"label": "顶部", "value": "top"},
        ]
    }),
    fi("options.labeloptions.labelwidth", "Label宽度", "input", "100px"),
    fi("options.comoptions.size", "子控件尺寸", "select", "default", {
        "optionValues": [
            {"label": "默认", "value": "default"},
            {"label": "大号", "value": "large"},
            {"label": "小号", "value": "small"},
        ]
    }),
]

# DynGridContainer: 列数 + 间距 + 通用容器配置
grid_items = [
    fi("options.comoptions.gridTemplateColumns", "列数", "select", "1fr 1fr", {
        "optionValues": [
            {"label": "1列", "value": "1fr"},
            {"label": "2列", "value": "1fr 1fr"},
            {"label": "3列", "value": "1fr 1fr 1fr"},
            {"label": "4列", "value": "1fr 1fr 1fr 1fr"},
        ]
    }),
    fi("options.comoptions.gap", "间距", "input", "16px"),
] + common_container_items

configs = {
    "DynGridContainer": container(grid_items),
    "DynElContainer": container(common_container_items),
    "DynForm": container(common_container_items + [
        fi("options.comoptions.inline", "行内表单", "switch", False),
        fi("options.labeloptions.labelwidth", "表单Label宽度", "input", "100px"),
    ]),
}

for name, cfg in configs.items():
    json_str = json.dumps(cfg, ensure_ascii=False)
    cur.execute("UPDATE ComponentMeta SET PropertyConfigJson = ? WHERE ComponentName = ?", (json_str, name))
    print(f"{name}: {cur.rowcount} row(s)")

conn.commit()
conn.close()
print('Done')
