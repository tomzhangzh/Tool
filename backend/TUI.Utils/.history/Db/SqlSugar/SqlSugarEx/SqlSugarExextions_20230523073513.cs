// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.DependencyInjection;

namespace Abc.Utils;

public static class SqlSugarExextions
{
    /// <summary>
    /// 添加 SqlSugar 初始化设置
    /// </summary>
    /// <param name="services">服务</param>
    /// <param name="configuration">全局配置选项</param>
    /// <param name="dbName">数据库连接字符串名称</param>
    public static void AddSqlsugarSetup(this IServiceCollection services, IConfiguration configuration, string dbName = "DbConnectionString")
    {
        ////如果多个数数据库传 List<ConnectionConfig>
        //var configConnection = new ConnectionConfig()
        //{
        //    DbType = SqlSugar.DbType.SqlServer,
        //    ConnectionString = configuration.GetConnectionString(dbName),
        //    IsAutoCloseConnection = true,
        //};

        //var configs = new List<ConnectionConfig>();
        //configs.Add(SqlSugarCore.GetConnectionConfig());

        //SqlSugarScope sqlSugar = new SqlSugarScope(configs,
        //    db => db.SqlSugarConfig());

        var sqlSugarDbSetting = SqlSugarHelper.SqlSugarDbSetting;
        //设置雪花ID 的 workId
        SnowFlakeSingle.WorkId = sqlSugarDbSetting.SnowFlakeSingle_WorkId;

        //
        SqlSugarScope sqlSugar = new SqlSugarScope(sqlSugarDbSetting.ConnectionConfigs, db => db.InitDbAop());

        //创建数据库和表
        SqlSugarHelper.InitDb();
        // DataSeed.InitData(isInit: true);
        //初始化数据种子
        SqlSugarHelper.InitDataSeed();

        //这边是SqlSugarScope用AddSingleton
        services.AddSingleton<ISqlSugarClient>(sqlSugar);

        //// 注册 SqlSugar 仓储
        //// **默认不在注册仓储，仓储模板不是线程安全，容易导致线程安全问题**
        //services.AddScoped(typeof(SqlSugarRepository<>));

        // 注册事务与工作单元
        //services.AddUnitOfWork<SqlSugarUnitOfWork>();

        SqlSugarHelper.Init();
    }
}

/*
用法： https://dotnetchina.gitee.io/furion/docs/sqlsugar

//1.构造函数注入
SqlSugar.ISqlSugarClient db;
public WeatherForecastController(ISqlSugarClient db)
{
this.db = db;
}

//2.手动获取
App.GetService<ISqlSugarClient>();

*/