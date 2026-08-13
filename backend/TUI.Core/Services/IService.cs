using Furion.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Core.Services
{
    public interface IService<TEntity>:IScoped where TEntity : class, new ()
    {
        TEntity Add(TEntity entity);
        void Add(IList<TEntity> entities);

        TEntity Update(TEntity entity);
        
        void Update(IList<TEntity> entities);

        void Delete(TEntity entity);
        void Delete(int Id);

        void Delete(IList<TEntity> entities);

        void Delete(Expression<Func<TEntity, bool>> predicate);
        TEntity AddOrUpdate(TEntity entity);
        TEntity AddOrUpdate(TEntity entity, Action<TEntity> updateModelAction);
        void BulkSave(List<TEntity> entities);
        void BulkCopy(IList<TEntity> entities);
        void BulkUpdate(List<TEntity> entities);
        void AddOrUpdate(List<TEntity> entities);
        void Update(Expression<Func<TEntity, TEntity>> setColumn, Expression<Func<TEntity, bool>> where = null);
        ISugarQueryable<TEntity> Queryable();
        TEntity GetOrNew(int id);
    }

}
