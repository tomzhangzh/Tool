// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Abc.Core.Utils.Web.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Claims;
using UAParser;

namespace Abc.Utils;

public static class WebExtensions
{
    /// <summary>
    /// 获取当前域名(http://xxx.com)
    /// </summary>
    /// <param name="httpContext">当前http上下文</param>
    /// <returns></returns>
    public static string GetDomain(this HttpContext httpContext)
    {
        return new StringBuilder()
.Append(httpContext.Request.Scheme)
.Append("://")
.Append(httpContext.Request.Host).ToString();
    }

    /// <summary>
    /// 获取不包含Host的url部分(/home/index?abc=12312)
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static string GetUrlNoHost(this HttpContext httpContext)
    {
        return new StringBuilder()
                    //.Append(request.Scheme)
                    //.Append("://")
                    //.Append(request.Host)
                    .Append(httpContext.Request.PathBase)
                    .Append(httpContext.Request.Path)
                    .Append(httpContext.Request.QueryString)
                    .ToString();
    }

    /// <summary>
    /// 获取不包含Host的url部分(http://xxx.com/home/index?abc=12312)
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static string GetUrl(this HttpContext httpContext)
    {
        return new StringBuilder()
                    .Append(httpContext.Request.Scheme)
                    .Append("://")
                    .Append(httpContext.Request.Host)
                    .Append(httpContext.Request.PathBase)
                    .Append(httpContext.Request.Path)
                    .Append(httpContext.Request.QueryString)
                    .ToString();
    }
    /// <summary>
    /// 获取路由的Area
    /// </summary>
    /// <param name="httpContent"></param>
    /// <returns></returns>
    public static string GetRouteArea(this HttpContext httpContent)
    {
       return httpContent.Request?.RouteValues["area"]?.ToString() ?? "";
    }

    /// <summary>
    /// 设置通过appsettings 可以下载文件类型 扩展
    /// 此扩展需要在  app.UseStaticFiles() 之前使用
    /// </summary>
    /// <param name="app"></param>
    public static void UseCustomStaticFile(this IApplicationBuilder app)
    {
        var mimes = App.GetConfig<Dictionary<string, string>>("StaticFileOptions:FileExtensionContentType");
        var serveUnknownFileTypes = App.GetConfig<bool>("StaticFileOptions:ServeUnknownFileTypes");
        var physicalFilePath = App.GetConfig<string>("StaticFileOptions:PhysicalFilePath");

        //var a = new PhysicalFileProvider(Directory.GetCurrentDirectory()); ;

        var options = new StaticFileOptions
        {
            //FileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory()),
            //设置不限制content-type 该设置可以下载所有类型的文件，但是不建议这么设置，因为不安全
            //ServeUnknownFileTypes = true
            //下面设置可以下载apk和nupkg类型的文件
            //ContentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>
            //{
            //  { ".apk","application/vnd.android.package-archive"},
            //  //{ ".nupkg","application/zip"}
            //})
            ServeUnknownFileTypes = serveUnknownFileTypes,
        };
        if (mimes != null && mimes.Any())
        {
            options.ContentTypeProvider = new FileExtensionContentTypeProvider(mimes);
        }
        if (!string.IsNullOrWhiteSpace(physicalFilePath) && Directory.Exists(physicalFilePath))
        {
            options.FileProvider = new PhysicalFileProvider(physicalFilePath);
        }
        app.UseStaticFiles(options);
        app.UseStaticFiles();//可重复设置，设置后不会异常
    }

    ///// <summary>
    ///// 获取访问信息
    ///// </summary>
    ///// <param name="httpContext"></param>
    ///// <returns></returns>
    //public static string GetWebVisitInfo(this HttpContext httpContext)
    //{
    //    if (httpContext == null) return "";
    //    var logcontent = new StringBuilder();
    //    logcontent.AppendLine($"  访问地址： {httpContext.Request.Path}{httpContext.Request.QueryString}");
    //    var ipv4 = httpContext.GetRemoteIpAddressToIPv4();
    //    var ipv6 = httpContext.GetRemoteIpAddressToIPv6();
    //    logcontent.AppendLine($"IPV4: {ipv4}    IPV6: {ipv6}");

    //    var headers = httpContext.Request.Headers.ToDictionary(x => x.Key, v => v.Value.ToString());
    //    logcontent.AppendLine($"Header：{headers.ToJson()}");

