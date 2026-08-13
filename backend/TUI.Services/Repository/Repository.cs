using TUI.Services.DBModel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TUI.Services.Extension;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TUI.Services.Repository
{
    public class Repository<TEntity> : IRespository<TEntity>
    //where TContext : DbContext
     where TEntity : class, new()
    {
        private TUIDbContext entityContext;

        public Repository(TUIDbContext dbContext)
        {
            this.entityContext = dbContext;
        }

        public TEntity Get(params object[] keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            var entitySet = entityContext.Set<TEntity>();
            return entitySet.Find(keys);
        }

        public bool Exists(params object[] keys)
        {
            return (this.Get(keys) != null);
        }

        public TEntity Get(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            var keyValues = entityContext.GetEntityKeys<TEntity>(entity);
            var entitySet = entityContext.Set<TEntity>();
            return entitySet.Find(keyValues.ToArray());
        }

        public TEntity GetOrNew(TEntity entity)
        {
            return this.Get(entity) ?? new TEntity();
        }
        public int SaveChanges()
        {
            return this.entityContext.SaveChanges();
        }


        public virtual IQueryable<TEntity> List(Expression<Func<TEntity, bool>> predicate = null,
            params Expression<Func<TEntity, object>>[] pathes)
        {
            var entitySet = entityContext.Set<TEntity>();
            if (predicate == null) predicate = (o) => true;
            var entities = entitySet.Where(predicate);
            foreach (var path in pathes)
            {
                entities = entities.Include(path);
            }
            return entities;
        }

        public TEntity Add(TEntity entity)
        {
            return this.InternalAdd(entity, this.entityContext.Set<TEntity>());
        }

        public void Add(IEnumerable<TEntity> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entity");
            }

            var entitySet = this.entityContext.Set<TEntity>();
            foreach (var entity in entities)
            {
                this.InternalAdd(entity, entitySet);
            }
        }

        public TEntity Delete(TEntity entity)
        {
            var entitySet = entityContext.Set<TEntity>();
            return this.InternalDelete(entity, entitySet);
        }

        public void Delete(params object[] keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            var entitySet = entityContext.Set<TEntity>();
            entitySet.Remove(entitySet.Find(keys));
        }

        public void Delete(IEnumerable<TEntity> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entity");
            }
            var entitySet = entityContext.Set<TEntity>();
            this.InternalDelete(entities, entitySet);
        }

        public void Delete(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            var entitySet = entityContext.Set<TEntity>();
            var entities = entitySet.Where(predicate);
            this.InternalDelete(entities, entitySet);
        }

        public TEntity Update(TEntity entity)
        {
            var context = this.entityContext;
            var entitySet = context.Set<TEntity>();
            return this.InternalUpdate(entity, context, entitySet);
        }

        public void Update(IEnumerable<TEntity> entities)
        {
            if (entities == null)
            {
                throw new ArgumentNullException("entity");
            }

            var context = this.entityContext;
            var entitySet = context.Set<TEntity>();
            foreach (var entity in entities)
            {
                this.InternalUpdate(entity, context, entitySet);
            }
        }

        protected TEntity InternalAdd(TEntity entity, DbSet<TEntity> entitySet)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            return entitySet.Add(entity).Entity;
        }

        protected TEntity InternalDelete(TEntity entity, DbSet<TEntity> entitySet)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            entitySet.Attach(entity);
            return entitySet.Remove(entity).Entity;
        }
        protected void InternalDelete(IEnumerable<TEntity> entities, DbSet<TEntity> entitySet)
        {
            foreach (var entity in entities)
            {
                InternalDelete(entity, entitySet);
            }
        }
        protected T UnProxy<T>(DbContext context, T proxyObject) where T : class
        {
            try
            {
                T poco = context.Entry(proxyObject).CurrentValues.ToObject() as T;
                return poco;
            }
            finally
            {
            }
        }
        protected TEntity InternalUpdate(TEntity entity, DbContext context, DbSet<TEntity> entitySet)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            if (context.Entry(entity).State == EntityState.Detached)
            {
                var find = this.Get(entity);
                context.Entry(find).CurrentValues.SetValues(entity);
                return UnProxy(context, find);
            }
            else
            {
                return entity;
            }
        }

        public TEntity AddOrUpdate(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            var context = this.entityContext;
            var keys = context.GetEntityKeys(entity);
            if (context.Entry(entity).State == EntityState.Detached && keys.Count() == 1 && keys.First().ToString() == "0")
            {
                return this.Add(entity);
            }
            var find = this.Get(entity);
            if (find == null)
            {
                return this.Add(entity);
            }
            else
            {
                context.Entry(find).CurrentValues.SetValues(entity);
                return context.Entry(find).Entity;
            }
        }

        public TEntity AddOrUpdate(TEntity entity, Action<TEntity> updateModelAction)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            var updatingEntity = this.Get(entity);
            if (updatingEntity == null)
            {
                updatingEntity = new TEntity();
                this.Add(updatingEntity);
            }
            updateModelAction(updatingEntity);
            return updatingEntity;
        }
        public DataSet ExecuteQuerySP(string procedure, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(this.entityContext.Database.GetDbConnection().ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procedure;
                    command.CommandTimeout = 360;
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }
                    var adapter = new SqlDataAdapter(command);
                    var ds = new DataSet();
                    adapter.Fill(ds);
                    return ds;
                }
            }
        }
        public bool ExecuteNonQuerySP(string procedure, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(this.entityContext.Database.GetDbConnection().ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procedure;
                    command.CommandTimeout = 360;
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }
                    connection.Open();
                    bool result = command.ExecuteNonQuery() > 0;
                    connection.Close();
                    return result;
                }
            }
        }
        public object ExecuteScalarSP(string procedure, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(this.entityContext.Database.GetDbConnection().ConnectionString))
            {
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procedure;
                    command.CommandTimeout = 360;
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }
                    connection.Open();
                    object result = command.ExecuteScalar();
                    connection.Close();
                    return result;
                }
            }
        }

    }
}
