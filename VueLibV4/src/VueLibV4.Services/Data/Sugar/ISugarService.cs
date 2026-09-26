using System.Linq.Expressions;
using SqlSugar;

namespace VueLibV4.Services.Data.Sugar;

/// <summary>分页结果（强类型）</summary>
public class SugarPage<T>
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int Total { get; set; }
    public List<T> Rows { get; set; } = new();
}

/// <summary>
/// 强类型 SqlSugar 数据服务接口：一个实体一个服务，封装常用查询/增删改/分页。
/// 平台业务接口（如 IDesktopShortcutService）继承本接口并叠加 IScopeDependency 标记，
/// 即可被启动扫描自动注册。
/// </summary>
public interface ISugarService<TEntity> where TEntity : class, new()
{
    /// <summary>当前服务绑定的库客户端（平台库）</summary>
    ISqlSugarClient Db { get; }

    TEntity GetById(object id);
    bool Exists(Expression<Func<TEntity, bool>> where);
    int Count(Expression<Func<TEntity, bool>> where = null);

    List<TEntity> List(Expression<Func<TEntity, bool>> where = null, string orderBy = null);
    ISugarQueryable<TEntity> Query(Expression<Func<TEntity, bool>> where = null);

    /// <summary>新增（自增主键回填到实体）；返回入库后的同一实体</summary>
    TEntity Insert(TEntity entity);

    /// <summary>整实体更新（按主键）；返回受影响行数</summary>
    int Update(TEntity entity);

    /// <summary>按表达式更新部分列：updateColumns 指定要写入的字段</summary>
    int UpdateColumns(TEntity entity, Expression<Func<TEntity, object>> updateColumns);

    int Delete(TEntity entity);
    int DeleteById(object id);
    int Delete(Expression<Func<TEntity, bool>> where);

    /// <summary>有主键→更新，无主键（Id=0）→新增；返回入库后的实体</summary>
    TEntity Save(TEntity entity, Func<TEntity, bool> hasKey = null);

    SugarPage<TEntity> Page(int pageIndex, int pageSize,
        Expression<Func<TEntity, bool>> where = null,
        string orderBy = null,
        Expression<Func<TEntity, object>> orderExpression = null,
        OrderByType orderType = OrderByType.Asc);
}
