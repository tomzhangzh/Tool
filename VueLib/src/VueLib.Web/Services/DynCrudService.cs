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

    /// <summary>
    /// 模板查询定义：查询字段（筛选）来自 Filter 屏定义，主键/排序/分页/导航来自 Summary 屏定义。
    /// filterDef 为空时回退用 summaryDef 自身的筛选列。
    /// </summary>
    public static DynPageDefinition? BuildQueryDef(DynPageDefinition? summaryDef, DynPageDefinition? filterDef)
    {
        if (summaryDef == null) return null;
        var qd = new DynPageDefinition
        {
            PrimaryKey = summaryDef.PrimaryKey,
            IsIdentity = summaryDef.IsIdentity,
            PageSize = summaryDef.PageSize,
            OrderBy = summaryDef.OrderBy,
            OrderDir = summaryDef.OrderDir,
            Navs = summaryDef.Navs
        };
        qd.Columns = filterDef != null && filterDef.Columns.Count > 0
            ? filterDef.Columns.ToList()
            : summaryDef.Columns.ToList();
        return qd;
    }

    /// <summary>按主键取单条（返回字典）；sourceName 为空则用 table，否则按真实视图/其它源读取</summary>
    public Dictionary<string, object?> GetByPk(ISqlSugarClient db, string table, object? pk, string pkName, string? sourceName = null)
    {
        if (pk == null || string.IsNullOrEmpty(pk.ToString())) return new();
        var from = string.IsNullOrWhiteSpace(sourceName) ? table : sourceName;
        var row = db.Queryable(from, "t").Where($"[{pkName}]=@pk", new { pk }).First();
        return row == null ? new() : ToDict(row);
    }

    /// <summary>按页面定义 + 筛选字典分页查询；sourceName 为空则用 table，否则按真实视图/其它源读取</summary>
    public PagedResult<Dictionary<string, object?>> ListPaged(
        ISqlSugarClient db, string table, DynPageDefinition? def,
        Dictionary<string, object?>? filter, int pageIndex, int pageSize, string? sourceName = null)
    {
        var from = string.IsNullOrWhiteSpace(sourceName) ? table : sourceName;
        var cols = ColumnNames(db, from);
        filter ??= new();
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Max(1, pageSize);

        var q = db.Queryable(from, "t");
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

        var result = new PagedResult<Dictionary<string, object?>>
        {
            Rows = rows.Select(ToDict).ToList(),
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize)
        };
        return result;
    }

    /// <summary>
    /// 外键导航：按页面定义 Navs 为每行注入 _nav = { NavKey: object|array }
    /// ManyToOne → 目标表主键 = 当前行外键值，注入单条（object）；无匹配为 null
    /// OneToMany → 目标表外键列 = 当前行主键值，注入列表（array）
    /// </summary>
    public void LoadNavs(ISqlSugarClient db, DynPageDefinition? def, IEnumerable<Dictionary<string, object?>> rows)
    {
        if (def == null || def.Navs == null || def.Navs.Count == 0 || rows == null) return;
        foreach (var row in rows)
        {
            if (row.ContainsKey("_nav")) continue;
            var nav = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var cfg in def.Navs)
            {
                if (string.IsNullOrWhiteSpace(cfg.TargetTable)) continue;
                var key = string.IsNullOrWhiteSpace(cfg.NavKey) ? cfg.TargetTable : cfg.NavKey;
                try
                {
                    if (cfg.Relation == NavRelation.ManyToOne)
                        nav[key] = LoadManyToOne(db, cfg, row);
                    else
                        nav[key] = LoadOneToMany(db, cfg, row, def);
                }
                catch { nav[key] = null; }
            }
            row["_nav"] = nav;
        }
    }

    private object? LoadManyToOne(ISqlSugarClient db, DynNavConfig cfg, Dictionary<string, object?> row)
    {
        var fkName = cfg.FkColumn;
        if (string.IsNullOrWhiteSpace(fkName) || !row.TryGetValue(fkName, out var fkVal) || fkVal == null) return null;
        var pkName = cfg.TargetPkColumn ?? FindPk(db, cfg.TargetTable);
        var t = db.Queryable(cfg.TargetTable, "t").Where($"[{pkName}]=@v", new { v = fkVal }).First();
        if (t == null) return null;
        return FilterColumns(ToDict(t), cfg);
    }

    private object? LoadOneToMany(ISqlSugarClient db, DynNavConfig cfg, Dictionary<string, object?> row, DynPageDefinition? def)
    {
        var fkName = cfg.TargetFkColumn;
        if (string.IsNullOrWhiteSpace(fkName)) return null;
        var pkName = def != null && !string.IsNullOrWhiteSpace(def.PrimaryKey) ? def.PrimaryKey : FindPk(db, cfg.TargetTable);
        if (string.IsNullOrWhiteSpace(pkName) || !row.TryGetValue(pkName, out var pkVal) || pkVal == null) return null;
        var list = db.Queryable(cfg.TargetTable, "t")
            .Where($"[{fkName}]=@v", new { v = pkVal })
            .OrderBy($"[{fkName}] DESC")
            .ToList();
        return list.Select(x => FilterColumns(ToDict(x), cfg)).ToList();
    }

    /// <summary>目标表主键列（按主键元数据识别，找不到用 Id）</summary>
    private string FindPk(ISqlSugarClient db, string table)
    {
        try
        {
            var pk = GetColumns(db, table).FirstOrDefault(c => c.IsPrimarykey);
            if (pk != null) return pk.DbColumnName;
        }
        catch { }
        return "Id";
    }

    private static Dictionary<string, object?> FilterColumns(Dictionary<string, object?> row, DynNavConfig cfg)
    {
        if (cfg.DisplayColumns == null || cfg.DisplayColumns.Count == 0) return row;
        var set = new HashSet<string>(cfg.DisplayColumns, StringComparer.OrdinalIgnoreCase);
        return row.Where(kv => set.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
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
