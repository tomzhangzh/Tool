// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// EF 扩展
/// </summary>
public static class EFExtension
{
    /// <summary>
    /// 【EF】获取实体更改状态 中文状态
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public static string GetEntityStateName(this EntityState state)
    {
        return state switch
        {
            EntityState.Added => "新增",
            EntityState.Deleted => "删除",
            EntityState.Detached => "未跟踪",
            EntityState.Modified => "更新",
            EntityState.Unchanged => "未改变",
            _ => state.ToString("G"),
        };
    }

    /// <summary>
    ///    ToList 的异步方法，并缓存数据
    /// </summary>
    /// <returns>
    ///     表示异步操作的任务。
    ///     任务结果包含一个<see cref="List{T}" /> 其中包含输入序列中的元素。
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> 是否取消的</exception>
    public static async Task<List<TEntity>> ToListWithCacheAsync<TEntity>(
        this IQueryable<TEntity> source, string cacheKey = "",
        CancellationToken cancellationToken = default)
    {
        //设置缓存
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            cacheKey = Caches.GetTableCacheKey<TEntity>();
        }
        //从缓存取出数据
        var cachedatas = await Caches.GetAsync<List<TEntity>>(cacheKey);
        if (cachedatas != null) return cachedatas;

        //从数据库查询数据，并设置缓存后返回
        var list = await source.ToListAsync(cancellationToken);
        Caches.Set(cacheKey, list);
        return list;
    }

    /// <summary>
    /// Tolist 并缓存数据
    /// </summary>
    /// <typeparam name="source"></typeparam>
    /// <param name="source"></param>
    /// <param name="cacheKey">缓存Key</param>
    /// <returns></returns>
    public static List<TEntity> ToListWithCache<TEntity>(this IQueryable<TEntity> source, string cacheKey = "")
    {
        //设置缓存
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            cacheKey = Caches.GetTableCacheKey<TEntity>();
        }
        //从缓存取出数据
        var cachedatas = Caches.Get<List<TEntity>>(cacheKey);
        if (cachedatas != null) return cachedatas;

        var list = source.ToList();
        Caches.Set(cacheKey, list);
        return list;
    }

    /// <summary>
    /// 新增返回ID，异步
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <param name="entity"></param>
    /// <returns>自增ID</returns>
    public static async Task<long> InsertReturnIdAsync<TEntity>(this IRepository<TEntity> repository, TEntity entity)
         where TEntity : class, IPrivateEntity, new()
    {
        var resultEntity = await repository.InsertNowAsync(entity);

        PropertyInfo? propertyInfo = repository.EntityType.FindPrimaryKey()!.Properties.AsEnumerable().FirstOrDefault()?.PropertyInfo;
        if (propertyInfo == null)
        {
            return resultEntity.Entity.GetPropertyValue("Id").ToInt32();
        }
        else
        {
            return resultEntity.Entity.GetPropertyValue(propertyInfo.Name).ToInt32();
        }
    }

    /// <summary>
    /// 新增返回自增ID
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <param name="entity"></param>
    /// <returns>自增ID</returns>
    public static long InsertReturnId<TEntity>(this IRepository<TEntity> repository, TEntity entity)
       where TEntity : class, IPrivateEntity, new()
    {
        var resultEntity = repository.InsertNow(entity);
        PropertyInfo? propertyInfo = repository.EntityType.FindPrimaryKey()!.Properties.AsEnumerable().FirstOrDefault()?.PropertyInfo;
        if (propertyInfo == null)
        {
            return resultEntity.Entity.GetPropertyValue("Id").ToInt32();
        }
        else
        {
            return resultEntity.Entity.GetPropertyValue(propertyInfo.Name).ToInt32();
        }
    }

    /// <summary>
    /// 批量删除ID集合，多个ID用英文逗号隔开【ID 实体不】
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <param name="ids"></param>
    public static void Delete<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> predicate)
         where TEntity : class, IPrivateEntity, new()
    {
        repository.DeleteAsync(predicate).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 批量删除ID集合，多个ID用英文逗号隔开【ID 实体不】
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    public static async Task DeleteAsync<TEntity>(this IRepository<TEntity> repository, Expression<Func<TEntity, bool>> predicate)
         where TEntity : class, IPrivateEntity, new()
    {
        //构建只查询出表Id的select 表达式
        var selectExpression = repository.SelectPrimaryKey<TEntity, TEntity>();//o=>new TEntity(){o.Id}

        List<TEntity> dbentities;
        var queryable = repository.Where(predicate);
        //if (selectExpression != null)
        //{
        //    //只查询出主键字段，一般默认是Id，提高效率
        //    dbentities = await queryable.Select(selectExpression).ToListAsync();
        //}
        //else
        {
            dbentities = await queryable.ToListAsync();//只查询出ID字段，提高效率
        }

        //遍历集合，检查改变实体状态
        foreach (var item in dbentities)
        {
            repository.ChangeEntityState(item, EntityState.Deleted);
        }

        await repository.DeleteAsync(dbentities);
    }

    /// <summary>
    /// 批量删除ID集合，多个ID用英文逗号隔开【ID 实体不】
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <param name="ids"></param>
    public static async Task DeleteByIdsAsync<TEntity>(this IRepository<TEntity> repository, string ids)
         where TEntity : class, IPrivateEntity, new()
    {
        var idtemps = ids.ToIntList();
        //需要数据库查询的实体
        var dbFindIds = new List<long>();
        //已构建或者查询到的实体
        var entities = new List<TEntity>();

        #region 不查询数据库构建 删除实体

        {
            foreach (var id in idtemps)
            {
                var entity = BuildDeletedEntity(repository, id);
                if (entity != null)
                {
                    entities.Add(entity);
                }
                else
                {
                    //加入需要进行数据库查询集合
                    dbFindIds.Add(id);
                }
            }
        }

        #endregion 不查询数据库构建 删除实体

        // dbFindIds.AddRange(idtemps);

        if (dbFindIds.Any())
        {
            var e = Expression.Parameter(typeof(TEntity), "e");
            var containsField = Expression.Property(e, "Id"); // e.Id

            var list = Expression.Constant(idtemps, typeof(List<long>));//List

            //获取linq的Contains 方法：Contains<TSource>(this IEnumerable<TSource> source, TSource value);
            //有2个参数，所以 length=2
            var containsMethod = typeof(Enumerable).GetMethods().First(info => info.GetParameters().Length == 2 && info.Name == "Contains").MakeGenericMethod(typeof(long));

            var contains = Expression.Call(null, containsMethod, list, containsField);//e.List.Contains("s")

            var lambda = Expression.Lambda<Func<TEntity, bool>>(contains, e);

            //构建只查询出表Id的select 表达式
            var selectExpression = repository.SelectPrimaryKey<TEntity, TEntity>();//o=>new TEntity(){o.Id}

            //var queryable = Queryable.Select(query, GetLamda<object, TTarget>(query.GetType().GetGenericArguments()[0])); repository.Where(lambda);
            //var dbentities = await repository.Where(lambda)
            //.Select(selectExpression)//只查询出ID字段，提高效率
            //    .ToListAsync();

            List<TEntity> dbentities;
            var queryable = repository.Where(lambda);
            if (selectExpression != null)
            {
                //只查询出主键字段，一般默认是Id，提高效率
                dbentities = await repository.Where(lambda).Select(selectExpression).ToListAsync();
            }
            else
            {
                dbentities = await repository.Where(lambda).ToListAsync();
            }

            //改变实体状态
            foreach (var item in dbentities)
            {
                repository.ChangeEntityState(item, EntityState.Deleted);
            }
        }

        await repository.DeleteAsync(entities);
    }

    /// <summary>
    /// 批量删除ID集合，多个ID用英文逗号隔开【ID 实体不】
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <param name="ids"></param>
    public static void DeleteByIds<TEntity>(this IRepository<TEntity> repository, string ids)
         where TEntity : class, IPrivateEntity, new()
    {
        repository.DeleteByIdsAsync(ids).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 获取select 只查询Id字段
    /// o=>o.Id
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    private static LambdaExpression SelectId2<TEntity>()
    {
        //ParameterExpression pe = Expression.Parameter(typeof(TEntity), "p");    //# 创建形参 p

        //MemberExpression meId = Expression.MakeMemberAccess(pe, typeof(TEntity).GetProperty("Id")); //# 要使用 MakeMemberAccess 方法

        ////Type personResultType = typeof(TEntity);
        ////MemberAssignment maId = Expression.Bind(personResultType.GetProperty("Id"), meId);    //# 使用 Bind 方法将目标类型的属性与源类型的属性值绑定

        ////NewExpression ne = Expression.New(personResultType);    //# 相当于 new 关键字创建一个对象

        ////MemberInitExpression mie = Expression.MemberInit(ne, maId);  //# 相当于初始化时赋值操作

        //Expression<Func<TEntity, TEntity>> selectExpression = Expression.Lambda<Func<TEntity, TEntity>>(pe);
        ////var personList1 = context.Person.Select(personSelectExpression).ToList();
        ///

        var e = Expression.Parameter(typeof(TEntity), "e");
        var selectlength = Expression.Property(e, "Id"); // e.Length
        var selectLambda = Expression.Lambda(selectlength, e); // e => e.Length

        //Expression<Func<TEntity, TEntity>> selectExpression = Expression.Lambda<Func<TEntity, TEntity>>(pe);

        return selectLambda;
    }

    /// <summary>
    /// 获取select 只查询Id字段
    /// o=>new TEntity(){Id=o.Id}
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns>TResult</returns>
    private static Func<TEntity, TResult>? SelectPrimaryKey<TEntity, TResult>(this IRepository<TEntity> repository)
        where TEntity : class, IPrivateEntity, new()
    {
        //PropertyInfo propertyInfo = repository.EntityType.FindPrimaryKey()!.Properties.AsEnumerable().FirstOrDefault()?.PropertyInfo;
        //if (propertyInfo == null)
        //{
        //    return null;
        //}

        var propertyInfos = repository.EntityType.FindPrimaryKey()!.Properties.AsEnumerable();
        if (propertyInfos == null)
        {
            return null;
        }

        ParameterExpression pe = Expression.Parameter(typeof(TEntity), "p");    //# 创建形参 p
        Type personResultType = typeof(TResult);
        NewExpression ne = Expression.New(personResultType);    //# 相当于 new 关键字创建一个对象

        var mas = new List<MemberAssignment>();
        foreach (var item in propertyInfos)
        {
            //由于组件可能不是Id字段，所有获取主键的信息来设置
            MemberExpression meId = Expression.MakeMemberAccess(pe, typeof(TEntity).GetProperty(item.PropertyInfo.Name)); //# 要使用 MakeMemberAccess 方法
            MemberAssignment ma = Expression.Bind(personResultType.GetProperty(item.PropertyInfo.Name), meId);    //# 使用 Bind 方法将目标类型的属性与源类型的属性值绑定
            mas.Add(ma);
        }
        MemberInitExpression? mie = null;
        switch (mas.Count)
        {
            case 1://select 2个字段 ，例如：select(a=>new TEntity(){Id=a.Id})
                mie = Expression.MemberInit(ne, mas[0]);  //# 相当于初始化时赋值操作
                break;

            case 2://select 2个字段 ，例如：select(a=>new TEntity(){Id=a.Id,Name=a.Name})
                mie = Expression.MemberInit(ne, mas[0], mas[1]);  //# 相当于初始化时赋值操作
                break;

            case 3://select 3个字段
                mie = Expression.MemberInit(ne, mas[0], mas[2]);  //# 相当于初始化时赋值操作
                break;
        }
        Expression<Func<TEntity, TResult>> selectExpression = Expression.Lambda<Func<TEntity, TResult>>(mie, pe);
        //var personList1 = context.Person.Select(personSelectExpression).ToList();

        return selectExpression.Compile();
    }

    /// <summary>
    /// 构建删除实体
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <param name="key"></param>
    /// <param name="isRealDelete"></param>
    /// <returns></returns>
    private static void CheckChangeEntityState<TEntity>(this IRepository<TEntity> repository, TEntity entity, object key, bool isRealDelete = true)
         where TEntity : class, IPrivateEntity, new()
    {
        if (repository == null) throw new ArgumentNullException();
        PropertyInfo? propertyInfo = repository.EntityType.FindPrimaryKey()!.Properties.AsEnumerable().FirstOrDefault()?.PropertyInfo;
        if (propertyInfo == null)
        {
            return;
        }

        if (repository.CheckTrackState(key, out var entityEntry, propertyInfo.Name))
        {
            if (isRealDelete)
            {
                repository.ChangeEntityState(entityEntry, EntityState.Deleted);
                return;
            }
        }

        if (isRealDelete)
        {
            repository.ChangeEntityState(entity, EntityState.Deleted);
        }
    }

    /// <summary>
    /// 构建删除实体
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="repository"></param>
    /// <param name="key"></param>
    /// <param name="isRealDelete"></param>
    /// <returns></returns>
    private static TEntity? BuildDeletedEntity<TEntity>(this IRepository<TEntity> repository, object key, bool isRealDelete = true)
         where TEntity : class, IPrivateEntity, new()
    {
        if (repository == null) throw new ArgumentNullException();
        PropertyInfo? propertyInfo = repository.EntityType.FindPrimaryKey()!.Properties.AsEnumerable().FirstOrDefault()?.PropertyInfo;
        if (propertyInfo == null)
        {
            return null;
        }

        if (repository.CheckTrackState(key, out var entityEntry, propertyInfo.Name))
        {
            if (isRealDelete)
            {
                repository.ChangeEntityState(entityEntry, EntityState.Deleted);
            }

            return entityEntry.Entity as TEntity;
        }

        TEntity val = new TEntity();
        propertyInfo.SetValue(val, key);
        if (isRealDelete)
        {
            repository.ChangeEntityState(val, EntityState.Deleted);
        }

        return val;
    }
}