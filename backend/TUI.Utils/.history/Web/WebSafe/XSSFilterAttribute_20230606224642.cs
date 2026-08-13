// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Ganss.Xss;

namespace TUI.Utils;

/// <summary>
/// xss攻击防御
/// </summary>
public class XSSFilterAttribute : IAsyncActionFilter
{
    private XSS xss;

    public XSSFilterAttribute()
    {
        xss = new XSS();
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //获取Action参数集合
        var ps = context.ActionDescriptor.Parameters;
        //遍历参数集合
        foreach (var p in ps)
        {
            if (context.ActionArguments.ContainsKey(p.Name) && context.ActionArguments[p.Name] != null)
            {
                //当参数等于字符串
                if (p.ParameterType.Equals(typeof(string)))
                {
                    context.ActionArguments[p.Name] = xss.Filter(context.ActionArguments[p.Name].ToString());
                }
                else if (p.ParameterType.IsClass)//当参数等于类
                {
                    //Action 上有默认参数为null时 context.ActionArguments[p.Name] 为空值却不为null 导致后续操作报错，目前暂无解决方案
                    //如下 facetFields=null时 context.ActionArguments[p.Name] 取出却不为null
                    //具体报错接口  public Task<object> SearchBySolr(string key, int size = 10, int page = 1, string sort = "df", List<SolrFacetFields> facetFields = null)
                    try
                    {
                        ModelFieldFilter(p.Name, p.ParameterType, context.ActionArguments[p.Name]);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        //执行
        var resultContext = await next();
    }

    /// <summary>
    /// 遍历修改类的字符串属性
    /// </summary>
    /// <param name="key">类名</param>
    /// <param name="t">数据类型</param>
    /// <param name="obj">对象</param>
    /// <returns></returns>
    private object ModelFieldFilter(string key, Type t, object obj)
    {
        //获取类的属性集合
        //var ats = t.GetCustomAttributes(typeof(FieldFilterAttribute), false);

        if (obj != null)
        {
            //获取类的属性集合
            var pps = t.GetProperties().Where(p => p.GetIndexParameters().Length == 0);

            foreach (var pp in pps)
            {
                if (pp.GetValue(obj) != null)
                {
                    //当属性等于字符串
                    if (pp.PropertyType.Equals(typeof(string)))
                    {
                        string value = pp.GetValue(obj).ToString();
                        pp.SetValue(obj, xss.Filter(value));
                    }
                    else if (pp.PropertyType.IsClass)//当属性等于类进行递归
                    {
                        pp.SetValue(obj, ModelFieldFilter(pp.Name, pp.PropertyType, pp.GetValue(obj)));
                    }
                }
            }
        }

        return obj;
    }
}

internal class XSS
{
    private HtmlSanitizer sanitizer;

    public XSS()
    {
        sanitizer = new HtmlSanitizer();
        //sanitizer.AllowedTags.Add("div");//标签白名单
        sanitizer.AllowedAttributes.Add("class");//标签属性白名单,默认没有class标签属性
        //sanitizer.AllowedCssProperties.Add("font-family");//CSS属性白名单
    }

    /// <summary>
    /// XSS过滤
    /// </summary>
    /// <param name="html">html代码</param>
    /// <returns>过滤结果</returns>
    public string Filter(string html)
    {
        string str = sanitizer.Sanitize(html);
        return str;
    }
}