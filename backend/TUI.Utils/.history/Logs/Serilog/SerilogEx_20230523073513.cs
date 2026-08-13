// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace Abc.Utils;

public static class SerilogEx
{
    #region Serilog  日志

    public static readonly string SerilogOutputTemplate = "{NewLine}Date：{Timestamp:yyyy-MM-dd HH:mm:ss.fff} LogLevel：{Level}{NewLine}Message：{Message}{Properties:j}{NewLine}{NewLine}{Exception}" + new string('-', 50);

    /// <summary>
    /// 日志对象集合
    /// </summary>
    private static readonly ConcurrentDictionary<string, LogCacheItem<Serilog.ILogger>> Loggers = new();

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
    public static Serilog.ILogger GetLogger(
                                    // string path,
                                    // LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,

                                    // LoggingLevelSwitch levelSwitch = null,

                                    string logFloderName = "default",//日志文件夹名称
                                    bool isLogEventLevelFloder = true,//是否按日志类别分类记录
                                                                      //ITextFormatter formatter,
                                    bool rollOnFileSizeLimit = true,//如果为true，则在达到文件大小限制时将创建一个新文件。文件名将以_NNN格式附加一个数字，第一个文件名没有数字。
                                                                    // Encoding encoding = null,
                                                                    // // FileLifecycleHooks hooks = null,
                                                                    //
                                                                    //
                                    LogEventLevel minLogEventLevel = LogEventLevel.Debug,//最低日志等级，默认Debug
                                    RollingInterval rollingInterval = RollingInterval.Hour,//滚动间隔：日志将滚动到新文件的间隔
                                    int? retainedFileCountLimit = null,//保留的最大日志文件数，包括当前
                                    TimeSpan? flushToDiskInterval = null,//TimeSpan 不为null，将定期在指定的时间执行完整磁盘刷新间隔。
                                    bool buffered = false,//指示是否可以缓存到输出文件。默认值 false
                                    bool shared = false,//允许多个进程共享日志文件。默认值为false
                                    long? fileSizeLimitBytes = 1073741824L,//文件大小限制字节：允许日志文件访问的近似最大大小（以字节为单位）成长。对于不受限制的增长，传递null。默认值为1 GB。避免写作部分事件，限制内的最后一个事件将全部写入偶数如果超过限制
                                    string outputTemplate = "",
                                    string filename = "")
    {
        //判断日志文件夹设置是否为空，如果为空，设置一个默认文件夹
        if (string.IsNullOrWhiteSpace(logFloderName))
        {
            logFloderName = "default";
        }
        else
        {
            logFloderName = logFloderName.Trim("\\");
        }
        //对自定义文件名进行处理，去除末尾的 反斜杠字符
        if (!string.IsNullOrWhiteSpace(filename))
        {
            filename = filename.Trim("\\");
        }
        //判断缓存的 ILogger 是否已存在，如果改以存在从字典集合直接取出使用，不在重新创建
        LogCacheItem<Serilog.ILogger> serilogCacheItem = null;
        var IsGetLogger = false;//是否从缓存中取出Logger对象
                                //if (Loggers.ContainsKey(logFloderName))
                                //{
        var state = Loggers.TryGetValue(logFloderName, out serilogCacheItem);
        if (state == true)
        {
            if (serilogCacheItem.Date == $"{DateTimeOffset.Now:yyyy-MM-dd}")
            {
                IsGetLogger = true;
            }
            else
            {
                //移除旧的Logger对象
                var removestate = Loggers.TryRemove(logFloderName, out var oldlogger);
            }
        }
        //}
        if (IsGetLogger == false)
        {
            outputTemplate = SerilogOutputTemplate;//日志输出模板
            if (!string.IsNullOrWhiteSpace(App.Configuration["Serilog:Customer:OutputTemplate"]))
            {
                outputTemplate = App.Configuration["Serilog:Customer:OutputTemplate"];
            }

            try
            {
                // if (string.IsNullOrWhiteSpace(outputTemplate)) outputTemplate = LogTemplate;
                //从配置文件取出配置数据
                //最低日志等级，默认Debug
                minLogEventLevel = App.Configuration["Serilog:Customer:MinLogEventLevel"]?.To<Serilog.Events.LogEventLevel>() ?? Serilog.Events.LogEventLevel.Debug;
                //滚动间隔：日志将滚动到新文件的间隔
                rollingInterval = App.Configuration["Serilog:Customer:RollingInterval"]?.To<Serilog.RollingInterval>() ?? Serilog.RollingInterval.Hour;
                //如果为true，则在达到文件大小限制时将创建一个新文件。文件名将以_NNN格式附加一个数字，第一个文件名没有数字。
                rollOnFileSizeLimit = App.Configuration["Serilog:Customer:RollOnFileSizeLimit"]?.To<bool>() ?? true;
                //指示是否可以缓存到输出文件。默认值 false
                buffered = App.Configuration["Serilog:Customer:Buffered"]?.To<bool>() ?? false;
                //允许多个进程共享日志文件。默认值为false
                shared = App.Configuration["Serilog:Customer:Shared"]?.To<bool>() ?? false;

                //文件大小限制字节：允许日志文件访问的近似最大大小（以字节为单位）成长。对于不受限制的增长，传递null。默认值为1 GB。避免写作部分事件，限制内的最后一个事件将全部写入偶数如果超过限制
                var tempFileSizeLimitBytes = App.Configuration["Serilog:Customer:FileSizeLimitBytes"];
                if (!string.IsNullOrWhiteSpace(tempFileSizeLimitBytes))
                {
                    fileSizeLimitBytes = tempFileSizeLimitBytes.To<long>();
                }
                //保留的最大日志文件数，包括当前
                var tempRetainedFileCountLimit = App.Configuration["Serilog:Customer:RetainedFileCountLimit"];
                if (!string.IsNullOrWhiteSpace(tempRetainedFileCountLimit))
                {
                    retainedFileCountLimit = tempRetainedFileCountLimit.To<int>();
                }
                //TimeSpan 不为null，将定期在指定的时间执行完整磁盘刷新间隔。
                var tempFlushToDiskInterval = App.Configuration["Serilog:Customer:FlushToDiskInterval"];
                if (!string.IsNullOrWhiteSpace(tempFlushToDiskInterval))
                {
                    flushToDiskInterval = tempFlushToDiskInterval.To<TimeSpan>();
                }
                //是否按日志类别分类记录
                isLogEventLevelFloder = App.Configuration["Serilog:Customer:IsLogEventLevelFloder"].To<bool>();
            }
            catch (Exception)
            {
            }
            if (isLogEventLevelFloder)//是否按日志类别分类记录
            {
                serilogCacheItem = new LogCacheItem<Serilog.ILogger>();
                serilogCacheItem.Logger = new Serilog.LoggerConfiguration()
                    .SetMinimumLevel(minLogEventLevel)
                    .SetWriteTo(logFloderName,//日志文件夹名称
                                              //ITextFormatter formatter,
                                              rollOnFileSizeLimit,
                                              // Encoding encoding = null,
                                              // FileLifecycleHooks hooks = null,
                                              minLogEventLevel,
                                              rollingInterval,
                                              retainedFileCountLimit,
                                              flushToDiskInterval,
                                              buffered,
                                              shared,
                                              fileSizeLimitBytes,
                                              outputTemplate,
                                              filename)
                    .CreateLogger();
            }
            else
            {
                //日志都记录到一个日志文件中
                serilogCacheItem = new LogCacheItem<Serilog.ILogger>();
                serilogCacheItem.Logger = new Serilog.LoggerConfiguration()
                                             .SetMinimumLevel(minLogEventLevel)
                                             .WriteTo.File($"logs\\{logFloderName}\\log.txt", rollingInterval: rollingInterval, rollOnFileSizeLimit: rollOnFileSizeLimit, buffered: buffered, shared: shared, fileSizeLimitBytes: fileSizeLimitBytes,
                                                 retainedFileCountLimit: retainedFileCountLimit, flushToDiskInterval: flushToDiskInterval, outputTemplate: outputTemplate)
                                             .CreateLogger();
            }
            Loggers.TryAdd(logFloderName, serilogCacheItem);
        }
        if (serilogCacheItem == null) serilogCacheItem.Logger = Serilog.Log.Logger;
        return serilogCacheItem.Logger;
    }

