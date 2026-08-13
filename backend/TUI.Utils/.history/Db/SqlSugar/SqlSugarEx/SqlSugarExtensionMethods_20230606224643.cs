// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.


using SqlSugar;
using System.Data;

namespace TUI.Utils;

/// <summary>
/// sqlsugar扩展方法
/// </summary>
public static class SqlSugarExtensions
{
    /// <summary>
    /// 更新忽略的字段
    /// </summary>
    private static readonly string[] UpdateIgnoreColumns = new string[] { "CreatorUserId", "CreatorUserName", "CreationTime" };

    #region 租户

    /// <summary>
    /// 转换为租户数据库
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static ISqlSugarClient ToTenant(this ISqlSugarClient db)
    {
        //获取登陆的用户信息
        //var tenantDbConfig = db.Queryable<dynamic>().First(o => o.TenantId == AppEx.TenantId);
        var tenantDbConfig = db.Queryable<dynamic>().Where("TenantId=@TenantId", new { TenantId = AppEx.TenantId }).First();
        if (tenantDbConfig == null) return db;

        //验证当前上下文不存在则添加
        if (!db.AsTenant().IsAnyConnection(tenantDbConfig.ConfigId))
        {
            var conn = new SqlSugar.ConnectionConfig()
            {
                DbType = tenantDbConfig.DbType.ToEnum<SqlSugar.DbType>(),
                ConfigId = tenantDbConfig.ConfigId,//设置库的唯一标识
                IsAutoCloseConnection = true,
                ConnectionString = tenantDbConfig.ConnString
            };

            db.AsTenant().AddConnection(conn);

            //注册sqlsugar对应的AOP
            var dbProvider = db.AsTenant().GetConnectionScope(tenantDbConfig.ConfigId);
            dbProvider.SetAop();
        }
        return db.AsTenant().GetConnectionScope(tenantDbConfig.ConfigId);
    }

    /// <summary>
    /// 转换为租户数据库
    /// </summary>
    /// <param name="db"></param>
    /// <param name="configId">配置ID</param>
    /// <returns></returns>
    public static ISqlSugarClient ToTenant(this ISqlSugarClient db, dynamic configId)
    {
        return db.AsTenant().GetConnectionScope(configId);
    }

    public static void AddOrUpdateConnection(this ISqlSugarClient db, SqlSugar.ConnectionConfig connection)
    {
        //如果存在，先移除，在新增
        if (db.AsTenant().IsAnyConnection(connection.ConfigId))
        {
            db.AsTenant().RemoveConnection(connection.ConfigId);
        }
        db.AsTenant().AddConnection(connection);

        //注册sqlsugar对应的AOP
        SqlSugarScopeProvider dbProvider = db.AsTenant().GetConnectionScope(connection.ConfigId);
        dbProvider.SetAop();

        ////从连接中移除
        //if (SqlSugarHelper.SqlSugarDbSetting.ConnectionConfigs.Any(o => o.ConfigId == connection.ConfigId))
        //{
        //    SqlSugarHelper.SqlSugarDbSetting.ConnectionConfigs.RemoveWhere(o => o.ConfigId == connection.ConfigId);
        //}
        ////新增连接
        //SqlSugarHelper.SqlSugarDbSetting.ConnectionConfigs.Add(connection);
    }

    #endregion 租户

    #region 查询数据-同步

    /// <summary>
    /// 根据ID查询一条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static T GetById<T>(this ISqlSugarClient _db, dynamic id)
    {
        return _db.Queryable<T>().InSingle(id);
    }

