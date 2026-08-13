// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using SkiaSharp;

namespace TUI.Utils;

/// <summary>
/// SkiaSharp 验证码
/// </summary>
public class SkiaCaptcha
{
    /// <summary>
    /// 干扰线的颜色集合
    /// </summary>
    private static List<SKColor> colors = new List<SKColor>()
    {
        SKColors.AliceBlue,
        SKColors.PaleGreen,
        SKColors.PaleGoldenrod,
        SKColors.Orchid,
        SKColors.OrangeRed,
        SKColors.Orange,
        SKColors.OliveDrab,
        SKColors.Olive,
        SKColors.OldLace,
        SKColors.Navy,
        SKColors.NavajoWhite,
        SKColors.Moccasin,
        SKColors.MistyRose,
        SKColors.MintCream,
        SKColors.MidnightBlue,
        SKColors.MediumVioletRed,
        SKColors.MediumTurquoise,
        SKColors.MediumSpringGreen,
        SKColors.LightSlateGray,
        SKColors.LightSteelBlue,
        SKColors.LightYellow,
        SKColors.Lime,
        SKColors.LimeGreen,
        SKColors.Linen,
        SKColors.PaleTurquoise,
        SKColors.Magenta,
        SKColors.MediumAquamarine,
        SKColors.MediumBlue,
        SKColors.MediumOrchid,
        SKColors.MediumPurple,
        SKColors.MediumSeaGreen,
        SKColors.MediumSlateBlue,
        SKColors.Maroon,
        SKColors.PaleVioletRed,
        SKColors.PapayaWhip,
        SKColors.PeachPuff,
        SKColors.Snow,
        SKColors.SpringGreen,
        SKColors.SteelBlue,
        SKColors.Tan,
        SKColors.Teal,
        SKColors.Thistle,
        SKColors.SlateGray,
        SKColors.Tomato,
        SKColors.Violet,
        SKColors.Wheat,
        SKColors.White,
        SKColors.WhiteSmoke,
        SKColors.Yellow,
        SKColors.YellowGreen,
        SKColors.Turquoise,
        SKColors.LightSkyBlue,
        SKColors.SlateBlue,
        SKColors.Silver,
        SKColors.Peru,
        SKColors.Pink,
        SKColors.Plum,
        SKColors.PowderBlue,
        SKColors.Purple,
        SKColors.Red,
        SKColors.SkyBlue,
        SKColors.RosyBrown,
        SKColors.SaddleBrown,
        SKColors.Salmon,
        SKColors.SandyBrown,
        SKColors.SeaGreen,
        SKColors.SeaShell,
        SKColors.Sienna,
        SKColors.RoyalBlue,
        SKColors.LightSeaGreen,
        SKColors.LightSalmon,
        SKColors.LightPink,
        SKColors.Crimson,
        SKColors.Cyan,
        SKColors.DarkBlue,
        SKColors.DarkCyan,
        SKColors.DarkGoldenrod,
        SKColors.DarkGray,
        SKColors.Cornsilk,
        SKColors.DarkGreen,
        SKColors.DarkMagenta,
        SKColors.DarkOliveGreen,
        SKColors.DarkOrange,
        SKColors.DarkOrchid,
        SKColors.DarkRed,
        SKColors.DarkSalmon,
        SKColors.DarkKhaki,
        SKColors.DarkSeaGreen,
        SKColors.CornflowerBlue,
        SKColors.Chocolate,
        SKColors.AntiqueWhite,
        SKColors.Aqua,
        SKColors.Aquamarine,
        SKColors.Azure,
        SKColors.Beige,
        SKColors.Bisque,
        SKColors.Coral,
        SKColors.Black,
        SKColors.Blue,
        SKColors.BlueViolet,
        SKColors.Brown,
        SKColors.BurlyWood,
        SKColors.CadetBlue,
        SKColors.Chartreuse,
        SKColors.BlanchedAlmond,
        SKColors.Transparent,
        SKColors.DarkSlateBlue,
        SKColors.DarkTurquoise,
        SKColors.IndianRed,
        SKColors.Indigo,
        SKColors.Ivory,
        SKColors.Khaki,
        SKColors.Lavender,
        SKColors.LavenderBlush,
        SKColors.HotPink,
        SKColors.LawnGreen,
        SKColors.LightBlue,
        SKColors.LightCoral,
        SKColors.LightCyan,
        SKColors.LightGoldenrodYellow,
        SKColors.LightGray,
        SKColors.LightGreen,
        SKColors.LemonChiffon,
        SKColors.DarkSlateGray,
        SKColors.Honeydew,
        SKColors.Green,
        SKColors.DarkViolet,
        SKColors.DeepPink,
        SKColors.DeepSkyBlue,
        SKColors.DimGray,
        SKColors.DodgerBlue,
        SKColors.Firebrick,
        SKColors.GreenYellow,
        SKColors.FloralWhite,
        SKColors.Fuchsia,
        SKColors.Gainsboro,
        SKColors.GhostWhite,
        SKColors.Gold,
        SKColors.Goldenrod,
        SKColors.Gray,
        SKColors.ForestGreen,
    };

