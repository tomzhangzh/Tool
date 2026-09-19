import sqlite3, json
db = r'E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\bin\Debug\net8.0\App_Data\Platform.db'
conn = sqlite3.connect(db)
cur = conn.cursor()

def item(modelname, label, kind, default, extra=None):
    """Create a form item bound to modelname with the given control kind"""
    comoptions = {"label": label, "showLabel": True, "labelWidth": "80px"}
    child_opts = {"kind": kind, "default": default}
    if extra: child_opts.update(extra)
    return {
        "component": "DynElFormItem",
        "modelname": modelname,
        "options": {
            "comoptions": comoptions,
            "itemoptions": {"style": {"marginBottom": "8px"}}
        },
        "childrenctrls": [{
            "component": "DynElInput" if kind == "input" else
                         "DynElSwitch" if kind == "switch" else
                         "DynElInputNumber" if kind == "number" else
                         "DynElSelect",
            "options": {"comoptions": child_opts, "itemoptions": {"style": {}}},
            "childrenctrls": []
        }]
    }

def container(items):
    return {
        "component": "DynElContainer",
        "options": {"comoptions": {"direction": "vertical"}, "itemoptions": {"style": {"padding": "0 8px"}}},
        "childrenctrls": items
    }

configs = {
    "DynElInput": container([
        item("options.comoptions.placeholder","占位符","input",""),
        item("options.comoptions.clearable","可清除","switch",True),
        item("options.comoptions.disabled","禁用","switch",False),
        item("options.comoptions.maxlength","最大长度","number",200),
    ]),
    "DynElTextarea": container([
        item("options.comoptions.placeholder","占位符","input",""),
        item("options.comoptions.rows","行数","number",3),
    ]),
    "DynElSelect": container([
        item("options.comoptions.placeholder","占位符","input","请选择"),
        item("options.comoptions.clearable","可清除","switch",True),
        item("options.comoptions.multiple","多选","switch",False),
    ]),
    "DynElSwitch": container([
        item("options.comoptions.activeText","开启文字","input","开启"),
        item("options.comoptions.inactiveText","关闭文字","input","关闭"),
    ]),
    "DynElInputNumber": container([
        item("options.comoptions.placeholder","占位符","input",""),
        item("options.comoptions.min","最小值","number",0),
        item("options.comoptions.max","最大值","number",100),
    ]),
    "DynElDatePicker": container([
        item("options.comoptions.placeholder","占位符","input","请选择日期"),
        item("options.comoptions.type","类型","select","date",{
            "optionValues":[{"label":"日期","value":"date"},{"label":"日期时间","value":"datetime"},{"label":"周","value":"week"},{"label":"月份","value":"month"}]
        }),
    ]),
    "DynElButton": container([
        item("options.comoptions.type","按钮类型","select","primary",{
            "optionValues":[{"label":"主要","value":"primary"},{"label":"成功","value":"success"},{"label":"警告","value":"warning"},{"label":"危险","value":"danger"}]
        }),
        item("options.comoptions.plain","朴素","switch",False),
    ]),
}

for name, cfg in configs.items():
    json_str = json.dumps(cfg, ensure_ascii=False)
    cur.execute("UPDATE ComponentMeta SET PropertyConfigJson = ? WHERE ComponentName = ?", (json_str, name))
    print(f"{name}: {cur.rowcount} row(s)")

conn.commit()
conn.close()
print('Done')