    /// <summary>
    /// 获取全部数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <returns></returns>
    public static List<T> GetList<T>(this ISqlSugarClient _db)
    {
        return _db.Queryable<T>().ToList();
    }

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static List<T> GetList<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return _db.Queryable<T>().Where(whereExpression).ToList();
    }

    /// <summary>
    /// 获取单条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static T GetSingle<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return _db.Queryable<T>().Single(whereExpression);
    }

    /// <summary>
    /// 获取第一条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static T GetFirst<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return _db.Queryable<T>().First(whereExpression);
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="page">分页数据</param>
    /// <returns></returns>
    public static List<T> GetPageList<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression, PageModel page)
    {
        int count = 0;
        var result = _db.Queryable<T>().Where(whereExpression).ToPageList(page.PageIndex, page.PageSize, ref count);
        page.TotalCount = count;
        return result;
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="page">分页数据</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="orderByType">排序方式，默认正序</param>
    /// <returns></returns>
    public static List<T> GetPageList<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        int count = 0;
        var result = _db.Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType).Where(whereExpression).ToPageList(page.PageIndex, page.PageSize, ref count);
        page.TotalCount = count;
        return result;
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="conditionalList">条件集合</param>
    /// <param name="page">分页数据</param>
    /// <returns></returns>
    public static List<T> GetPageList<T>(this ISqlSugarClient _db, List<IConditionalModel> conditionalList, PageModel page)
    {
        int count = 0;
        var result = _db.Queryable<T>().Where(conditionalList).ToPageList(page.PageIndex, page.PageSize, ref count);
        page.TotalCount = count;
        return result;
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="conditionalList">条件集合</param>
    /// <param name="page">分页数据</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="orderByType">排序方式，默认正序</param>
    /// <returns></returns>
    public static List<T> GetPageList<T>(this ISqlSugarClient _db, List<IConditionalModel> conditionalList, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        int count = 0;
        var result = _db.Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType).Where(conditionalList).ToPageList(page.PageIndex, page.PageSize, ref count);
        page.TotalCount = count;
        return result;
    }

    #endregion 查询数据-同步

    #region 查询数据-异步

    /// <summary>
    /// 更加 主键查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    public static async Task<T> GetByIdAsync<T>(this ISqlSugarClient _db, dynamic id)
    {
        return await _db.Queryable<T>().InSingleAsync(id);
    }

    /// <summary>
    /// 获取全部数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <returns></returns>
    public static async Task<List<T>> GetListAsync<T>(this ISqlSugarClient _db)
    {
        return await _db.Queryable<T>().ToListAsync();
    }

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<List<T>> GetListAsync<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return await _db.Queryable<T>().Where(whereExpression).ToListAsync();
    }

    /// <summary>
    /// 查询唯一 一条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<T> GetSingleAsync<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return await _db.Queryable<T>().SingleAsync(whereExpression);
    }

    /// <summary>
    /// 获取第一个数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<T> GetFirstAsync<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return await _db.Queryable<T>().FirstAsync(whereExpression);
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="page">分页信息</param>
    /// <returns></returns>
    public static async Task<List<T>> GetPageListAsync<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression, PageModel page)
    {
        RefAsync<int> count = 0;
        var result = await _db.Queryable<T>().Where(whereExpression).ToPageListAsync(page.PageIndex, page.PageSize, count);
        page.TotalCount = count;
        return result;
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="page">分页信息</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="orderByType">升降排序</param>
    /// <returns></returns>
    public static async Task<List<T>> GetPageListAsync<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        RefAsync<int> count = 0;
        var result = await _db.Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType).Where(whereExpression).ToPageListAsync(page.PageIndex, page.PageSize, count);
        page.TotalCount = count;
        return result;
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="conditionalList">条件集合</param>
    /// <param name="page">分页信息</param>
    /// <returns></returns>
    public static async Task<List<T>> GetPageListAsync<T>(this ISqlSugarClient _db, List<IConditionalModel> conditionalList, PageModel page)
    {
        RefAsync<int> count = 0;
        var result = await _db.Queryable<T>().Where(conditionalList).ToPageListAsync(page.PageIndex, page.PageSize, count);
        page.TotalCount = count;
        return result;
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="conditionalList">条件集合</param>
    /// <param name="page">分页信息</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="orderByType">升降排序</param>
    /// <returns></returns>
    public static async Task<List<T>> GetPageListAsync<T>(this ISqlSugarClient _db, List<IConditionalModel> conditionalList, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        RefAsync<int> count = 0;
        var result = await _db.Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType).Where(conditionalList).ToPageListAsync(page.PageIndex, page.PageSize, count);
        page.TotalCount = count;
        return result;
    }

    #endregion 查询数据-异步

    #region 查询数据-同步-映射转换为指定对象

    /// <summary>
    /// 根据ID查询一条数据
    /// </summary>
    /// <typeparam name="T">数据库实体</typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static OutEntity GetById<T, OutEntity>(this ISqlSugarClient _db, object id)
    {
        return _db.Queryable<T>().InSingle(id).Adapt<OutEntity>();
    }

    /// <summary>
    /// 获取全部数据
    /// </summary>
    /// <typeparam name="T">数据库实体</typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <returns></returns>
    public static List<OutEntity> GetList<T, OutEntity>(this ISqlSugarClient _db)
    {
        return _db.Queryable<T>().ToList().Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static List<OutEntity> GetList<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return _db.Queryable<T>().Where(whereExpression).ToList().Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 获取单条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static OutEntity GetSingle<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return _db.Queryable<T>().Single(whereExpression).Adapt<OutEntity>();
    }

    /// <summary>
    /// 获取第一条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static OutEntity GetFirst<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return _db.Queryable<T>().First(whereExpression).Adapt<OutEntity>();
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="page">分页数据</param>
    /// <returns></returns>
    public static List<OutEntity> GetPageList<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression, PageModel page)
    {
        int count = 0;
        var result = _db.Queryable<T>().Where(whereExpression).ToPageList(page.PageIndex, page.PageSize, ref count);
        page.TotalCount = count;
        return result.Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="page">分页数据</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="orderByType">排序方式，默认正序</param>
    /// <returns></returns>
    public static List<OutEntity> GetPageList<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        int count = 0;
        var result = _db.Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType).Where(whereExpression).ToPageList(page.PageIndex, page.PageSize, ref count);
        page.TotalCount = count;
        return result.Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="conditionalList">条件集合</param>
    /// <param name="page">分页数据</param>
    /// <returns></returns>
    public static List<OutEntity> GetPageList<T, OutEntity>(this ISqlSugarClient _db, List<IConditionalModel> conditionalList, PageModel page)
    {
        int count = 0;
        var result = _db.Queryable<T>().Where(conditionalList).ToPageList(page.PageIndex, page.PageSize, ref count);
        page.TotalCount = count;
        return result.Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="conditionalList">条件集合</param>
    /// <param name="page">分页数据</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="orderByType">排序方式，默认正序</param>
    /// <returns></returns>
    public static List<OutEntity> GetPageList<T, OutEntity>(this ISqlSugarClient _db, List<IConditionalModel> conditionalList, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        int count = 0;
        var result = _db.Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType).Where(conditionalList).ToPageList(page.PageIndex, page.PageSize, ref count);
        page.TotalCount = count;
        return result.Adapt<List<OutEntity>>();
    }

    #endregion 查询数据-同步-映射转换为指定对象

    #region 查询数据-异步-映射转换为指定对象

    /// <summary>
    /// 更加 主键查询
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    public static async Task<OutEntity> GetByIdAsync<T, OutEntity>(this ISqlSugarClient _db, object id)
    {
        var result = await _db.Queryable<T>().InSingleAsync(id);
        return result.Adapt<OutEntity>();
    }

    /// <summary>
    /// 获取全部数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <returns></returns>
    public static async Task<List<OutEntity>> GetListAsync<T, OutEntity>(this ISqlSugarClient _db)
    {
        var result = await _db.Queryable<T>().ToListAsync();
        return result.Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 获取列表
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<List<OutEntity>> GetListAsync<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        var result = await _db.Queryable<T>().Where(whereExpression).ToListAsync();
        return result.Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 查询唯一 一条数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<OutEntity> GetSingleAsync<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        var result = await _db.Queryable<T>().SingleAsync(whereExpression);
        return result.Adapt<OutEntity>();
    }

    /// <summary>
    /// 获取第一个数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<OutEntity> GetFirstAsync<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        var result = await _db.Queryable<T>().FirstAsync(whereExpression);
        return result.Adapt<OutEntity>();
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="page">分页信息</param>
    /// <returns></returns>
    public static async Task<List<OutEntity>> GetPageListAsync<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression, PageModel page)
    {
        RefAsync<int> count = 0;
        var result = await _db.Queryable<T>().Where(whereExpression).ToPageListAsync(page.PageIndex, page.PageSize, count);
        page.TotalCount = count;
        return result.Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <param name="page">分页信息</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="orderByType">升降排序</param>
    /// <returns></returns>
    public static async Task<List<OutEntity>> GetPageListAsync<T, OutEntity>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        RefAsync<int> count = 0;
        var result = await _db.Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType).Where(whereExpression).ToPageListAsync(page.PageIndex, page.PageSize, count);
        page.TotalCount = count;
        return result.Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="conditionalList">条件集合</param>
    /// <param name="page">分页信息</param>
    /// <returns></returns>
    public static async Task<List<OutEntity>> GetPageListAsync<T, OutEntity>(this ISqlSugarClient _db, List<IConditionalModel> conditionalList, PageModel page)
    {
        RefAsync<int> count = 0;
        var result = await _db.Queryable<T>().Where(conditionalList).ToPageListAsync(page.PageIndex, page.PageSize, count);
        page.TotalCount = count;
        return result.Adapt<List<OutEntity>>();
    }

    /// <summary>
    /// 查询分页数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="OutEntity">输出实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="conditionalList">条件集合</param>
    /// <param name="page">分页信息</param>
    /// <param name="orderByExpression">排序表达式</param>
    /// <param name="orderByType">升降排序</param>
    /// <returns></returns>
    public static async Task<List<OutEntity>> GetPageListAsync<T, OutEntity>(this ISqlSugarClient _db, List<IConditionalModel> conditionalList, PageModel page, Expression<Func<T, object>> orderByExpression = null, OrderByType orderByType = OrderByType.Asc)
    {
        RefAsync<int> count = 0;
        var result = await _db.Queryable<T>().OrderByIF(orderByExpression != null, orderByExpression, orderByType).Where(conditionalList).ToPageListAsync(page.PageIndex, page.PageSize, count);
        page.TotalCount = count;
        return result.Adapt<List<OutEntity>>();
    }

    #endregion 查询数据-异步-映射转换为指定对象

    #region 新增-同步

    /// <summary>
    /// 新增
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>返回bool类型</returns>
    public static bool Insert<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return _db.Insertable(insertObj).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 新增或更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="data">对象</param>
    /// <returns>返回bool类型</returns>
    public static bool InsertOrUpdate<T>(this ISqlSugarClient _db, T data) where T : class, new()
    {
        return _db.Storageable(data).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 新增或更新集合
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="datas">对象</param>
    /// <returns>返回bool类型</returns>
    public static bool InsertOrUpdate<T>(this ISqlSugarClient _db, List<T> datas) where T : class, new()
    {
        return _db.Storageable(datas).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 新增并返回自增ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>自增ID</returns>
    public static int InsertReturnIdentity<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return _db.Insertable(insertObj).ExecuteReturnIdentity();
    }

    /// <summary>
    /// 新增并返回自增long ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>自增long ID</returns>
    public static long InsertReturnBigIdentity<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return _db.Insertable(insertObj).ExecuteReturnBigIdentity();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>雪花ID</returns>
    public static long InsertReturnSnowflakeId<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return _db.Insertable(insertObj).ExecuteReturnSnowflakeId();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObjs">集合</param>
    /// <returns>雪花ID集合</returns>
    public static List<long> InsertReturnSnowflakeId<T>(this ISqlSugarClient _db, List<T> insertObjs) where T : class, new()
    {
        return _db.Insertable(insertObjs).ExecuteReturnSnowflakeIdList();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>雪花ID</returns>
    public static Task<long> InsertReturnSnowflakeIdAsync<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return _db.Insertable(insertObj).ExecuteReturnSnowflakeIdAsync();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObjs">集合</param>
    /// <returns>雪花ID集合</returns>
    public static Task<List<long>> InsertReturnSnowflakeIdAsync<T>(this ISqlSugarClient _db, List<T> insertObjs) where T : class, new()
    {
        return _db.Insertable(insertObjs).ExecuteReturnSnowflakeIdListAsync();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>雪花ID</returns>
    public static T InsertReturnEntity<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return _db.Insertable(insertObj).ExecuteReturnEntity();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObjs">数组</param>
    /// <returns>雪花ID</returns>
    public static bool Insert<T>(this ISqlSugarClient _db, T[] insertObjs) where T : class, new()
    {
        return _db.Insertable(insertObjs).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObjs">数组</param>
    /// <returns>雪花ID</returns>
    public static bool Insert<T>(this ISqlSugarClient _db, List<T> insertObjs) where T : class, new()
    {
        return _db.Insertable(insertObjs).ExecuteCommand() > 0;
    }

    #endregion 新增-同步

    #region 新增-异步

    /// <summary>
    /// 新增或更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="data">对象</param>
    /// <returns></returns>
    public static async Task<bool> InsertOrUpdateAsync<T>(this ISqlSugarClient _db, T data) where T : class, new()
    {
        return await _db.Storageable(data).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 新增或更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="datas">集合</param>
    /// <returns></returns>
    public static async Task<bool> InsertOrUpdateAsync<T>(this ISqlSugarClient _db, List<T> datas) where T : class, new()
    {
        return await _db.Storageable(datas).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 新增
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns></returns>
    public static async Task<bool> InsertAsync<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return await _db.Insertable(insertObj).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 新增 -自增ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns></returns>
    public static async Task<int> InsertReturnIdentityAsync<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return await _db.Insertable(insertObj).ExecuteReturnIdentityAsync();
    }

    /// <summary>
    /// 新增-long自增ID
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns></returns>
    public static async Task<long> InsertReturnBigIdentityAsync<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return await _db.Insertable(insertObj).ExecuteReturnBigIdentityAsync();
    }

    /// <summary>
    /// 新增
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>新增实体</returns>
    public static async Task<T> InsertReturnEntityAsync<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return await _db.Insertable(insertObj).ExecuteReturnEntityAsync();
    }

    /// <summary>
    /// 新增
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObjs">数组</param>
    /// <returns></returns>
    public static async Task<bool> InsertAsync<T>(this ISqlSugarClient _db, T[] insertObjs) where T : class, new()
    {
        return await _db.Insertable(insertObjs).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 新增
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObjs">集合</param>
    /// <returns></returns>
    public static async Task<bool> InsertAsync<T>(this ISqlSugarClient _db, List<T> insertObjs) where T : class, new()
    {
        return await _db.Insertable(insertObjs).ExecuteCommandAsync() > 0;
    }

    #endregion 新增-异步

    #region 表达式更新-同步

    /// <summary>
    /// 更新
    /// 【不追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="columns"></param>
    /// <param name="whereExpression"></param>
    /// <returns></returns>
    public static bool Update<T>(this ISqlSugarClient _db, Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        return _db.Updateable<T>().SetColumns(columns).Where(whereExpression).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 更新
    /// 【追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="columns"></param>
    /// <param name="whereExpression"></param>
    /// <returns></returns>
    public static bool UpdateSetColumnsTrue<T>(this ISqlSugarClient _db, Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        return _db.Updateable<T>().SetColumns(columns, true).Where(whereExpression).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 设置状态，bool类型的字段
    /// 例如：IsEnable 开关字段
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="id">主键的值</param>
    /// <param name="state">状态，bool类型</param>
    /// <param name="field">更新的字段名</param>
    /// <param name="primaryKeyField">组件字段，默认 id</param>
    /// <returns></returns>
    public static bool SetStateById<T>(this ISqlSugarClient _db, dynamic id, bool state, string field, string primaryKeyField = "id") where T : class, new()
    {
        var dt = new Dictionary<string, object>();
        dt.Add(primaryKeyField, id);
        dt.Add(field, state);//需要更新的字段值
        return _db.Updateable<T>(dt).WhereColumns("id").ExecuteCommand() > 0;
    }

    #endregion 表达式更新-同步

    #region 实体对象更新-同步

    /// <summary>
    /// 更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="updateObj">对象</param>
    /// <returns></returns>
    public static bool Update<T>(this ISqlSugarClient _db, T updateObj) where T : class, new()
    {
        return _db.Updateable(updateObj).IgnoreColumns(UpdateIgnoreColumns).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="updateObjs">数组</param>
    /// <returns></returns>
    public static bool Update<T>(this ISqlSugarClient _db, T[] updateObjs) where T : class, new()
    {
        return _db.Updateable(updateObjs).IgnoreColumns(UpdateIgnoreColumns).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="updateObjs">集合</param>
    /// <returns></returns>
    public static bool Update<T>(this ISqlSugarClient _db, List<T> updateObjs) where T : class, new()
    {
        return _db.Updateable(updateObjs).IgnoreColumns(UpdateIgnoreColumns).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 更新
    /// 【不追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="obj">更新的对象</param>
    /// <param name="columns"></param>
    /// <returns></returns>
    public static bool Update<T>(this ISqlSugarClient _db, T obj, Expression<Func<T, object>> columns) where T : class, new()
    {
        return _db.Updateable<T>(obj).UpdateColumns(columns).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 更新
    /// 【追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="obj">更新的对象</param>
    /// <param name="columns"></param>
    /// <returns></returns>
    public static bool UpdateSetColumnsTrue<T>(this ISqlSugarClient _db, T obj, Expression<Func<T, object>> columns) where T : class, new()
    {
        return _db.Updateable<T>(obj).UpdateColumns(columns).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 更新
    /// 【追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="obj">更新的对象</param>
    /// <param name="columns">指定条件更新的列,例如：WhereColumns(it=>new { it.Id,it.Name}) //条件列不会被更新，只会作为条件</param>
    /// <returns></returns>
    public static bool UpdateByWhere<T>(this ISqlSugarClient _db, T obj, Expression<Func<T, object>> columns) where T : class, new()
    {
        return _db.Updateable<T>(obj).WhereColumns(columns).ExecuteCommand() > 0;
    }

    #endregion 实体对象更新-同步

    #region 表达式更新-异步

    /// <summary>
    /// 更新
    /// 【不追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="columns">更新的列</param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<bool> UpdateAsync<T>(this ISqlSugarClient _db, Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        return await _db.Updateable<T>().SetColumns(columns).Where(whereExpression).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 更新
    /// 【追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="columns">更新的列</param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<bool> UpdateSetColumnsTrueAsync<T>(this ISqlSugarClient _db, Expression<Func<T, T>> columns, Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        return await _db.Updateable<T>().SetColumns(columns, true).Where(whereExpression).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 设置状态，bool类型的字段
    /// 例如：IsEnable 开关字段
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="id">主键的值</param>
    /// <param name="state">状态，bool类型</param>
    /// <param name="field">更新的字段名</param>
    /// <param name="primaryKeyField">组件字段，默认 id</param>
    /// <returns></returns>
    public static async Task<bool> SetStateByIdAsync<T>(this ISqlSugarClient _db, dynamic id, bool state, string field, string primaryKeyField = "id") where T : class, new()
    {
        var dt = new Dictionary<string, object>();
        dt.Add(primaryKeyField, id);
        dt.Add(field, state);//需要更新的字段值
        return await _db.Updateable<T>(dt).WhereColumns("id").ExecuteCommandAsync() > 0;
    }

    #endregion 表达式更新-异步

    #region 实体更新-异步

    /// <summary>
    /// 更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="updateObj">对象</param>
    /// <returns></returns>
    public static async Task<bool> UpdateAsync<T>(this ISqlSugarClient _db, T updateObj) where T : class, new()
    {
        return await _db.Updateable(updateObj).IgnoreColumns(UpdateIgnoreColumns).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="updateObjs">数组</param>
    /// <returns></returns>
    public static async Task<bool> UpdateAsync<T>(this ISqlSugarClient _db, T[] updateObjs) where T : class, new()
    {
        return await _db.Updateable(updateObjs).IgnoreColumns(UpdateIgnoreColumns).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="updateObjs">集合</param>
    /// <returns></returns>
    public static async Task<bool> UpdateAsync<T>(this ISqlSugarClient _db, List<T> updateObjs) where T : class, new()
    {
        return await _db.Updateable(updateObjs).IgnoreColumns(UpdateIgnoreColumns).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 更新
    /// 【不追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="obj">更新的对象</param>
    /// <param name="columns">更新的列</param>
    /// <returns></returns>
    public static async Task<bool> UpdateAsync<T>(this ISqlSugarClient _db, T obj, Expression<Func<T, object>> columns) where T : class, new()
    {
        return await _db.Updateable<T>(obj).UpdateColumns(columns).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 更新
    /// 【追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="obj">更新的对象</param>
    /// <param name="columns">更新的列</param>
    /// <returns></returns>
    public static async Task<bool> UpdateSetColumnsTrueAsync<T>(this ISqlSugarClient _db, T obj, Expression<Func<T, object>> columns) where T : class, new()
    {
        return await _db.Updateable<T>(obj).UpdateColumns(columns).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 更新
    /// 【追加更新过滤器赋值字段】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="obj">更新的对象</param>
    /// <param name="columns">指定条件更新的列,例如：WhereColumns(it=>new { it.Id,it.Name}) //条件列不会被更新，只会作为条件</param>
    /// <returns></returns>
    public static async Task<bool> UpdateByWhereAsync<T>(this ISqlSugarClient _db, T obj, Expression<Func<T, object>> columns) where T : class, new()
    {
        return await _db.Updateable<T>(obj).WhereColumns(columns).ExecuteCommandAsync() > 0;
    }

    #endregion 实体更新-异步

    #region 是否存在，数量

    /// <summary>
    /// 是否存在
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static bool IsAny<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return _db.Queryable<T>().Where(whereExpression).Any();
    }

    /// <summary>
    /// 获取数量
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static int Count<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return _db.Queryable<T>().Where(whereExpression).Count();
    }

    /// <summary>
    /// 是否存在
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression"></param>
    /// <returns></returns>
    public static async Task<bool> IsAnyAsync<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return await _db.Queryable<T>().Where(whereExpression).AnyAsync();
    }

    /// <summary>
    /// 数量
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression"></param>
    /// <returns></returns>
    public static async Task<int> CountAsync<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression)
    {
        return await _db.Queryable<T>().Where(whereExpression).CountAsync();
    }

    #endregion 是否存在，数量

    #region 删除-同步

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="deleteObj">对象</param>
    /// <returns></returns>
    public static bool Delete<T>(this ISqlSugarClient _db, T deleteObj) where T : class, new()
    {
        return _db.Deleteable<T>().Where(deleteObj).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="deleteObjs">集合</param>
    /// <returns></returns>
    public static bool Delete<T>(this ISqlSugarClient _db, List<T> deleteObjs) where T : class, new()
    {
        return _db.Deleteable<T>().Where(deleteObjs).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static bool Delete<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        return _db.Deleteable<T>().Where(whereExpression).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 更加主键删除数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool DeleteById<T>(this ISqlSugarClient _db, dynamic id) where T : class, new()
    {
        return _db.Deleteable<T>().In(id).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 根据数组删除数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="ids"></param>
    /// <returns></returns>
    public static bool DeleteByIds<T>(this ISqlSugarClient _db, dynamic[] ids) where T : class, new()
    {
        return _db.Deleteable<T>().In(ids).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 根据数组删除数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="ids"></param>
    /// <returns></returns>
    public static bool DeleteByIds<T>(this ISqlSugarClient _db, List<dynamic> ids) where T : class, new()
    {
        return _db.Deleteable<T>().In(ids).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="_db"></param>
    /// <param name="ids">id字符串，英文逗号隔开</param>
    /// <returns></returns>
    public static bool DeleteByIds<T>(this ISqlSugarClient _db, string ids) where T : class, new()
    {
        var objs = ids.ToSplitObjects();
        return _db.Deleteable<T>().In(objs).ExecuteCommand() > 0;
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="_db"></param>
    /// <param name="ids">id字符串，英文逗号隔开</param>
    /// <returns></returns>
    public static async Task<bool> DeleteByIdsAsync<T>(this ISqlSugarClient _db, string ids) where T : class, new()
    {
        var objs = ids.ToSplitObjects();
        return await _db.Deleteable<T>().In(objs).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 删除数据 根据ID字符串
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="ids">主键字符串</param>
    /// <returns></returns>
    public static bool DeleteByIdStr<T>(this ISqlSugarClient _db, string ids) where T : class, new()
    {
        var objs = ids.ToSplitObjects();
        return _db.Deleteable<T>().In(objs).ExecuteCommand() > 0;
    }

    #endregion 删除-同步

    #region 删除-异步

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="deleteObj">对象</param>
    /// <returns></returns>
    public static async Task<bool> DeleteAsync<T>(this ISqlSugarClient _db, T deleteObj) where T : class, new()
    {
        return await _db.Deleteable<T>().Where(deleteObj).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="deleteObjs">集合</param>
    /// <returns></returns>
    public static async Task<bool> DeleteAsync<T>(this ISqlSugarClient _db, List<T> deleteObjs) where T : class, new()
    {
        return await _db.Deleteable<T>().Where(deleteObjs).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<bool> DeleteAsync<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        return await _db.Deleteable<T>().Where(whereExpression).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 更加主键删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="id">主键ID</param>
    /// <returns></returns>
    public static async Task<bool> DeleteByIdAsync<T>(this ISqlSugarClient _db, dynamic id) where T : class, new()
    {
        return await _db.Deleteable<T>().In(id).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="ids">主键集合</param>
    /// <returns></returns>
    public static async Task<bool> DeleteByIdsAsync<T>(this ISqlSugarClient _db, dynamic[] ids) where T : class, new()
    {
        return await _db.Deleteable<T>().In(ids).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="ids">主键集合</param>
    /// <returns></returns>
    public static async Task<bool> DeleteByIdsAsync<T>(this ISqlSugarClient _db, List<dynamic> ids) where T : class, new()
    {
        return await _db.Deleteable<T>().In(ids).ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 删除数据 根据ID字符串
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="ids">主键字符串</param>
    /// <returns></returns>
    public static async Task<bool> DeleteByIdStrAsync<T>(this ISqlSugarClient _db, string ids) where T : class, new()
    {
        var objs = ids.ToSplitObjects();
        return await _db.Deleteable<T>().In(objs).ExecuteCommandAsync() > 0;
    }

    #endregion 删除-异步

    #region 假删除

    /// <summary>
    /// 假删除
    /// </summary>
    /// <typeparam name="T">实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="ids">删除的id字符串，英文逗号隔开</param>
    /// <returns></returns>
    public static bool FakeDelete<T>(this ISqlSugarClient _db, string ids) where T : class, new()
    {
        var objs = ids.ToSplitObjects();
        return _db.Deleteable<T>().In(objs)
            .IsLogic()
            .ExecuteCommand("IsDeleted", true, "DeletedTime", "DeletedUserName", App.HttpContext.GetLoginUserAccount()) > 0;
    }

    /// <summary>
    /// 假删除
    /// </summary>
    /// <typeparam name="T">实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="ids">删除的id字符串，英文逗号隔开</param>
    /// <returns></returns>
    public static async Task<bool> FakeDeleteAsync<T>(this ISqlSugarClient _db, string ids) where T : class, new()
    {
        var objs = ids.ToSplitObjects();
        return await _db.Deleteable<T>().In(objs)
             .IsLogic()
             .ExecuteCommandAsync("IsDeleted", true, "DeletedTime", "DeletedUserName", App.HttpContext.GetLoginUserAccount()) > 0;
    }

    /// <summary>
    /// 假删除
    /// </summary>
    /// <typeparam name="T">实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static bool FakeDelete<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        return _db.Deleteable<T>().Where(whereExpression)
            .IsLogic()
            .ExecuteCommand("IsDeleted", true, "DeletedTime", "DeletedUserName", App.HttpContext.GetLoginUserAccount()) > 0;
    }

    /// <summary>
    /// 假删除
    /// </summary>
    /// <typeparam name="T">实体</typeparam>
    /// <param name="_db"></param>
    /// <param name="whereExpression">条件表达式</param>
    /// <returns></returns>
    public static async Task<bool> FakeDeleteAsync<T>(this ISqlSugarClient _db, Expression<Func<T, bool>> whereExpression) where T : class, new()
    {
        return await _db.Deleteable<T>().Where(whereExpression)
             .IsLogic()
             .ExecuteCommandAsync("IsDeleted", true, "DeletedTime", "DeletedUserName", App.HttpContext.GetLoginUserAccount()) > 0;
    }

    #endregion 假删除

    #region 新增简写扩展

    /// <summary>
    /// 新增并返回自增ID
    /// 【InsertReturnIdentity】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>自增ID</returns>
    public static int InsertRId<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return _db.Insertable(insertObj).ExecuteReturnIdentity();
    }

    /// <summary>
    /// 新增并返回自增long ID
    /// 【InsertReturnBigIdentity】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>自增long ID</returns>
    public static long InsertRbId<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return _db.Insertable(insertObj).ExecuteReturnBigIdentity();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// 【InsertReturnSnowflakeId】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>雪花ID</returns>
    public static long InsertRsId<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return _db.Insertable(insertObj).ExecuteReturnSnowflakeId();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// 【InsertReturnSnowflakeId】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObjs">集合</param>
    /// <returns>雪花ID集合</returns>
    public static List<long> InsertRsId<T>(this ISqlSugarClient _db, List<T> insertObjs) where T : class, new()
    {
        return _db.Insertable(insertObjs).ExecuteReturnSnowflakeIdList();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// 【InsertReturnSnowflakeIdAsync】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObj">对象</param>
    /// <returns>雪花ID</returns>
    public static async Task<long> InsertRsIdAsync<T>(this ISqlSugarClient _db, T insertObj) where T : class, new()
    {
        return await _db.Insertable(insertObj).ExecuteReturnSnowflakeIdAsync();
    }

    /// <summary>
    /// 新增并返回雪花ID
    /// 【InsertReturnSnowflakeIdAsync】
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_db"></param>
    /// <param name="insertObjs">集合</param>
    /// <returns>雪花ID集合</returns>
    public static async Task<List<long>> InsertRsIdAsync<T>(this ISqlSugarClient _db, List<T> insertObjs) where T : class, new()
    {
        return await _db.Insertable(insertObjs).ExecuteReturnSnowflakeIdListAsync();
    }

    #endregion 新增简写扩展

    #region AOP

    /// <summary>
    /// 初始化数据库AOP
    /// </summary>
    /// <param name="db"></param>
    public static void InitDbAop(this ISqlSugarClient db)
    {
        SqlSugarHelper.SqlSugarDbSetting.ConnectionConfigs.ForEach(config =>
        {
            ////配置
            var dbProvider = db.AsTenant().GetConnectionScope((string)config.ConfigId);
            // 设置AOP
            dbProvider.SetAop();
        });
    }

    /// <summary>
    /// 设置 sqlsugar config
    /// </summary>
    /// <param name="db"></param>
    /// <param name="configId"></param>
    public static void SetAopByConfigId(this ISqlSugarClient db, string configId = "TUI")
    {
        var dbProvider = db.AsTenant().GetConnectionScope(configId);
        dbProvider.SetAop();
    }

    /// <summary>
    /// 设置AOP 和日志
    /// </summary>
    /// <param name="dbProvider"></param>
    public static void SetAop(this ISqlSugarClient dbProvider)
    {
        // 设置超时时间
        dbProvider.Ado.CommandTimeOut = 30;

        //每次Sql执行前事件
        dbProvider.SqlSugar_OnLogExecuting();
        //SQL执行完
        dbProvider.SqlSugar_OnLogExecuted();
        //可以修改SQL和参数的值
        dbProvider.SqlSugar_OnExecutingChangeSql();
        //SQL报错
        dbProvider.SqlSugar_OnError();
        // 差异日志功能
        dbProvider.SqlSugar_OnDiffLogEvent();
        //插入和更新过滤器
        dbProvider.SqlSugar_DataExecuting();
        //全局过滤器
        dbProvider.SqlSugar_Filter();
    }

    /// <summary>
    /// 可以修改SQL和参数的值
    /// </summary>
    /// <param name="dbProvider"></param>
    /// <returns></returns>
    public static void SqlSugar_OnExecutingChangeSql(this ISqlSugarClient dbProvider)
    {
        dbProvider.Aop.OnExecutingChangeSql = (sql, pars) =>
        {
            //LogEx.Debug(sql,"SqlSugar\\修改SQL和参数的值");
            //sql=newsql
            //foreach(var p in pars) //修改
            return new KeyValuePair<string, SugarParameter[]>(sql, pars);
        };
    }

    /// <summary>
    /// 每次Sql执行前事件
    /// </summary>
    /// <param name="dbProvider"></param>
    public static void SqlSugar_OnLogExecuting(this ISqlSugarClient dbProvider)
    {
        dbProvider.Aop.OnLogExecuting = (sql, pars) =>
        {
            //5.0.8.2 获取无参数化 SQL
            var sqlStr = UtilMethods.GetSqlString(dbProvider.CurrentConnectionConfig.DbType, sql, pars);

            var sqlSugarDbSetting = AppEx.GetConfig<SqlSugarDbSettingOptions>();
            if (sqlSugarDbSetting.IsRecordExecSql == true)
            {
                LogEx.Debug($"执行SQL:{sqlStr}", "SqlSugar\\AOP\\每次Sql执行前事件");
            }

            #region 控制台打印

            if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Green;
            if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) || sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.White;
            if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("【" + DateTime.Now + "——执行SQL】\r\n" + sqlStr + "\r\n");

            App.PrintToMiniProfiler("SqlSugar", "Info", sqlStr);

            #endregion 控制台打印
        };
    }

    /// <summary>
    /// SQL执行完
    /// </summary>
    /// <param name="dbProvider"></param>
    public static void SqlSugar_OnLogExecuted(this ISqlSugarClient dbProvider)
    {
        dbProvider.Aop.OnLogExecuted = (sql, pars) =>
        {
            //执行时间超过1秒
            if (dbProvider.Ado.SqlExecutionTime.TotalSeconds > 1)
            {
                //代码CS文件名
                var fileName = dbProvider.Ado.SqlStackTrace.FirstFileName;
                //代码行数
                var fileLine = dbProvider.Ado.SqlStackTrace.FirstLine;
                //方法名
                var FirstMethodName = dbProvider.Ado.SqlStackTrace.FirstMethodName;
                //dbProvider.Ado.SqlStackTrace.MyStackTraceList[1].xxx 获取上层方法的信息

                LogEx.Debug($"代码cs文件夹名：{fileName}\r\n 代码行数：{fileLine}\r\n 方法名：{FirstMethodName}\r\nsql:{sql}\r\n参数：{pars.ToJson()}", "SqlSugar\\AOP\\SQL执行时间超过1秒事件");
            }
        };
    }

    /// <summary>
    /// 差异日志功能
    /// </summary>
    /// <param name="dbProvider"></param>
    public static void SqlSugar_OnDiffLogEvent(this ISqlSugarClient dbProvider)
    {
        //差异日志功能
        //Sql执行完后会进该事件，该事件可以拿到更改前记录和更改后记录，执行时间等参数（可以监控表变动）
        dbProvider.Aop.OnDiffLogEvent = (diffLogModel) =>
        {
            var sqlSugarDbSetting = AppEx.GetConfig<SqlSugarDbSettingOptions>();
            if (sqlSugarDbSetting.EnableDiffLog == false) return;
            try
            {
                //调用
                foreach (var item in SqlSugarHelper.GetMethods(typeof(ISqlSugarDiffLogListener)))
                {
                    var instance = Activator.CreateInstance(item.Key);
                    item.Value.Invoke(instance, new object[] { dbProvider, diffLogModel });
                }
            }
            catch (Exception ex)
            {
                LogEx.Error(ex, "SqlSugar\\AOP\\差异日志");
            }
        };
    }

    /// <summary>
    /// SQL报错
    /// </summary>
    /// <param name="dbProvider"></param>
    public static void SqlSugar_OnError(this ISqlSugarClient dbProvider)
    {
        dbProvider.Aop.OnError = (exp) =>
        {
            try
            {
                #region 控制台打印

                Console.ForegroundColor = ConsoleColor.Red;
                //var pars = dbProvider.Utilities.SerializeObject(((SugarParameter[])exp.Parametres).ToDictionary(it => it.ParameterName, it => it.Value));
                var pars = "";
                Console.WriteLine("【" + DateTime.Now + "——错误SQL】\r\n" + UtilMethods.GetSqlString(dbProvider.CurrentConnectionConfig.DbType, exp.Sql, (SugarParameter[])exp.Parametres) + "\r\n");
                App.PrintToMiniProfiler("SqlSugar", "Error", $"{exp.Message}{Environment.NewLine}{exp.Sql}{pars}{Environment.NewLine}");

                #endregion 控制台打印

                //exp.sql 这样可以拿到错误SQL
                //5.0.8.2 获取无参数化 SQL
                //UtilMethods.GetSqlString(DbType.SqlServer,exp.sql,exp.parameters)

                var sqlStr = UtilMethods.GetSqlString(dbProvider.CurrentConnectionConfig.DbType, exp.Sql, (SugarParameter[])exp.Parametres);

                LogEx.Debug($"Source:{exp.Source}\r\nSQL：{sqlStr}\r\nStackTrace:{exp.StackTrace}\r\nInnerException:{exp.InnerException.ToStringEx()}", "SqlSugar\\AOP\\OnError");
            }
            catch (Exception ex)
            {
                LogEx.Error(ex, "SqlSugar\\AOP\\OnError");
            }
        };
    }

    /// <summary>
    /// 插入和更新过滤器
    /// </summary>
    /// <param name="dbProvider"></param>
    public static void SqlSugar_DataExecuting(this ISqlSugarClient dbProvider)
    {
        dbProvider.Aop.DataExecuting = (oldValue, entityInfo) =>
        {
            /*oldValue表示当前字段值 等同于下面写法*/
            //var value=entityInfo.EntityColumnInfo.PropertyInfo.GetValue(entityInfo.EntityValue);

            try
            {
                if (entityInfo.EntityColumnInfo.IsPrimarykey) //通过主键保证只进一次事件
                {
                    //获取主键的值
                    //var value = entityInfo.EntityColumnInfo.PropertyInfo.GetValue(entityInfo.EntityValue);
                    foreach (var item in SqlSugarHelper.GetMethods(typeof(ISqlSugarDataChangedListener)))
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(item.Key);
                            item.Value.Invoke(instance, new object[] { dbProvider, entityInfo });
                        }
                        catch (Exception ex2)
                        {
                            LogEx.Error(ex2, "SqlSugar\\AOP\\数据改变监听");
                        }

                    }
                }
                //登陆的用户ID
                var loginUserId = App.HttpContext.GetLoginUserId<long>();
                //登陆的用户账号
                var loginUserAccount = App.HttpContext.GetLoginUserAccount();

                //这样每条记录就只执行一次
                // SqlSugarCustomerCacheCenter.DataFilter(entityInfo);
                //entityInfo 有字段所有参数

                //兼容其它ORM AOP写法
                //SqlSugar是通过每行记录每个值进行的细粒度AOP,如果一行数据只想进一次事件
                if (entityInfo.EntityColumnInfo.IsPrimarykey) //通过主键保证只进一次事件
                {
                    #region inset生效

                    ////判断是否是自增，不是自增就使用雪花ID，必须是long类型
                    //if (entityInfo.EntityColumnInfo.IsPrimarykey == true && entityInfo.EntityColumnInfo.IsIdentity == false && entityInfo.EntityColumnInfo.PropertyInfo.PropertyType.Name == nameof(System.Int64))
                    //{
                    //    var filedvalue = entityInfo.EntityValue.GetFieldValue<long>(entityInfo.EntityColumnInfo.DbColumnName);
                    //    if (filedvalue <= 0)
                    //    {
                    //        //SetFiledValue(entityInfo, "Id", SnowFlakeSingle.Instance.NextId());
                    //        SetFiledValue(entityInfo, entityInfo.EntityColumnInfo.DbColumnName, YitIdHelper.NextId());
                    //    }
                    //}

                    if (entityInfo.OperationType == DataFilterType.InsertByObject)
                    {
                        {
                            var isManual = false;
                            //获取当前字段的值
                            var properyInfo = entityInfo.EntityValue.GetType().GetProperty("CreatorUserId");
                            if (properyInfo != null)
                            {
                                var value1 = properyInfo.GetValue(entityInfo.EntityValue);
                                if (value1 != null && value1.ToInt32() > 0)
                                {
                                    //跳过
                                    isManual = true;//手动赋值了，不再自动赋值
                                }
                            }
                            //获取到的登陆用户ID大于0，且没有手动设置，才自动设置
                            if (loginUserId > 0 && isManual == false) SetFiledValue(entityInfo, "CreatorUserId", loginUserId);
                        }
                        {
                            var isManual = false;
                            //获取当前字段的值
                            var properyInfo = entityInfo.EntityValue.GetType().GetProperty("CreatorUserName");
                            if (properyInfo != null)
                            {
                                var value1 = properyInfo.GetValue(entityInfo.EntityValue);
                                if (value1 != null && !string.IsNullOrWhiteSpace(value1.ToString()))
                                {
                                    //跳过
                                    isManual = true;//手动赋值了，不再自动赋值
                                }
                            }
                            //获取到的登陆账号不为空，且没有手动设置，才自动设置
                            if (!string.IsNullOrWhiteSpace(loginUserAccount) && isManual == false) SetFiledValue(entityInfo, "CreatorUserName", loginUserAccount);
                        }
                        {
                            var isManual = false;
                            //获取当前字段的值
                            var properyInfo = entityInfo.EntityValue.GetType().GetProperty("CreationTime");
                            if (properyInfo != null)
                            {
                                var value1 = properyInfo.GetValue(entityInfo.EntityValue);
                                if (value1 != null && DateTimeOffset.TryParse(value1.ToString(), out DateTimeOffset dt))
                                {
                                    if (dt > DateTimeOffset.MinValue)
                                    {
                                        //跳过
                                        isManual = true;//手动赋值了，不再自动赋值
                                    }
                                }
                            }
                            //没有手动设置，才自动设置
                            if (isManual == false) SetFiledValue(entityInfo, "CreationTime", DateTimeOffset.Now);
                        }
                    }

                    #endregion inset生效

                    #region update生效

                    if (entityInfo.OperationType == DataFilterType.UpdateByObject)
                    {
                        //是否逻辑删除(假删除)
                        //if (entityInfo.PropertyName == "IsDeleted")
                        //{
                        var properyDate = entityInfo.EntityValue.GetType().GetProperty("IsDeleted");
                        //判断是否有 IsDeleted字段，如果有才执行下面代码，否则会报错
                        if (properyDate != null)
                        {
                            var isdeleted = properyDate.GetValue(entityInfo.EntityValue, null).To<bool>();
                            if (isdeleted)
                            {
                                if (loginUserId > 0) SetFiledValue(entityInfo, "DeletedUserId", loginUserId);
                                if (!string.IsNullOrWhiteSpace(loginUserAccount)) SetFiledValue(entityInfo, "DeletedUserName", loginUserAccount);
                                SetFiledValue(entityInfo, "DeletedTime", DateTimeOffset.Now);
                            }
                            else
                            {
                                SetUpdateInfo(entityInfo);
                            }
                        }
                        else
                        {
                            SetUpdateInfo(entityInfo);
                        }

                        void SetUpdateInfo(DataFilterModel entityInfo)
                        {
                            if (loginUserId > 0) SetFiledValue(entityInfo, "UpdateUserId", loginUserId);
                            if (!string.IsNullOrWhiteSpace(loginUserAccount)) SetFiledValue(entityInfo, "UpdateUserName", loginUserAccount);
                            SetFiledValue(entityInfo, "UpdateTime", DateTimeOffset.Now);
                        }
                    }

                    #endregion update生效

                    #region delete

                    if (entityInfo.OperationType == DataFilterType.DeleteByObject)
                    {
                        var properyDate = entityInfo.EntityValue.GetType().GetProperty("IsDeleted");
                        if (properyDate != null)
                        {
                            //判断是否假删除，如果是，则标记为删除就可以了
                            var isdeleted = properyDate.GetValue(entityInfo.EntityValue, null).To<bool>();
                            if (isdeleted)
                            {
                                if (loginUserId > 0) SetFiledValue(entityInfo, "DeletedUserId", loginUserId);
                                if (!string.IsNullOrWhiteSpace(loginUserAccount)) SetFiledValue(entityInfo, "DeletedUserName", loginUserAccount);
                                SetFiledValue(entityInfo, "DeletedTime", DateTimeOffset.Now);
                            }
                            else
                            {
                                //真删除，不用处理
                            }
                        }
                        else
                        {
                            //真删除，不用处理
                        }
                    }

                    #endregion delete
                }

                //设置指定字段的值
                static void SetFiledValue(DataFilterModel entityInfo, string filedName, object value)
                {
                    var properyInfo = entityInfo.EntityValue.GetType().GetProperty(filedName);
                    //获取当前字段的值
                    //var value1 = properyInfo.GetValue(entityInfo.EntityValue);
                    if (properyInfo != null)
                    {
                        properyInfo.SetValue(entityInfo.EntityValue, value);
                    }
                }

                //根据当前列修改另一列 可以么写
                //if(当前列逻辑==XXX)
                //var properyDate = entityInfo.EntityValue.GetType().GetProperty("Date");
                //if(properyDate!=null)
                //properyDate.SetValue(entityInfo.EntityValue,1);
            }
            catch (Exception ex)
            {
                LogEx.Error(ex, "SqlSugar\\AOP\\插入更新删除过滤器");
            }
        };
    }

    /// <summary>
    /// 全局过滤器
    /// </summary>
    public static void SqlSugar_Filter(this ISqlSugarClient dbProvider)
    {
        try
        {
            List<Type> types = App.EffectiveTypes.Where(a => !a.IsAbstract && a.IsClass
            && (a.GetCustomAttributes(typeof(SugarTable), true)?.FirstOrDefault() != null
            || a.GetCustomAttributes(typeof(TableAttribute), true)?.FirstOrDefault() != null)

            ).ToList();

            foreach (var entityType in types)
            {
                //// 配置多租户全局过滤器
                //if (!entityType.GetProperty("TenantId").IsEmpty())
                //{ //判断实体类中包含TenantId属性
                //  //构建动态Lambda
                //    var lambda = DynamicExpressionParser.ParseLambda
                //    (new[] { Expression.Parameter(entityType, "it") },
                //     typeof(bool), $"{nameof(EntityTenantBase.TenantId)} ==  @0 or (@1 and @2)",
                //      GetTenantId(), IsSuperAdmin(), superAdminViewAllData);
                //    dbProvider.QueryFilter.Add(new TableFilterItem<object>(entityType, lambda)); //将Lambda传入过滤器
                //}

                // 配置多租户全局过滤器
                // 共享数据表，共享 Schema (基于 TenantId 的方式)
                if (entityType != null && !entityType.GetProperty("TenantId").IsEmpty())
                {
                    var tenantId = AppEx.TenantId;
                    //判断实体类中包含TenantId属性
                    //构建动态Lambda
                    var lambda = DynamicExpressionParser.ParseLambda
                    (new[] { Expression.Parameter(entityType, "it") },
                     typeof(bool), $"{nameof(ITenantEntity.TenantId)} ==  @0",
                      tenantId);
                    dbProvider.QueryFilter.Add(new TableFilterItem<object>(entityType, lambda)); //将Lambda传入过滤器
                }

                // 配置加删除全局过滤器
                var deletePropertyInfo = entityType.GetProperty("IsDeleted");
                if (deletePropertyInfo != null && !deletePropertyInfo.IsEmpty())
                { //判断实体类中包含IsDeleted属性
                  //构建动态Lambda
                    var lambda = DynamicExpressionParser.ParseLambda
                    (new[] { Expression.Parameter(entityType, "it") },
    typeof(bool), $"{nameof(IDeleted.IsDeleted)} ==  @0",
                      false);
                    dbProvider.QueryFilter.Add(new TableFilterItem<object>(entityType, lambda)
                    {
                        IsJoinQuery = true //多表生效
                    }); //将Lambda传入过滤器
                }
            }
            //自定义全局 AOP 过滤器
            foreach (var item in SqlSugarHelper.GetMethods(typeof(ISqlSugarAopFilter)))
            {
                var instance = Activator.CreateInstance(item.Key);
                item.Value.Invoke(instance, new object[] { dbProvider });
            }


            ////全局过滤器
            //dbProvider.QueryFilter
            //.Add(new SqlFilterItem()//单表全局过滤器
            //{
            //    FilterValue = filterDb =>
            //    {
            //        return new SqlFilterResult() { Sql = " isDeleted=0" };
            //    },
            //    IsJoinQuery = false
            //}).Add(new SqlFilterItem()//多表全局过滤器
            //{
            //    FilterValue = filterDb =>
            //    {
            //        return new SqlFilterResult() { Sql = " f.isDeleted=0" };
            //    },
            //    IsJoinQuery = true
            //});
            //.Add(new SqlFilterItem() //qurery1过滤器
            //{
            //    FilterName = "query1",
            //    FilterValue = filterDb =>
            //    {
            //        return new SqlFilterResult() { Sql = " id>@id", Parameters = new { id = 1 } };
            //    },
            //    IsJoinQuery = false
            //});
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "SqlSugar\\AOP\\全局过滤器");
        }
    }

    #endregion AOP

    #region 缓存

    /// <summary>
    /// 移除sqlsugar 指定实体类型的缓存
    /// </summary>
    /// <typeparam name="TEntity">数据库实体名</typeparam>
    public static void RemoveCacheByContainEntityName<TEntity>(this ISqlSugarClient db)
    {
        try
        {
            var entityInfo = SqlSugarHelper.Db.EntityMaintenance.GetEntityInfo<TEntity>();

            db.RemoveCacheByContainStr(entityInfo.DbTableName);
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "SqlSugar\\删除缓存");
        }
    }

    /// <summary>
    ///  移除sqlsugar 包含指定字符串的 SqlSugar 缓存
    /// </summary>
    /// <param name="_db"></param>
    /// <param name="likeString">相似字符串</param>
    public static void RemoveCacheByContainStr(this ISqlSugarClient _db, string likeString)
    {
        try
        {
            var cacheKeys = Caches.GetAllCacheKeys("SqlSugarDataCache.");
            foreach (var item in cacheKeys)
            {
                if (item.Contains(likeString))
                {
                    Caches.Remove(item);
                }
            }
        }
        catch (Exception ex)
        {
            LogEx.Error(ex, "SqlSugar\\删除缓存");
        }
    }

    /// <summary>
    /// 移除sqlsugar 相关所有缓存
    /// </summary>
    /// <param name="_db"></param>
    public static void RemoveAllCache(this ISqlSugarClient _db)
    {
        var sqlsugarCacheKeys = Caches.GetAllCacheKeys("SqlSugarDataCache");
        foreach (var sqlsugarCacheKey in sqlsugarCacheKeys)
        {
            Caches.Remove(sqlsugarCacheKey);
        }
    }

    #region 异步

    /// <summary>
    /// 刷新表缓存
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static async Task<List<TEntity>> RefreshTableCacheByEntityAsync<TEntity>(this ISqlSugarClient db)
    {
        var entities = await db.Queryable<TEntity>().WithCache().ToListAsync();
        return entities;
    }

    /// <summary>
    /// 刷新表缓存
    /// </summary>
    /// <param name="db"></param>
    /// <param name="entityName"></param>
    /// <returns></returns>
    public static async Task<List<object>> RefreshTableCacheByEntityNameAsync(this ISqlSugarClient db, string entityName)
    {
        var entities = await db.Queryable<object>().AS(entityName).WithCache().ToListAsync();
        return entities;
    }

    #endregion 异步

    #region 同步

    /// <summary>
    /// 刷新表缓存
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static List<TEntity> RefreshTableCacheByEntity<TEntity>(this ISqlSugarClient db)
    {
        var entities = db.Queryable<TEntity>().WithCache().ToList();
        return entities;
    }

    /// <summary>
    /// 刷新表缓存
    /// </summary>
    /// <param name="db"></param>
    /// <param name="entityName"></param>
    /// <returns></returns>
    public static List<object> RefreshTableCacheByEntityName(this ISqlSugarClient db, string entityName)
    {
        var entities = db.Queryable<object>().AS(entityName).WithCache().ToList();
        return entities;
    }

    #endregion 同步

    #endregion 缓存

    #region 公共扩展方法
    /// <summary>
    /// List转DataTable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static DataTable ToDataTable<T>(this List<T> list)
    {
        DataTable result = new();
        if (list != null && list.Count > 0)
        {
            // result.TableName = list[0].GetType().Name; // 表名赋值
            PropertyInfo[] propertys = list[0].GetType().GetProperties();
            foreach (PropertyInfo pi in propertys)
            {
                Type colType = pi.PropertyType;
                if (colType.IsGenericType && colType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    colType = colType.GetGenericArguments()[0];
                }
                if (pi.IsIgnoreColumn())
                    continue;
                result.Columns.Add(pi.Name, colType);
            }
            for (int i = 0; i < list.Count; i++)
            {
                ArrayList tempList = new();
                foreach (PropertyInfo pi in propertys)
                {
                    if (pi.IsIgnoreColumn())
                        continue;
                    object obj = pi.GetValue(list[i], null);
                    tempList.Add(obj);
                }
                object[] array = tempList.ToArray();
                result.LoadDataRow(array, true);
            }
        }
        return result;
    }
    /// <summary>
    /// 排除SqlSugar忽略的列
    /// </summary>
    /// <param name="pi"></param>
    /// <returns></returns>
    private static bool IsIgnoreColumn(this PropertyInfo pi)
    {
        var sc = pi.GetCustomAttributes<SugarColumn>(false).FirstOrDefault(u => u.IsIgnore == true);
        return sc != null;
    }
    /// <summary>
    /// SqlSugar获取实体信息
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="db"></param>
    /// <returns></returns>
    public static SqlSugar.EntityInfo GetEntityInfoByAllDb<T>(this ISqlSugarClient db)
    {
        return SqlSugarHelper.GetEntityInfoByAllDb(typeof(T), db);
    }

    /// <summary>
    /// SqlSugar获取实体信息
    /// </summary>
    /// <param name="db"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static SqlSugar.EntityInfo GetEntityInfoByAllDb(this ISqlSugarClient db, Type type)
    {
        return SqlSugarHelper.GetEntityInfoByAllDb(type, db);
    }
    /// <summary>
    /// 转化为线程安全的数据库上下文
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static SqlSugarScope ToScope(this ISqlSugarClient db)
    {
        var scope = new SqlSugarScope(db.CurrentConnectionConfig);
        scope.SetAop();
        return scope;
    }
    /// <summary>
    /// 转化为 SqlSugarClient
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static SqlSugarClient ToClient(this ISqlSugarClient db)
    {
        return db.CopyNew();
    }
    /// <summary>
    /// 线程安全的数据库上下文转化为不安全的数据库上下文
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static SqlSugarClient ToClient(this SqlSugarScope db)
    {
        return db.ScopedContext;
    }
    /// <summary>
    /// 转化为 SqlSugarProvider
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static SqlSugarProvider ToProvider(this SqlSugarClient db)
    {
        return db.Context;
    }
    /// <summary>
    /// 转化为 SqlSugarProvider
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public static SqlSugarProvider ToProvider(this SqlSugarScope db)
    {
        return db.ScopedContext.Context;
    }

    #endregion 公共扩展方法
}