    //    //var area = context.RouteData.Values["area"]?.ToString() ?? "";
    //    //var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
    //    //var action = actionContext.RouteData.Values["action"]?.ToString() ?? "";
    //    // method ：OPTIONS、PUT、PATCH、DELETE、TRACE 和 CONNECT 方法
    //    try
    //    {
    //        if (httpContext.Request.Method.ToLower() != "get")
    //        {
    //            if (httpContext.Request.HasFormContentType)
    //            {
    //                var form = httpContext.Request.Form;
    //                logcontent.AppendLine($"Form 参数：{form.ToDictionary(x => x.Key, v => v.Value.ToString()).ToJson()}");
    //            }
    //            else
    //            {
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        var _logger = AppEx.GetSeriLogger("WebActionFilter");
    //        _logger.Error(ex.ToStringEx());
    //    }
    //    //URL  参数
    //    if (httpContext.Request.Query.Any())
    //    {
    //        var urlpars = httpContext.Request.Query.ToDictionary(x => x.Key, v => v.Value.ToString());
    //        logcontent.AppendLine($"URL 参数：{urlpars.ToJson()}");
    //    }

    //    return logcontent.ToString();
    //}

    ///// <summary>
    ///// 获取访问信息
    ///// </summary>
    ///// <param name="httpContext"></param>
    ///// <returns></returns>
    //public static void GetWebVisitInfo(this ActionExecutingContext context, LogVisitEntity logVisitEntity)
    //{
    //    if (context == null) return;
    //    if (logVisitEntity == null) logVisitEntity = new();

    //    context.HttpContext.GetWebVisitInfo(logVisitEntity);
    //    //logVisitEntity.Area = context.RouteData.Values["area"]?.ToString() ?? "";
    //    //logVisitEntity.Controller = context.RouteData.Values["controller"]?.ToString() ?? "";
    //    //logVisitEntity.Action = context.RouteData.Values["action"]?.ToString() ?? "";
    //}

