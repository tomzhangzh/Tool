// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using TUI.Core.Utils.Web.Model;

namespace TUI.Utils;

/// <summary>
/// Cookie 帮助类
/// </summary>
public static class AppEx
{
    #region 日志

    /// <summary>
    /// 获取  Microsoft.Extensions.Logging.ILogger
    /// </summary>
    /// <param name="floderName">文件夹名称，例如：TUI/file</param>
    /// <param name="categoryName">分类</param>
    /// <param name="maxRollingFiles"> 如果指定了该值，那么超出该值将从最初日志文件中从头写入覆盖，默认按一天24个文件，一个小时一个，保存30天</param>
    /// <param name="fileSizeLimitBytes"> 如果指定了该值，那么日志文件大小超出了该配置就会创建的日志文件，新创建的日志文件命名规则：文件名+[递增序号].log，默认100M一个文件</param>
    /// <param name="useUtcTimestamp">是否使用 UTC 时间戳，默认 false</param>
    /// <param name="minimumLevel">最低日志记录级别，默认 Trace</param>
    /// <returns></returns>
    public static Microsoft.Extensions.Logging.ILogger GetMLogger(string floderName = "all"
        , string categoryName = "default"
        , int maxRollingFiles = 24 * 30
        , long fileSizeLimitBytes = 100 * 1024 * 1024
        , bool useUtcTimestamp = false
        , LogLevel minimumLevel = LogLevel.Trace)
    {
        return MsLogEx.GetLogger(floderName, categoryName, maxRollingFiles, fileSizeLimitBytes, useUtcTimestamp, minimumLevel);
    }

    /// <summary>
    /// 获取 Serilog Logger
    /// </summary>
    /// <param name="logFloderName">日志文件夹名称</param>
    /// <param name="isLogEventLevelFloder">是否每个事件一个文件夹</param>
    /// <param name="rollOnFileSizeLimit">如果为true，则在达到文件大小限制时将创建一个新文件。文件名将以_NNN格式附加一个数字，第一个文件名没有数字。</param>
    /// <param name="minLogEventLevel">最低日志等级，默认Debug</param>
    /// <param name="rollingInterval">滚动间隔：日志将滚动到新文件的间隔</param>
    /// <param name="retainedFileCountLimit">保留的最大日志文件数，包括当前</param>
    /// <param name="flushToDiskInterval">TimeSpan 不为null，将定期在指定的时间执行完整磁盘刷新间隔。</param>
    /// <param name="buffered">指示是否可以缓存到输出文件。默认值 false</param>
    /// <param name="shared">允许多个进程共享日志文件。默认值为false</param>
    /// <param name="fileSizeLimitBytes">文件大小限制字节：允许日志文件访问的近似最大大小（以字节为单位）成长。对于不受限制的增长，传递null。默认值为1 GB。避免写作部分事件，限制内的最后一个事件将全部写入偶数如果超过限制</param>
    /// <param name="outputTemplate">日志输出模板</param>
    /// <param name="filename">文件名</param>
    /// <returns></returns>
    public static Serilog.ILogger GetSeriLogger(
          string logFloderName = "default",//日志文件夹名称
          bool isLogEventLevelFloder = true,//是否按日志类别分类记录
                                            //ITextFormatter formatter,
          bool rollOnFileSizeLimit = true,//如果为true，则在达到文件大小限制时将创建一个新文件。文件名将以_NNN格式附加一个数字，第一个文件名没有数字。
                                          // Encoding encoding = null,
                                          // // FileLifecycleHooks hooks = null,
                                          //
                                          //
          Serilog.Events.LogEventLevel minLogEventLevel = Serilog.Events.LogEventLevel.Debug,//最低日志等级，默认Debug
          Serilog.RollingInterval rollingInterval = Serilog.RollingInterval.Hour,//滚动间隔：日志将滚动到新文件的间隔
          int? retainedFileCountLimit = null,//保留的最大日志文件数，包括当前
          TimeSpan? flushToDiskInterval = null,//TimeSpan 不为null，将定期在指定的时间执行完整磁盘刷新间隔。
          bool buffered = false,//指示是否可以缓存到输出文件。默认值 false
          bool shared = false,//允许多个进程共享日志文件。默认值为false
          long? fileSizeLimitBytes = 1073741824L,//文件大小限制字节：允许日志文件访问的近似最大大小（以字节为单位）成长。对于不受限制的增长，传递null。默认值为1GB。避免写作部分事件，限制内的最后一个事件将全部写入偶数如果超过限制
          string outputTemplate = "",
          string filename = ""
        )
    {
        return SerilogEx.GetLogger(logFloderName, isLogEventLevelFloder, rollOnFileSizeLimit, minLogEventLevel, rollingInterval, retainedFileCountLimit, flushToDiskInterval, buffered, shared, fileSizeLimitBytes, outputTemplate, filename);
    }

