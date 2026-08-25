using System.Dynamic;
using SqlSugar;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>
/// 动态 CRUD：针对任意表名 + 页面定义做动态查询 / 分页 / 增删改。
/// 预览运行时连接"工程"指定的数据库，按表名动态操作（不依赖实体类）。
/// </summary>
public class DynCrudService
{
    /// <summary>获取表的列信息（列名/类型/是否主键/自增/可空）</summary>
    public List<DbColumnInfo> GetColumns(ISqlSugarClient db, string table)
        => db.DbMaintenance.GetColumnInfosByTableName(table);

    private HashSet<string> ColumnNames(ISqlSugarClient db, string table)
        => GetColumns(db, table).Select(c => c.DbColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>按主键取单条（返回字典）</summary>
    public Dictionary<string, object?> GetByPk(ISqlSugarClient db, string table, object? pk, string pkName)
    {
        if (pk == null || string.IsNullOrEmpty(pk.ToString())) return new();
        var row = db.Queryable(table, "t").Where($"[{pkName}]=@pk", new { pk }).First();
        return row == null ? new() : ToDict(row);
    }

    /// <summary>按页面定义 + 筛选字典分页查询</summary>
    public PagedResult<Dictionary<string, object?>> ListPaged(
        ISqlSugarClient db, string table, DynPageDefinition? def,
        Dictionary<string, object?>? filter, int pageIndex, int pageSize)
    {
        var cols = ColumnNames(db, table);
        filter ??= new();
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Max(1, pageSize);

        var q = db.Queryable(table, "t");
        var (where, ps) = BuildWhere(def, filter, cols);
        if (!string.IsNullOrEmpty(where)) q = q.Where(where, ps);

        var total = q.Count();

        var pkName = def != null && cols.Contains(def.PrimaryKey) ? def.PrimaryKey : "Id";
        var orderBy = pkName;
        var orderDir = "desc";
        if (def != null && !string.IsNullOrWhiteSpace(def.OrderBy) && cols.Contains(def.OrderBy))
        {
            orderBy = def.OrderBy;
            orderDir = string.Equals(def.OrderDir, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        }

        var rows = q.OrderBy($"[{orderBy}] {orderDir}").ToPageList(pageIndex, pageSize);

        return new PagedResult<Dictionary<string, object?>>
        {
            Rows = rows.Select(ToDict).ToList(),
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
    }

    /// <summary>新增，返回自增主键</summary>
    public object? Insert(ISqlSugarClient db, string table, Dictionary<string, object?> data, DynPageDefinition? def)
    {
        var clean = NormalizeForWrite(db, table, data);
        return db.Insertable(clean).AS(table).ExecuteReturnIdentity();
    }

    /// <summary>更新，按主键条件</summary>
    public int Update(ISqlSugarClient db, string table, Dictionary<string, object?> data, DynPageDefinition? def)
    {
        var clean = NormalizeForWrite(db, table, data);
        var pkName = def != null ? def.PrimaryKey : "Id";
        if (!clean.ContainsKey(pkName) || clean[pkName] == null) return 0;
        var pkVal = clean[pkName];
        clean.Remove(pkName);
        if (clean.Count == 0) return 1;
        return db.Updateable(clean).AS(table).Where($"[{pkName}]=@pk", new { pk = pkVal }).ExecuteCommand();
    }

    /// <summary>按主键删除</summary>
    public int Delete(ISqlSugarClient db, string table, object pk, string pkName)
        => db.Ado.ExecuteCommand($"DELETE FROM [{table}] WHERE [{pkName}]=@pk", new { pk });

    /// <summary>依据列类型把前端提交的字典值转换为正确 CLR 类型（空串→null 等）</summary>
    public Dictionary<string, object?> NormalizeForWrite(ISqlSugarClient db, string table, Dictionary<string, object?> data)
    {
        var cols = GetColumns(db, table);
        var map = cols.ToDictionary(c => c.DbColumnName, c => c, StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in data)
        {
            if (kv.Key.StartsWith("_")) continue;
            if (!map.TryGetValue(kv.Key, out var col)) continue;
            var val = kv.Value;
            // 主键无值（自增新增）跳过；数值 0 同样视为无主键
            if (col.IsPrimarykey && (val == null || string.IsNullOrEmpty(val.ToString()) || val.ToString() == "0")) continue;
            result[kv.Key] = ConvertValue(val, col.DataType ?? "");
        }
        return result;
    }

    private object? ConvertValue(object? val, string sqlType)
    {
        if (val == null) return null;
        var t = sqlType.ToLowerInvariant();
        if (val is string s)
        {
            if (s == "")
            {
                if (t.Contains("int") || t.Contains("decimal") || t.Contains("numeric") || t.Contains("money")
                    || t.Contains("float") || t.Contains("real") || t.Contains("datetime") || t == "date"
                    || t.Contains("time") || t == "bit" || t.Contains("uniqueidentifier"))
                    return null;
                return s;
            }
            if (t.Contains("datetime") || t == "date" || t.Contains("time") || t == "smalldatetime")
                return DateTime.TryParse(s, out var dt) ? dt : null;
            if (t == "bit")
                return s is "true" or "True" or "1" or "是" or "Y" or "y";
            if (t.Contains("tinyint") || t.Contains("smallint") || t.Contains("int") || t.Contains("bigint"))
                return long.TryParse(s, out var l) ? l : null;
            if (t.Contains("decimal") || t.Contains("numeric") || t.Contains("money"))
                return decimal.TryParse(s, out var d) ? d : null;
            if (t.Contains("float") || t.Contains("real"))
                return double.TryParse(s, out var db) ? db : null;
            if (t.Contains("uniqueidentifier"))
                return Guid.TryParse(s, out var g) ? g : null;
            return s;
        }
        return val;
    }

    /// <summary>按页面定义中的筛选列构建 WHERE（白名单 + 参数化）</summary>
    private (string, object) BuildWhere(DynPageDefinition? def, Dictionary<string, object?> filter, HashSet<string> cols)
    {
        var parts = new List<string>();
        var ps = new Dictionary<string, object>();
        if (def == null) return ("", ps);
        var i = 0;
        foreach (var col in def.Columns.Where(c => c.IsFilter))
        {
            if (!cols.Contains(col.Name)) continue;
            if (!filter.TryGetValue(col.Name, out var v) || v == null) continue;
            if (v is string s && string.IsNullOrWhiteSpace(s)) continue;
            var p = "p" + (i++);
            var colName = $"[{col.Name}]";
            switch (col.FilterOp)
            {
                case "like":
                case "start":
                case "end":
                    var raw = v.ToString() ?? "";
                    var esc = raw.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                    var pattern = col.FilterOp switch
                    {
                        "start" => esc + "%",
                        "end" => "%" + esc,
                        _ => "%" + esc + "%"
                    };
                    parts.Add($"({colName} LIKE @{p} ESCAPE '\\')");
                    ps[p] = pattern;
                    break;
                case "gt": parts.Add($"{colName} > @{p}"); ps[p] = v; break;
                case "ge": parts.Add($"{colName} >= @{p}"); ps[p] = v; break;
                case "lt": parts.Add($"{colName} < @{p}"); ps[p] = v; break;
                case "le": parts.Add($"{colName} <= @{p}"); ps[p] = v; break;
                case "ne": parts.Add($"{colName} <> @{p}"); ps[p] = v; break;
                default: parts.Add($"{colName} = @{p}"); ps[p] = v; break;
            }
        }
        return (string.Join(" AND ", parts), ps);
    }

    private static Dictionary<string, object?> ToDict(ExpandoObject eo)
    {
        var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in eo)
        {
            if (kv.Key.Equals("RowIndex", StringComparison.OrdinalIgnoreCase)) continue;
            d[kv.Key] = kv.Value;
        }
        return d;
    }
}
