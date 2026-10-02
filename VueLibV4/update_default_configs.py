import sqlite3, json

db = r"E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\bin\Debug\net8.0\App_Data\Platform.db"
conn = sqlite3.connect(db)
cur = conn.cursor()

def cfg(name, comoptions, label=None, extra=None):
    o = {"comoptions": comoptions, "comlisteners": {},
         "labeloptions": {"label": label or "", "required": False, "show": bool(label), "labelwidth": "80px"},
         "itemoptions": {"style": {}, "class": ""}}
    c = {"component": name, "modelname": "", "options": o,
         "validators": [], "childrenctrls": [], "slots": {}, "extendinfo": extra or {}}
    return c

updates = {
    "DynElButton": cfg("DynElButton", {"type": "primary"}, extra={"text": "主要按钮"}),
    "DynElInput": cfg("DynElInput", {"placeholder": "请输入内容"}, "输入框"),
    "DynElSelect": cfg("DynElSelect", {"placeholder": "请选择", "options": [
        {"label": "选项一", "value": 1}, {"label": "选项二", "value": 2}, {"label": "选项三", "value": 3}
    ]}, "下拉选择"),
    "DynElSwitch": cfg("DynElSwitch", {}, "开关"),
    "DynElCard": {
        "component": "DynElCard", "modelname": "",
        "options": {"comoptions": {"title": "卡片标题"}, "comlisteners": {},
                    "labeloptions": {"label": "", "required": False, "show": False},
                    "itemoptions": {"style": {}, "class": ""}},
        "validators": [],
        "childrenctrls": [cfg("DynText", {"text": "这是卡片内容区域"})],
        "slots": {}, "extendinfo": {}
    },
    "DynElAlert": cfg("DynElAlert", {"title": "操作成功", "type": "success", "description": "这是一条成功提示消息"}),
    "DynElTag": cfg("DynElTag", {"type": "primary"}, extra={"text": "标签"}),
    "DynElProgress": cfg("DynElProgress", {"percentage": 60}),
    "DynElSlider": cfg("DynElSlider", {}, "滑块"),
    "DynElRate": cfg("DynElRate", {}, "评分"),
    "DynElColorPicker": cfg("DynElColorPicker", {}, "颜色"),
    "DynElDatePicker": cfg("DynElDatePicker", {"placeholder": "选择日期"}, "日期"),
    "DynElRadioGroup": cfg("DynElRadioGroup", {"options": [
        {"label": "选项一", "value": "1"}, {"label": "选项二", "value": "2"}
    ]}, "单选"),
    "DynElCheckboxGroup": cfg("DynElCheckboxGroup", {"options": [
        {"label": "选项一", "value": "1"}, {"label": "选项二", "value": "2"}
    ]}, "多选"),
    "DynElDivider": cfg("DynElDivider", {"content": "分割线"}),
    "DynElTextarea": cfg("DynElTextarea", {"placeholder": "请输入多行文本", "rows": 3}, "多行文本"),
    "DynElInputNumber": cfg("DynElInputNumber", {}, "数字"),
    "DynElPassword": cfg("DynElPassword", {"placeholder": "请输入密码"}, "密码"),
    "DynElTimePicker": cfg("DynElTimePicker", {"placeholder": "选择时间"}, "时间"),
    "DynText": cfg("DynText", {"text": "示例文本"}),
}

for name, c in updates.items():
    cur.execute("UPDATE ComponentMeta SET DefaultConfigJson = ? WHERE ComponentName = ?",
                (json.dumps(c, ensure_ascii=False), name))
    print(f"Updated {name}: {cur.rowcount} rows")

conn.commit()
conn.close()
print("Done!")