    private static Serilog.LoggerConfiguration SetWriteTo(this Serilog.LoggerConfiguration loggerConfiguration,// string path,
                                                                                                               // LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,

                                    // LoggingLevelSwitch levelSwitch = null,
                                    string LogFloderName = "",//日志文件夹名称

                                    bool rollOnFileSizeLimit = true,//如果为true，则在达到文件大小限制时将创建一个新文件。文件名将以_NNN格式附加一个数字，第一个文件名没有数字。
                                                                    // Encoding encoding = null,
                                                                    // FileLifecycleHooks hooks = null,
                                    LogEventLevel minLogEventLevel = LogEventLevel.Debug,//最低日志等级，默认Debug
                                    RollingInterval rollingInterval = RollingInterval.Hour,//滚动间隔：日志将滚动到新文件的间隔
                                    int? retainedFileCountLimit = null,//保留的最大日志文件数，包括当前
                                    TimeSpan? flushToDiskInterval = null,//TimeSpan 不为null，将定期在指定的时间执行完整磁盘刷新间隔。
                                    bool buffered = false,//指示是否可以缓存到输出文件。默认值 false
                                    bool shared = false,//允许多个进程共享日志文件。默认值为false
                                    long? fileSizeLimitBytes = 1073741824L,//文件大小限制字节：允许日志文件访问的近似最大大小（以字节为单位）成长。对于不受限制的增长，传递null。默认值为1 GB。避免写作部分事件，限制内的最后一个事件将全部写入偶数如果超过限制
                                    string outputTemplate = "",
                                    string filename = "")
    {
        //日志是否输出到控制台
        var isConsole = App.Configuration["Serilog:Customer:IsConsole"]?.To<bool>() ?? false;
        if (isConsole)
        {
            loggerConfiguration.WriteTo.Console(outputTemplate: outputTemplate);
        }
        //日志是否写入到文件
        var isFile = App.Configuration["Serilog:Customer:IsFile"]?.To<bool>() ?? true;
        if (isFile)
        {
            foreach (Serilog.Events.LogEventLevel logEventLevel in Enum.GetValues(typeof(Serilog.Events.LogEventLevel)))
            {
                loggerConfiguration.WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(p => p.Level == logEventLevel)
                    .WriteTo.File(LogFilePath(LogFloderName, logEventLevel, filename),
                        rollingInterval: rollingInterval, outputTemplate: outputTemplate,
                        rollOnFileSizeLimit: rollOnFileSizeLimit, fileSizeLimitBytes: fileSizeLimitBytes,
                        retainedFileCountLimit: retainedFileCountLimit, flushToDiskInterval: flushToDiskInterval,
                        shared: shared));
            }
        }
        //日志是否保存到MSSQL数据库
        var isMsSql = App.Configuration["Serilog:Customer:IsMsSql"]?.To<bool>() ?? false;
        if (isMsSql)
        {
            //var columnOpts = new ColumnOptions();
            //columnOpts.Store.Remove(StandardColumn.Properties);
            //columnOpts.Store.Add(StandardColumn.LogEvent);
            //columnOpts.LogEvent.DataLength = 2048;
            //columnOpts.PrimaryKey = options.TimeStamp;
            //columnOpts.TimeStamp.NonClusteredIndex = true;
            var columnOptions = new ColumnOptions();
            //连接字符串
            var connectionString = App.Configuration["Serilog:Customer:MSSQLSERVER:ConnectionString"];
            //表名
            var tableName = App.Configuration["Serilog:Customer:MSSQLSERVER:TableName"] ?? "SeriLogs";
            //    限制每个批将多少日志事件写入数据库（默认值：50）
            var batchPostingLimit = App.Configuration["Serilog:Customer:MSSQLSERVER:batchPostingLimit"]?.ToInt32() ?? 50;
            //记录日志最低等级
            var minimumLevel = App.Configuration["Serilog:Customer:MSSQLSERVER:restrictedToMinimumLevel"]?.To<LogEventLevel>() ?? LogEventLevel.Verbose;

            //选项
            var sinkOpts = new MSSqlServerSinkOptions
            {
                TableName = $"{tableName}-{DateTimeOffset.Now:yyyyMMdd}",
                BatchPostingLimit = batchPostingLimit,
                BatchPeriod = TimeSpan.FromSeconds(3),//执行时间间隔
                AutoCreateSqlTable = true
            };
            //写入到数据库
            loggerConfiguration.WriteTo.MSSqlServer(
                connectionString: connectionString,
                sinkOptions: sinkOpts,
                // tableName: tableName,
                columnOptions: columnOptions,
                // batchPostingLimit: batchPostingLimit,//批量插入数据库条数
                // period: TimeSpan.FromSeconds(5),//执行时间间隔
                restrictedToMinimumLevel: minimumLevel
            //autoCreateSqlTable: true
            );
        }

