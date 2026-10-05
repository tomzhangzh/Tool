using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using VueLibV4.Web.Core;

namespace VueLibV4.Web.Areas.Platform.Controllers;

/// <summary>
/// 数据库元数据 API：ER 关系图用。读 SQLite 系统表 + DynSchemaLabel 中文字段名。
/// </summary>
[Area("Platform")]
[Route("api/platform/dbmeta")]
[ApiController]
public class DbMetaController : ControllerBase
{
    private readonly IConfiguration _config;
    public DbMetaController(IConfiguration config) { _config = config; }

    private SqliteConnection OpenConn(string conn)
    {
        var key = conn == "business" ? "BusinessDb" : "PlatformDb";
        var cs = _config.GetConnectionString(key);
        var bld = new SqliteConnectionStringBuilder(cs);
        if (!Path.IsPathRooted(bld.DataSource))
            bld.DataSource = Path.Combine(AppContext.BaseDirectory, bld.DataSource.Replace('/', Path.DirectorySeparatorChar));
        var c = new SqliteConnection(bld.ConnectionString);
        c.Open();
        return c;
    }

    /// <summary>列所有表</summary>
    [HttpGet("tables")]
    public ApiResult Tables(string conn = "platform")
    {
        var path = "";
        using var c = OpenConn(conn);
        var list = new List<string>();
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetString(0));
        }
        return ApiResult.Ok(list);
    }

    /// <summary>
    /// 选中表 + 一级关联表 + 外键关系 + 字段中文名。
    /// 返回 { nodes:[{id,label,fields,tableName}], edges:[{source,target,label}] }
    /// </summary>
    [HttpGet("er")]
    public ApiResult Er(string conn = "platform", string table = "")
    {
        using var c = OpenConn(conn);

        // 收集全库表 + 全部外键
        var allTables = new List<string>();
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
            using var r = cmd.ExecuteReader();
            while (r.Read()) allTables.Add(r.GetString(0));
        }

        var fks = new List<(string fromTable, string fromCol, string toTable, string toCol)>();
        foreach (var t in allTables)
        {
            foreach (var fk in FkList(c, t))
            {
                if (allTables.Contains(fk.toTable))
                    fks.Add((t, fk.fromCol, fk.toTable, fk.toCol));
            }
        }

        // 命名约定推断：字段 XxxId 且存在表 Xxx（Id 本身不算）
        var fkSet = new HashSet<string>(fks.Select(f => $"{f.fromTable}.{f.fromCol}->{f.toTable}.{f.toCol}"));
        foreach (var t in allTables)
        {
            var colNames = new List<string>();
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info(\"{Q(t)}\")";
                using var r = cmd.ExecuteReader();
                while (r.Read()) colNames.Add(r.GetString(1));
            }
            foreach (var colName in colNames)
            {
                if (colName.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;
                if (!colName.EndsWith("Id", StringComparison.OrdinalIgnoreCase)) continue;
                var refTable = colName.Substring(0, colName.Length - 2);
                // 尝试多种前缀组合：ProjectId -> Project / DynProject
                var candidates = new List<string> { refTable, "Dyn" + refTable };
                if (refTable.EndsWith('s')) candidates.Add(refTable.TrimEnd('s'));
                candidates.Add("Dyn" + refTable.TrimEnd('s'));
                string? found = null;
                foreach (var cand in candidates)
                {
                    if (allTables.Contains(cand)) { found = cand; break; }
                }
                if (found == null) continue;
                if (found == t) continue;
                var key = $"{t}.{colName}->{found}.Id";
                if (fkSet.Add(key))
                    fks.Add((t, colName, found, "Id"));
            }
        }

        var labels = LoadLabels(c);

        // 整库模式：table == "__all__"
        if (table == "__all__")
        {
            var allNodes = new List<object>();
            foreach (var t in allTables)
            {
                var cols = Cols(c, t, labels);
                allNodes.Add(new { id = "tbl_" + t, tableName = t, isRoot = false, fields = cols });
            }
            var allEdges = fks.Select(f => (object)new
            {
                id = "e_" + Guid.NewGuid().ToString("N"),
                source = "tbl_" + f.fromTable,
                sourceCol = f.fromCol,
                target = "tbl_" + f.toTable,
                targetCol = f.toCol,
                label = f.fromCol + " → " + f.toCol
            }).ToList();
            return ApiResult.Ok(new { nodes = allNodes, edges = allEdges });
        }

        if (string.IsNullOrWhiteSpace(table)) return ApiResult.Fail("缺少 table");

        // 单表模式：一级关联
        var related = new HashSet<string> { table };
        foreach (var f in fks.Where(f => f.fromTable == table))
        {
            related.Add(f.toTable);
        }
        foreach (var f in fks.Where(f => f.toTable == table))
        {
            related.Add(f.fromTable);
        }
        var filteredFks = fks.Where(f => related.Contains(f.fromTable) && related.Contains(f.toTable)).ToList();

        var nodes = new List<object>();
        foreach (var t in related)
        {
            var cols = Cols(c, t, labels);
            nodes.Add(new { id = "tbl_" + t, tableName = t, isRoot = t == table, fields = cols });
        }
        var edges = filteredFks.Select(f => (object)new
        {
            id = "e_" + Guid.NewGuid().ToString("N"),
            source = "tbl_" + f.fromTable,
            sourceCol = f.fromCol,
            target = "tbl_" + f.toTable,
            targetCol = f.toCol,
            label = f.fromCol + " → " + f.toCol
        }).ToList();
        return ApiResult.Ok(new { nodes, edges });
    }

    private static List<(string fromCol, string toTable, string toCol)> FkList(SqliteConnection c, string table)
    {
        var result = new List<(string, string, string)>();
        using var cmd = c.CreateCommand();
        cmd.CommandText = $"PRAGMA foreign_key_list(\"{Q(table)}\")";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            // columns: id, seq, table, from, to, on_update, on_delete, match
            // 引用目标表主键时 to 列可能为 NULL，默认回退到 Id
            var fromCol = r.GetString(3);
            var toTable = r.GetString(2);
            var toCol = r.IsDBNull(4) ? "Id" : r.GetString(4);
            result.Add((fromCol, toTable, toCol));
        }
        return result;
    }

    /// <summary>SQLite 标识符转义：双引号双写（PRAGMA 不支持参数化）</summary>
    private static string Q(string name) => name.Replace("\"", "\"\"");

    private static List<object> Cols(SqliteConnection c, string table, Dictionary<string, string> labels)
    {
        var result = new List<object>();
        using var cmd = c.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{Q(table)}\")";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var colName = r.GetString(1);
            var type = r.GetString(2);
            var pk = r.GetInt32(5) > 0;
            var label = labels.TryGetValue($"{table}.{colName}", out var l1) ? l1
                      : (labels.TryGetValue(colName, out var l2) ? l2 : "");
            result.Add(new { name = colName, type, pk, label });
        }
        return result;
    }

    private Dictionary<string, string> LoadLabels(SqliteConnection c)
    {
        // 读 DynSchemaLabel（在 Platform.db；业务库没有就空）
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT TableName, ColumnName, Label FROM DynSchemaLabel";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var tn = r.GetString(0) ?? "";
                var cn = r.GetString(1) ?? "";
                var lb = r.GetString(2) ?? "";
                if (string.IsNullOrEmpty(tn)) dict[cn] = lb;       // 全局默认
                else dict[$"{tn}.{cn}"] = lb;
            }
        }
        catch { /* 业务库没有这张表，忽略 */ }
        return dict;
    }

    /// <summary>保存字段中文注释（upsert DynSchemaLabel）</summary>
    [HttpPost("label")]
    public ApiResult SaveLabel([FromQuery] string conn, [FromQuery] string table, [FromQuery] string col, [FromQuery] string label)
    {
        if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(col)) return ApiResult.Fail("缺少 table/col");
        using var c = OpenConn(conn);
        using var cmd = c.CreateCommand();
        cmd.CommandText = @"
INSERT INTO DynSchemaLabel (ProjectId, TableName, ColumnName, Label, CreateTime, UpdateTime)
VALUES (0, @tn, @cn, @lb, datetime('now','localtime'), datetime('now','localtime'))
ON CONFLICT(ProjectId, TableName, ColumnName) DO UPDATE SET Label=@lb, UpdateTime=datetime('now','localtime')";
        cmd.Parameters.AddWithValue("@tn", table);
        cmd.Parameters.AddWithValue("@cn", col);
        cmd.Parameters.AddWithValue("@lb", label ?? "");
        cmd.ExecuteNonQuery();
        return ApiResult.Ok(true);
    }
}
