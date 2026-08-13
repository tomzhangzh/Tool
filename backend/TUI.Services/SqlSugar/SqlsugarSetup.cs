using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services
{
    public static class SqlsugarSetup
    {
        public static void AddSqlsugarSetup(this IServiceCollection services, IConfiguration configuration, string dbName = "TUIDatabase")
        {
            //如果多个数数据库传 List<ConnectionConfig>
            var configConnection = new ConnectionConfig()
            {
                DbType = SqlSugar.DbType.SqlServer,
                ConnectionString = configuration.GetConnectionString(dbName),
                IsAutoCloseConnection = true,
                ConfigureExternalServices = new ConfigureExternalServices()
                {
                    EntityService = (t, column) =>
                    {
                        
                        if (column.PropertyName.ToLower() == "id") //是id的设为主键
                        {
                            column.IsPrimarykey = true;
                            if (column.PropertyInfo.PropertyType == typeof(int) || column.PropertyInfo.PropertyType == typeof(long)) //是id并且是int的是自增
                            {
                                column.IsIdentity = true;
                            }
                        }
                    },
                    EntityNameService = (type, entity) =>
                    {
                       
                        if (type.Name=="LogEvent")
                        {
                            entity.DbTableName = "LogEvents";
                        }
                    }
                }
            };

            SqlSugarScope sqlSugar = new SqlSugarScope(configConnection,
                db =>
                {
                //单例参数配置，所有上下文生效
                db.Aop.OnLogExecuting = (sql, pars) =>
                    {
                    //Console.WriteLine(sql);//输出sql
                };
                });

            services.AddSingleton<ISqlSugarClient>(sqlSugar);//这边是SqlSugarScope用AddSingleton
        }
    }
}
