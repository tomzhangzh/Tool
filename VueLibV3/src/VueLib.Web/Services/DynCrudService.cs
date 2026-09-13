using System.Data;
using System.Text.Json;
using SqlSugar;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>
/// 动态 CRUD 服务 —— 无需 model，给定 table 名即可增删改查、分页排序。
/// 通过 SqlSugar 自动探测主键、列信息，使用 ADO 参数化 SQL 执行。
/// </summary>
public class DynCrudService
{
    private readonly BusinessDbFactory _factory;

    public DynCrudService(BusinessDbFactory factory)
    {
        _factory = factory;
    }

    /// <summary>把 JsonElement 递归转成 .NET 原生类型（ADO/SqlSugar 能直接用）</summary>
    private static object? FromJsonElement(object? val)
    {
        if (val is not JsonElement je) return val;
        return je.ValueKind switch
        {
            JsonValueKind.String => je.GetString(),
            JsonValueKind.Number => je.TryGetInt32(out var i) ? i :
                                   je.TryGetInt64(out var l) ? l :
                                   je.TryGetDecimal(out var d) ? d :
                                   je.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Object => je.EnumerateObject().ToDictionary(p => p.Name, p => FromJsonElement((object?)p.Value)),
            JsonValueKind.Array => je.EnumerateArray().Select(e => FromJsonElement((object?)e)).ToList(),
            _ => je.GetRawText()
        };
    }

    /// <summary>把字典里的 JsonElement 值全部转成原生类型（原地修改）</summary>
    private static void NormalizeDict(Dictionary<string, object?>? dict)
    {
        if (dict == null) return;
        var keys = dict.Keys.ToList();
        foreach (var k in keys)
            dict[k] = FromJsonElement(dict[k]);
    }

    /// <summary>从列信息中找主键列名</summary>
    private static string FindPk(ISqlSugarClient db, string tableName)
    {
        var cols = db.DbMaintenance.GetColumnInfosByTableName(tableName);
        var pk = cols.FirstOrDefault(c => c.IsPrimarykey);
        return pk?.DbColumnName ?? "Id";
    }

    /// <summary>校验列名防注入（只允许字母数字下划线）</summary>
    private static string SafeName(string name)
    {
        if (string.IsNullOrEmpty(name) || !System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            throw new ArgumentException($"非法标识符: {name}");
        return name;
    }

    /// <summary>获取表元数据（主键、列信息）</summary>
    public async Task<object> GetTableMetaAsync(string connectionString, string tableName)
    {
        using var db = _factory.Create(connectionString);
        var pk = FindPk(db, tableName);
        var columns = db.DbMaintenance.GetColumnInfosByTableName(tableName)
            .Select(c => new
            {
                columnName = c.DbColumnName,
                dataType = c.DataType,
                isNullable = c.IsNullable,
                isPrimary = c.IsPrimarykey,
                isIdentity = c.IsIdentity,
                maxLength = c.Length,
                columnDescription = c.ColumnDescription
            }).ToList();
        return await Task.FromResult(new { tableName, primaryKey = pk, columns });
    }

    /// <summary>分页查询（动态 ADO SQL）</summary>
    public async Task<PagedResult<Dictionary<string, object?>>> QueryAsync(string connectionString, DynCrudQuery q)
    {
        using var db = _factory.Create(connectionString);
        NormalizeDict(q.Filter);
        var pk = SafeName(FindPk(db, q.TableName));
        var table = SafeName(q.TableName);

        // 构建 WHERE
        var where = new List<string>();
        var pars = new List<SugarParameter>();

        if (q.Filter != null)
        {
            foreach (var kv in q.Filter)
            {
                if (kv.Value == null || string.IsNullOrEmpty(kv.Value.ToString())) continue;
                var col = SafeName(kv.Key);
                where.Add($"[{col}] = @{col}");
                pars.Add(new SugarParameter($"@{col}", kv.Value));
            }
        }

        if (!string.IsNullOrEmpty(q.Keyword) && q.KeywordColumns?.Any() == true)
        {
            var kwConds = q.KeywordColumns.Select(col =>
            {
                var c = SafeName(col);
                return $"[{c}] LIKE @kw";
            }).ToList();
            where.Add("(" + string.Join(" OR ", kwConds) + ")");
            pars.Add(new SugarParameter("@kw", $"%{q.Keyword}%"));
        }

        var whereSql = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";

        // 排序
        var orderBy = SafeName(string.IsNullOrEmpty(q.OrderBy) ? pk : q.OrderBy);
        var orderDir = string.Equals(q.OrderDir, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        // Count
        var countSql = $"SELECT COUNT(1) FROM [{table}]{whereSql}";
        var total = Convert.ToInt64(await db.Ado.GetScalarAsync(countSql, pars.ToArray()));

        // Paged query
        var offset = (q.PageIndex - 1) * q.PageSize;
        var pageSql = $@"
SELECT * FROM (
    SELECT ROW_NUMBER() OVER (ORDER BY [{orderBy}] {orderDir}) AS _rn, *
    FROM [{table}]{whereSql}
) t WHERE t._rn > {offset} AND t._rn <= {offset + q.PageSize}";

        var dt = await db.Ado.GetDataTableAsync(pageSql, pars.ToArray());
        var rows = new List<Dictionary<string, object?>>();
        foreach (DataRow row in dt.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName == "_rn") continue;
                dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }
            rows.Add(dict);
        }

        return new PagedResult<Dictionary<string, object?>>
        {
            Rows = rows,
            TotalCount = total,
            PageIndex = q.PageIndex,
            PageSize = q.PageSize,
            TotalPages = (int)Math.Ceiling((double)total / q.PageSize)
        };
    }