        return loggerConfiguration;
    }

    private static Serilog.LoggerConfiguration SetMinimumLevel(this Serilog.LoggerConfiguration loggerConfiguration, Serilog.Events.LogEventLevel minLevel)
    {
        switch (minLevel)
        {
            case LogEventLevel.Verbose://冗余的
                return loggerConfiguration.MinimumLevel.Verbose();

            case LogEventLevel.Debug://调试
                return loggerConfiguration.MinimumLevel.Debug();

            case LogEventLevel.Information://信息
                return loggerConfiguration.MinimumLevel.Information();

            case LogEventLevel.Warning://警告
                return loggerConfiguration.MinimumLevel.Warning();

            case LogEventLevel.Error://错误
                return loggerConfiguration.MinimumLevel.Error();

            case LogEventLevel.Fatal://致命
                return loggerConfiguration.MinimumLevel.Fatal();

            default:
                break;
        }
        return loggerConfiguration.MinimumLevel.Debug();
    }

    private static string LogFilePath(string LogFloderName, Serilog.Events.LogEventLevel loglevel, string filename = "")
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");//按时间创建文件夹
        if (!string.IsNullOrWhiteSpace(filename))
        {
            return $"logs\\{LogFloderName}\\{date}\\{filename}_.txt";
        }
        else
        {
            return $"logs\\{LogFloderName}\\{date}\\{loglevel:G}_.txt";
        }
    }

    /// <summary>
    /// 获取Serilog 自定义日志配置
    /// </summary>
    /// <returns></returns>
    public static void GetDefaultSerilogLoggerConfiguration(LoggerConfiguration config)
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");//按时间创建文件夹
        config.WriteTo.Console(outputTemplate: SerilogOutputTemplate)
              .WriteTo.File($"logs\\all\\{date}\\log_.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true, fileSizeLimitBytes: 20971520, shared: true);
        foreach (Serilog.Events.LogEventLevel logEventLevel in Enum.GetValues(typeof(Serilog.Events.LogEventLevel)))
        {
            config.WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(p => p.Level == logEventLevel)
              .WriteTo.File(LogFilePath("all", logEventLevel), rollingInterval: RollingInterval.Day, outputTemplate: SerilogOutputTemplate, rollOnFileSizeLimit: true, fileSizeLimitBytes: 20971520, shared: true));
        }
    }

    #endregion Serilog  日志
}