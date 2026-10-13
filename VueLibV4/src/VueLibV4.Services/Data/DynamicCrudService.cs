using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Collections.Concurrent;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

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

    /// <summary>
    /// 非致命告警（未知筛选字段 / 不支持的 op 等"写错了但被静默忽略"的情形）。
    /// 只在确有告警时序列化；前端收到后 console.warn / 调试浮层提示——让配置错误尽早暴露。
    /// </summary>
    [Newtonsoft.Json.JsonProperty("warnings", NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public List<string> Warnings { get; set; }
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
    // ---------------- 元数据缓存 ----------------
    // 表结构（列信息）很稳定，每次请求都查 information_schema 很贵。
    // 按 连接串+表名 缓存短 TTL（60s）：开发期改表最多延迟一分钟生效。
    private static readonly ConcurrentDictionary<string, (List<DbColumnInfo> cols, DateTime at)> _colCache = new();
    private static readonly TimeSpan _colCacheTtl = TimeSpan.FromSeconds(60);

    /// <summary>取表的原始列信息（带缓存）。返回 SqlSugar 原生 DbColumnInfo（含 IsIdentity 等）。</summary>
    private static List<DbColumnInfo> GetRawColumns(SqlSugarClient db, string table)
    {
        var key = (db.CurrentConnectionConfig.ConnectionString ?? "") + "||" + table;
        if (_colCache.TryGetValue(key, out var hit) && DateTime.Now - hit.at < _colCacheTtl)
            return hit.cols;
        var cols = db.DbMaintenance.GetColumnInfosByTableName(table, false);
        _colCache[key] = (cols, DateTime.Now);
        return cols;
    }

    /// <summary>清空列元数据缓存（改表结构后调用）。</summary>
    public static void ClearColumnCache() => _colCache.Clear();

    // ---------------- 元数据 ----------------

    public List<string> Tables(SqlSugarClient db)
        => db.DbMaintenance.GetTableInfoList(false).Select(t => t.Name).OrderBy(x => x).ToList();

    public List<ColumnInfo> Columns(SqlSugarClient db, string table)
    {
        EnsureTable(db, table);
        return GetRawColumns(db, table)
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
        return GetRawColumns(db, table)
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
        var cols = GetRawColumns(db, table);
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
        => GetRawColumns(db, table)
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

        var colTypes = GetRawColumns(db, table)
            .ToDictionary(c => c.DbColumnName, c => c.DataType, StringComparer.OrdinalIgnoreCase);

        var pars = new List<SugarParameter>();
        var sb = new StringBuilder(" 1=1 ");
        var warnings = new List<string>();
        if (filter != null)
        {
            foreach (var prop in filter.Properties())
            {
                if (prop.Name.StartsWith("__")) continue;
                if (!colTypes.TryGetValue(prop.Name, out var dt))
                {
                    // 失败变吵：筛选了不存在的列，旧实现静默跳过 → 条件"看起来生效了"实则全量
                    warnings.Add($"筛选字段 [{prop.Name}] 不在表 {table} 的列中，已忽略（检查表名/拼写/大小写）");
                    continue;
                }
                var val = prop.Value;
                if (val.Type == JTokenType.Object)
                {
                    // filter 还原语义：{field:{op,value}} 中 value 为空(null/Undefined) 表示"该字段不参与筛选"，
                    // 直接跳过，避免生成 [col] = NULL / LIKE '%%' 这类误筛条件（前端完整结构原样提交时字段恒为 null 属常态）
                    var fv = val["value"];
                    if (fv == null || fv.Type == JTokenType.Null || fv.Type == JTokenType.Undefined) continue;
                    var op = val["op"]?.ToString() ?? "eq";

                    // orFields：条件对象内数组，多字段共用同 op/value，字段间 OR，忽略外层 key
                    // 例：{ "tmp": {"op":"like","value":"fff","orFields":["Name","Code"]} } → (Name LIKE .. OR Code LIKE ..)
                    var orFieldsArr = val["orFields"] as JArray;
                    if (orFieldsArr != null && orFieldsArr.Count > 0)
                    {
                        var fields = orFieldsArr
                            .Select(t => t?.ToString()?.Trim())
                            .Where(f => !string.IsNullOrEmpty(f) && colTypes.ContainsKey(f))
                            .ToList();
                        if (fields.Count > 1)
                        {
                            var orSb = new StringBuilder();
                            foreach (var f in fields)
                            {
                                var itemSb = new StringBuilder();
                                AppendOp(itemSb, pars, f, colTypes[f], op, fv, warnings);
                                var seg = itemSb.ToString().Trim();
                                // 去掉单条件前导 "AND "，避免 OR 组内重复 AND
                                if (seg.StartsWith("AND ", StringComparison.OrdinalIgnoreCase))
                                    seg = seg.Substring(4).TrimStart();
                                if (orSb.Length > 0) orSb.Append(" OR ");
                                orSb.Append(seg);
                            }
                            var orStr = orSb.ToString().Trim();
                            if (orStr.Length > 0) sb.Append(" AND ( ").Append(orStr).Append(" ) ");
                        }
                        else if (fields.Count == 1)
                        {
                            AppendOp(sb, pars, fields[0], colTypes[fields[0]], op, fv, warnings);
                        }
                        // fields.Count == 0：orFields 内无任何有效字段 → 跳过该条件
                    }
                    else
                    {
                        // 无 orFields：沿用原有单字段条件（外层 key 即查询字段）
                        AppendOp(sb, pars, prop.Name, dt, op, fv, warnings);
                    }
                }
                else if (val.Type == JTokenType.Null || val.Type == JTokenType.Undefined)
                {
                    sb.Append($" AND {QuoteIdent(prop.Name)} IS NULL ");
                }
                else
                {
                    var p = $"@p{pars.Count}";
                    sb.Append($" AND {QuoteIdent(prop.Name)} = {p} ");
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

        return new PageResult
        {
            Total = total,
            Page = page,
            Size = size,
            Rows = DataTableToJson(rows),
            Warnings = warnings.Count > 0 ? warnings : null
        };
    }

    // ---------------- 增删改 ----------------

    /// <summary>新增；主键为自增 BIGINT 时返回新生成的 Id（前端拿到后二次更新用）</summary>
    public object Insert(SqlSugarClient db, string table, JObject data)
    {
        EnsureTable(db, table);
        var colInfos = ColumnMap(db, table);
        var dict = ToColumnDict(data, colInfos);
        // 自增列的空值（null/0/空串）必须剔除：SQLite 仅在自增列收到 NULL 时分配新 Id，
        // 显式插入 0 会真的落 0（前端新增常带 Id:0），第二次新增即主键冲突。
        foreach (var col in colInfos.Values.Where(c => c.IsIdentity))
        {
            if (!dict.TryGetValue(col.DbColumnName, out var v)) continue;
            if (v == null || v is 0 || v is long l && l == 0 || v is string s && string.IsNullOrWhiteSpace(s))
                dict.Remove(col.DbColumnName);
        }
        if (colInfos.ContainsKey("CreateTime") && !dict.ContainsKey("CreateTime"))
            dict["CreateTime"] = DateTime.Now;
        if (colInfos.TryGetValue("Id", out var idCol) && idCol.IsIdentity)
        {
            db.InsertableByObject(dict).AS(table).ExecuteCommand();
            // 同连接取回自增主键（SQLite last_insert_rowid / SqlServer SCOPE_IDENTITY）。
            // 失败必须抛错——原 catch{return 1} 会让前端拿着假 Id 做后续 update/delete。
            var isSqlite = db.CurrentConnectionConfig.DbType == SqlSugar.DbType.Sqlite;
            var sql = isSqlite ? "SELECT last_insert_rowid()" : "SELECT CAST(SCOPE_IDENTITY() AS BIGINT)";
            return db.Ado.GetLong(sql);
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
        // 防御：除主键外没有任何可更新列时，SQL 会生成 "UPDATE t SET WHERE ..." 裸语法异常。
        // 显式报友好错误（失败变吵），调用方一般是表单提交 data 异常为空。
        var updatable = dict.Keys.Where(k => !pks.Contains(k)).ToList();
        if (updatable.Count == 0)
            throw new Exception($"更新数据为空：除主键({string.Join(",", pks)})外没有任何字段");
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
            conds.Add($"{QuoteIdent(pk)} = {p}");
            colInfos.TryGetValue(pk, out var ci);
            pars.Add(new SugarParameter(p, ToDbValue(v, ci?.DataType)));
        }
        var where = string.Join(" AND ", conds);
        return db.Ado.ExecuteCommand($"DELETE FROM {QuoteIdent(table)} WHERE {where}", pars.ToArray());
    }

    // ---------------- 多对多关系 ----------------

    /// <summary>
    /// 多对多关系配置（前端提交，描述一个字段如何对应中间表）。
    /// 简单例：SysUser.RoleIds → SysUserRole(UserName=当前UserName, RoleId∈[1,2,3])
    /// 带额外列例：SysRole.Permissions → SysResourcePermission(RoleId,ResourceId,Read,Edit,Delete)
    ///   此时 data[fieldName] 为对象数组 [{ResourceId:1,Read:true,Edit:false,Delete:false}]，
    ///   ExtraFields=["Read","Edit","Delete"] 指定额外写入的列。
    /// </summary>
    public class RelationConfig
    {
        public string Table { get; set; }          // 中间表名
        public string LeftField { get; set; }      // 中间表左列（如 UserName / RoleId）
        public string LeftValueField { get; set; } // 主表取值列（如 UserName），缺省用主键
        public string RightField { get; set; }     // 中间表右列（如 RoleId / ResourceId）
        /// <summary>额外列名数组（如 Read/Edit/Delete）；为空时右值数组是纯 id 列表，非空时右值是对象数组</summary>
        public string[] ExtraFields { get; set; }
    }

    /// <summary>
    /// 保存主表 + 多对多中间表。在一个事务内：
    /// 1. Insert/Update 主表记录（仅提交主键、无其它列时视为"只维护关系"，跳过主表写入）
    /// 2. 对每个 relation 字段做"差量同步"（<see cref="SyncRelation"/>）：
    ///    提交集合视为目标完整状态，只删被移除的、只插新增的、只更新额外列变化的，
    ///    未变化的中间行原样保留（自增 Id / 审计列 / 触发器引用不被破坏）。
    /// 任一步失败整体回滚，不会出现主表已存、中间表半成的脏状态。
    /// </summary>
    public object SaveWithRelations(SqlSugarClient db, string table, JObject data, JObject relations)
    {
        EnsureTable(db, table);
        var pks = PrimaryKeys(db, table);
        if (pks.Count == 0) throw new Exception($"表 {table} 没有主键，无法保存关系");

        // 分离 relation 字段（不写主表列）。配置值必须是对象：
        // 前端误把数组直接塞进 relations 时静默跳过该字段，而不是 ToObject 抛 500。
        var mainData = (JObject)data.DeepClone();
        var relList = new List<(string fieldName, RelationConfig cfg)>();
        if (relations != null)
        {
            foreach (var prop in relations.Properties())
            {
                var cfgObj = prop.Value as JObject;
                if (cfgObj == null) continue;
                RelationConfig cfg;
                try { cfg = cfgObj.ToObject<RelationConfig>(); }
                catch { continue; }
                if (cfg == null || string.IsNullOrWhiteSpace(cfg.Table)
                    || string.IsNullOrWhiteSpace(cfg.LeftField) || string.IsNullOrWhiteSpace(cfg.RightField)) continue;
                relList.Add((prop.Name, cfg));
                mainData.Remove(prop.Name); // relation 字段不是主表列，从主表数据剔除
            }
        }

        var hasPk = pks.All(p => mainData[p] != null && mainData[p].Type != JTokenType.Null && IsUsablePk(mainData[p]));
        // 除主键外是否还有要写主表的列：没有就是"仅维护关系"（如角色权限页只提交 {Id}+Permissions）
        var hasMainColumns = mainData.Properties().Any(p => !pks.Contains(p.Name));

        object mainResult = hasPk ? mainData[pks[0]]?.Value<object>() : null;

        db.Ado.BeginTran();
        try
        {
            if (hasPk)
            {
                if (hasMainColumns) mainResult = Update(db, table, mainData);
                // else：只更新关系，主表不动
            }
            else
            {
                if (!hasMainColumns) throw new Exception("新增数据为空：既无主键也无任何字段");
                mainResult = Insert(db, table, mainData);
            }

            // 左值解析：更新时主键/业务键本就在提交数据里，直接用、省一次回查；
            // 提交数据里没有（典型：insert 后的自增 Id）才回查主记录，整单最多查一次。
            JObject mainRow = null;
            JObject GetMainRow()
            {
                if (mainRow != null) return mainRow;
                var kv = hasPk ? mainData[pks[0]]?.ToString() : mainResult?.ToString();
                mainRow = First(db, table, $"{QuoteIdent(pks[0])}=@v", new { v = kv });
                if (mainRow == null) throw new Exception("主表记录保存后回查失败，已中止关系写入");
                return mainRow;
            }
            string LeftValOf(string field)
            {
                var t = mainData[field];
                if (t != null && t.Type != JTokenType.Null && IsUsablePk(t)) return t.ToString();
                return GetMainRow()[field]?.ToString();
            }

            foreach (var (fieldName, cfg) in relList)
            {
                var leftValueField = string.IsNullOrWhiteSpace(cfg.LeftValueField) ? pks[0] : cfg.LeftValueField;
                var leftVal = LeftValOf(leftValueField);
                if (leftVal == null) continue;
                // 差量同步：只写变化的中间行，未变化行的 Id/审计列原样保留
                SyncRelation(db, cfg, leftVal, data[fieldName]);
            }
            db.Ado.CommitTran();
        }
        catch
        {
            db.Ado.RollbackTran();
            throw;
        }
        return mainResult;
    }

    /// <summary>宽松布尔转换：JSON bool / 0,1 / "true","false" 均可；无法识别一律 false。</summary>
    private static bool ToBool(JToken v)
    {
        if (v == null || v.Type == JTokenType.Null) return false;
        if (v.Type == JTokenType.Boolean) return v.Value<bool>();
        if (v.Type == JTokenType.Integer) return v.Value<long>() != 0;
        try { return v.ToObject<bool>(); } catch { return false; }
    }

    // 单条 SQL 的参数上限：SQLite 老版本 999、SQL Server 2100，统一保守取 900 分块。
    private const int SqlVarLimit = 900;

    /// <summary>
    /// 单个多对多关系的差量同步（须在外部事务内调用）。
    /// submitted 是该左值的目标完整集合：缺失/非数组 = 目标为空（清空，兼容旧契约）。
    /// 比对库内现状后分组落库：
    ///   删 —— 库里有、提交没有：一条 DELETE ... IN（分块）
    ///   增 —— 提交有、库里没有：一条多行 VALUES INSERT（分块）
    ///   改 —— 两边都有但额外列变化：每个变化列一条 CASE WHEN 批量 UPDATE（分块）
    /// 提交数组按右值去重（后出现的覆盖先出现的）；列名先过中间表 schema 预校验。
    /// </summary>
    private void SyncRelation(SqlSugarClient db, RelationConfig cfg, string leftVal, JToken submitted)
    {
        EnsureTable(db, cfg.Table);
        // 列名先过真实 schema：给友好错误，也让任何非白名单列名在拼 SQL 前失败
        var cols = ColumnMap(db, cfg.Table);
        if (!cols.ContainsKey(cfg.LeftField)) throw new Exception($"中间表 {cfg.Table} 不存在左列 {cfg.LeftField}");
        if (!cols.ContainsKey(cfg.RightField)) throw new Exception($"中间表 {cfg.Table} 不存在右列 {cfg.RightField}");
        var extras = cfg.ExtraFields ?? Array.Empty<string>();
        foreach (var ef in extras)
            if (!cols.ContainsKey(ef)) throw new Exception($"中间表 {cfg.Table} 不存在额外列 {ef}");
        bool hasExtra = extras.Length > 0;

        // 1. 解析目标集合：右值 → 额外列值（简单模式为空数组）；按右值去重，后者覆盖前者
        var target = new Dictionary<long, bool[]>();
        if (submitted is JArray arr)
        {
            foreach (var item in arr)
            {
                long rid;
                bool[] efVals;
                if (!hasExtra)
                {
                    if (!TryRid(item, out rid) || rid == 0) continue;
                    efVals = Array.Empty<bool>();
                }
                else
                {
                    var obj = item as JObject;
                    if (obj == null || !TryRid(obj[cfg.RightField], out rid) || rid == 0) continue;
                    efVals = extras.Select(ef => ToBool(obj[ef])).ToArray();
                }
                target[rid] = efVals;
            }
        }

        // 2. 读库内现状（一条 SQL）
        var existing = new Dictionary<long, bool[]>();
        var selectCols = hasExtra
            ? string.Join(", ", new[] { cfg.RightField }.Concat(extras).Select(QuoteIdent))
            : QuoteIdent(cfg.RightField);
        var dt = db.Ado.GetDataTable(
            $"SELECT {selectCols} FROM {QuoteIdent(cfg.Table)} WHERE {QuoteIdent(cfg.LeftField)} = @lv",
            new SugarParameter("@lv", leftVal));
        foreach (DataRow r in dt.Rows)
        {
            var rid = Convert.ToInt64(r[cfg.RightField]);
            existing[rid] = hasExtra
                ? extras.Select(ef => !r.IsNull(ef) && Convert.ToBoolean(r[ef])).ToArray()
                : Array.Empty<bool>();
        }

        // 3. diff 分组
        var toDelete = existing.Keys.Where(k => !target.ContainsKey(k)).ToList();
        var toInsert = new List<KeyValuePair<long, bool[]>>();
        var toUpdate = new List<KeyValuePair<long, bool[]>>();
        foreach (var kv in target)
        {
            if (!existing.TryGetValue(kv.Key, out var old))
                toInsert.Add(kv);
            else if (hasExtra && !old.AsSpan().SequenceEqual(kv.Value))
                toUpdate.Add(kv);
        }

        var t = QuoteIdent(cfg.Table);
        var lf = QuoteIdent(cfg.LeftField);
        var rf = QuoteIdent(cfg.RightField);

        // 4. 批量删：DELETE ... WHERE Left=@lv AND Right IN (...)
        for (int i = 0; i < toDelete.Count;)
        {
            var chunk = toDelete.Skip(i).Take(SqlVarLimit - 1).ToList();
            var pars = new List<SugarParameter> { new("@lv", leftVal) };
            var names = chunk.Select((rid, j) =>
            {
                pars.Add(new SugarParameter("@d" + j, rid));
                return "@d" + j;
            }).ToList();
            db.Ado.ExecuteCommand(
                $"DELETE FROM {t} WHERE {lf} = @lv AND {rf} IN ({string.Join(",", names)})",
                pars.ToArray());
            i += chunk.Count;
        }

        // 5. 批量增：多行 VALUES（@lv 全组共用，每行 @r/@e 唯一名）
        if (toInsert.Count > 0)
        {
            var colList = string.Join(", ", new[] { cfg.LeftField, cfg.RightField }.Concat(extras).Select(QuoteIdent));
            int perRow = 1 + extras.Length;
            int chunkSize = Math.Max(1, (SqlVarLimit - 1) / perRow);
            for (int i = 0; i < toInsert.Count;)
            {
                var chunk = toInsert.Skip(i).Take(chunkSize).ToList();
                var sb = new StringBuilder();
                sb.Append($"INSERT INTO {t} ({colList}) VALUES ");
                var pars = new List<SugarParameter> { new("@lv", leftVal) };
                for (int j = 0; j < chunk.Count; j++)
                {
                    sb.Append(j == 0 ? "(" : ",(");
                    sb.Append("@lv,@r").Append(j);
                    pars.Add(new SugarParameter("@r" + j, chunk[j].Key));
                    for (int k = 0; k < extras.Length; k++)
                    {
                        sb.Append(",@e").Append(j).Append('_').Append(k);
                        pars.Add(new SugarParameter("@e" + j + "_" + k, chunk[j].Value[k]));
                    }
                    sb.Append(')');
                }
                db.Ado.ExecuteCommand(sb.ToString(), pars.ToArray());
                i += chunk.Count;
            }
        }

        // 6. 批量改：每个变化列一条 CASE WHEN（只针对该列真正变化的行，避免无谓写入）
        for (int k = 0; k < extras.Length; k++)
        {
            var changed = toUpdate.Where(kv => existing[kv.Key][k] != target[kv.Key][k]).ToList();
            for (int i = 0; i < changed.Count;)
            {
                var chunk = changed.Skip(i).Take((SqlVarLimit - 1) / 2).ToList();
                var sb = new StringBuilder();
                sb.Append($"UPDATE {t} SET {QuoteIdent(extras[k])} = CASE {rf}");
                var pars = new List<SugarParameter> { new("@lv", leftVal) };
                var whenNames = new List<string>();
                for (int j = 0; j < chunk.Count; j++)
                {
                    sb.Append(" WHEN @w").Append(j).Append(" THEN @v").Append(j);
                    pars.Add(new SugarParameter("@w" + j, chunk[j].Key));
                    pars.Add(new SugarParameter("@v" + j, chunk[j].Value[k]));
                    whenNames.Add("@w" + j);
                }
                sb.Append($" END WHERE {lf} = @lv AND {rf} IN ({string.Join(",", whenNames)})");
                db.Ado.ExecuteCommand(sb.ToString(), pars.ToArray());
                i += chunk.Count;
            }
        }
    }

    /// <summary>右值容错解析：数字或数字字符串均可；无法解析返回 false（该条跳过）。</summary>
    private static bool TryRid(JToken token, out long rid)
    {
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) { rid = 0; return false; }
        return long.TryParse(token.ToString(), out rid);
    }

    /// <summary>
    /// 读取主表记录 + 多对多中间表数组。
    /// 简单模式：row[fieldName] = [id1, id2, ...]
    /// 带额外列：row[fieldName] = [{RightField:id, ef1:bool, ef2:bool, ...}, ...]
    /// </summary>
    public JObject GetWithRelations(SqlSugarClient db, string table, object keyValue, JObject relations)
    {
        EnsureTable(db, table);
        var pks = PrimaryKeys(db, table);
        if (pks.Count == 0) return null;
        var row = First(db, table, $"{QuoteIdent(pks[0])}=@v", new { v = keyValue });
        if (row == null || relations == null) return row;

        foreach (var prop in relations.Properties())
        {
            RelationConfig cfg;
            try { cfg = (prop.Value as JObject)?.ToObject<RelationConfig>(); }
            catch { continue; }
            if (cfg == null || string.IsNullOrWhiteSpace(cfg.Table)
                || string.IsNullOrWhiteSpace(cfg.LeftField) || string.IsNullOrWhiteSpace(cfg.RightField)) continue;

            var leftValueField = string.IsNullOrWhiteSpace(cfg.LeftValueField) ? pks[0] : cfg.LeftValueField;
            var leftVal = row[leftValueField]?.ToString();
            if (leftVal == null) { row[prop.Name] = new JArray(); continue; }

            var hasExtra = cfg.ExtraFields != null && cfg.ExtraFields.Length > 0;
            var selectCols = hasExtra
                ? string.Join(", ", new[] { cfg.RightField }.Concat(cfg.ExtraFields).Select(QuoteIdent))
                : QuoteIdent(cfg.RightField);

            var dt = db.Ado.GetDataTable(
                $"SELECT {selectCols} FROM {QuoteIdent(cfg.Table)} WHERE {QuoteIdent(cfg.LeftField)} = @lv",
                new SugarParameter("@lv", leftVal));
            var arr = new JArray();
            foreach (System.Data.DataRow r in dt.Rows)
            {
                if (!hasExtra)
                {
                    arr.Add(Convert.ToInt64(r[cfg.RightField]));
                }
                else
                {
                    var obj = new JObject();
                    obj[cfg.RightField] = Convert.ToInt64(r[cfg.RightField]);
                    foreach (var ef in cfg.ExtraFields)
                    {
                        // 可空布尔列存 NULL 时 DataRow 值为 DBNull，直接 Convert.ToBoolean 会抛异常
                        obj[ef] = r.IsNull(ef) ? false : Convert.ToBoolean(r[ef]);
                    }
                    arr.Add(obj);
                }
            }
            row[prop.Name] = arr;
        }
        return row;
    }

    private static bool IsUsablePk(JToken v)
    {
        if (v == null || v.Type == JTokenType.Null || v.Type == JTokenType.Undefined) return false;
        if (v.Type == JTokenType.Integer) return v.Value<long>() != 0;
        if (v.Type == JTokenType.Float) return v.Value<double>() != 0;
        if (v.Type == JTokenType.String) return !string.IsNullOrWhiteSpace(v.Value<string>());
        return true;
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

    /// <summary>dyndata 支持的全部筛选操作符（未知 op 会回流告警，禁止再静默吞掉）</summary>
    private static readonly HashSet<string> SupportedOps =
        new(StringComparer.OrdinalIgnoreCase) { "eq", "neq", "gt", "ge", "lt", "le", "like", "notlike", "in", "between" };

    private void AppendOp(StringBuilder sb, List<SugarParameter> pars, string col, string dataType, string op, JToken v, List<string> warnings = null)
    {
        op = string.IsNullOrWhiteSpace(op) ? "eq" : op.Trim();
        if (!SupportedOps.Contains(op))
        {
            // 失败变吵：如误写 contains/startsWith，旧实现 switch 无 default → 条件被静默丢弃（返回全量）
            warnings?.Add($"不支持的筛选操作符 [{op}]（字段 [{col}]，值 {v}），该条件已忽略。支持：eq/neq/gt/ge/lt/le/like/notlike/in/between");
            return;
        }
        var c = QuoteIdent(col); // 列名来自 schema 白名单，此处再做方括号转义兜底
        switch (op.ToLower())
        {
            case "eq":
                AddParam(sb, pars, $" AND {c} = {{p}} ", ToDbValue(v, dataType));
                break;
            case "neq":
                AddParam(sb, pars, $" AND {c} <> {{p}} ", ToDbValue(v, dataType));
                break;
            case "gt":
                AddParam(sb, pars, $" AND {c} > {{p}} ", ToDbValue(v, dataType));
                break;
            case "ge":
                AddParam(sb, pars, $" AND {c} >= {{p}} ", ToDbValue(v, dataType));
                break;
            case "lt":
                AddParam(sb, pars, $" AND {c} < {{p}} ", ToDbValue(v, dataType));
                break;
            case "le":
                AddParam(sb, pars, $" AND {c} <= {{p}} ", ToDbValue(v, dataType));
                break;
            case "like":
                AddParam(sb, pars, $" AND {c} LIKE {{p}} ", "%" + (v?.ToString() ?? "") + "%");
                break;
            case "notlike":
                AddParam(sb, pars, $" AND {c} NOT LIKE {{p}} ", "%" + (v?.ToString() ?? "") + "%");
                break;
            case "in":
                if (v is JArray arr && arr.Count > 0)
                {
                    var names = new List<string>();
                    foreach (var item in arr)
                    {
                        var p = $"@p{pars.Count}";
                        names.Add(p);
                        pars.Add(new SugarParameter(p, ToDbValue(item, dataType)));
                    }
                    sb.Append($" AND {c} IN ({string.Join(",", names)}) ");
                }
                else
                {
                    warnings?.Add($"操作符 [in] 的值必须是非空数组（字段 [{col}]），该条件已忽略");
                }
                break;
            case "between":
                if (v is JArray bt && bt.Count == 2)
                {
                    var p1 = $"@p{pars.Count}";
                    pars.Add(new SugarParameter(p1, ToDbValue(bt[0], dataType)));
                    var p2 = $"@p{pars.Count}";
                    pars.Add(new SugarParameter(p2, ToDbValue(bt[1], dataType)));
                    sb.Append($" AND {c} BETWEEN {p1} AND {p2} ");
                }
                else
                {
                    warnings?.Add($"操作符 [between] 的值必须是长度为 2 的数组（字段 [{col}]），该条件已忽略");
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
                var cols = GetRawColumns(db, table)
                    .Select(c => c.DbColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (cols.Contains(col)) return $"{QuoteIdent(col)} {dir}";
            }
            // 非法排序子句直接忽略（回退主键排序），不抛异常以免影响列表加载
        }
        var pks = PrimaryKeys(db, table);
        if (pks.Count > 0) return $"{QuoteIdent(pks[0])} desc";
        var first = db.DbMaintenance.GetColumnInfosByTableName(table, false).FirstOrDefault();
        return first != null ? $"{QuoteIdent(first.DbColumnName)} asc" : "(SELECT 0)";
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
        // 布尔值：bit 列原生存布尔；SQLite 等无原生布尔类型的库以 INTEGER 0/1 存储（t="integer"），
        // 不能落到下面 int 分支做 int.TryParse("True")——会恒转成 0，导致 true/false 筛选都查不到
        if (token.Type == JTokenType.Boolean)
        {
            var b = token.Value<bool>();
            if (t.Contains("bit")) return b;
            if (t.Contains("int") || t.Contains("bigint") || t.Contains("decimal") || t.Contains("numeric"))
                return b ? 1 : 0;
            return b;
        }
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

    /// <summary>
    /// 标识符白名单（Unicode 字母/下划线开头，字母数字下划线，≤128）：
    /// 表名/列名进入 SQL 前的第一道防线，挡掉 ] ; -- 空格 连字符等任何引号逃逸字符；
    /// 第二道防线是 EnsureTable 的"表必须真实存在"与各调用点的列存在性校验。
    /// </summary>
    private static readonly Regex IdentRx = new(@"^[\p{L}_][\p{L}0-9_]{0,127}$", RegexOptions.Compiled);

    /// <summary>把标识符安全包进方括号（转义内部 ]）。仅用于来自 schema/请求且已过白名单的名字。</summary>
    public static string QuoteIdent(string name) => "[" + (name ?? "").Replace("]", "]]") + "]";

    private static string EnsureIdent(string name, string kind)
    {
        if (string.IsNullOrWhiteSpace(name) || !IdentRx.IsMatch(name))
            throw new Exception($"非法{kind}名：{name}（只允许字母/下划线开头的字母数字下划线）");
        return name;
    }

    private void EnsureTable(SqlSugarClient db, string table)
    {
        if (string.IsNullOrWhiteSpace(table)) throw new Exception("表名不能为空");
        // 即使后面会做"表存在"校验，也先过标识符正则：防止构造奇特的库内表名后利用方括号拼接
        EnsureIdent(table, "表");
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
