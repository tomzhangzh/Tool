using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Data;
using System.Text;

namespace VueLibV4.Services.Data;

/// <summary>列元信息</summary>
public class ColumnInfo
{
    public string Name { get; set; }
    public string DataType { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsNullable { get; set; }
    public int Length { get; set; }
    public string Description { get; set; }
}

/// <summary>分页结果（JSON 统一小写命名，前端 DynCrudPage 直接读 rows/total/page/size）</summary>
public class PageResult
{
    [Newtonsoft.Json.JsonProperty("total")] public int Total { get; set; }
    [Newtonsoft.Json.JsonProperty("page")] public int Page { get; set; }
    [Newtonsoft.Json.JsonProperty("size")] public int Size { get; set; }
    [Newtonsoft.Json.JsonProperty("rows")] public List<JObject> Rows { get; set; } = new();
}

/// <summary>
/// 免模型动态 CRUD 服务：
/// 只要知道表名，通过 SqlSugar DbMaintenance 读取主键/字段元数据即可完成 增删改查、分页、排序、过滤。
/// 平台库与业务库通用。列类型规则：
///   字符串 → NVARCHAR（按字符串处理）；BIGINT/INT/BIT/DATETIME2/DECIMAL 保持原生类型，
///   写入时按列 DataType 自动转换（前端传入的字符串值转为目标类型）。
/// </summary>
public class DynamicCrudService
{
    // ---------------- 元数据 ----------------

    public List<string> Tables(SqlSugarClient db)
        => db.DbMaintenance.GetTableInfoList(false).Select(t => t.Name).OrderBy(x => x).ToList();

    public List<ColumnInfo> Columns(SqlSugarClient db, string table)
    {
        EnsureTable(db, table);
        return db.DbMaintenance.GetColumnInfosByTableName(table, false)
            .Select(c => new ColumnInfo
            {
                Name = c.DbColumnName,
                DataType = c.DataType,
                IsPrimaryKey = c.IsPrimarykey,
                IsNullable = c.IsNullable,
                Length = c.Length,
                Description = c.ColumnDescription
            })
            .ToList();
    }

    public List<string> PrimaryKeys(SqlSugarClient db, string table)
    {
        EnsureTable(db, table);
        return db.DbMaintenance.GetColumnInfosByTableName(table, false)
            .Where(c => c.IsPrimarykey)
            .Select(c => c.DbColumnName)
            .ToList();
    }

    /// <summary>主键已改为自增 BIGINT 后，前端常以业务 Code 引用：数字→按 Id 查；否则按 Code/DictCode 查</summary>
    public JObject FirstByIdOrCode(SqlSugarClient db, string table, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (long.TryParse(key, out var id))
            return First(db, table, "[Id]=@id", new { id });
        var cols = db.DbMaintenance.GetColumnInfosByTableName(table, false);
        var codeCol = cols.FirstOrDefault(c => string.Equals(c.DbColumnName, "Code", StringComparison.OrdinalIgnoreCase));
        if (codeCol != null)
            return First(db, table, "[Code]=@code", new { code = key });
        var dictCodeCol = cols.FirstOrDefault(c => string.Equals(c.DbColumnName, "DictCode", StringComparison.OrdinalIgnoreCase));
        if (dictCodeCol != null)
            return First(db, table, "[DictCode]=@dc", new { dc = key });
        return null;
    }

    /// <summary>表列名 → 列信息（含 DataType / IsIdentity）</summary>
    private static Dictionary<string, DbColumnInfo> ColumnMap(SqlSugarClient db, string table)
        => db.DbMaintenance.GetColumnInfosByTableName(table, false)
            .ToDictionary(c => c.DbColumnName, StringComparer.OrdinalIgnoreCase);

    // ---------------- 查询 ----------------

    public List<JObject> Query(SqlSugarClient db, string table, string whereSql = null, object whereParams = null, string orderBy = null)
    {
        EnsureTable(db, table);
        var q = db.Queryable<dynamic>().AS(table);
        if (!string.IsNullOrWhiteSpace(whereSql))
        {
            var pars = ToSugarParams(whereParams);
            q = pars.Length > 0 ? q.Where(whereSql, pars) : q.Where(whereSql);
        }
        if (!string.IsNullOrWhiteSpace(orderBy)) q = q.OrderBy(orderBy);
        return DataTableToJson(q.ToDataTable());
    }