    /// <summary>
    /// 获取webHttp访问信息
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static WebHttpInfo GetWebInfo(this HttpContext httpContext)
    {
        if (httpContext == null) return null;
        var webHttpInfo = new WebHttpInfo();

        webHttpInfo.RemoteIPv4 = httpContext.GetRemoteIpAddressToIPv4();
        webHttpInfo.LocalIPv4 = httpContext.GetLocalIpAddressToIPv4();
        webHttpInfo.RemoteIPv6 = httpContext.GetRemoteIpAddressToIPv6();
        webHttpInfo.LocalIPv6 = httpContext.GetLocalIpAddressToIPv6();
        //HttpRequest 对象
        var httpRequest = httpContext.Request;

        //请求的 Url 地址
        webHttpInfo.RequestUrl = Uri.UnescapeDataString(httpRequest.GetRequestUrlAddress());
        //请求方式
        webHttpInfo.HttpMethod = httpContext.Request.Method;

        webHttpInfo.AreaName = httpContext.Request.RouteValues["area"]?.ToString() ?? "";
        webHttpInfo.ControllerName = httpContext.Request.RouteValues["controller"]?.ToString() ?? "";
        webHttpInfo.ActionName = httpContext.Request.RouteValues["action"]?.ToString() ?? "";

        // 获取来源 Url 地址
        webHttpInfo.RefererUrl = Uri.UnescapeDataString(httpRequest.GetRefererUrlAddress());

        //var logcontent = $"访问地址：{httpContext.Request.Path}{httpContext.Request.QueryString}";
        //var ipv4 = httpContext.GetRemoteIpAddressToIPv4();
        //var ipv6 = httpContext.GetRemoteIpAddressToIPv6();
        //logcontent += $"IPV4:{ipv4}    IPV6:{ipv6}";

        //var headers = httpContext.Request.Headers.ToDictionary(x => x.Key, v => v.Value.ToString());
        //logcontent += $"Header：{System.Text.Json.JsonSerializer.Serialize(headers, AppEx.GetDefaultJsonSerializerOptions)}";
        webHttpInfo.Header = httpContext?.Request?.Headers?.ToDictionary(x => x.Key, v => v.Value.ToString())?.ToJson() ?? "";

        var par = "";

        // method ：OPTIONS、PUT、PATCH、DELETE、TRACE 和 CONNECT 方法
        try
        {
            if (httpContext.Request.Method.ToLower() != "get")
            {
                var form = httpContext.Request.Form;
                //Form 参数：
                webHttpInfo.Params = $"{form.ToDictionary(x => x.Key, v => v.Value).ToJson()}";
            }
        }
        catch (Exception ex)
        {
            AppEx.GetSeriLogger(logFloderName: "WebActionFilter").Error(ex.ToStringEx());
        }
        //URL  参数
        if (httpContext.Request.Query.Any())
        {
            var urlpars = httpContext.Request.Query.ToDictionary(x => x.Key, v => v.Value.ToString());
            //URL 参数：
            webHttpInfo.UrlParams = $"{urlpars.ToJson()}";
        }

        //浏览器信息
        //#if NET5
        //            var useragent = httpContext?.Request?.Headers["User-Agent"].To<string>();
        //#elif NET6 || NET7 || NET8
        //           var useragent = httpContext?.Request?.Headers?.UserAgent.ToString();
        //#endif

        // 客户端浏览器信息
        webHttpInfo.UserAgent = httpRequest.Headers["User-Agent"];

        //客户端内容类型
        webHttpInfo.ContentType = httpRequest.Headers["Content-Type"];

        if (!string.IsNullOrWhiteSpace(webHttpInfo.UserAgent))
        {
            // var s = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.131 Safari/537.36";
            // 获取具有嵌入正则表达式模式的解析器
            var uaParser = Parser.GetDefault();

            // get a parser using externally supplied yaml definitions
            // var uaParser = Parser.FromYaml(yamlString);
            //使用外部提供的yaml定义获取解析器
            UAParser.ClientInfo c = uaParser.Parse(webHttpInfo.UserAgent);
            webHttpInfo.Os = c.OS.ToString();
            webHttpInfo.Device = c.Device.ToString();
            webHttpInfo.Browser = c.UA.Family;
        }

        // 客户端请求区域语言
        webHttpInfo.AcceptLanguage = httpRequest.Headers["accept-language"];

        // 请求来源（swagger还是其他）
        webHttpInfo.RequestFrom = httpRequest.Headers["request-from"].ToString();
        webHttpInfo.RequestFrom = string.IsNullOrWhiteSpace(webHttpInfo.RequestFrom) ? "client" : webHttpInfo.RequestFrom;

        // 获取授权用户
        var user = httpContext.User;

        // 获取请求 cookies 信息
        webHttpInfo.RequestHeaderCookies = Uri.UnescapeDataString(httpRequest.Headers["cookie"].ToString());

        //// 判断是否是授权访问
        //var isAuth = actionMethod.GetFoundAttribute<AllowAnonymousAttribute>(true) == null
        //    && httpContext.User != null
        //    && httpContext.User.Identity.IsAuthenticated;

        // 获取响应头信息
        webHttpInfo.AccessToken = httpContext.Response.Headers["access-token"].ToString();
        webHttpInfo.Authorization = string.IsNullOrWhiteSpace(webHttpInfo.AccessToken)
            ? httpRequest.Headers["Authorization"].ToString()
            : "Bearer " + webHttpInfo.AccessToken;

        // 获取响应 cookies 信息
        webHttpInfo.ResponseHeaderCookies = Uri.UnescapeDataString(httpContext.Response.Headers["Set-Cookie"].ToString());

        // 获取系统信息
        webHttpInfo.osDescription = RuntimeInformation.OSDescription;
        webHttpInfo.osArchitecture = RuntimeInformation.OSArchitecture.ToString();
        webHttpInfo.frameworkDescription = RuntimeInformation.FrameworkDescription;
        var basicFrameworkDescription = typeof(App).Assembly.GetName();
        webHttpInfo.basicFramework = basicFrameworkDescription.Name;
        webHttpInfo.basicFrameworkVersion = basicFrameworkDescription.Version?.ToString();

        // 获取启动信息
        webHttpInfo.EntryAssemblyName = Assembly.GetEntryAssembly().GetName().Name;

        // 获取进程信息
        var process = Process.GetCurrentProcess();
        webHttpInfo.ProcessName = process.ProcessName;

        // 获取部署程序
        webHttpInfo.DeployServer = webHttpInfo.ProcessName == webHttpInfo.EntryAssemblyName ? "Kestrel" : webHttpInfo.ProcessName;

        // 服务器环境
        webHttpInfo.Environment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName;
        return webHttpInfo;
    }

