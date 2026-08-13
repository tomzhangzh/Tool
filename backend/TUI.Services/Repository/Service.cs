using TUI.Services.DBModel;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TUI.Services.Repository
{
    public class Service : IService
    {
        public Service()
        {

        }
    }
    public  class Service<TEntity> : Service, IService<TEntity>
  where TEntity : class, new()
    {
      
        private IRespository<TEntity> repository;
        private SqlSugar.ISqlSugarClient db;

        public ISqlSugarClient dbSqlSugar => db;

        public Service(IRespository<TEntity> repository, SqlSugar.ISqlSugarClient dbSqlSugar)
        {
           
            this.repository = repository;
            this.db = dbSqlSugar;
        }

        public void Add(IEnumerable<TEntity> entities)
        {
           
                repository.Add(entities);
            repository.SaveChanges();
         
        }

        public TEntity Add(TEntity entity)
        {
           
               var  newEntity = repository.Add(entity);
            repository.SaveChanges();
         
            return newEntity;
        }

        public void Delete(IEnumerable<TEntity> entities)
        {
            
                repository.Delete(entities);
                repository.SaveChanges();
           
        }

        public virtual void Delete(Expression<Func<TEntity, bool>> predicate)
        {
           
                repository.Delete(predicate);
                repository.SaveChanges();
            
        }

        public virtual TEntity Delete(TEntity entity)
        {
            TEntity newEntity;
            
                newEntity = repository.Delete(entity);
                repository.SaveChanges();
           
            return newEntity;
        }

        public virtual void Delete(params object[] keys)
        {
            
                repository.Delete(keys);
                repository.SaveChanges();
           
        }


        public bool Exists(params object[] keys)
        {
            
                return repository.Exists(keys);
            
        }

        public TEntity Get(params object[] keys)
        {
            
                return repository.Get(keys);
            
        }

        public TEntity Get(TEntity entity)
        {
            
                return repository.Get(entity);
            
        }

        public TEntity GetOrNew(TEntity entity)
        {
            
                return repository.GetOrNew(entity);
            
        }
        public TEntity GetOrNew(params object[] keys)
        {
            return this.Get(keys) ?? new TEntity();
        }
        public List<TEntity> List(Expression<Func<TEntity, bool>> predicate = null,
            params Expression<Func<TEntity, object>>[] pathes)
        {
           
                return repository.List(predicate, pathes).ToList();
            
        }
        public IQueryable<TEntity> Queryable(Expression<Func<TEntity, bool>> predicate = null,
           params Expression<Func<TEntity, object>>[] pathes)
        {
           
                return repository.List(predicate, pathes);
          
        }
        public void Update(IEnumerable<TEntity> entities)
        {
            
                repository.Update(entities);
                repository.SaveChanges();
            
        }

        public TEntity Update(TEntity entity)
        {
            TEntity newEntity;
           
                newEntity = repository.Update(entity);
                repository.SaveChanges();
            
            return newEntity;
        }

        public TEntity AddOrUpdate(TEntity entity)
        {
            TEntity newEntity;
           
                newEntity = repository.AddOrUpdate(entity);
                repository.SaveChanges();
            
            return newEntity;
        }

        public virtual TEntity AddOrUpdate(TEntity entity, Action<TEntity> updateModelAction)
        {
            
                entity = repository.AddOrUpdate(entity, updateModelAction);
                repository.SaveChanges();
           
            return entity;
        }

        public void SaveChanges()
        {
            repository.SaveChanges();
        }

       
    }
}
