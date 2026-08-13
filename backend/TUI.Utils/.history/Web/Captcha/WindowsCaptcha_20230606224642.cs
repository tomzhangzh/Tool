// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System.Drawing;
using System.Drawing.Imaging;

namespace TUI.Utils;

/// <summary>
/// Windows 系统验证码，仅限于windows系统可用
/// 使用  System.Drawing 实现
/// </summary>
public partial class WindowsCaptcha
{
    /// <summary>
    /// 获取验证码
    /// </summary>
    /// <param name="captchaText">验证码文字</param>
    /// <param name="width">图片宽度</param>
    /// <param name="height">图片高度</param>
    /// <param name="lineNum">干扰线数量</param>
    /// <returns></returns>
    public static byte[] GetCaptcha(string code, int width, int height, int lineNum = 10)
    {
        //const int codeW = 80;
        //const int codeH = 30;
        const int fontSize = 16;
        //string chkCode = string.Empty;
        //颜色列表，用于验证码、噪线、噪点
        Color[] color = { Color.Black, Color.Red, Color.Blue, Color.Green, Color.Orange, Color.Brown, Color.Brown, Color.DarkBlue };
        //字体列表，用于验证码
        string[] font = { "Times New Roman" };
        //验证码的字符集，去掉了一些容易混淆的字符
        //char[] character = { '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'd', 'e', 'f', 'h', 'k', 'm', 'n', 'r', 'x', 'y', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J', 'K', 'L', 'M', 'N', 'P', 'R', 'S', 'T', 'W', 'X', 'Y' };
        Random rnd = new Random();
        ////生成验证码字符串
        //for (int i = 0; i < 4; i++)
        //{
        //    chkCode += character[rnd.Next(character.Length)];
        //}
        //写入Session、验证码加密
        //WebHelper.WriteSession("czfw_session_verifycode", DesEncrypt.Encrypt(chkCode.ToLower(), "MD5"));

        //创建画布
        Bitmap bmp = new Bitmap(width, height);
        Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        //画噪线
        for (int i = 0; i < lineNum; i++)
        {
            int x1 = rnd.Next(width);
            int y1 = rnd.Next(height);
            int x2 = rnd.Next(width);
            int y2 = rnd.Next(height);
            Color clr = color[rnd.Next(color.Length)];
            g.DrawLine(new Pen(clr), x1, y1, x2, y2);
        }
        //画验证码字符串
        for (int i = 0; i < code.Length; i++)
        {
            string fnt = font[rnd.Next(font.Length)];
            Font ft = new Font(fnt, fontSize);
            Color clr = color[rnd.Next(color.Length)];
            g.DrawString(code[i].ToString(), ft, new SolidBrush(clr), (float)i * 18, 0);
        }
        //将验证码图片写入内存流，并将其以 "image/Png" 格式输出
        MemoryStream ms = new MemoryStream();
        try
        {
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            g.Dispose();
            bmp.Dispose();
        }
    }
}