    /// <summary>
    /// 获取webHttp访问信息
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="stringBuilder"></param>
    /// <returns></returns>
    public static StringBuilder GetWebInfoToString(this HttpContext httpContext, StringBuilder stringBuilder = null)
    {
        if (stringBuilder == null) stringBuilder = new StringBuilder();
        WebHttpInfo webHttpInfo = httpContext.GetWebInfo();
        if (webHttpInfo == null) return stringBuilder;
        var monitorItems = new List<string>()
        {
            $"##路由信息## [area]: {webHttpInfo.AreaName}; [controller]: {webHttpInfo.ControllerName}; [action]: {webHttpInfo.ActionName}"
            , $"##请求方式## {webHttpInfo.HttpMethod}"
            , $"##请求地址## {webHttpInfo.RequestUrl}"
            , $"##来源地址## {webHttpInfo.RefererUrl}"
            , $"##请求端源## {webHttpInfo.RequestFrom}"
            , $"##浏览器标识## {webHttpInfo.UserAgent}"
            , $"##客户端区域语言## {webHttpInfo.AcceptLanguage}"
            , $"##客户端 IP 地址## {webHttpInfo.RemoteIPv4}"
            , $"##服务端 IP 地址## {webHttpInfo.LocalIPv4}"
            //, $"##客户端连接 ID## {traceId}"
            //, $"##服务线程 ID## #{threadId}"
            //, $"##执行耗时## {timeOperation.ElapsedMilliseconds}ms"
            ,"━━━━━━━━━━━━━━━  Cookies ━━━━━━━━━━━━━━━"
            , $"##请求端## {webHttpInfo.RequestHeaderCookies}"
            , $"##响应端## {webHttpInfo.ResponseHeaderCookies}"
            ,"━━━━━━━━━━━━━━━  系统信息 ━━━━━━━━━━━━━━━"
            , $"##系统名称## {webHttpInfo.osDescription}"
            , $"##系统架构## {webHttpInfo.osArchitecture}"
            , $"##基础框架## {webHttpInfo.basicFramework} v{webHttpInfo.basicFrameworkVersion}"
            , $"##.NET 架构## {webHttpInfo.frameworkDescription}"
            ,"━━━━━━━━━━━━━━━  启动信息 ━━━━━━━━━━━━━━━"
            , $"##运行环境## {webHttpInfo.Environment}"
            , $"##启动程序集## {webHttpInfo.EntryAssemblyName}"
            , $"##进程名称## {webHttpInfo.ProcessName}"
            , $"##托管程序## {webHttpInfo.DeployServer}"
        };
        foreach (var item in monitorItems)
        {
            stringBuilder.AppendLine(item);
        }
        httpContext.GetJWTInfo(webHttpInfo, stringBuilder);
        return stringBuilder;
    }

    /// <summary>
    /// 生成 JWT授权日志信息
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="webHttpInfo"></param>
    /// <param name="stringBuilder"></param>
    /// <returns></returns>
    public static StringBuilder GetJWTInfo(this HttpContext httpContext, WebHttpInfo webHttpInfo, StringBuilder stringBuilder = null)
    {
        if (stringBuilder == null) stringBuilder = new StringBuilder("━━━━━━━━━━━━━━━  授权信息 ━━━━━━━━━━━━━━━");
        stringBuilder.AppendLine($"##JWT Token## {webHttpInfo.Authorization}");
        if (!httpContext.User.Claims.Any()) return stringBuilder;

        foreach (var claim in httpContext.User.Claims)
        {
            var valueType = claim.ValueType.Replace("http://www.w3.org/2001/XMLSchema#", "");
            var value = claim.Value;

            // 解析时间戳并转换
            if (!string.IsNullOrEmpty(value) && (claim.Type == "iat" || claim.Type == "nbf" || claim.Type == "exp"))
            {
                var succeed = long.TryParse(value, out var seconds);
                if (succeed)
                {
                    value = $"{value} ({DateTimeOffset.FromUnixTimeSeconds(seconds).ToLocalTime():yyyy-MM-dd HH:mm:ss:ffff(zzz) dddd} L)";
                }
            }
            stringBuilder.AppendLine($"##{claim.Type} ({valueType})## {value}");
        }
        return stringBuilder;
    }

    /// <summary>
    /// 获取IPV4
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static string GetIpV4(this HttpContext httpContext)
    {
        return httpContext.GetRemoteIpAddressToIPv4();
    }

    /// <summary>
    /// 获取IPV4
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <returns></returns>
    public static string GetIpV4(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext.GetRemoteIpAddressToIPv4();
    }

    /// <summary>
    /// 获取IPV6
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static string GetIpV6(this HttpContext httpContext)
    {
        return httpContext.GetRemoteIpAddressToIPv6();
    }

    /// <summary>
    /// 获取IPV6
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    /// <returns></returns>
    public static string GetIpV6(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext.GetRemoteIpAddressToIPv6();
    }

