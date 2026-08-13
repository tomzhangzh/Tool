using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TUI.Services.Repository
{
    public interface IService
    {

    }
    public interface IServiceCanRetrieve<TEntity> : IService
    {
        TEntity Get(TEntity entity);
        TEntity GetOrNew(TEntity entity);
        TEntity Get(params object[] keys);
        TEntity GetOrNew(params object[] keys);
        bool Exists(params object[] keys);

        List<TEntity> List(Expression<Func<TEntity, bool>> predicate = null,
            params Expression<Func<TEntity, object>>[] pathes);
        IQueryable<TEntity> Queryable(Expression<Func<TEntity, bool>> predicate = null,
          params Expression<Func<TEntity, object>>[] pathes);
        SqlSugar.ISqlSugarClient dbSqlSugar { get; }
    }

    public interface IServiceCanRetrieve<TEntity, TKey> : IServiceCanRetrieve<TEntity>
    {
        TEntity Get(TKey key);
        List<TEntity> ListByKeys(params TKey[] keys);
        List<TEntity> ListByKeys(IEnumerable<TKey> keys);
        IDictionary<TKey, TEntity> ListAsDictionary(params TKey[] keys);
        IDictionary<TKey, TEntity> ListAsDictionary(IEnumerable<TKey> keys);
        bool Exists(TKey key);
    }

    public interface IServiceCanRetrieve<TEntity, TKey, TKey2> : IServiceCanRetrieve<TEntity>
    {
        TEntity Get(TKey key, TKey2 key2);
    }
    public interface IServiceCanUpdate<TEntity> : IService
    {
        TEntity Add(TEntity entity);
        void Add(IEnumerable<TEntity> entities);

        TEntity Update(TEntity entity);
        void Update(IEnumerable<TEntity> entities);

        TEntity Delete(TEntity entity);
        void Delete(params object[] keys);

        void Delete(IEnumerable<TEntity> entities);

        void Delete(Expression<Func<TEntity, bool>> predicate);
        TEntity AddOrUpdate(TEntity entity);
        TEntity AddOrUpdate(TEntity entity, Action<TEntity> updateModelAction);
        void SaveChanges();
    }

    public interface IServiceCanUpdate<TEntity, TKey> : IServiceCanUpdate<TEntity>
    {
        void Delete(TKey key);
       
    }

    public interface IServiceCanUpdate<TEntity, TKey, TKey2> : IServiceCanUpdate<TEntity>
    {
        void Delete(TKey key, TKey2 key2);
    }

    public interface IServiceCanUpdate<TEntity, TKey, TKey2, TKey3> : IServiceCanUpdate<TEntity>
    {
        void Delete(TKey key, TKey2 key2, TKey3 key3);
    }


    public interface IServiceCanRetrieve<TEntity, TKey, TKey2, TKey3> : IServiceCanRetrieve<TEntity>
    {
        TEntity Get(TKey key, TKey2 key2, TKey3 key3);

    }
    public interface IService<TEntity> : IServiceCanRetrieve<TEntity>, IServiceCanUpdate<TEntity> where TEntity : class
    {

    }

    public interface IService<TEntity, TKey> : IService<TEntity>, IServiceCanRetrieve<TEntity, TKey>, IServiceCanUpdate<TEntity, TKey> where TEntity : class
    {

    }

    public interface IService<TEntity, TKey, Tkey2> : IService<TEntity>, IServiceCanRetrieve<TEntity, TKey, Tkey2>, IServiceCanUpdate<TEntity, TKey, Tkey2> where TEntity : class
    {

    }

    public interface IService<TEntity, TKey, Tkey2, TKey3> : IService<TEntity>, IServiceCanRetrieve<TEntity, TKey, Tkey2, TKey3>, IServiceCanUpdate<TEntity, TKey, Tkey2, TKey3> where TEntity : class
    {

    }

    public interface IServiceDefault<TEntity> : IService<TEntity, int> where TEntity : class
    {

    }
}
