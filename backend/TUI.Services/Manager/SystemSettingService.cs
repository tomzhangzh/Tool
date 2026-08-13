using TUI.Services.DBModel;
using TUI.Services.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using TUI.Services.Extension;
namespace TUI.Services.Manager
{
    public interface ISystemSettingService :ISingletonDependency
    {
        SystemSetting GetSystemSetting();
        SystemSetting RefreshSystemSetting();
        T GetSetting<T>(string Type, string Category = nameof(SystemSetting)) where T : class, new();
    }
    public class SystemSettingService:ISystemSettingService
    {
        private readonly SqlSugar.ISqlSugarClient dbSqlSugar;
        private SystemSetting SystemSetting = null;
        public SystemSettingService(SqlSugar.ISqlSugarClient dbSqlSugar)
        {
            this.dbSqlSugar = dbSqlSugar;
        
        }
        public T GetSetting<T>(string Type,string Category= nameof(SystemSetting)) where T : class, new()
        {
            var find = this.dbSqlSugar.Queryable<SystemSettingItem>().Where(x => x.Type == Type && x.Category==Category).First();
            if (find != null && find.Json.IsNullOrEmpty()==false)
            {
                return find.Json.Deserialize<T>();
            }
            else
            {
                var result = new T();
                var initData = typeof(T).GetMethod("InitData");
                if (initData != null)
                {
                    initData.Invoke(result, null);
                }
                return result;
            }            
        }
        public SystemSetting RefreshSystemSetting()
        {
            this.SystemSetting = null;
            return GetSystemSetting();
        }
        public SystemSetting GetSystemSetting()
        {
           if (SystemSetting == null)
            {
                SystemSetting = new SystemSetting();
                foreach (var propertyInfo in typeof(SystemSetting).GetProperties())
                {
                    var find = this.dbSqlSugar.Queryable<SystemSettingItem>().Where(x => x.Type == propertyInfo.Name && x.Category== nameof(SystemSetting)).First();
                    if (find != null && find.Json.IsNullOrEmpty() == false)
                    {
                        var obj = find.Json.DeserializeObject(propertyInfo.PropertyType);
                        propertyInfo.SetValue(SystemSetting, obj);
                    }
                    else
                    {
                        this.dbSqlSugar.Insertable(new SystemSettingItem()
                        {
                            Category = nameof(SystemSetting),
                            Json = propertyInfo.GetValue(SystemSetting).ToJSON(),
                            Type = propertyInfo.Name,
                            TypeFullName = propertyInfo.PropertyType.FullName,
                        }).ExecuteCommandIdentityIntoEntity();
                    }
                }
            }
            return SystemSetting;
        }

    }
}
