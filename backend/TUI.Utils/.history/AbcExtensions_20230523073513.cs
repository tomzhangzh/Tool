// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.DependencyInjection;

using Yitter.IdGenerator;

namespace Abc.Utils;

public static class AbcExtensions
{
    /// <summary>
    /// ABC 初始化
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddAbcMvcInit(this IServiceCollection services)
    {
        //MVC 登陆安全验证配置
        services.AddConfigurableOptions<MvcLoginSafeVerOptions>();
        ////ABC 基本信息配置
        //services.AddConfigurableOptions<ap>();
        //雪花ID生成配置
        services.AddConfigurableOptions<DbIdGeneratorOptions>();
        //PBKDF2
        services.AddPBKDF2EncryptionOptions();

        var snowflakeIdOptions = AppEx.GetConfig<DbIdGeneratorOptions>();
        // 保存参数（必须的操作，否则以上设置都不能生效）：
        if (snowflakeIdOptions == null) snowflakeIdOptions = new DbIdGeneratorOptions();
        YitIdHelper.SetIdGenerator(snowflakeIdOptions);

        return services;
    }
}