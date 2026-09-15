using Newtonsoft.Json.Linq;
using SqlSugar;
using System.Text;
using System.Text.RegularExpressions;

namespace VueLibV3.Web.Core;

/// <summary>
/// 启动种子器：
/// 1. 自动创建 PlatformDb / BusinessDb（不存在时）
/// 2. 执行 SeedData/*.sql 结构脚本（按 GO 分批）
/// 3. 导入 SeedData/*.seed.json 种子数据（表为空时）
/// </summary>
public class Seeder
{
    private readonly IConfiguration _config;
    private readonly ILogger _logger;
    private readonly string _seedDir;

    public Seeder(IConfiguration config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        _seedDir = Path.Combine(AppContext.BaseDirectory, "SeedData");
    }

    public void Run()
    {
        _logger.LogInformation("[Seed] 开始初始化数据库...");
        EnsureDatabase("PlatformDb", "01_PlatformDb.sql");
        EnsureDatabase("BusinessDb", "02_BusinessDb.sql");

        SeedPlatform();
        SeedBusiness();
        _logger.LogInformation("[Seed] 数据库初始化完成");
    }

    private void EnsureDatabase(string name, string sqlFile)
    {
        var cs = _config.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(cs)) { _logger.LogWarning("[Seed] 缺少连接串 {Name}", name); return; }

        using var db = NewDb(cs);
        try
        {
            db.Ado.CheckConnection();
            _logger.LogInformation("[Seed] 数据库 {Name} 已存在", name);
        }
        catch
        {
            _logger.LogInformation("[Seed] 数据库 {Name} 不存在，正在创建...", name);
            db.DbMaintenance.CreateDatabase();
        }

