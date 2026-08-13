// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 登陆失败对象
/// </summary>
public class LoginFailDto
{
    public LoginFailDto()
    {
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="account">登陆账号</param>
    public LoginFailDto(string account)
    {
        this.Account = account;

        Ip = App.HttpContext.GetIpV4();
        if (string.IsNullOrWhiteSpace(Ip)) Ip = App.HttpContext.GetIpV4();
        FailCount = 1;//第一次，默认为1
    }

    /// <summary>
    /// 登陆账号
    /// </summary>
    public string Account { get; set; }

    /// <summary>
    /// 登陆IP地址
    /// </summary>
    public string Ip { get; set; }

    /// <summary>
    /// 登陆失败次数
    /// </summary>
    public int FailCount { get; set; } = 1;

    /// <summary>
    /// 禁止登陆结束时间
    /// </summary>
    public DateTimeOffset? ExpireTime { get; set; }
}