// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.DependencyInjection;

namespace TUI.Base;

/// <summary>
/// sqlsugar 仓储
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public class SqlSugarRepository<TEntity> : SimpleClient<TEntity> where TEntity : class, new()
{
    /// <summary>
    /// 更新忽略的字段
    /// </summary>
    private readonly string[] UpdateIgnoreColumns = new string[] { "CreatorUserId", "CreatorUserName", "CreationTime" };

    /// <summary>
    /// 原生 Ado 对象
    /// </summary>
    public virtual IAdo Ado { get; }

    /// <summary>
    /// 初始化 SqlSugar 客户端
    /// </summary>
    public ISqlSugarClient _db { get; set; }

    /// <summary>
    /// 服务提供器
    /// </summary>
    public readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 实体集合
    /// </summary>
    public virtual ISugarQueryable<TEntity> Entities => _db.Queryable<TEntity>();

    public SqlSugarRepository(ISqlSugarClient context = null, IServiceProvider serviceProvider = null) : base(context)//注意这里要有默认值等于null
    {
        //base.Context = context;//ioc注入的对象
        // base.Context=DbScoped.SugarScope; SqlSugar.Ioc这样写
        // base.Context=DbHelper.GetDbInstance()当然也可以手动去赋值

        _serviceProvider = serviceProvider;
        if (_serviceProvider == null) _serviceProvider = App.GetService<IServiceProvider>();
        base.Context = context;
        if (base.Context == null) base.Context = App.GetService<ISqlSugarClient>();//用手动获取方式支持切换仓储;

        _db = base.Context;
        Ado = _db.Ado;

        //// 数据库上下文根据实体切换,业务分库(使用环境例如微服务)
        //var entityType = typeof(TEntity);
        //if (entityType.IsDefined(typeof(TenantAttribute), false))
        //{
        //    var tenantAttribute = entityType.GetCustomAttribute<TenantAttribute>(false)!;
        //    ((SqlSugarClient)db).ChangeDatabase(tenantAttribute.configId);
        //}
        //else
        //{
        //   ((SqlSugarClient)db).ChangeDatabase(App.GetOptions<ConnectionStringsOptions>().DefaultDbNumber);
        //}
    }

    /// <summary>
    ///  改变仓储
    /// </summary>
    /// <typeparam name="TEntity2">实体</typeparam>
    /// <returns></returns>
    public new SqlSugarRepository<TEntity2> ChangeRepository<TEntity2>() where TEntity2 : class, new()
    {
        return _serviceProvider.GetService<SqlSugarRepository<TEntity2>>();
    }

    #region PagedList 分页

    /// <summary>
    /// 获取分页
    /// </summary>
    /// <param name="whereExpression">表达式</param>
    /// <param name="pageIndex">页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public virtual async Task<PagedList<TEntity>> GetPageListAsync(Expression<Func<TEntity, bool>> whereExpression, PagedBaseQuery searchQuery)
    {
        return await GetPageListAsync(whereExpression, searchQuery.PageIndex, searchQuery.PageSize);
    }

    /// <summary>
    /// 获取分页
    /// </summary>
    /// <param name="whereExpression">表达式</param>
    /// <param name="pageIndex">页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public virtual async Task<PagedList<TEntity>> GetPageListAsync(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20)
    {
        var entities = await GetListAsync();
        var result = entities.AsQueryable().Where(whereExpression).ToPagedList(pageIndex, pageSize);
        return result;
    }

    /// <summary>
    /// 获取分页
    /// </summary>
    /// <param name="whereExpression">表达式</param>
    /// <param name="pageIndex">页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public virtual PagedList<TEntity> GetPageList(Expression<Func<TEntity, bool>> whereExpression, PagedBaseQuery searchQuery)
    {
        return GetPageList(whereExpression, searchQuery.PageIndex, searchQuery.PageSize);
    }

    /// <summary>
    /// 获取分页
    /// </summary>
    /// <param name="whereExpression">表达式</param>
    /// <param name="pageIndex">页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public virtual PagedList<TEntity> GetPageList(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20)
    {
        var entities = GetList();
        var result = entities.AsQueryable().Where(whereExpression).ToPagedList(pageIndex, pageSize);
        return result;
    }

    #endregion PagedList 分页

    #region layui分页

    /// <summary>
    /// 获取Layui分页
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <param name="pageIndex">页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public virtual async Task<LayuiPagedList<TEntity>> GetLayuiPageListAsync(Expression<Func<TEntity, bool>> whereExpression, LayuiPageBaseQuery searchQuery)
    {
        return await GetLayuiPageListAsync(whereExpression, searchQuery.Page, searchQuery.Limit);
    }

    /// <summary>
    /// 获取Layui分页
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <param name="pageIndex">页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public virtual async Task<LayuiPagedList<TEntity>> GetLayuiPageListAsync(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20)
    {
        var entities = await GetListAsync();
        var result = await entities.AsQueryable().Where(whereExpression).ToLayuiPagedListAsync(pageIndex, pageSize);
        return result;
    }

    /// <summary>
    /// 获取Layui分页
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <param name="pageIndex">页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public virtual LayuiPagedList<TEntity> GetLayuiPageList(Expression<Func<TEntity, bool>> whereExpression, LayuiPageBaseQuery searchQuery)
    {
        return GetLayuiPageList(whereExpression, searchQuery.Page, searchQuery.Limit);
    }

    /// <summary>
    /// 获取Layui分页
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <param name="pageIndex">页数</param>
    /// <param name="pageSize">条数</param>
    /// <returns></returns>
    public virtual LayuiPagedList<TEntity> GetLayuiPageList(Expression<Func<TEntity, bool>> whereExpression, int pageIndex = 1, int pageSize = 20)
    {
        var entities = GetList();
        var result = entities.AsQueryable().Where(whereExpression).ToLayuiPagedList(pageIndex, pageSize);
        return result;
    }

    #endregion layui分页

    #region 更新

    /// <summary>
    /// 更新一条
    /// </summary>
    /// <param name="updateObj"></param>
    /// <returns></returns>
    public override bool Update(TEntity updateObj)
    {
        var state = _db.Updateable(updateObj)
            .IgnoreColumns(UpdateIgnoreColumns)
            .ExecuteCommand() > 0;
        return state;
    }

    /// <summary>
    /// 更新多条
    /// </summary>
    /// <param name="updateObjs"></param>
    /// <returns></returns>
    public override bool UpdateRange(List<TEntity> updateObjs)
    {
        var state = _db.Updateable(updateObjs)
            .IgnoreColumns(UpdateIgnoreColumns)
            .ExecuteCommand() > 0;
        return state;
    }

    /// <summary>
    /// 更新一条
    /// </summary>
    /// <param name="updateObj"></param>
    /// <returns></returns>
    public override async Task<bool> UpdateAsync(TEntity updateObj)
    {
        return await _db.Updateable(updateObj)
            .IgnoreColumns(UpdateIgnoreColumns)
            .ExecuteCommandAsync() > 0;
    }

    /// <summary>
    /// 更新多条
    /// </summary>
    /// <param name="updateObjs"></param>
    /// <returns></returns>
    public override async Task<bool> UpdateRangeAsync(List<TEntity> updateObjs)
    {
        return await _db.Updateable(updateObjs)
             .IgnoreColumns(UpdateIgnoreColumns)
            .ExecuteCommandAsync() > 0;
    }

    #endregion 更新

    #region 删除

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="ids">id字符串，英文逗号隔开</param>
    /// <returns></returns>
    public virtual bool DeleteByIds(string ids)
    {
        var objs = ids.ToSplitObjects();
        return base.DeleteByIds(objs);
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="ids">id字符串，英文逗号隔开</param>
    /// <returns></returns>
    public virtual async Task<bool> DeleteByIdsAsync(string ids)
    {
        var objs = ids.ToSplitObjects();
        return await base.DeleteByIdsAsync(objs);
    }

    #endregion 删除

    #region 其它

    /// <summary>
    /// 是否有数据
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <returns></returns>
    public virtual new async Task<bool> IsAnyAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        var entities = await GetListAsync();
        return await entities.AsQueryable().Where(whereExpression).AnyAsync();
    }

    /// <summary>
    /// 按条件获取数据条数
    /// </summary>
    /// <param name="whereExpression"></param>
    /// <returns></returns>
    public virtual new async Task<int> CountAsync(Expression<Func<TEntity, bool>> whereExpression)
    {
        var entities = await GetListAsync();
        return await entities.AsQueryable().Where(whereExpression).CountAsync();
    }

    #endregion 其它
}