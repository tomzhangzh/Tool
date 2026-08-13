using Furion;
using Furion.Logging.Extensions;
using TUI.Core;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
namespace TUI.Core;

public static class SqlSugarSetup
{
    /// <summary>
    /// SqlsugarScope的配置
    /// Scope必须用单例注入
    /// 不可以用Action委托注入
    /// </summary>
    /// <param name="services"></param>
    public static void SqlSugarScopeConfigure(this IServiceCollection services)
    {
        //数据库序号从0开始,默认数据库为0
        var config = App.GetConfig<ConnectionStringsOptions>("ConnectionStrings", true);

        //默认数据库
        List<DbConfig> dbList = new List<DbConfig>();
        DbConfig defaultdb = new DbConfig()
        {
            DbNumber = config.DefaultDbNumber,
            DbString = config.DefaultDbString,
            DbType = config.DefaultDbType
        };
        dbList.Add(defaultdb);
        //业务数据库集合
        if (config.DbConfigs != null)
        {
            foreach (var item in config.DbConfigs)
            {
                dbList.Add(item);
            }
        }
        

        List<ConnectionConfig> connectConfigList = new List<ConnectionConfig>();

        foreach (var item in dbList)
        {
            //防止数据库重复，导致的事务异常
            if (connectConfigList.Any(a => a.ConfigId == (dynamic)item.DbNumber || a.ConnectionString == item.DbString))
            {
                continue;
            }
            connectConfigList.Add(new ConnectionConfig()
            {
                ConnectionString = item.DbString,
                DbType = (DbType)Convert.ToInt32(Enum.Parse(typeof(DbType), item.DbType)),
                IsAutoCloseConnection = true,
                ConfigId = item.DbNumber,
                InitKeyType = InitKeyType.Attribute,
                MoreSettings = new ConnMoreSettings()
                {
                    IsAutoRemoveDataCache = true//自动清理缓存

                },
                ConfigureExternalServices = new ConfigureExternalServices()
                {
                    DataInfoCacheService = new SqlSugarCache(),
                    EntityNameService = (type, entity) =>
                    {
                        var attributes = type.GetCustomAttributes(true);
                        if (attributes.Any(it => it is TableAttribute))
                        {
                            entity.DbTableName = (attributes.First(it => it is TableAttribute) as TableAttribute).Name;
                        }
                    },
                    EntityService = (type, column) =>
                    {
                        var attributes = type.GetCustomAttributes(true);
                        if (attributes.Any(it => it is KeyAttribute))// by attribute set primarykey
                        {
                            column.IsPrimarykey = true; //有哪些特性可以看 1.2 特性明细
                        }
                        if (attributes.Any(it => it is ColumnAttribute))
                        {
                            column.DbColumnName = (attributes.First(it => it is ColumnAttribute) as ColumnAttribute).Name;
                        }
                    }
                }
            });
        }

        List<Type> types = App.EffectiveTypes.Where(a => !a.IsAbstract && a.IsClass && a.GetCustomAttributes(typeof(SugarTable), true)?.FirstOrDefault() != null).ToList();

        SqlSugarScope sqlSugarScope = new SqlSugarScope(connectConfigList,
            //全局上下文生效
            db =>
            {
                /*
                 * 默认只会配置到第一个数据库，这里按照官方文档进行多数据库/多租户文档的说明进行循环配置
                 */
                foreach (var c in connectConfigList)
                {
                    var dbProvider = db.GetConnectionScope((string)c.ConfigId);
                    //执行超时时间
                    dbProvider.Ado.CommandTimeOut = 30;

                    dbProvider.Aop.OnLogExecuting = (sql, pars) =>
                    {
                        if (sql.StartsWith("SELECT"))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                        }
                        if (sql.StartsWith("UPDATE") || sql.StartsWith("INSERT"))
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        if (sql.StartsWith("DELETE"))
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                        }
                        App.PrintToMiniProfiler("SqlSugar", "Info", UtilMethods.GetSqlString(c.DbType, sql, pars));
                        $"DB:{c.ConfigId}, Sql:\r\n\r\n {UtilMethods.GetSqlString(c.DbType, sql, pars)}".LogInformation();
                        //Console.WriteLine(db.GetHashCode());

                    };

                    dbProvider.Aop.DataExecuting = (oldValue, entityInfo) =>
                    {
                        // 新增操作
                        if (entityInfo.OperationType == DataFilterType.InsertByObject)
                        {
                            // 主键(long)-赋值雪花Id
                            if (entityInfo.EntityColumnInfo.IsPrimarykey && entityInfo.EntityColumnInfo.PropertyInfo.PropertyType == typeof(long))
                            {
                                var id = ((dynamic)entityInfo.EntityValue).Id;
                                if (id == null || id == 0)
                                    entityInfo.SetValue(Yitter.IdGenerator.YitIdHelper.NextId());
                            }


                            if (entityInfo.PropertyName == "CreatedTime")
                                entityInfo.SetValue(DateTime.Now);
                            if (App.User != null)
                            {
                                //if (entityInfo.PropertyName == "TenantId")
                                //{
                                //    var tenantId = ((dynamic)entityInfo.EntityValue).TenantId;
                                //    if (tenantId == null || tenantId == 0)
                                //        entityInfo.SetValue(App.User.FindFirst(ClaimConst.TENANT_ID)?.Value);
                                //}
                                if (entityInfo.PropertyName == "CreatedUserId")
                                {
                                    var createUserId = ((dynamic)entityInfo.EntityValue).CreatedUserId;
                                    if (createUserId == null || createUserId == 0)
                                        entityInfo.SetValue(AppEx.CurrentUser.Id);
                                }

                                if (entityInfo.PropertyName == "CreatedUserName")
                                    entityInfo.SetValue(AppEx.CurrentUser.UserName);
                            }
                        }
                        // 更新操作
                        if (entityInfo.OperationType == DataFilterType.UpdateByObject)
                        {
                            if (entityInfo.PropertyName == "UpdatedTime")
                                entityInfo.SetValue(DateTime.Now);
                            if (entityInfo.PropertyName == "UpdatedUserId")
                                entityInfo.SetValue(AppEx.CurrentUser.Id);
                            if (entityInfo.PropertyName == "UpdatedUserName")
                                entityInfo.SetValue(AppEx.CurrentUser.UserName);

                        }
                    };

                    #region 全局过滤
                    ////全局租户id过滤
                    //var superAdminViewAllData = Convert.ToBoolean(App.GetOptions<SystemSettingsOptions>().SuperAdminViewAllData);//超管是否可以查看所有数据

                    ////如果是超管
                    //if ((UserManager.IsSuperAdmin && !superAdminViewAllData) || !UserManager.IsSuperAdmin)
                    //{

                    //    dbProvider.QueryFilter.AddTableFilter<ITenantId>(m => m.TenantId == UserManager.TenanId);
                    //}
                    //全局软删除过滤
                    var tables = db.DbMaintenance.GetTableInfoList();
                    tables.ForEach(t =>
                    {
                        var hasIsDeleted = db.DbMaintenance.GetColumnInfosByTableName(t.Name).Any(x => x.PropertyName == CommonConst.DELETE_FIELD);
                        if (hasIsDeleted == true)
                        {
                            Type returnType = Type.GetType($"TUI.Core.Entities.{t.Name}");
                            if (returnType != null)
                            {
                                var dataParameter = Expression.Parameter(returnType, "data");
                                var expr = DynamicExpressionParser.ParseLambda(new[] { dataParameter }, typeof(bool), "data.IsDeleted ==False");
                                dbProvider.QueryFilter.AddTableFilter(returnType, expr);
                            }
                        }
                    });
                    // dbProvider.QueryFilter.AddTableFilter<IIsDeleted>(m => m.IsDeleted == false);
                    #endregion
                }
            });
        services.AddSingleton<ISqlSugarClient>(sqlSugarScope);
        // 注册 SqlSugar 仓储
        services.AddScoped(typeof(SqlSugarRepository<>));
    }

    /// <summary>
    /// 添加 SqlSugar 拓展
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <param name="buildAction"></param>
    /// <returns></returns>
    public static IServiceCollection AddSqlSugar(this IServiceCollection services, ConnectionConfig config, Action<ISqlSugarClient> buildAction = default)
    {
        var list = new List<ConnectionConfig>();
        list.Add(config);
        return services.AddSqlSugar(list, buildAction);
    }

    /// <summary>
    /// 添加 SqlSugar 拓展
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configs"></param>
    /// <param name="buildAction"></param>
    /// <returns></returns>
    public static IServiceCollection AddSqlSugar(this IServiceCollection services, List<ConnectionConfig> configs, Action<ISqlSugarClient> buildAction = default)
    {
        // 注册 SqlSugar 客户端
        services.AddScoped<ISqlSugarClient>(u =>
        {
            var db = new SqlSugarClient(configs);
            buildAction?.Invoke(db);
            return db;
        });

        // 注册 SqlSugar 仓储
        services.AddScoped(typeof(SqlSugarRepository<>));
        return services;
    }
}
