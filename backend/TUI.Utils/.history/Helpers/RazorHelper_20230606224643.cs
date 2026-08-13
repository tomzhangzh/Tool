// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using RazorEngine.Text;

namespace TUI.Utils;

public static class RazorHelper
{
    ///// <summary>
    ///// 自己封装一个Razor.Parse()方法
    ///// </summary>
    ///// <param name="context">上下文对象</param>
    ///// <param name="cshtmlPath">.cshtml文件的相对路径</param>
    ///// <param name="obj">要传入的对象参数</param>
    ///// <returns>返回一个解析过的Razor模板字符串</returns>
    //private static string RazorParse(HttpContext context, string cshtmlPath, params object[] obj)
    //{
    //    //获得.cshtml文件的绝对路径
    //    string path = context.Server.MapPath(cshtmlPath);
    //    //通过路径将文件读成文本
    //    string txt = File.ReadAllText(path);

    //    //为模板准备一个缓冲，根据文件的修改时间动态组合
    //    string cacheName = path + File.GetLastWriteTime(path);

    //    Engine.Razor.RunCompile(template, "templateKey2", null, model);

    //    //根据需求是否要传入对象参数，在调用Razor.Parse方法返回解析好的Razor模板
    //    return obj.Length > 0 ? GetRazorEngineService().Parse(txt, obj[0], cacheName) : RazorEngine.Razor.Parse(txt, cacheName);

    //}
    /// <summary>
    /// RazorEngineService
    /// </summary>
    private static IRazorEngineService Res { get; set; }

    /// <summary>
    /// Razor 引擎服务
    /// </summary>
    /// <returns></returns>
    public static IRazorEngineService CustomerEngine()
    {
        if (Res == null)
        {
            //创建配置
            var config = new TemplateServiceConfiguration();
            config.Language = Language.CSharp; // VB.NET as template language.
            config.EncodedStringFactory = new RawStringFactory(); // Raw string encoding. 可解决输出的html字符是编码后的
                                                                  //config.EncodedStringFactory = new HtmlEncodedStringFactory(); // Html encoding.

            //根据配置创建服务
            Res = RazorEngineService.Create(config);
        }
        return Res;
    }
}