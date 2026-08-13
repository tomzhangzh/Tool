// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// PBKDF2 加密服务拓展
/// </summary>
[SuppressSniffer]
public static class DataEncryptionServiceCollectionExtensions
{
    /// <summary>
    /// 注册 PBKDF2 加密服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddPBKDF2EncryptionOptions(this IServiceCollection services)
    {
        // 添加默认配置
        services.AddConfigurableOptions<PBKDF2EncryptionSettingsOptions>();

        return services;
    }
}