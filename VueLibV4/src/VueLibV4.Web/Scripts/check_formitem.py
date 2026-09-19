import sqlite3, json
db = r'E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\bin\Debug\net8.0\App_Data\Platform.db'
conn = sqlite3.connect(db)
cur = conn.cursor()
cur.execute("SELECT ComponentName, IsActive, LoadUrl, ViewPath, ExtJson, TemplateContent FROM ComponentMeta WHERE ComponentName='DynElFormItem'")
row = cur.fetchone()
if row:
    print('ComponentName:', row[0])
    print('IsActive:', row[1])
    print('LoadUrl:', row[2])
    print('ViewPath:', row[3])
    print('ExtJson:', (row[4] or '')[:500])
    print('TemplateContent:', (row[5] or '')[:300])
else:
    print('Not found')
conn.close()
