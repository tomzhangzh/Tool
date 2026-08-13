// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Microsoft.Extensions.DependencyInjection;

namespace TUI.Utils;

public static class WebSafeExtensions
{
    /// <summary>
    /// 防止跨站点请请求伪造xsrf/csrf
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCSRF(this IServiceCollection services, CookieBuilder antiforgeryCookieBuilder = null)
    {
        #region 防止跨站点请请求伪造xsrf/csrf

        //防止跨站点请请求伪造xsrf/csrf
        //ajax提交时，需要自定义Head时使用
        services.AddAntiforgery(options =>
        {
            //options.FormFieldName = "AntiforgeryFieldname";//防伪造系统用于在视图中呈现防伪造令牌的隐藏表单域的名称。
            //options.HeaderName = "X-CSRF-TOKEN-HEADERNAME";//	防伪造系统使用的标头的名称。 如果为 null，则系统仅考虑表单数据。
            options.SuppressXFrameOptionsHeader = false;
            if (antiforgeryCookieBuilder != null)
            {
                options.Cookie = antiforgeryCookieBuilder;
            }
            //options.Cookie = new CookieBuilder()
            //{
            //    Name = $".AspNetCore.Antiforgery.{RandomHelper.GetString(10)}",
            //    IsEssential = true,
            //    SameSite = SameSiteMode.Unspecified,
            //    SecurePolicy = CookieSecurePolicy.SameAsRequest,
            //    Expiration = TimeSpan.FromSeconds(120),//Cookie有效期
            //    HttpOnly = true,//此属性为true，则只有在http请求头中会带有此cookie的信息，而不能通过document.cookie来访问此cookie。
            //};
        });
        //全局验证
        services.AddMvc(options =>
                     options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

        #endregion 防止跨站点请请求伪造xsrf/csrf

        return services;
    }
}