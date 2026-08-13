// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

public static class MsLogEx
{
    #region Furion Log

    /// <summary>
    /// Microsoft.Extensions.Logging.ILogger 静态写日志快捷方式，与Furion提供Log.xxx 有区别，做了一些个性化设置
    /// </summary>
    public static Microsoft.Extensions.Logging.ILogger Log => GetLogger();

    /// <summary>
    /// 日志对象集合
    /// </summary>
    private static readonly ConcurrentDictionary<string, LogCacheItem<Microsoft.Extensions.Logging.ILogger>> Loggers = new();

    /// <summary>
    ///
    /// </summary>
    /// <param name="floderName">文件夹名称，例如：abc/file</param>
    /// <param name="categoryName">分类</param>
    /// <param name="maxRollingFiles"> 如果指定了该值，那么超出该值将从最初日志文件中从头写入覆盖，默认按一天24个文件，一个小时一个，保存30天</param>
    /// <param name="fileSizeLimitBytes"> 如果指定了该值，那么日志文件大小超出了该配置就会创建的日志文件，新创建的日志文件命名规则：文件名+[递增序号].log，默认100M一个文件</param>
    /// <param name="useUtcTimestamp">是否使用 UTC 时间戳，默认 false</param>
    /// <param name="minimumLevel">最低日志记录级别，默认 Trace</param>
    /// <returns></returns>
    public static Microsoft.Extensions.Logging.ILogger GetLogger(string floderName = "all"
        , string categoryName = "default"
        , int maxRollingFiles = 24 * 30
        , long fileSizeLimitBytes = 100 * 1024 * 1024
        , bool useUtcTimestamp = false
        , LogLevel minimumLevel = LogLevel.Trace)
    {
        //判断缓存的 ILogger 是否已存在，如果改以存在从字典集合直接取出使用，不在重新创建
        LogCacheItem<Microsoft.Extensions.Logging.ILogger> logCacheItem;
        var state = Loggers.TryGetValue(floderName, out logCacheItem);
        var IsGetLogger = false;//是否从缓存中取出Logger对象
        if (state == true && logCacheItem != null)
        {
            if (logCacheItem.Date == $"{DateTimeOffset.Now:yyyy-MM-dd}")
            {
                IsGetLogger = true;
            }
            else
            {
                //移除旧的Logger对象
                var removestate = Loggers.TryRemove(floderName, out var oldlogger);
            }
        }
        if (IsGetLogger == false)
        {
            var loggerFactory = App.GetRequiredService<ILoggerFactory>(App.RootServices);
            //Array.ForEach(new[] { LogLevel.Information, LogLevel.Warning, LogLevel.Error }, logLevel =>
            //Array.ForEach(Enum.GetValues(typeof(LogLevel)), logLevel =>
            foreach (LogLevel logLevel in Enum.GetValues(typeof(LogLevel)))
            {
                loggerFactory.AddFile("logs/{2}/{1}-{0:yyyy}-{0:MM}-{0:dd}-{0:HH}.log", options =>
                {
                    options.FileNameRule = fileName => string.Format(fileName, DateTime.UtcNow, logLevel.ToString(), floderName.Replace("\\", "/").Trim('/'));
                    options.WriteFilter = logMsg => logMsg.LogLevel == logLevel;
                    options.MaxRollingFiles = maxRollingFiles;
                    options.FileSizeLimitBytes = fileSizeLimitBytes;
                    options.UseUtcTimestamp = useUtcTimestamp;//是否使用 UTC 时间戳，默认 false
                    options.MinimumLevel = minimumLevel;
                });
            };

            logCacheItem = new LogCacheItem<Microsoft.Extensions.Logging.ILogger>();
            logCacheItem.Logger = loggerFactory.CreateLogger(categoryName);

            Loggers.TryAdd(floderName, logCacheItem);
        }
        else
        {
            // return serilogCacheItem.Logger;
            if (logCacheItem != null)
            {
                return logCacheItem.Logger;
            }
        }
        return Furion.Logging.Log.CreateLogger<Microsoft.Extensions.Logging.ILogger>(); ;
    }

    #endregion Furion Log
}