    /// <summary>
    /// 获取文件
    /// </summary>
    /// <param name="logicalPath">逻辑路径（假路径，不是真的，防止从url直接访问到文件）</param>
    /// <returns></returns>
    //[Route("/u/f/{name?}/{name2?}/{name3?}/{name4?}/{name5?}/{name6?}/{name7?}/{name8?}/{name9?}/{name10?}")]
    //[UploadFileFilter]
    public static IActionResult GetFile(this Controller controller, string defaultPhysicalPath, string logicalPath = "/u/f", string baseFloder = "upload")  //要下载文件码
    {
        var httpContext = App.HttpContext;

        ////图片防盗链 验证（基础认证，能防御基础的爬虫手段）
        ////var applicationUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}";
        //var headersDictionary = httpContext.Request.Headers;
        //var urlReferrer = headersDictionary[HeaderNames.Referer].ToString()??"";

        //var domain = httpContext.GetDomain();

        //if (!string.IsNullOrEmpty(urlReferrer) && !urlReferrer.StartsWith(domain))
        //{
        //   //返回默认图片，提示
        //}

        var pathurl = Path.Combine(httpContext.Request.Path, httpContext.Request.QueryString.Value ?? "");
        //默认物理文件夹
        //var defaultPhysicalPath = App.Configuration["AppInfo:WebUploadFloder"];
        if (string.IsNullOrWhiteSpace(defaultPhysicalPath) || !Directory.Exists(defaultPhysicalPath))
        {
            //如果不存在，设置默认的物理文件，网站更目录下
            defaultPhysicalPath = AppEx.GetUploadDefaultFloder();
        }
        var filepath = Path.Combine(defaultPhysicalPath, baseFloder, $"{pathurl.TrimStart(logicalPath)}");

        if (!System.IO.File.Exists(filepath)) return controller.Content("");
        var stream = System.IO.File.OpenRead(filepath);  //创建文件流
        return controller.File(stream, filepath.GetFileMimeType());
    }

    /// <summary>
    /// 获取文件
    /// </summary>
    /// <param name="relativePath">逻辑路径（假路径，不是真的，防止从url直接访问到文件）</param>
    /// <returns></returns>
    //[Route("/u/f/{name?}/{name2?}/{name3?}/{name4?}/{name5?}/{name6?}/{name7?}/{name8?}/{name9?}/{name10?}")]
    //[UploadFileFilter]
    public static IActionResult GetDbFile(this Controller controller, string defaultPhysicalPath, string relativePath)  //要下载文件码
    {
        //var httpContext = App.HttpContext;

        ////图片防盗链 验证（基础认证，能防御基础的爬虫手段）
        ////var applicationUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}";
        //var headersDictionary = httpContext.Request.Headers;
        //var urlReferrer = headersDictionary[HeaderNames.Referer].ToString()??"";

        //var domain = httpContext.GetDomain();

        //if (!string.IsNullOrEmpty(urlReferrer) && !urlReferrer.StartsWith(domain))
        //{
        //   //返回默认图片，提示
        //}

        //var pathurl = Path.Combine(httpContext.Request.Path, httpContext.Request.QueryString.Value ?? "");
        //默认物理文件夹
        //var defaultPhysicalPath = App.Configuration["AppInfo:WebUploadFloder"];
        if (string.IsNullOrWhiteSpace(defaultPhysicalPath) || !Directory.Exists(defaultPhysicalPath))
        {
            //如果不存在，设置默认的物理文件，网站更目录下
            defaultPhysicalPath = AppEx.GetUploadDefaultFloder();
        }
        var filepath = Path.Combine(defaultPhysicalPath, "upload", $"{relativePath.TrimStart('/').TrimStart('\\')}");

        if (!System.IO.File.Exists(filepath)) return controller.Content("");
        var stream = System.IO.File.OpenRead(filepath);  //创建文件流
        return controller.File(stream, filepath.GetFileMimeType());
    }

