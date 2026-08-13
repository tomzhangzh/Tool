using TUI.Services.Extension;
using TUI.Services.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.Manager
{
   
    public class LookupItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public interface ILookupService : IScopeDependency
    {
        List<LookupItem> List(LookupSettingInfo info, object model);
        List<LookupItem> List(LookupSettingInfo info);
        string GetYesOrNo(Boolean? yes);
        string GetYesOrNo(Boolean yes);
    }
    public class LookupService: ILookupService
    {
        private readonly SqlSugar.ISqlSugarClient dbSqlSugar;
        public LookupService(SqlSugar.ISqlSugarClient dbSqlSugar)
        {
            this.dbSqlSugar = dbSqlSugar;
        }
        public List<LookupItem> List(LookupSettingInfo info, object model)
        {
            if (string.IsNullOrEmpty(info.PlusSQL) == false)
            {
                info.PlusSQL = model.ToString4(info.PlusSQL);
            }
            var result = new List<LookupItem>();
            var sql = string.Format("select {0} as [Name],CAST({1} as nvarchar(50)) as [Value] from {2} {3}",
                info.NameField
                , info.ValueField
                , info.TableName
                , info.PlusSQL
                );

            var list = this.dbSqlSugar.SqlQueryable<LookupItem>(sql).ToList();
            foreach (var item in list)
            {
                result.Add(new LookupItem()
                {
                    Name = string.Format("{0}", item.Name),
                    Value = string.Format("{0}", item.Value),
                });
            }
            return result;
        }
        public List<LookupItem> List(LookupSettingInfo info)
        {
            var result = new List<LookupItem>();
            var sql = string.Format("select {0} as [Name],CAST({1} as nvarchar(50)) as [Value] from {2} {3}",
                info.NameField
                , info.ValueField
                , info.TableName
                , info.PlusSQL
                );

            var list = this.dbSqlSugar.Queryable<LookupItem>(sql).ToList();
            foreach (var item in list)
            {
                result.Add(new LookupItem()
                {
                    Name = string.Format("{0}", item.Name),
                    Value = string.Format("{0}", item.Value),
                });
            }
            return result;
        }

        public string GetYesOrNo(Boolean? yes)
        {
            if (yes == null)
                return "";
            else
                return GetYesOrNo(yes.Value);
        }
        public  string GetYesOrNo(Boolean yes)
        {

            if (yes == true)

                return "Yes";

            else
                return "No";

        }
    }
}