    #endregion 日志

    #region 访问计数，访问防重复执行

    #region 执行中

    /// <summary>
    /// 执行中的
    /// </summary>
    public static ConcurrentDictionary<string, object> Executing = new();

    /// <summary>
    /// 新增执行中
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool AddExecuting(string key, object value = null)
    {
        value ??= 1;
        var state = Executing.TryAdd(key, value);
        return state;
    }

    /// <summary>
    /// 新增或更新执行中
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void AddOrUpdateExecuting(string key, object value)
    {
        object? state = Executing.AddOrUpdate(key, value, (key, oldValue) => value);
    }

    /// <summary>
    /// 删除执行中的
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Tuple<bool, object> ClearExecuting(string key)
    {
        var state = Executing.TryRemove(key, out var value);
        return Tuple.Create(state, value);
    }

    /// <summary>
    /// 获取执行中的
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static Tuple<bool, object> GEtExecuting(string key)
    {
        var state = Executing.TryGetValue(key, out var value);
        return Tuple.Create(state, value);
    }

    #endregion 执行中

    #region 旧方法【超过限制次数，可继续执行】

    /// <summary>
    /// 获取访问次数
    /// 每次调用方法，对应key的值都会加 1，创建的新值默认为1
    /// </summary>
    /// <param name="key">关键词</param>
    /// <param name="maxcount">最大计数，超过此计数会清0</param>
    /// <returns></returns>
    public static int AllowMultithreadingExec(string key, int maxcount = -1)
    {
        var value = ParallelExecutions.AddOrUpdate(key, 1, (key, oldvalue) => oldvalue + 1);

        if (maxcount > 0 && value > maxcount)
        {
            value = ParallelExecutions.AddOrUpdate(key, 1, (key, oldvalue) => 0);
            return value;
        }
        else
        {
            return value;
        }
    }

    /// <summary>
    /// 清理访问次数，清理后的访问次数为0
    /// </summary>
    /// <param name="key"></param>
    public static void AllowMultithreadingExecClear(string key)
    {
        var value = ParallelExecutions.AddOrUpdate(key, 0, (key, oldvalue) => 0);
    }

    #endregion 旧方法【超过限制次数，可继续执行】

    /// <summary>
    /// 访问计数，防止锁死
    /// </summary>
    private static readonly ConcurrentDictionary<string, int> ParallelExecutions = new();

    #region 新方式【不能超过并行执行的最大值】

    /// <summary>
    /// 新增并行任务
    /// </summary>
    /// <param name="key">关键词</param>
    /// <param name="maxParallelCount">最大并行数，默认为0，不限制</param>
    /// <returns><是否添加并行成功,并行数></returns>
    public static Tuple<bool, int> PeAdd(string key, int maxParallelCount = 0)
    {
        var value = ParallelExecutions.AddOrUpdate(key, 1, (key, oldvalue) => oldvalue + 1);
        if (maxParallelCount > 0 && value > maxParallelCount)
        {
            value = ParallelExecutions.AddOrUpdate(key, 1, (key, oldvalue) => (oldvalue > 0 ? oldvalue - 1 : 0));
        }
        return Tuple.Create(true, value);
    }

    /// <summary>
    /// 清理并行任务
    /// </summary>
    /// <param name="key">关键词</param>
    public static void PeClear(string key)
    {
        var value = ParallelExecutions.AddOrUpdate(key, 0, (key, oldvalue) => 0);
    }

    /// <summary>
    /// 从集合移除并行任务
    /// </summary>
    /// <param name="key">关键词</param>
    /// <returns></returns>
    public static Tuple<bool, int> PeRemove(string key)
    {
        var state = ParallelExecutions.TryRemove(key, out var value);
        return Tuple.Create(state, value);
    }

    /// <summary>
    /// 并行任务完成,并行数减1
    /// </summary>
    /// <param name="key">关键词</param>
    /// <returns>剩余并行数</returns>
    public static int PeEnd(string key)
    {
        //新增或减1
        var value = ParallelExecutions.AddOrUpdate(key, 0, (key, oldvalue) => (oldvalue > 0 ? oldvalue - 1 : 0));
        return value;
    }

    /// <summary>
    /// 获取PE的值
    /// </summary>
    /// <param name="key">关键词</param>
    /// <returns></returns>
    public static int PeGetValue(string key)
    {
        var state = ParallelExecutions.TryGetValue(key, out var value);
        return state ? value : 0;
    }

    #endregion 新方式【不能超过并行执行的最大值】

    #endregion 访问计数，访问防重复执行

