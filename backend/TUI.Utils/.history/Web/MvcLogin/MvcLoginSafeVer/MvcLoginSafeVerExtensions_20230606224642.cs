// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// MVC登陆扩展
/// </summary>
public static class MvcLoginSafeVerExtensions
{
    /// <summary>
    /// 获取MVC登陆安全验证配置
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public static MvcLoginSafeVerItem GetMvcLoginSafeVerConfig(string code)
    {
        var mvcLoginSafeVerOptions = App.GetOptionsMonitor<MvcLoginSafeVerOptions>();
        if (mvcLoginSafeVerOptions == null) mvcLoginSafeVerOptions = new MvcLoginSafeVerOptions();
        var config = mvcLoginSafeVerOptions.configs.FirstOrDefault(o => o.Code.Trim().ToLower() == code.Trim().ToLower());
        if (config == null) config = new MvcLoginSafeVerItem();
        return config;
    }

    #region 登陆暴力破解

    /// <summary>
    /// 获取MVC登陆缓存Key
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    private static string GetMvcLoginAccountCacheKey(string account, string code) => Caches.GetCacheKey($"MVC_{code}_LoginFail_Account_{account}");

    /// <summary>
    /// 获取MVC登陆缓存Key
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    private static string GetMvcLoginIPCacheKey(string code)
    {
        var Ip = App.HttpContext.GetIpV4();
        if (string.IsNullOrWhiteSpace(Ip)) Ip = App.HttpContext.GetIpV4();

        return $"MVC_{code}_LoginFail_IP_{Ip}";
    }

    /// <summary>
    /// 是否允许登陆
    /// </summary>
    /// <param name="account"></param>
    /// <param name="code">缓存code</param>
    /// <param name="errorMsg"></param>
    /// <returns></returns>
    public static bool IsAllowLogin(this string account, string code, out string errorMsg)
    {
        var isallowLoginByIp = account.IsAllowLoginByIP(code, out errorMsg);
        if (isallowLoginByIp == false) return isallowLoginByIp;

        //验证账号是否被限制登录
        var state = account.IsAllowLoginByAccount(code, out errorMsg);
        if (state == false)
        {
            account.LoginFail(code);
        }
        return state;
    }

    /// <summary>
    /// 是否允许登录-账号
    /// </summary>
    /// <param name="account"></param>
    /// <param name="code"></param>
    /// <param name="errorMsg"></param>
    /// <returns></returns>
    public static bool IsAllowLoginByAccount(this string account, string code, out string errorMsg)
    {
        errorMsg = "";
        var accountCacheKey = GetMvcLoginAccountCacheKey(account, code);
        var logContent = $"账号登陆账号：{account} \r\n登陆信息：{App.HttpContext.GetWebInfoToString()?.ToString()}";

        //var _logger = AppEx.GetSeriLogger($"登陆安全判断_{code}");
        var _logger = AppEx.GetSeriLogger($"登录验证\\{code}");

        var state = IsAllowLogin(account, code, accountCacheKey, "Account", logContent, out errorMsg);
        if (state)
        {
            logContent += $"\r\n==================\r\n 账号允许登陆";
        }
        _logger.Debug(logContent);
        return state;//允许登陆
    }

    /// <summary>
    /// 是否允许登录-IP
    /// </summary>
    /// <param name="account"></param>
    /// <param name="code"></param>
    /// <param name="errorMsg"></param>
    /// <returns></returns>
    public static bool IsAllowLoginByIP(this string account, string code, out string errorMsg)
    {
        errorMsg = "";
        var ipCacheKey = GetMvcLoginIPCacheKey(code); ;
        var logContent = $"IP登陆账号：{account} \r\n登陆信息：{App.HttpContext.GetWebInfoToString()?.ToString()}";

        //var _logger = AppEx.GetSeriLogger($"登陆安全判断_{code}");
        var _logger = AppEx.GetSeriLogger($"登录验证\\{code}");
        var state = IsAllowLogin(account, code, ipCacheKey, "IP", logContent, out errorMsg);

        if (state)
        {
            logContent += $"\r\n==================\r\n IP允许登陆";
        }
        _logger.Debug(logContent);
        return state;//允许登陆
    }

    /// <summary>
    /// 是否允许登录-账号
    /// </summary>
    /// <param name="account"></param>
    /// <param name="code"></param>
    /// <param name="errorMsg"></param>
    /// <returns></returns>
    private static bool IsAllowLogin(string account, string code, string cacheKey, string verType, string logContent, out string errorMsg)
    {
        errorMsg = "";
        logContent = $"账号登陆账号：{account} \r\n登陆信息：{App.HttpContext.GetWebInfoToString()?.ToString()}";

        //获取MVC登陆安全验证配置
        var config = GetMvcLoginSafeVerConfig(code);

        #region 验证账号是否被限制

        {
            var loginFailCaches = Caches.Get<ConcurrentDictionary<string, LoginFailDto>>(cacheKey);
            if (loginFailCaches != null && loginFailCaches.ContainsKey(cacheKey))
            {
                //验证禁止时间是否过期
                var loginfailinfo = loginFailCaches[cacheKey];

                //验证登陆失败次数是否超过设定的次数
                if (loginfailinfo.FailCount >= config.AccountMaxFailCount && loginfailinfo.ExpireTime != null)
                {
                    //return false;//超过设定的最大次数，不允许登陆
                    //验证禁止登陆过期时间

                    if (loginfailinfo.ExpireTime > DateTimeOffset.Now)
                    {
                        //禁止登陆时间未过期，不允许登陆

                        var ts = loginfailinfo.ExpireTime.Value - DateTimeOffset.Now;
                        if (verType == "Account")
                        {
                            errorMsg = $"登陆失败次数过多 ，请 {(int)ts.TotalMinutes} 分钟后再试！";
                        }
                        else if (verType == "IP")
                        {
                            errorMsg = $"该IP尝试登陆次数过多 ，请 {(int)ts.TotalMinutes} 分钟后再试！";
                        }

                        logContent += errorMsg;

                        return false;
                    }
                    else
                    {
                        //禁止登陆时间已到，移除
                        loginFailCaches.TryRemove(cacheKey, out _);
                        //更新缓存
                        Caches.Set(cacheKey, loginFailCaches, new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions()
                        {
                            SlidingExpiration = new TimeSpan(0, 2, 0, 0)
                        });
                    }
                }
            }

            return true;//允许登陆
        }

        #endregion 验证账号是否被限制
    }

