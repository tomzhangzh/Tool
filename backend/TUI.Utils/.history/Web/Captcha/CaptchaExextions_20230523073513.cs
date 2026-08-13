// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 验证码扩展
/// </summary>
public static class CaptchaExextions
{
    /// <summary>
    /// 验证输入的验证码
    /// </summary>
    /// <param name="inputVerifyCode">输入的验证码</param>
    /// <param name="verifyKey">验证码Key</param>
    /// <returns></returns>
    public static bool VerifyCode(this string inputVerifyCode, string verifyKey)
    {
        return CaptchaHelper.VerifyCode(inputVerifyCode, verifyKey);
    }
}