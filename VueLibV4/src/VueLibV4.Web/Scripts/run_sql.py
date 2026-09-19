import sqlite3
db = r'E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\bin\Debug\net8.0\App_Data\Platform.db'
conn = sqlite3.connect(db)
cur = conn.cursor()
sql = open(r'E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\Scripts\update_property_config.sql', 'r', encoding='utf-8').read()
cur.executescript(sql)
conn.commit()
cur.execute("SELECT ComponentName, CASE WHEN PropertyConfigJson IS NULL THEN 'NULL' ELSE 'OK' END FROM ComponentMeta WHERE Platform='ElementUI' ORDER BY ComponentName")
for row in cur.fetchall():
    print(row)
conn.close()
print('Done')