    /// <summary>
    /// 登陆失败
    /// </summary>
    /// <param name="account">账号</param>
    /// <param name="code">缓存code</param>
    public static void LoginFail(this string account, string code)
    {
        account.LoginFailByAccount(code);
        account.LoginFailByIP(code);
    }

    /// <summary>
    /// 登录失败-账号
    /// </summary>
    /// <param name="account"></param>
    /// <param name="code"></param>
    public static void LoginFailByAccount(this string account, string code)
    {
        var accountCacheKey = GetMvcLoginAccountCacheKey(account, code);

        var logContent = $"登陆账号：{account} \r\n登陆信息：{App.HttpContext.GetWebInfoToString()?.ToString()}";

        //获取MVC登陆安全验证配置
        var config = GetMvcLoginSafeVerConfig(code);

        LoginFail(account, code, accountCacheKey, config.AccountMaxFailCount, config.AccountLimitLoginMinute, logContent);

        var _logger = AppEx.GetSeriLogger($"登录验证\\{code}");
        _logger.Debug(logContent);
    }

    /// <summary>
    /// 登录失败-IP
    /// </summary>
    /// <param name="account"></param>
    /// <param name="code"></param>
    public static void LoginFailByIP(this string account, string code)
    {
        var ipCacheKey = GetMvcLoginIPCacheKey(code);

        var logContent = $"登陆账号：{account} \r\n登陆信息：{App.HttpContext.GetWebInfoToString()?.ToString()}";

        //获取MVC登陆安全验证配置
        var config = GetMvcLoginSafeVerConfig(code);

        LoginFail(account, code, ipCacheKey, config.IpMaxFailCount, config.IpLimitLoginMinute, logContent);

        var _logger = AppEx.GetSeriLogger($"登录验证\\{code}");
        _logger.Debug(logContent);
    }

    private static void LoginFail(string account, string code, string cacheKey, int maxFailCount, int limitLoginMinute, string logContent)
    {
        logContent = $"登陆账号：{account} \r\n登陆信息：{App.HttpContext.GetWebInfoToString()?.ToString()}";

        #region 处理账号

        var loginCaches = Caches.Get<ConcurrentDictionary<string, LoginFailDto>>(cacheKey);
        if (loginCaches == null) loginCaches = new ConcurrentDictionary<string, LoginFailDto>();
        if (loginCaches.ContainsKey(cacheKey))
        {
            //存在账号，验证
            var loginFailInfo = loginCaches[cacheKey];
            loginFailInfo.FailCount++;
            //var surplusFailCount = maxFailCount - loginFailInfo.FailCount + 1;//登陆失败次数

            //验证登陆失败次数是否超过设定的次数
            if (loginFailInfo.FailCount >= maxFailCount)
            {
                loginFailInfo.ExpireTime = DateTimeOffset.Now.AddMinutes(limitLoginMinute);
            }
        }
        else
        {
            //不存在账号
            loginCaches.TryAdd(cacheKey, new LoginFailDto(account));

            logContent += "\r\n=====================\r\n不存在，新增";
        }
        //更新缓存
        Caches.Set(cacheKey, loginCaches, new Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions()
        {
            SlidingExpiration = new TimeSpan(0, 2, 0, 0)
        });

        #endregion 处理账号

        //var _logger = AppEx.GetSeriLogger($"登录验证\\{code}");
        //_logger.Debug(logContent);
    }

    /// <summary>
    /// 登陆成功
    /// </summary>
    /// <param name="account">账号</param>
    /// <param name="code">缓存code</param>
    public static void LoginSuccess(this string account, string code)
    {
        var accountCacheKey = GetMvcLoginAccountCacheKey(account, code);
        //var ipCacheKey = GetMvcLoginIPCacheKey(code);
        var loginCaches = Caches.Get<ConcurrentDictionary<string, LoginFailDto>>(accountCacheKey);
        if (loginCaches == null || loginCaches.Count <= 0) return;
        loginCaches.TryRemove(account, out _);

        var _logger = AppEx.GetSeriLogger($"登陆失败日志_{code}");
        _logger.Debug($"登陆成功，移除登陆失败限制缓存\r\n登陆信息：{App.HttpContext.GetWebInfoToString()?.ToString()}");
    }

    #endregion 登陆暴力破解
}