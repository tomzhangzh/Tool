// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// 验证码
/// </summary>
public class CaptchaHelper
{
    /// <summary>
    /// 获取图片验证码
    /// </summary>
    /// <param name="verCode">out 验证码</param>
    /// <param name="verifyKey">验证码Key（如：Admin）</param>
    /// <param name="codeLenght">验证码长度</param>
    /// <param name="randType">验证码字符类型</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="lineNum">干扰线数量</param>
    /// <param name="expiresSecond">过期秒数</param>
    /// <returns></returns>
    public static byte[] GetVerifyCode(out string verCode, string verifyKey = "Admin", int codeLenght = 4, RandType randType = RandType.NumAndStr, int width = 80, int height = 30, int lineNum = 6, int expiresSecond = 180)
    {
        var httpContext = App.HttpContext;
        //注册编码
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        //获取随机的验证码
        verCode = RandomHelper.GetString(codeLenght, randType, "0123456789TUIDEFGHJKMNPQRSTUVWXYZTUIdefghjkmnpqrstuvwxyz");

        //生成验证码
        var codebyte = SkiaCaptcha.GetCaptcha(verCode, width, height, lineNum);
        //var codebyte = SkiaCaptcha.GetCaptcha(verCode);
        //验证码加密
        var vercode = verCode.ToLower().ToPBKDF2();
        httpContext.SetSession(verifyKey, vercode);
        return codebyte;
    }

    /// <summary>
    /// 验证输入的验证码
    /// </summary>
    /// <param name="inputVerifyCode">输入的验证码</param>
    /// <param name="verifyKey">验证码Key</param>
    /// <returns></returns>
    public static bool VerifyCode(string inputVerifyCode, string verifyKey)
    {
        if (string.IsNullOrWhiteSpace(verifyKey)) throw new ArgumentNullException(nameof(verifyKey));
        var httpContext = App.HttpContext;
        //注册编码
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        //从session中取出验证码
        var encryptText = httpContext.GetSessionValue(verifyKey);
        //if (state == false) return false;//没有找到存储在session的图片验证码
        // var encryptText = Encoding.UTF8.GetString(vbytes);
        // var code2 = inputVerifyCode.top;
        //对比验证码
        if (inputVerifyCode.ToLower().ToPBKDF2Compare2(encryptText))
        {
            httpContext.DeleteSession(verifyKey);//清理cookie
                                                 //httpContext.Session.Remove(verifyKey);
            return true;
        }
        httpContext.DeleteSession(verifyKey);//清理cookie
        return false;
    }
}