    /// <summary>新增或更新（按主键自动判断）</summary>
    public async Task<object> SaveAsync(string connectionString, DynCrudSave save)
    {
        using var db = _factory.Create(connectionString);
        NormalizeDict(save.Data);
        var pk = SafeName(string.IsNullOrEmpty(save.PrimaryKey) ? FindPk(db, save.TableName) : save.PrimaryKey);
        var table = SafeName(save.TableName);

        if (save.Data.TryGetValue(pk, out var pkVal) && pkVal != null && !string.IsNullOrEmpty(pkVal.ToString()) && pkVal.ToString() != "0")
        {
            // UPDATE —— 构建 SET
            var setCols = save.Data.Where(kv => kv.Key != pk).ToList();
            var setSql = string.Join(", ", setCols.Select(kv => $"[{SafeName(kv.Key)}] = @{SafeName(kv.Key)}"));
            var pars = setCols.Select(kv => new SugarParameter($"@{SafeName(kv.Key)}", kv.Value ?? DBNull.Value)).ToList();
            pars.Add(new SugarParameter($"@{pk}", pkVal));
            var sql = $"UPDATE [{table}] SET {setSql} WHERE [{pk}] = @{pk}";
            await db.Ado.ExecuteCommandAsync(sql, pars.ToArray());
            return await Task.FromResult(new { id = pkVal, isNew = false });
        }
        else
        {
            // INSERT —— 去掉自增主键
            var data = save.Data.Where(kv => kv.Key != pk).ToList();
            var cols = string.Join(", ", data.Select(kv => $"[{SafeName(kv.Key)}]"));
            var vals = string.Join(", ", data.Select(kv => $"@{SafeName(kv.Key)}"));
            var pars = data.Select(kv => new SugarParameter($"@{SafeName(kv.Key)}", kv.Value ?? DBNull.Value)).ToList();
            var sql = $"INSERT INTO [{table}] ({cols}) OUTPUT INSERTED.[{pk}] VALUES ({vals})";
            var newId = Convert.ToInt64(await db.Ado.GetScalarAsync(sql, pars.ToArray()));
            return await Task.FromResult(new { id = newId, isNew = true });
        }
    }

    /// <summary>删除</summary>
    public async Task DeleteAsync(string connectionString, DynCrudDelete del)
    {
        using var db = _factory.Create(connectionString);
        del.Id = FromJsonElement(del.Id);
        var pk = SafeName(string.IsNullOrEmpty(del.PrimaryKey) ? FindPk(db, del.TableName) : del.PrimaryKey);
        var table = SafeName(del.TableName);
        var sql = $"DELETE FROM [{table}] WHERE [{pk}] = @pk";
        await db.Ado.ExecuteCommandAsync(sql, new SugarParameter("@pk", del.Id));
    }

    /// <summary>获取单条</summary>
    public async Task<Dictionary<string, object?>?> GetByIdAsync(string connectionString, string tableName, object id)
    {
        using var db = _factory.Create(connectionString);
        var pk = SafeName(FindPk(db, tableName));
        var table = SafeName(tableName);
        var sql = $"SELECT * FROM [{table}] WHERE [{pk}] = @id";
        var dt = await db.Ado.GetDataTableAsync(sql, new SugarParameter("@id", id));
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        var dict = new Dictionary<string, object?>();
        foreach (DataColumn col in dt.Columns)
            dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
        return dict;
    }

    /// <summary>列出业务库所有表（用于设计器选表）</summary>
    public List<string> GetTables(string connectionString)
    {
        using var db = _factory.Create(connectionString);
        return db.DbMaintenance.GetTableInfoList().Select(t => t.Name).ToList();
    }
}
