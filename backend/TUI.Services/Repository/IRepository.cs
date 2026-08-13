using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TUI.Services.Repository
{
    public interface IRepository
    {

    }
    public interface IRepositoryCanRetrieve<TEntity> : IRepository
    {
        TEntity Get(TEntity entity);
        TEntity GetOrNew(TEntity entity);
        TEntity Get(params object[] keys);
        bool Exists(params object[] keys);

        IQueryable<TEntity> List(Expression<Func<TEntity, bool>> predicate = null,
            params Expression<Func<TEntity, object>>[] pathes);
    }

    public interface IRepositoryCanRetrieve<TEntity, TKey> : IRepositoryCanRetrieve<TEntity>
    {
        TEntity Get(TKey key);
        IEnumerable<TEntity> ListByKeys(params TKey[] keys);
        IEnumerable<TEntity> ListByKeys(IEnumerable<TKey> keys);
        IDictionary<TKey, TEntity> ListAsDictionary(params TKey[] keys);
        IDictionary<TKey, TEntity> ListAsDictionary(IEnumerable<TKey> keys);
        bool Exists(TKey key);
    }

    public interface IRepositoryCanRetrieve<TEntity, TKey, TKey2> : IRepositoryCanRetrieve<TEntity>
    {
        TEntity Get(TKey key, TKey2 key2);
    }

    public interface IRepositoryCanRetrieve<TEntity, TKey, TKey2, TKey3> : IRepositoryCanRetrieve<TEntity>
    {
        TEntity Get(TKey key, TKey2 key2, TKey3 key3);

    }

    public interface IRepositoryCanUpdate<TEntity> : IRepository
    {
        TEntity Add(TEntity entity);
        void Add(IEnumerable<TEntity> entities);

        TEntity Update(TEntity entity);
        void Update(IEnumerable<TEntity> entities);

        TEntity Delete(TEntity entity);
        void Delete(params object[] keys);
        void Delete(IEnumerable<TEntity> entities);
        void Delete(Expression<Func<TEntity, bool>> predicate);
        int SaveChanges();
        TEntity AddOrUpdate(TEntity entity);
        TEntity AddOrUpdate(TEntity entity, Action<TEntity> updateModelAction);

        bool ExecuteNonQuerySP(string procedure, params SqlParameter[] parameters);
    }

    public interface IRepositoryCanUpdate<TEntity, TKey> : IRepositoryCanUpdate<TEntity>
    {
        void Delete(TKey key);
    }

    public interface IRepositoryCanUpdate<TEntity, TKey, TKey2> : IRepositoryCanUpdate<TEntity>
    {
        void Delete(TKey key, TKey2 key2);
    }

    public interface IRepositoryCanUpdate<TEntity, TKey, TKey2, TKey3> : IRepositoryCanUpdate<TEntity>
    {
        void Delete(TKey key, TKey2 key2, TKey3 key3);
    }

    public interface IRespository<TEntity> : IRepositoryCanRetrieve<TEntity>, IRepositoryCanUpdate<TEntity> where TEntity : class
    {

    }

    public interface IRepository<TEntity, TKey> : IRespository<TEntity>, IRepositoryCanRetrieve<TEntity, TKey>, IRepositoryCanUpdate<TEntity, TKey> where TEntity : class
    {

    }

    public interface IRepository<TEntity, TKey, Tkey2> : IRespository<TEntity>, IRepositoryCanRetrieve<TEntity, TKey, Tkey2>, IRepositoryCanUpdate<TEntity, TKey, Tkey2> where TEntity : class
    {

    }

    public interface IRepository<TEntity, TKey, Tkey2, TKey3> : IRespository<TEntity>, IRepositoryCanRetrieve<TEntity, TKey, Tkey2, TKey3>, IRepositoryCanUpdate<TEntity, TKey, Tkey2, TKey3> where TEntity : class
    {

    }

    public interface IRepositoryDefault<TEntity> : IRepository<TEntity, int> where TEntity : class
    {

    }
    //public interface IRepositoryContext<TEntity, TContext> : IRespository<TEntity>
    //where TContext : DbContext
    // where TEntity : class, new()
    //{

    //}

}