    /// <summary>
    /// 获取上传文件的文件夹
    /// </summary>
    /// <returns></returns>
    public static string GetUploadDefaultFloder()
    {
        //默认物理文件夹
        var defaultPhysicalPath = App.Configuration["AppInfo:WebUploadFloder"];
        if (string.IsNullOrWhiteSpace(defaultPhysicalPath) || !Directory.Exists(defaultPhysicalPath))
        {
            //如果不存在，设置默认的物理文件，网站更目录下
            if (App.WebHostEnvironment != null)
            {
                defaultPhysicalPath = App.WebHostEnvironment.ContentRootPath;
            }
            else if (App.HostEnvironment != null)
            {
                defaultPhysicalPath = App.HostEnvironment.ContentRootPath;
            }
            else
            {
                throw Oops.Bah("WebHostEnvironment 和 HostEnvironment 都为空了");
            }
        }

        return defaultPhysicalPath;
    }

    /// <summary>
    /// 获取上传文件完整路径
    /// </summary>
    /// <param name="fileRelativePath">文件相对路径（例如：/u/f/20220101/xxxx.png）</param>
    /// <param name="logicalPath">URL访问的逻辑路径 域名/{逻辑路径}/文件相对路径 （默认:/u/f）</param>
    /// <returns></returns>
    public static string GetUploadFileFullPath(string fileRelativePath, string logicalPath = "/u/f")
    {
        var floder = GetUploadDefaultFloder();
        //拼接完整文件路径

        //获取魔法路径
        logicalPath = GetWebUploadLogicalPath(logicalPath);
        var filepath = "";
        if (fileRelativePath.StartsWith(logicalPath))
        {
            filepath = Path.Combine(floder, "upload", $"{fileRelativePath.TrimStart(logicalPath).Replace("/", "\\").TrimStart("\\")}");
        }
        else
        {
            filepath = Path.Combine(floder, $"{fileRelativePath.Replace("/", "\\").TrimStart("\\")}");
        }

        if (!System.IO.File.Exists(filepath)) return null;

        return filepath;
    }

    /// <summary>
    /// 获取文件上传魔法路径
    /// 例如 ：/u/f/TUI/20220101/xxx.png
    /// 魔法路径：/u/f
    /// </summary>
    /// <param name="logicalPath">魔法路径，默认为:/u/f</param>
    /// <returns></returns>
    public static string GetWebUploadLogicalPath(string logicalPath = "/u/f")
    {
        var logicalPathTemp = App.Configuration["AppInfo:WebUploadLogicalPath"];
        if (!string.IsNullOrWhiteSpace(logicalPathTemp))
        {
            logicalPath = logicalPathTemp;
        }
        return logicalPath;
    }

    /// <summary>
    /// 获取配置(默认)
    /// </summary>
    /// <typeparam name="TOptions">强类型选项类</typeparam>
    /// <param name="loadPostConfigure"></param>
    /// <returns>TOptions</returns>
    public static TOptions GetConfig<TOptions>(bool loadPostConfigure = false)
    {
        var path = typeof(TOptions).Name;
        if (path.EndsWith("Options"))
        {
            path = path.ReplaceLast("Options", "");
        }
        return App.GetConfig<TOptions>(path);
    }

    /// <summary>
    /// 获取访问信息
    /// </summary>
    /// <returns></returns>
    public static WebHttpInfo GetWebInfo()
    {
        return App.HttpContext?.GetWebInfo();
    }

    /// <summary>
    /// 获取访问信息
    /// </summary>
    /// <returns></returns>
    public static StringBuilder GetWebInfoToString(StringBuilder stringBuilder = null)
    {
        return App.HttpContext.GetWebInfoToString(stringBuilder);
    }

    #region 登陆信息

    /// <summary>
    /// 用户id
    /// </summary>
    public static int UserIdByInt => App.User.GetLoginUserId<int>();

    /// <summary>
    /// 用户id
    /// </summary>
    public static long UserId => App.User.GetLoginUserId<long>();

    /// <summary>
    /// 账号
    /// </summary>
    public static string? Account => App.User.GetLoginUserAccount();

    /// <summary>
    /// 昵称
    /// </summary>
    public static string? Name => App.User.GetLoginUserNick();

    /// <summary>
    /// 获取租户 Id
    /// </summary>
    public static string? TenantIdStr => App.User.GetValue<string>(ClaimConst.Claim_TenantId);

    /// <summary>
    /// 获取租户 Id
    /// </summary>
    public static long TenantId => App.User.GetValue<long>(ClaimConst.Claim_TenantId);

    /// <summary>
    /// 获取租户 Id
    /// </summary>
    public static T GetTenantId<T>() => App.User.GetValue<T>(ClaimConst.Claim_TenantId);

    #endregion 登陆信息
}