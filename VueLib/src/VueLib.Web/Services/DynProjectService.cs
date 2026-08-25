using SqlSugar;
using VueLib.Web.Data;
using VueLib.Web.Models;

namespace VueLib.Web.Services;

/// <summary>
/// 动态工程服务：工程 CRUD / 连接测试 / 元数据库表结构发现 / 页面定义生成 / 页面 CRUD / SQL 视图生成
/// </summary>
public class DynProjectService
{
    private readonly AppDbContext _db;
    private readonly DynCrudService _crud;

    public DynProjectService(AppDbContext db, DynCrudService crud)
    {
        _db = db;
        _crud = crud;
    }

    // ==================== 工程 ====================

    public List<DynProject> GetProjects()
    {
        using var db = _db.Create();
        return db.Queryable<DynProject>().OrderBy(p => p.Id).ToList();
    }

    public DynProject? GetProject(int id)
    {
        using var db = _db.Create();
        return db.Queryable<DynProject>().InSingle(id);
    }

    public OpResult SaveProject(DynProject p)
    {
        if (string.IsNullOrWhiteSpace(p.Name)) return Op(false, "工程名称不能为空");
        if (string.IsNullOrWhiteSpace(p.ConnectionString)) return Op(false, "数据库连接串不能为空");
        using var db = _db.Create();
        p.Name = p.Name.Trim();
        p.UpdatedAt = DateTime.UtcNow;
        if (p.Id == 0)
        {
            var exists = db.Queryable<DynProject>().Where(x => x.Name == p.Name).Any();
            if (exists) return Op(false, "工程名称已存在");
            p.CreatedAt = DateTime.UtcNow;
            var id = db.Insertable(p).ExecuteReturnIdentity();
            return Op(true, "创建成功", new { Id = (int)id });
        }
        db.Updateable(p).ExecuteCommand();
        return Op(true, "保存成功", p);
    }

    public OpResult DeleteProject(int id)
    {
        using var db = _db.Create();
        db.Deleteable<DynPage>().Where(x => x.ProjectId == id).ExecuteCommand();
        db.Deleteable<DynProject>().In(id).ExecuteCommand();
        return Op(true, "已删除");
    }

