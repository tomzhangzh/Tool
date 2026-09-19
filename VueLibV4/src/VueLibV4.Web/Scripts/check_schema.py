import sqlite3
db = r'E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\bin\Debug\net8.0\App_Data\Platform.db'
conn = sqlite3.connect(db)
cur = conn.cursor()
# Check table schema
cur.execute("PRAGMA table_info(ComponentMeta)")
for row in cur.fetchall():
    print(row)
conn.close()
