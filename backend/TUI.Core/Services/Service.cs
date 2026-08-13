using AngleSharp.Dom;
using Microsoft.EntityFrameworkCore.Query.Internal;
using NPOI.Util;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TUI.Utils.Extensions;

namespace TUI.Core.Services
{
    public class Service<TEntity> : IService<TEntity> where TEntity : class, new()
    {
        //private readonly ISqlSugarClient sqlSugarClient = null;
        public ISqlSugarClient SqlSugarClient => AppEx.dbSqlSugar;
        private  ISqlSugarClient db=>this.SqlSugarClient;
        public  ISugarQueryable<TEntity> Entities => db.AsTenant().GetConnectionScopeWithAttr<TEntity>().Queryable<TEntity>();
        public TEntity Add(TEntity entity)
        {
            this.db.Insertable<TEntity>(entity).ExecuteCommandIdentityIntoEntity();
            return entity;
        }

        public void Add(IList<TEntity> entities)
        {
            entities.ForEach(e => this.Add(e));
        }
        public void BulkCopy(IList<TEntity> entities)
        {
            var result = this.db.Fastest<TEntity>().BulkCopy(entities.ToList());
        }
        public void BulkUpdate(List<TEntity> entities)
        {
            db.Fastest<TEntity>().BulkUpdate(entities);
        }

        public TEntity AddOrUpdate(TEntity entity)
        {
            var id = this.getId(entity);
            if (id == 0)
            {
                Add(entity);
            }
            else
            {
                Update(entity);
            }
            return entity;
        }
        public void AddOrUpdate(List<TEntity> entities)
        {
            var inserts = entities.Where(x => this.getId(x) == 0).ToList();
            var updates = entities.Except(inserts).ToList();
            this.Add(inserts);
            this.Update(updates);
        }
        public void BulkSave(List<TEntity> entities)
        {
            var inserts = entities.Where(x => this.getId(x) == 0).ToList();
            var updates = entities.Except(inserts).ToList();
            BulkCopy(inserts);
            BulkUpdate(updates);
        }
        public TEntity AddOrUpdate(TEntity entity, Action<TEntity> updateModelAction)
        {
            this.AddOrUpdate(entity);
            updateModelAction?.Invoke(entity);
            return entity;
        }
        public bool IsLogicDelete()
        {
            return this.db.EntityMaintenance.GetEntityInfo<TEntity>().Columns.Any(x => CommonConst.DELETE_FIELD.Split(',').Contains(x.PropertyName));
        }
        public void Delete(TEntity entity)
        {
            if (this.IsLogicDelete())
            {
                entity.SetPropertyValue(CommonConst.DELETE_FIELD, true);
                this.Update(entity);
            }
            else
            {
                this.db.Deleteable(entity).ExecuteCommand();
            }

        }

        public void Delete(int Id)
        {
            if (this.IsLogicDelete())
            {
                db.Deleteable<TEntity>().In(Id).IsLogic().ExecuteCommand();
            }
            else
            {
                this.db.Deleteable<TEntity>().In(Id).ExecuteCommand();
            }
                
        }

        public void Delete(IList<TEntity> entities)
        {
            var ids = entities.Select(x => x.GetPropValue<int>("Id")).ToList();
            if (this.IsLogicDelete())
            {
                db.Deleteable<TEntity>().In(ids).IsLogic().ExecuteCommand();
            }
            else
            {
                this.db.Deleteable<TEntity>().In(ids).ExecuteCommand();
            }
        
        }

        public void Delete(Expression<Func<TEntity, bool>> predicate)
        {
            this.db.Deleteable<TEntity>().Where(predicate).ExecuteCommand();
        }

        public TEntity Update(TEntity entity)
        {
            this.db.Updateable(entity).ExecuteCommand();
            return entity;
        }

        public void Update(IList<TEntity> entities)
        {
            this.db.Updateable(entities.ToList()).ExecuteCommand();
        }

        public void Update(Expression<Func<TEntity, TEntity>> setColumn, Expression<Func<TEntity, bool>> where = null)
        {
            Expression<Func<TEntity, bool>> condition = x => true;
            this.db.Updateable<TEntity>().SetColumns(setColumn).Where(where ?? condition).ExecuteCommand();
        }
        private int getId(TEntity entity)
        {
            if (entity.IsEmpty())
            {
                throw new ArgumentNullException(nameof(entity));
            }
            return entity.GetPropValue<int>("Id");
        }

        public ISugarQueryable<TEntity> Queryable()
        {
            if (this.IsLogicDelete())
            {
                var dataParameter = Expression.Parameter(typeof(TEntity), "entity");
                //var expr = DynamicExpressionParser.ParseLambda(new[] { dataParameter }, typeof(bool), "entity.IsDeleted ==True");
                return this.Entities.Where($"{CommonConst.DELETE_FIELD}=0");
            }
            else
            {
                return  this.Entities;
            }

        }

        public TEntity GetOrNew(int id)
        {
            return this.Entities.InSingle(id)?? new TEntity();
        }
         
    }
}