    /// <summary>测试连接串是否可用</summary>
    public OpResult TestConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return Op(false, "连接串为空");
        try
        {
            using var sc = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true
            });
            sc.Ado.Open();
            sc.Ado.Close();
            return Op(true, "连接成功");
        }
        catch (Exception ex)
        {
            return Op(false, "连接失败：" + ex.Message);
        }
    }

    /// <summary>为工程创建 SqlSugar 客户端（连接工程自己的数据库）</summary>
    public ISqlSugarClient CreateProjectClient(DynProject p)
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = p.ConnectionString,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }

    /// <summary>读取某表列信息（供控制器调用）</summary>
    public List<DbColumnInfo> DynCrudColumns(ISqlSugarClient db, string table)
        => _crud.GetColumns(db, table);

    // ==================== 元数据发现 ====================

    public OpResult GetTables(int projectId)
    {
        var p = GetProject(projectId);
        if (p == null) return Op(false, "工程不存在");
        try
        {
            using var db = CreateProjectClient(p);
            var tables = db.DbMaintenance.GetTableInfoList(false)
                .Where(t => !t.Name.StartsWith("sys") && !t.Name.StartsWith("dt"))
                .OrderBy(t => t.Name)
                .Select(t => new { t.Name, t.Description })
                .ToList();
            return Op(true, "ok", tables);
        }
        catch (Exception ex) { return Op(false, "连接工程数据库失败：" + ex.Message); }
    }

    public OpResult GetColumns(int projectId, string table)
    {
        var p = GetProject(projectId);
        if (p == null) return Op(false, "工程不存在");
        try
        {
            using var db = CreateProjectClient(p);
            var cols = _crud.GetColumns(db, table)
                .OrderBy(c => c.DbColumnName)
                .Select(c => new
                {
                    c.DbColumnName,
                    c.DataType,
                    c.Length,
                    c.IsNullable,
                    c.IsPrimarykey,
                    c.IsIdentity,
                    c.ColumnDescription
                })
                .ToList();
            return Op(true, "ok", cols);
        }
        catch (Exception ex) { return Op(false, "读取列失败：" + ex.Message); }
    }

    // ==================== 页面定义生成 ====================

    /// <summary>
    /// 依据表结构自动生成页面定义（列类型 → 控件/筛选/表格/表单 启发式映射）
    /// </summary>
    public DynPageDefinition GenerateDefinition(string table, List<DbColumnInfo> cols)
    {
        var def = new DynPageDefinition();
        var pk = cols.FirstOrDefault(c => c.IsPrimarykey);
        if (pk != null)
        {
            def.PrimaryKey = pk.DbColumnName;
            def.IsIdentity = pk.IsIdentity;
        }
        else
        {
            def.PrimaryKey = cols.FirstOrDefault()?.DbColumnName ?? "Id";
            def.IsIdentity = false;
        }
        def.OrderBy = def.PrimaryKey;
        def.OrderDir = "desc";

        int order = 0;
        foreach (var c in cols)
        {
            var col = MapColumn(c, order++);
            def.Columns.Add(col);
        }
        return def;
    }

    private DynColumnDef MapColumn(DbColumnInfo c, int order)
    {
        var name = c.DbColumnName;
        var sqlType = (c.DataType ?? "").ToLowerInvariant();
        var dbType = "string";
        if (sqlType.Contains("int") || sqlType.Contains("bigint") || sqlType.Contains("smallint") || sqlType.Contains("tinyint"))
            dbType = sqlType.Contains("bigint") || sqlType.Contains("tinyint") || sqlType.Contains("smallint") ? "long" : "int";
        else if (sqlType.Contains("decimal") || sqlType.Contains("numeric") || sqlType.Contains("money"))
            dbType = "decimal";
        else if (sqlType.Contains("float") || sqlType.Contains("real"))
            dbType = "decimal";
        else if (sqlType.Contains("datetime") || sqlType.Contains("date") || sqlType.Contains("time") || sqlType.Contains("smalldatetime"))
            dbType = "datetime";
        else if (sqlType == "bit")
            dbType = "bool";
        else if (sqlType.Contains("uniqueidentifier"))
            dbType = "guid";

        var col = new DynColumnDef
        {
            Name = name,
            Label = c.ColumnDescription ?? name,
            DbType = dbType,
            SqlType = sqlType,
            IsPrimary = c.IsPrimarykey,
            IsIdentity = c.IsIdentity,
            IsNullable = c.IsNullable,
            Order = order
        };

        // 控件类型
        switch (dbType)
        {
            case "bool": col.Control = "switch"; break;
            case "datetime": col.Control = sqlType.Contains("date") && !sqlType.Contains("datetime") ? "date" : "datetime"; break;
            case "int":
            case "long":
            case "decimal": col.Control = "input-number"; break;
            case "guid": col.Control = "input"; col.IsReadOnly = true; break;
            default: col.Control = "input"; break;
        }

        if (col.IsPrimary)
        {
            col.IsGrid = true;
            col.IsForm = c.IsIdentity;   // 自增主键在表单展示但只读
            col.IsFilter = false;
            col.IsReadOnly = true;
            col.Width = 90;
        }
        else
        {
            col.IsGrid = true;
            col.IsForm = true;
            col.IsFilter = dbType is "string" or "int" or "long" or "decimal" or "datetime";
            col.FilterOp = dbType == "string" ? "like" : "eq";
            col.Required = !c.IsNullable;
            col.Width = dbType switch
            {
                "string" => 140,
                "datetime" => 170,
                _ => 110
            };
            // 常见下拉/开关启发式
            var lower = name.ToLowerInvariant();
            if (lower.Contains("gender") || name.Contains("性别"))
            {
                col.Control = "select";
                col.Options = new List<DynOption> { new("男", "男"), new("女", "女") };
            }
            else if (lower.Contains("level") || lower.Contains("grade") || name.Contains("等级") || name.Contains("级别"))
            {
                col.Control = "select";
                col.Options = new List<DynOption> { new("初级", "初级"), new("中级", "中级"), new("高级", "高级") };
            }
            else if (lower.Contains("status") || lower.Contains("state") || name.Contains("状态") || name.Contains("启用") || name.Contains("是否"))
            {
                col.Control = "select";
                col.Options = new List<DynOption> { new("启用", "启用"), new("停用", "停用") };
            }
            if (col.Control == "select" && col.Options.Count == 0)
                col.Control = "input";
        }

        return col;
    }

    // ==================== 页面 CRUD ====================

    public List<DynPage> GetPages(int projectId)
    {
        using var db = _db.Create();
        return db.Queryable<DynPage>().Where(x => x.ProjectId == projectId).OrderBy(x => x.PageType).OrderBy(x => x.Name).ToList();
    }

    public DynPage? GetPage(int id)
    {
        using var db = _db.Create();
        return db.Queryable<DynPage>().InSingle(id);
    }

    public OpResult SavePage(DynPage p)
    {
        if (string.IsNullOrWhiteSpace(p.Name)) return Op(false, "页面编码不能为空");
        if (string.IsNullOrWhiteSpace(p.TableName)) return Op(false, "请选择表");
        if (string.IsNullOrWhiteSpace(p.ColumnDefs)) return Op(false, "页面定义不能为空");
        using var db = _db.Create();
        p.Name = p.Name.Trim();
        p.UpdatedAt = DateTime.UtcNow;
        if (p.Id == 0)
        {
            var exists = db.Queryable<DynPage>().Where(x => x.ProjectId == p.ProjectId && x.Name == p.Name && x.PageType == p.PageType).Any();
            if (exists) return Op(false, "同工程的页面编码已存在");
            p.CreatedAt = DateTime.UtcNow;
            var id = db.Insertable(p).ExecuteReturnIdentity();
            return Op(true, "保存成功", new { Id = (int)id });
        }
        db.Updateable(p).ExecuteCommand();
        return Op(true, "保存成功", p);
    }

    public OpResult DeletePage(int id)
    {
        using var db = _db.Create();
        db.Deleteable<DynPage>().In(id).ExecuteCommand();
        return Op(true, "已删除");
    }

    // ==================== SQL 视图生成 ====================

    /// <summary>为该页面生成 CREATE VIEW 脚本（在工程库中执行）</summary>
    public OpResult BuildSqlView(DynProject p, DynPage page, DynPageDefinition def)
    {
        var viewName = "v" + page.Name;
        var cols = new List<string>();
        foreach (var c in def.Columns.Where(x => x.IsGrid || x.IsPrimary))
            cols.Add($"[{c.Name}]");
        if (cols.Count == 0) cols.Add("*");
        var sql = $"IF OBJECT_ID('dbo.{viewName}','V') IS NOT NULL DROP VIEW dbo.{viewName};\n" +
                  $"GO\nCREATE VIEW dbo.{viewName} AS\nSELECT {string.Join(",\n       ", cols)}\nFROM dbo.[{page.TableName}];\nGO";
        return Op(true, "ok", sql);
    }

    private static OpResult Op(bool ok, string msg, object? data = null) => new() { Success = ok, Message = msg, Data = data };
}