    public JObject First(SqlSugarClient db, string table, string whereSql, object whereParams)
        => Query(db, table, whereSql, whereParams).FirstOrDefault();

    // ---------------- 分页 + 过滤 + 排序 ----------------

    /// <summary>
    /// 过滤器格式（JObject）：
    ///   { "Name": "张三", "Age": {"op":"gt","value":"18"}, "ClassName": {"op":"like","value":"一"}, "__orderby":"CreateTime desc" }
    /// 支持的 op：eq / neq / gt / ge / lt / le / like / notlike / in / between
    /// </summary>
    public PageResult Page(SqlSugarClient db, string table, int page, int size, JObject filter)
    {
        EnsureTable(db, table);
        page = page < 1 ? 1 : page;
        size = size < 1 ? 20 : Math.Min(size, 200);

        var colTypes = db.DbMaintenance.GetColumnInfosByTableName(table, false)
            .ToDictionary(c => c.DbColumnName, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        var pars = new List<SugarParameter>();
        var sb = new StringBuilder(" 1=1 ");
        if (filter != null)
        {
            foreach (var prop in filter.Properties())
            {
                if (prop.Name.StartsWith("__")) continue;
                if (!colTypes.TryGetValue(prop.Name, out var dt)) continue;
                var val = prop.Value;
                if (val.Type == JTokenType.Object)
                {
                    // filter 还原语义：{field:{op,value}} 中 value 为空(null/Undefined) 表示"该字段不参与筛选"，
                    // 直接跳过，避免生成 [col] = NULL / LIKE '%%' 这类误筛条件（前端完整结构原样提交时字段恒为 null 属常态）
                    var fv = val["value"];
                    if (fv == null || fv.Type == JTokenType.Null || fv.Type == JTokenType.Undefined) continue;
                    var op = val["op"]?.ToString() ?? "eq";
                    AppendOp(sb, pars, prop.Name, dt, op, fv);
                }
                else if (val.Type == JTokenType.Null || val.Type == JTokenType.Undefined)
                {
                    sb.Append($" AND [{prop.Name}] IS NULL ");
                }
                else
                {
                    var p = $"@p{pars.Count}";
                    sb.Append($" AND [{prop.Name}] = {p} ");
                    pars.Add(new SugarParameter(p, ToDbValue(val, dt)));
                }
            }
        }

        var whereSql = sb.ToString();
        var total = db.Queryable<dynamic>().AS(table).Where(whereSql, pars.ToArray()).Count();
        var rows = db.Queryable<dynamic>().AS(table)
            .Where(whereSql, pars.ToArray())
            .OrderBy(BuildOrderBy(db, table, filter))
            .Skip((page - 1) * size)
            .Take(size)
            .ToDataTable();

        return new PageResult { Total = total, Page = page, Size = size, Rows = DataTableToJson(rows) };
    }

    // ---------------- 增删改 ----------------

    /// <summary>新增；主键为自增 BIGINT 时返回新生成的 Id（前端拿到后二次更新用）</summary>
    public object Insert(SqlSugarClient db, string table, JObject data)
    {
        EnsureTable(db, table);
        var colInfos = ColumnMap(db, table);
        var dict = ToColumnDict(data, colInfos);
        if (colInfos.ContainsKey("CreateTime") && !dict.ContainsKey("CreateTime"))
            dict["CreateTime"] = DateTime.Now;
        if (colInfos.TryGetValue("Id", out var idCol) && idCol.IsIdentity)
        {
            db.InsertableByObject(dict).AS(table).ExecuteCommand();
            try
            {
                // 自增主键取回：SQLite 用 last_insert_rowid()，SQL Server 用 SCOPE_IDENTITY()
                var isSqlite = db.CurrentConnectionConfig.DbType == SqlSugar.DbType.Sqlite;
                var sql = isSqlite ? "SELECT last_insert_rowid()" : "SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";
                return db.Ado.GetLong(sql);
            }
            catch { return 1; }
        }
        return db.InsertableByObject(dict).AS(table).ExecuteCommand();
    }

    public int Update(SqlSugarClient db, string table, JObject data)
    {
        EnsureTable(db, table);
        var pks = PrimaryKeys(db, table);
        if (pks.Count == 0) throw new Exception($"表 {table} 没有主键，无法更新");
        var missing = pks.FirstOrDefault(p => data[p] == null || data[p].Type == JTokenType.Null);
        if (missing != null) throw new Exception($"更新缺少主键值: {missing}");
        var dict = ToColumnDict(data, ColumnMap(db, table));
        return db.UpdateableByObject(dict).AS(table).WhereColumns(pks.ToArray()).ExecuteCommand();
    }

    public int Delete(SqlSugarClient db, string table, JObject keys)
    {
        EnsureTable(db, table);
        var pks = PrimaryKeys(db, table);
        if (pks.Count == 0) throw new Exception($"表 {table} 没有主键，无法删除");
        var colInfos = ColumnMap(db, table);
        var conds = new List<string>();
        var pars = new List<SugarParameter>();
        foreach (var pk in pks)
        {
            var v = keys[pk];
            if (v == null || v.Type == JTokenType.Null) throw new Exception($"删除缺少主键值: {pk}");
            var p = $"@d{pars.Count}";
            conds.Add($"[{pk}] = {p}");
            colInfos.TryGetValue(pk, out var ci);
            pars.Add(new SugarParameter(p, ToDbValue(v, ci?.DataType)));
        }
        var where = string.Join(" AND ", conds);
        return db.Ado.ExecuteCommand($"DELETE FROM [{table}] WHERE {where}", pars.ToArray());
    }

    // ---------------- 辅助 ----------------

    /// <summary>匿名对象/单参/SugarParameter 数组 → SugarParameter[]（SqlSugar dynamic 实体需要显式参数数组）</summary>
    private static SugarParameter[] ToSugarParams(object whereParams)
    {
        if (whereParams == null) return Array.Empty<SugarParameter>();
        if (whereParams is SugarParameter[] arr) return arr;
        if (whereParams is SugarParameter one) return new[] { one };
        var props = whereParams.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var list = new List<SugarParameter>();
        foreach (var p in props)
        {
            var v = p.GetValue(whereParams);
            list.Add(new SugarParameter("@" + p.Name, v));
        }
        return list.ToArray();
    }

    private void AppendOp(StringBuilder sb, List<SugarParameter> pars, string col, string dataType, string op, JToken v)
    {
        switch (op.ToLower())
        {
            case "eq":
                AddParam(sb, pars, $" AND [{col}] = {{p}} ", ToDbValue(v, dataType));
                break;
            case "neq":
                AddParam(sb, pars, $" AND [{col}] <> {{p}} ", ToDbValue(v, dataType));
                break;
            case "gt":
                AddParam(sb, pars, $" AND [{col}] > {{p}} ", ToDbValue(v, dataType));
                break;
            case "ge":
                AddParam(sb, pars, $" AND [{col}] >= {{p}} ", ToDbValue(v, dataType));
                break;
            case "lt":
                AddParam(sb, pars, $" AND [{col}] < {{p}} ", ToDbValue(v, dataType));
                break;
            case "le":
                AddParam(sb, pars, $" AND [{col}] <= {{p}} ", ToDbValue(v, dataType));
                break;
            case "like":
                AddParam(sb, pars, $" AND [{col}] LIKE {{p}} ", "%" + (v?.ToString() ?? "") + "%");
                break;
            case "notlike":
                AddParam(sb, pars, $" AND [{col}] NOT LIKE {{p}} ", "%" + (v?.ToString() ?? "") + "%");
                break;
            case "in":
                if (v is JArray arr)
                {
                    var names = new List<string>();
                    foreach (var item in arr)
                    {
                        var p = $"@p{pars.Count}";
                        names.Add(p);
                        pars.Add(new SugarParameter(p, ToDbValue(item, dataType)));
                    }
                    if (names.Count > 0)
                        sb.Append($" AND [{col}] IN ({string.Join(",", names)}) ");
                }
                break;
            case "between":
                if (v is JArray bt && bt.Count == 2)
                {
                    var p1 = $"@p{pars.Count}";
                    pars.Add(new SugarParameter(p1, ToDbValue(bt[0], dataType)));
                    var p2 = $"@p{pars.Count}";
                    pars.Add(new SugarParameter(p2, ToDbValue(bt[1], dataType)));
                    sb.Append($" AND [{col}] BETWEEN {p1} AND {p2} ");
                }
                break;
        }
    }

    private static void AddParam(StringBuilder sb, List<SugarParameter> pars, string sqlTemplate, object value)
    {
        var p = $"@p{pars.Count}";
        sb.Append(sqlTemplate.Replace("{p}", p));
        pars.Add(new SugarParameter(p, value));
    }

    private string BuildOrderBy(SqlSugarClient db, string table, JObject filter)
    {
        // __orderby 白名单：仅允许“真实列名 + asc/desc”单字段，防止 SQL 注入
        if (filter?["__orderby"] != null && !string.IsNullOrWhiteSpace(filter["__orderby"].ToString()))
        {
            var raw = filter["__orderby"].ToString().Trim();
            var match = System.Text.RegularExpressions.Regex.Match(
                raw, @"^\[?(?<col>[A-Za-z_][A-Za-z0-9_]*)\]?\s+(?<dir>asc|desc)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var col = match.Groups["col"].Value;
                var dir = match.Groups["dir"].Value.ToLower();
                var cols = db.DbMaintenance.GetColumnInfosByTableName(table, false)
                    .Select(c => c.DbColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (cols.Contains(col)) return $"[{col}] {dir}";
            }
            // 非法排序子句直接忽略（回退主键排序），不抛异常以免影响列表加载
        }
        var pks = PrimaryKeys(db, table);
        if (pks.Count > 0) return $"[{pks[0]}] desc";
        var first = db.DbMaintenance.GetColumnInfosByTableName(table, false).FirstOrDefault();
        return first != null ? $"[{first.DbColumnName}] asc" : "(SELECT 0)";
    }

    private static Dictionary<string, object> ToColumnDict(JObject data, Dictionary<string, DbColumnInfo> colInfos)
    {
        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (data == null) return dict;
        foreach (var prop in data.Properties())
        {
            if (!colInfos.TryGetValue(prop.Name, out var ci)) continue;
            dict[prop.Name] = ToDbValue(prop.Value, ci.DataType);
        }
        return dict;
    }

    /// <summary>按目标列 DataType 转换值：字符串→原样；BIGINT/INT/BIT/DATETIME2/DECIMAL → 原生类型；对象/数组→JSON 字符串</summary>
    private static object ToDbValue(JToken token, string dataType)
    {
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
            return null;
        var t = (dataType ?? "").ToLower();
        if (t.Contains("bigint"))
            return long.TryParse(token.ToString(), out var l) ? l : 0L;
        if (t.Contains("int"))
            return int.TryParse(token.ToString(), out var i) ? i : 0;
        if (t.Contains("bit"))
            return token.ToString() is "1" or "true" or "True" or "TRUE";
        if (t.Contains("decimal") || t.Contains("numeric") || t.Contains("money"))
            return decimal.TryParse(token.ToString(), out var d) ? d : 0m;
        if (t.Contains("float") || t.Contains("real"))
            return double.TryParse(token.ToString(), out var f) ? f : 0d;
        if (t.Contains("date") || t.Contains("time"))
            return DateTime.TryParse(token.ToString(), out var dt) ? dt : (object)DateTime.Now;
        if (token.Type == JTokenType.Object || token.Type == JTokenType.Array)
            return token.ToString(Newtonsoft.Json.Formatting.None);
        return token.ToString();
    }

    private void EnsureTable(SqlSugarClient db, string table)
    {
        if (string.IsNullOrWhiteSpace(table)) throw new Exception("表名不能为空");
        var names = db.DbMaintenance.GetTableInfoList(false).Select(t => t.Name);
        if (!names.Any(n => string.Equals(n, table, StringComparison.OrdinalIgnoreCase)))
            throw new Exception($"表 {table} 不存在");
    }

    public static List<JObject> DataTableToJson(DataTable dt)
    {
        var list = new List<JObject>();
        if (dt == null) return list;
        foreach (DataRow row in dt.Rows)
        {
            var obj = new JObject();
            foreach (DataColumn col in dt.Columns)
            {
                var v = row[col];
                obj[col.ColumnName] = v == null || v == DBNull.Value ? JValue.CreateNull() : new JValue(v);
            }
            list.Add(obj);
        }
        return list;
    }
}