    /// <summary>
    /// 创建画笔
    /// </summary>
    /// <param name="color"></param>
    /// <param name="fontSize"></param>
    /// <returns></returns>
    private static SKPaint CreatePaint(SKColor color, float fontSize)
    {
        var font = SKTypeface.FromFamilyName(null, SKFontStyleWeight.SemiBold, SKFontStyleWidth.ExtraCondensed, SKFontStyleSlant.Upright);
        var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Color = color;
        paint.Typeface = font;
        paint.TextSize = fontSize;
        return paint;
    }

    /// <summary>
    /// 获取验证码
    /// </summary>
    /// <param name="captchaText">验证码文字</param>
    /// <param name="width">图片宽度</param>
    /// <param name="height">图片高度</param>
    /// <param name="lineNum">干扰线数量</param>
    /// <param name="lineStrookeWidth">干扰线宽度</param>
    /// <returns></returns>
    public static byte[] GetCaptcha(string captchaText, int width, int height, int lineNum = 10, int lineStrookeWidth = -1)
    {
        //创建bitmap位图，宽度和高度都适当添加点，防止字超出画布，看不见
        using (var image2d = new SKBitmap(width + 10, height + 5, SKColorType.Bgra8888, SKAlphaType.Premul))
        //创建画笔
        using (var canvas = new SKCanvas(image2d))
        {
            //填充背景颜色为白色
            canvas.DrawColor(SKColors.White);
            //将文字写到画布上
            SKColor[] fontColors = { SKColors.Blue, SKColors.Orange, SKColors.Green, SKColors.Red, SKColors.Brown, SKColors.DarkRed };
            var chars = captchaText.ToCharArray();
            //foreach (var fontitem in captchaText.ToCharArray())
            //一个字一个字写到画布上去
            for (int i = 0; i < chars.Length; i++)
            {
                var x = (float)i * 15;//x坐标
                var y1 = RandomHelper.GetInt(0, 3);
                var y = height - y1;//y坐标

                using (var drawStyle = CreatePaint(fontColors[RandomHelper.GetInt(0, fontColors.Length)], height))
                {
                    //将文字写到画布上去
                    canvas.DrawText(chars[i].ToString(), x, y, drawStyle);
                }
            }
            //using (var drawStyle = CreatePaint(SKColors.Black, height))
            //    canvas.DrawText(captchaText, 0 + 5, height - 5, drawStyle);
            //画随机干扰线
            using (var drawStyle = new SKPaint())
            {
                var random = new Random();
                for (var i = 0; i < lineNum; i++)
                {
                    drawStyle.Color = colors[random.Next(colors.Count)];
                    if (lineStrookeWidth <= 0)
                        drawStyle.StrokeWidth = RandomHelper.GetInt(5, 20) / 10;
                    else
                        drawStyle.StrokeWidth = lineStrookeWidth;
                    canvas.DrawLine(random.Next(0, width), random.Next(0, height), random.Next(0, width), random.Next(0, height), drawStyle);
                }
            }
            //返回图片byte
            using (var img = SKImage.FromBitmap(image2d))
            using (var p = img.Encode(SKEncodedImageFormat.Png, 100))
                return p.ToArray();
        }
    }
}