    /// <summary>
    /// 登陆跳转地址验证
    /// </summary>
    /// <param name="returnUrl"></param>
    /// <param name="defaultReturnUrl">如果验证失败，默认的跳转地址</param>
    /// <returns></returns>
    public static string LoginReturnUrlVer(this string returnUrl, string defaultReturnUrl)
    {
        var domain = App.HttpContext.GetDomain().TrimEnd("/").ToLower();
        //跳转地址为空
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            if (defaultReturnUrl.ToLower().StartsWith(domain))
            {
                return defaultReturnUrl;
            }
            else
            {
                return domain.ToLower() + "/" + defaultReturnUrl.TrimStart("/");
            }
        }
        else
        {
            //跳转地址不包含当前域名
            if (returnUrl.ToLower().StartsWith(domain))
            {
                return returnUrl;
            }
            else
            {
                //使用默认地址
                if (defaultReturnUrl.ToLower().StartsWith(domain))
                {
                    return defaultReturnUrl;
                }
                else
                {
                    return domain.ToLower() + "/" + defaultReturnUrl.TrimStart("/");
                }
            }
        }
    }

    /// <summary>
    /// 获取MVC登陆安全验证配置
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public static MvcLoginSafeVerItem GetMvcLoginSafeVerConfig(string code)
    {
        var mvcLoginSafeVerOptions = App.GetOptionsMonitor<MvcLoginSafeVerOptions>();
        var config = mvcLoginSafeVerOptions.configs.FirstOrDefault(o => o.Code.Trim().ToLower() == code.Trim().ToLower());
        if (config == null) config = new MvcLoginSafeVerItem();
        return config;
    }

    /// <summary>
    /// 获取登陆用户ID
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public static int GetLoginUserId(this HttpContext httpContext)
    {
        return httpContext.GetLoginUserId<int>();
    }

    /// <summary>
    /// 获取用户 Id
    /// </summary>
    public static T GetLoginUserId<T>(this ClaimsPrincipal user)
    {
        if (user == null || user?.Identity?.IsAuthenticated == false) return default;
        if (user?.Claims?.Any(o => o.Type == ClaimConst.Claim_UserId) ?? false)
        {
            var v = user?.FindFirst(ClaimConst.Claim_UserId)?.Value;
            return v.To<T>();
        }
        return default;
    }

    /// <summary>
    /// 获取用户 Id
    /// </summary>
    public static T GetLoginUserId<T>(this HttpContext httpContext)
    {
        return GetLoginUserId<T>(httpContext?.User ?? null);
    }

    /// <summary>
    /// 获取用户 UserNick
    /// </summary>
    public static string GetLoginUserNick(this ClaimsPrincipal user)
    {
        if (user == null || user.Identity.IsAuthenticated == false) return "";
        if (user.Claims.Any(o => o.Type == ClaimConst.Claim_Name))
        {
            var v = user?.FindFirst(ClaimConst.Claim_Name)?.Value;
            return v.To<string>();
        }
        return user.GetLoginUserAccount();
    }

    /// <summary>
    /// 获取用户 UserNick
    /// </summary>
    public static string GetLoginUserNick(this HttpContext httpContext)
    {
        return httpContext.User.GetLoginUserNick();
    }

    /// <summary>
    /// 获取用户 账户
    /// </summary>
    public static string GetLoginUserAccount(this ClaimsPrincipal user)
    {
        //return httpContext.User.Identity.Name;
        return user?.GetValue<string>(ClaimConst.Claim_Account) ?? "";
    }

    // <summary>
    /// 获取用户 账户
    /// </summary>
    public static string GetLoginUserAccount(this HttpContext httpContext)
    {
        //return httpContext.User.Identity.Name;
        return httpContext?.GetValue<string>(ClaimConst.Claim_Account) ?? "";
    }

    /// <summary>
    /// 用获取值
    /// </summary>
    /// <param name="user"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string GetValue(this ClaimsPrincipal user, string key)
    {
        return user?.GetValue<string>(key) ?? "";
    }

    /// <summary>
    /// 用获取值
    /// </summary>
    /// <param name="httpContext"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string GetValue(this HttpContext httpContext, string key)
    {
        return httpContext.GetValue<string>(key) ?? "";
    }

    /// <summary>
    ///  用获取值
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="user"></param>
    /// <param name="key"></param>
    /// <returns>返回 TEntity</returns>
    public static TEntity? GetValue<TEntity>(this ClaimsPrincipal user, string key)
    {
        if (user == null) return default(TEntity);
        if (user.FindFirst(key) == null) return default(TEntity);
        var claim = user.FindFirst(key);
        if (claim == null || string.IsNullOrEmpty(claim.Value)) return default(TEntity);
        return claim.Value.To<TEntity>();
    }

    /// <summary>
    ///  用获取值
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="httpContext"></param>
    /// <param name="key"></param>
    /// <returns>返回 TEntity</returns>
    public static TEntity? GetValue<TEntity>(this HttpContext httpContext, string key)
    {
        return GetValue<TEntity>(httpContext.User, key);
    }
}