using System.Linq.Expressions;
using SqlSugar;

namespace VueLibV4.Services.Data.Sugar;

/// <summary>
/// 强类型 SqlSugar 数据服务基类：注入平台库 ISqlSugarClient（由 VueLibV4.Platform 的 AddVueLibPlatform 注册）。
/// 用法：
///   public interface IDesktopShortcutService : ISugarService&lt;DesktopShortcut&gt;, IScopeDependency { }
///   public class DesktopShortcutService : SugarService&lt;DesktopShortcut&gt;, IDesktopShortcutService { }
/// </summary>
public class SugarService<TEntity> : ISugarService<TEntity>
    where TEntity : class, new()
{
    protected readonly ISqlSugarClient _db;

    public SugarService(ISqlSugarClient db)
    {
        _db = db;
    }

    public ISqlSugarClient Db => _db;

    public virtual TEntity GetById(object id) => _db.Queryable<TEntity>().In(id).First();

    public virtual bool Exists(Expression<Func<TEntity, bool>> where)
        => where != null && _db.Queryable<TEntity>().Where(where).Any();

    public virtual int Count(Expression<Func<TEntity, bool>> where = null)
    {
        var q = _db.Queryable<TEntity>();
        return where == null ? q.Count() : q.Where(where).Count();
    }

    public virtual List<TEntity> List(Expression<Func<TEntity, bool>> where = null, string orderBy = null)
    {
        var q = _db.Queryable<TEntity>();
        if (where != null) q = q.Where(where);
        if (!string.IsNullOrWhiteSpace(orderBy)) q = q.OrderBy(orderBy);
        return q.ToList();
    }

    public virtual ISugarQueryable<TEntity> Query(Expression<Func<TEntity, bool>> where = null)
    {
        var q = _db.Queryable<TEntity>();
        return where == null ? q : q.Where(where);
    }

    public virtual TEntity Insert(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var identity = _db.Insertable(entity).ExecuteReturnBigIdentity();
        FillIdentity(entity, identity);
        return entity;
    }

    /// <summary>新增并返回自增主键（需要拿新 Id 时使用）</summary>
    public virtual long InsertReturnIdentity(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var identity = _db.Insertable(entity).ExecuteReturnBigIdentity();
        FillIdentity(entity, identity);
        return identity;
    }

    /// <summary>
    /// 把自增主键回填到实体的 Id 属性。
    /// 说明：ExecuteCommand() 在 SQLite 等库下不保证回填，ExecuteReturnBigIdentity 各库通用。
    /// </summary>
    private static void FillIdentity(TEntity entity, long identity)
    {
        if (identity <= 0) return;
        var idProp = typeof(TEntity).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        if (idProp is not { CanWrite: true }) return;
        var current = idProp.GetValue(entity, null);
        if (current != null && Convert.ToInt64(current) != 0) return;
        var targetType = Nullable.GetUnderlyingType(idProp.PropertyType) ?? idProp.PropertyType;
        idProp.SetValue(entity, Convert.ChangeType(identity, targetType), null);
    }

    public virtual int Update(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        return _db.Updateable(entity).ExecuteCommand();
    }

    public virtual int UpdateColumns(TEntity entity, Expression<Func<TEntity, object>> updateColumns)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        if (updateColumns == null) return Update(entity);
        return _db.Updateable(entity).UpdateColumns(updateColumns).ExecuteCommand();
    }

    public virtual int Delete(TEntity entity)
        => entity == null ? 0 : _db.Deleteable(entity).ExecuteCommand();

    public virtual int DeleteById(object id)
        => id == null ? 0 : _db.Deleteable<TEntity>().In(id).ExecuteCommand();

    public virtual int Delete(Expression<Func<TEntity, bool>> where)
        => where == null ? 0 : _db.Deleteable<TEntity>().Where(where).ExecuteCommand();

    /// <summary>
    /// 有主键→更新，无主键→新增。默认按实体中名为 Id 的属性判断，可通过 hasKey 自定义。
    /// </summary>
    public virtual TEntity Save(TEntity entity, Func<TEntity, bool> hasKey = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        var exists = hasKey != null
            ? hasKey(entity)
            : DefaultHasKey(entity);
        if (exists) Update(entity);
        else Insert(entity);
        return entity;
    }

    public virtual SugarPage<TEntity> Page(int pageIndex, int pageSize,
        Expression<Func<TEntity, bool>> where = null,
        string orderBy = null,
        Expression<Func<TEntity, object>> orderExpression = null,
        OrderByType orderType = OrderByType.Asc)
    {
        pageIndex = pageIndex < 1 ? 1 : pageIndex;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 200);

        int total = 0;
        var q = _db.Queryable<TEntity>();
        if (where != null) q = q.Where(where);
        if (!string.IsNullOrWhiteSpace(orderBy)) q = q.OrderBy(orderBy);
        else if (orderExpression != null) q = q.OrderBy(orderExpression, orderType);

        var rows = q.ToPageList(pageIndex, pageSize, ref total);
        return new SugarPage<TEntity>
        {
            Page = pageIndex,
            Size = pageSize,
            Total = total,
            Rows = rows
        };
    }

    /// <summary>默认主键判定：Id 属性存在且为非零数值 / 非空字符串/Guid</summary>
    private static bool DefaultHasKey(TEntity entity)
    {
        var idProp = typeof(TEntity).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        if (idProp == null) return false;
        var v = idProp.GetValue(entity, null);
        return v switch
        {
            null => false,
            int i => i != 0,
            long l => l != 0,
            string s => !string.IsNullOrWhiteSpace(s),
            Guid g => g != Guid.Empty,
            _ => !v.Equals(Activator.CreateInstance(v.GetType()))
        };
    }
}
