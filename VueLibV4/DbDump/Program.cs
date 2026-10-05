using Microsoft.Data.Sqlite;
using System.Text;

var dbPath = args[0];
var outPath = args[1];
var sb = new StringBuilder();

using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

sb.AppendLine("-- Platform.db dump");
sb.AppendLine("-- Generated: " + DateTime.Now);
sb.AppendLine("BEGIN TRANSACTION;");
sb.AppendLine();

var tables = new List<string>();
using (var cmd = conn.CreateCommand())
{
    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
    using var reader = cmd.ExecuteReader();
    while (reader.Read()) tables.Add(reader.GetString(0));
}

foreach (var table in tables)
{
    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = $"SELECT sql FROM sqlite_master WHERE type='table' AND name='{table}'";
        var sql = cmd.ExecuteScalar()?.ToString();
        if (!string.IsNullOrEmpty(sql))
        {
            sb.AppendLine($"-- Table: {table}");
            sb.AppendLine($"DROP TABLE IF EXISTS [{table}];");
            sb.AppendLine(sql + ";");
            sb.AppendLine();
        }
    }

    using (var cmd = conn.CreateCommand())
    {
        cmd.CommandText = $"SELECT * FROM [{table}]";
        using var reader = cmd.ExecuteReader();
        var cols = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++) cols[i] = reader.GetName(i);

        int count = 0;
        while (reader.Read())
        {
            var values = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var v = reader.IsDBNull(i) ? "NULL" : FormatValue(reader.GetValue(i));
                values[i] = v;
            }
            sb.AppendLine($"INSERT INTO [{table}] ({string.Join(",", cols.Select(c => $"[{c}]"))}) VALUES ({string.Join(",", values)});");
            count++;
        }
        sb.AppendLine($"-- {count} rows");
        sb.AppendLine();
    }
}

sb.AppendLine("COMMIT;");
File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
Console.WriteLine($"Exported: {outPath} ({new FileInfo(outPath).Length} bytes)");

static string FormatValue(object v)
{
    if (v is string s) return $"'{s.Replace("'", "''")}'";
    if (v is bool b) return b ? "1" : "0";
    if (v is DateTime dt) return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
    return v.ToString() ?? "NULL";
}
