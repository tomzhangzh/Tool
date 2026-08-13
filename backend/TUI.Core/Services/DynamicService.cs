using FluentFTP.Helpers;
using Furion.ClayObject;
using SqlSugar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TUI.Utils.Extensions;

namespace TUI.Core.Services
{
    /// <summary>
    /// 这个类名为DynamicService，是TUI.Core应用程序的一部分，实现了IDynamicService接口。
    /// 它提供了一些通用的数据库操作方法，可以用于处理动态实体。
    /// 这些方法包括添加、添加或更新、删除、查询等。
    /// 其中，添加和更新方法使用SqlSugar ORM库来执行数据库操作。此外，DynamicService还提供了一些辅助方法，
    /// 如获取实体的字典表示、获取实体的ID等。通过这些方法，DynamicService可以方便地处理动态实体的增删改查操作。 
    /// </summary>
    public class DynamicService : IDynamicService
    {
        public ISqlSugarClient SqlSugarClient => AppEx.dbSqlSugar;
        private ISqlSugarClient db => this.SqlSugarClient;
        public string TableName { get; set; }
        public string ShortName { get; set; }

        public dynamic Add(dynamic entity)
        {
            Dictionary<string, object> dict = getDictionary(entity);
            dict = dict.RemoveId();
            var id = this.db.Insertable(dict).AS(this.TableName).ExecuteReturnIdentity();
            entity.Id = id;
            return entity;
        }
        public dynamic AddOrUpdate(dynamic entity)
        {
            var id = entity.Id;
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

        public void Add(IList<dynamic> entities)
        {
            entities.ForEach(e => this.Add(e));
        }

        public void AddOrUpdate(List<dynamic> entities)
        {
            var inserts = entities.Where(x => this.getId(x) == 0).ToList();
            var updates = entities.Except(inserts).ToList();
            if (inserts.Count > 0)
            {
                this.Add(inserts);
            }
            if (updates.Count > 0)
            {
                this.Update(updates);
            }
        }

        public bool IsLogicDelete()
        {
            var xx = this.db.DbMaintenance.GetColumnInfosByTableName(this.TableName);
            var result = this.db.DbMaintenance.GetColumnInfosByTableName(this.TableName).Where(x => x.DbColumnName == CommonConst.DELETE_FIELD).Any();
            return result;
        }


        public void Delete(dynamic entity)
        {
            if (this.IsLogicDelete())
            {
                entity.IsDeleted = true;
                this.Update(entity);
            }
            else
            {
                db.Deleteable<object>().AS(this.TableName).Where("id=@id", new { id = entity.Id }).ExecuteCommand();
            }

        }

        //这段代码可以使用更简洁的方式来实现。我们可以使用 `Deleteable` 方法来删除实体。如果是逻辑删除，我们可以使用 `IsLogic()` 方法来实现。如果是物理删除，我们可以使用 `Where` 方法来实现。下面是重写后的代码：

        public void Delete(int Id)
        {
            if (this.IsLogicDelete())
            {
                db.Deleteable<object>().AS(this.TableName).Where("id=@id", new { id = Id }).IsLogic().ExecuteCommand();
            }
            else
            {
                db.Deleteable<object>().AS(this.TableName).Where("id=@id", new { id = Id }).ExecuteCommand();
            }
        }


        public void Delete(IList<dynamic> entities)
        {
            var ids = entities.Select(x => x.Id).ToList();
            if (this.IsLogicDelete())
            {
                db.Deleteable<object>().AS(this.TableName).In(ids).IsLogic().ExecuteCommand();
            }
            else
            {
                db.Deleteable<object>().AS(this.TableName).Where("id in (@ids)", new { ids = ids }).ExecuteCommand();
            }
        }

        public Dictionary<string, object> getDictionary(dynamic entity)
        {
            var dict = (entity as object).OnlyPropertiesToDictionary(this.db.DbMaintenance.GetColumnInfosByTableName(this.TableName).Select(x => x.DbColumnName).ToList());

            return dict;
        }
        public dynamic Update(dynamic entity)
        {
            var dict = getDictionary(entity);
            this.db.Updateable(dict).AS(this.TableName).WhereColumns("id").ExecuteCommand();
            return entity;
        }

        public void Update(IList<dynamic> entities)
        {
            entities.ForEach(x =>
            {
                this.Update(x);
            });
        }

        public void Update(Expression<Func<dynamic, dynamic>> setColumn, Expression<Func<dynamic, bool>> where = null)
        {
            Expression<Func<dynamic, bool>> condition = x => true;
            this.db.Updateable<dynamic>().SetColumns(setColumn).Where(where ?? condition).ExecuteCommand();
        }
        private int getId(dynamic entity)
        {
            if (entity.IsEmpty())
            {
                throw new ArgumentNullException(nameof(entity));
            }
            return entity.Id;
        }

        public ISugarQueryable<System.Dynamic.ExpandoObject> Queryable(string where = null)
        {
            var result= this.db.Queryable(this.TableName, this.ShortName);
            if (!where.IsNullOrEmpty())
            {
                result= result.Where(where);
            }
            return result;

        }
        public DynamicService SetTableInfo(string TableName, string ShortName)
        {
            this.TableName = TableName;
            this.ShortName = ShortName;
            return this;
        }

        public dynamic GetOrNew(int id)
        {
            dynamic entity = this.db.Queryable(this.TableName, this.ShortName).InSingle(id);
            if (entity == null)
            {
                entity = new ExpandoObject();
                entity.Id = id;
            }
            return entity;

        }
        public (string whereClause, SugarParameter[] parameters) GenerateWhereClauseAndParameters(dynamic filter)
        {
            List<string> whereBuilder = new List<string>();
            var paramList = new List<SugarParameter>();
            var columns = this.db.DbMaintenance.GetColumnInfosByTableName(this.TableName);
             // 遍历dynamic object的属性
             foreach (var property in Clay.Object(filter).ToDictionary() as IDictionary<string,object>)
            {
                if (property.Value == null) continue;
                var paramName = $"@{property.Key}";
               
                var type=property.Value.GetType();
                //if (typeof(IEnumerable<>).IsAssignableFrom(type))
                //{
                //    if (property.Value==null || (property.Value as IEnumerable).Cast<object>().Count() == 0)
                //    {
                //        continue;
                //    }
                //    else
                //    {
                //        var columnInfo = columns.First(c => c.DbColumnName.Equals(property.Key, StringComparison.OrdinalIgnoreCase));
                //        whereBuilder.Add($"{columnInfo.DbColumnName} in ({paramName})");
                //        paramList.Add(new SugarParameter(paramName, property.Value));
                //    }
                //}
                if (property.Key.EndsWith("__In"))
                {
                    (string whereBuild, SugarParameter param)? where = getWhereBuild(columns, property, property.Key.TrimEnd("__In"), "in");
                    if (where != null)
                    {
                        whereBuilder.Add(where.Value.whereBuild);
                        paramList.Add(where.Value.param);
                    }
                }
                else if (property.Key.EndsWith("__Like"))
                {
                    (string whereBuild, SugarParameter param)? where = getWhereBuild(columns,property,property.Key.TrimEnd("__Like"), "like");
                    if (where != null)
                    {
                        whereBuilder.Add(where.Value.whereBuild);
                        paramList.Add(where.Value.param);
                    }
                }
                else if(property.Key.EndsWith("__Start"))
                {
                    (string whereBuild, SugarParameter param)? where = getWhereBuild(columns, property, property.Key.TrimEnd("__Start"), ">");
                    if (where != null)
                    {
                        whereBuilder.Add(where.Value.whereBuild);
                        paramList.Add(where.Value.param);
                    }
                }
                else if (property.Key.EndsWith("__End"))
                {
                    (string whereBuild, SugarParameter param)? where = getWhereBuild(columns, property, property.Key.TrimEnd("__End"), "<");
                    if (where != null)
                    {
                        whereBuilder.Add(where.Value.whereBuild);
                        paramList.Add(where.Value.param);
                    }
                }
                else
                {
                    (string whereBuild, SugarParameter param)? where = getWhereBuild(columns, property, property.Key, " = ");
                    if (where != null)
                    {
                        whereBuilder.Add(where.Value.whereBuild);
                        paramList.Add(where.Value.param);
                    }
                }
              
            }
            var whereClause = "";
            if (whereBuilder.Count > 0)
            {
                whereClause = whereBuilder.Join(" AND ");
            }
            return (whereClause, paramList.ToArray());
        }

        private (string whereBuild, SugarParameter param)? getWhereBuild(List<DbColumnInfo> columns, KeyValuePair<string, object> property,string dbColumnName, string v)
        {
           // 判断表是否包含对应的字段
            if (columns.Any(c => c.DbColumnName.Equals(dbColumnName, StringComparison.OrdinalIgnoreCase)))
            {
                var columnInfo = columns.FirstOrDefault(c => c.DbColumnName.Equals(dbColumnName, StringComparison.OrdinalIgnoreCase));
                var paramName = $"@{property.Key}";
                if ($"{property.Value}".IsNullOrEmpty() || columnInfo==null)
                {
                    return null;
                }
                var type = property.Value.GetType();
                if (v == "like")
                {
                    paramName = $"'%'+{paramName}+'%'";
                }
                if (v == "in")
                {
                    paramName = $"({paramName})";
                }
                return ($"{dbColumnName} {v} {paramName}", new SugarParameter(paramName, property.Value));

            }
            else
            {
                return null;
            }
        }
    }

}