        var sqlPath = Path.Combine(_seedDir, sqlFile);
        if (File.Exists(sqlPath))
        {
            var sql = File.ReadAllText(sqlPath, Encoding.UTF8);
            foreach (var batch in SplitByGo(sql))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                ExecuteWithRetry(db, batch, sqlFile);
            }
            _logger.LogInformation("[Seed] 执行结构脚本 {SqlFile} 完成", sqlFile);
        }
        else
        {
            _logger.LogWarning("[Seed] 未找到结构脚本 {SqlFile}", sqlFile);
        }
    }

    /// <summary>刚创建的数据库可能瞬时不可访问（登录失败），重试最多 4 次</summary>
    private static void ExecuteWithRetry(SqlSugarClient db, string sql, string file)
    {
        Exception last = null;
        for (var i = 0; i < 4; i++)
        {
            try { db.Ado.ExecuteCommand(sql); return; }
            catch (Exception ex)
            {
                last = ex;
                db.Ado.Close();
                Thread.Sleep(1000 * (i + 1));
            }
        }
        throw new Exception($"执行 {file} 失败: {last?.Message}");
    }

    private void SeedPlatform()
    {
        using var db = NewDb(_config.GetConnectionString("PlatformDb"));

        SeedTable(db, "ComponentMeta", "component-meta.seed.json", "ComponentName");
        SeedTable(db, "DynActionHelper", "action-helper.seed.json", "Code");
        SeedTable(db, "DynTemplate", "template.seed.json", "Code");

        // 组合种子：桌面解决方案/快捷方式/项目/网页/平台字典
        var platPath = Path.Combine(_seedDir, "platform-seed.json");
        if (File.Exists(platPath))
        {
            var root = JObject.Parse(File.ReadAllText(platPath, Encoding.UTF8));
            SeedTable(db, "DesktopSolution", root["DesktopSolution"] as JArray, "Code");
            SeedTable(db, "DesktopShortcut", root["DesktopShortcut"] as JArray, "Code");
            SeedTable(db, "DynDict", root["DynDict"] as JArray, "DictCode");
            SeedTable(db, "DynWebPage", root["DynWebPage"] as JArray, "Code");

            // DynProject 连接串跟随配置，避免写死
            var projects = root["DynProject"] as JArray;
            if (projects != null)
            {
                var biz = _config.GetConnectionString("BusinessDb");
                foreach (var p in projects)
                {
                    var code = p["Code"]?.ToString();
                    if (code == "Business-School") p["ConnectionString"] = biz;
                }
                SeedTable(db, "DynProject", projects, "Code");
            }
        }

        // DynCom 组件库种子（数据库中的组件配置）
        var dyncomPath = Path.Combine(_seedDir, "dyn-com.seed.json");
        if (File.Exists(dyncomPath))
            SeedTable(db, "DynCom", "dyn-com.seed.json", "Code");
    }

    private void SeedBusiness()
    {
        using var db = NewDb(_config.GetConnectionString("BusinessDb"));
        var path = Path.Combine(_seedDir, "business-seed.json");
        if (!File.Exists(path)) return;
        var root = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
        SeedTable(db, "Student", root["Student"] as JArray, "Id");
        SeedTable(db, "Course", root["Course"] as JArray, "Id");
        SeedTable(db, "BusinessDict", root["BusinessDict"] as JArray, "DictCode");
    }

    // ---------------- 通用种子写入 ----------------

    private void SeedTable(SqlSugarClient db, string table, string jsonFile, string uniqueCol)
    {
        var path = Path.Combine(_seedDir, jsonFile);
        if (!File.Exists(path)) { _logger.LogWarning("[Seed] 未找到种子文件 {JsonFile}", jsonFile); return; }
        var arr = JArray.Parse(File.ReadAllText(path, Encoding.UTF8));
        SeedTable(db, table, arr, uniqueCol);
    }

    private void SeedTable(SqlSugarClient db, string table, JArray rows, string uniqueCol)
    {
        if (rows == null || rows.Count == 0) return;
        var exist = db.Queryable<dynamic>().AS(table).Count();
        if (exist > 0)
        {
            _logger.LogInformation("[Seed] 表 {Table} 已有 {Exist} 条数据，跳过", table, exist);
            return;
        }

        var colInfos = db.DbMaintenance.GetColumnInfosByTableName(table, false)
            .ToDictionary(c => c.DbColumnName, StringComparer.OrdinalIgnoreCase);
        var colNames = new HashSet<string>(colInfos.Keys, StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        foreach (var row in rows)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in ((JObject)row).Properties())
            {
                if (!colInfos.TryGetValue(prop.Name, out var ci)) continue;
                if (ci.IsIdentity) continue; // 自增主键跳过，由数据库生成
                var v = prop.Value;
                dict[prop.Name] = (v.Type == JTokenType.Null || v.Type == JTokenType.Undefined)
                    ? null
                    : ConvertDbValue(v, ci.DataType);
            }
            if (colNames.Contains("CreateTime") && !dict.ContainsKey("CreateTime"))
                dict["CreateTime"] = DateTime.Now;
            db.InsertableByObject(dict).AS(table).ExecuteCommand();
            inserted++;
        }
        _logger.LogInformation("[Seed] 表 {Table} 写入 {Count} 条种子", table, inserted);
    }

    /// <summary>按目标列类型转换种子值（NVARCHAR 原样字符串；BIGINT/INT/BIT/DATETIME2/DECIMAL 转原生类型）</summary>
    private static object ConvertDbValue(JToken v, string dataType)
    {
        var t = (dataType ?? "").ToLower();
        if (t.Contains("bigint"))
            return long.TryParse(v.ToString(), out var l) ? l : 0L;
        if (t.Contains("int"))
            return int.TryParse(v.ToString(), out var i) ? i : 0;
        if (t.Contains("bit"))
            return v.ToString() is "1" or "true" or "True" or "TRUE";
        if (t.Contains("decimal") || t.Contains("numeric") || t.Contains("money"))
            return decimal.TryParse(v.ToString(), out var d) ? d : 0m;
        if (t.Contains("float") || t.Contains("real"))
            return double.TryParse(v.ToString(), out var f) ? f : 0d;
        if (t.Contains("date") || t.Contains("time"))
            return DateTime.TryParse(v.ToString(), out var dt) ? dt : DateTime.Now;
        if (v.Type == JTokenType.Object || v.Type == JTokenType.Array)
            return v.ToString(Newtonsoft.Json.Formatting.None);
        return v.ToString();
    }

    private static SqlSugarClient NewDb(string connStr) => new(new ConnectionConfig
    {
        ConnectionString = connStr,
        DbType = DbType.SqlServer,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute
    });

    private static IEnumerable<string> SplitByGo(string sql)
        => Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0);
}
