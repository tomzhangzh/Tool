using Furion.ClayObject;
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
    public interface IDynamicService : IScoped 
    {
        dynamic Add(dynamic entity);
        void Add(IList<dynamic> entities);

        dynamic Update(dynamic entity);

        void Update(IList<dynamic> entities);

        void Delete(dynamic entity);
        void Delete(int Id);
       void Delete(IList<dynamic> entities);

        dynamic AddOrUpdate(dynamic entity);
        
        void AddOrUpdate(List<dynamic> entities);
        void Update(Expression<Func<dynamic, dynamic>> setColumn, Expression<Func<dynamic, bool>> where = null);
        ISugarQueryable<System.Dynamic.ExpandoObject> Queryable(string where=null);
        dynamic GetOrNew(int id);
        DynamicService SetTableInfo(string TableName, string ShortName);
        (string whereClause, SugarParameter[] parameters) GenerateWhereClauseAndParameters(dynamic filter);

    }
}
