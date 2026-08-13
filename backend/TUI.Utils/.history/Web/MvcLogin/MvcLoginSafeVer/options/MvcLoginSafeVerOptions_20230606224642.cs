// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// MVC 登陆安全验证
/// </summary>
public class MvcLoginSafeVerOptions : IConfigurableOptionsListener<MvcLoginSafeVerOptions>
{
    /// <summary>
    /// MVC登陆安全验证 集合
    /// </summary>
    public List<MvcLoginSafeVerItem> configs { get; set; } = new();

    public void OnListener(MvcLoginSafeVerOptions options, IConfiguration configuration)
    {
        Type entityType = GetType();//获得该类的Type

        var dics = options.GetDictionary();
        foreach (var item in dics)
        {
            PropertyInfo propertyInfo = entityType.GetProperty(item.Key);
            propertyInfo.FieldSetValue(item.Key, item.Value as object);
        }
    }

    public void PostConfigure(MvcLoginSafeVerOptions options, IConfiguration configuration)
    {
    }
}

/// <summary>
/// MVC登陆安全验证
/// </summary>
public class MvcLoginSafeVerItem
{
    /// <summary>
    /// 代码
    /// </summary>
    public string Code { get; set; } = "HtAdmin";

    /// <summary>
    /// 登陆最大失败次数
    /// </summary>
    public int AccountMaxFailCount { get; set; } = 5;

    /// <summary>
    /// 限制登陆分钟数
    /// </summary>
    public int AccountLimitLoginMinute { get; set; } = 10;

    /// <summary>
    /// IP最大失败次数
    /// </summary>
    public int IpMaxFailCount { get; set; } = 20;

    /// <summary>
    /// IP限制登录分钟
    /// </summary>
    public int IpLimitLoginMinute { get; set; } = 30;
}