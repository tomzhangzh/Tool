// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Serilog.Events;

namespace System;

/// <summary>
/// 日志快捷方式(使用Serilog)
/// </summary>
public static class LogEx
{
    /// <summary>
    /// Debug  调试
    /// </summary>
    /// <param name="msg">日志内容</param>
    /// <param name="logFloderName">日志文件夹</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Debug(string msg, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(msg, logFloderName, LogEventLevel.Debug, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    /// Debug  调试
    /// </summary>
    /// <param name="ex">异常</param>
    /// <param name="logFloderName">文件夹：例如：错误\致命错误</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Debug(Exception ex, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(ex, logFloderName, LogEventLevel.Debug, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    /// Error 错误
    /// </summary>
    /// <param name="msg">日志内容</param>
    /// <param name="logFloderName">日志文件夹</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Error(string msg, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(msg, logFloderName, LogEventLevel.Error, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    /// Error 错误
    /// </summary>
    /// <param name="ex">异常</param>
    /// <param name="logFloderName">文件夹：例如：错误\致命错误</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Error(Exception ex, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(ex, logFloderName, LogEventLevel.Error, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="msg">日志内容</param>
    /// <param name="logFloderName">日志文件夹</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Fatal(string msg, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(msg, logFloderName, LogEventLevel.Fatal, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    ///  Info 信息
    /// </summary>
    /// <param name="ex">异常</param>
    /// <param name="logFloderName">文件夹：例如：错误\致命错误</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Fatal(Exception ex, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(ex, logFloderName, LogEventLevel.Fatal, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    /// Info 信息
    /// </summary>
    /// <param name="msg">日志内容</param>
    /// <param name="logFloderName">日志文件夹</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Info(string msg, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(msg, logFloderName, LogEventLevel.Information, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    /// Info 信息
    /// </summary>
    /// <param name="ex">异常</param>
    /// <param name="logFloderName">文件夹：例如：错误\致命错误</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Info(Exception ex, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(ex, logFloderName, LogEventLevel.Information, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    /// Verbose 冗余
    /// </summary>
    /// <param name="msg">日志内容</param>
    /// <param name="logFloderName">日志文件夹</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>

    public static void Verbose(string msg, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(msg, logFloderName, LogEventLevel.Verbose, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    ///  Verbose 冗余
    /// </summary>
    /// <param name="ex">异常</param>
    /// <param name="logFloderName">文件夹：例如：错误\致命错误</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Verbose(Exception ex, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(ex, logFloderName, LogEventLevel.Verbose, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    /// ///  Warning 警告
    /// </summary>
    /// <param name="msg">日志内容</param>
    /// <param name="logFloderName">日志文件夹</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Warning(string msg, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(msg, logFloderName, LogEventLevel.Warning, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    ///  Warning 警告
    /// </summary>
    /// <param name="ex">异常</param>
    /// <param name="logFloderName">文件夹：例如：错误\致命错误</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Warning(Exception ex, string logFloderName = "", bool isWebInfoLog = true)
    {
        Log(ex, logFloderName, LogEventLevel.Warning, isWebInfoLog: isWebInfoLog);
    }

    /// <summary>
    /// 写日志，使用的Serilog
    /// </summary>
    /// <param name="exception">异常</param>
    /// <param name="logFloderName">文件夹：例如：错误\致命错误</param>
    /// <param name="minLevel">日志等级</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Log(Exception exception, string logFloderName = "", Serilog.Events.LogEventLevel minLevel = Serilog.Events.LogEventLevel.Debug, bool isWebInfoLog = false)
    {
        Log(msg: null, exception, logFloderName, minLevel, isWebInfoLog);
    }

    /// <summary>
    /// 写日志，使用的Serilog
    /// </summary>
    /// <param name="msg">日志内容</param>
    /// <param name="logFloderName">文件夹：例如：错误\致命错误</param>
    /// <param name="minLevel">日志等级</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Log(string msg, string logFloderName = "", Serilog.Events.LogEventLevel minLevel = Serilog.Events.LogEventLevel.Debug, bool isWebInfoLog = false)
    {
        Log(msg, exception: null, logFloderName, minLevel, isWebInfoLog);
    }

    /// <summary>
    /// 写日志，使用的Serilog
    /// </summary>
    /// <param name="msg">日志内容</param>
    /// <param name="exception">异常</param>
    /// <param name="logFloderName">文件夹：例如：错误\致命错误</param>
    /// <param name="minLevel">日志等级</param>
    /// <param name="isWebInfoLog">是否记录web信息</param>
    public static void Log(string msg = "", Exception exception = null, string logFloderName = "", Serilog.Events.LogEventLevel minLevel = Serilog.Events.LogEventLevel.Debug, bool isWebInfoLog = false)
    {
        //如果日志消息同时为空了，则不记录日志，减少错误操作的系统开销
        if (string.IsNullOrWhiteSpace(msg) && exception != null) return;

        var sb = new StringBuilder();
        if (isWebInfoLog)
        {
            AppEx.GetWebInfoToString(sb);
        }
        if (!string.IsNullOrWhiteSpace(msg))
        {
            sb.AppendLine(msg);
        }
        if (exception != null)
        {
            sb.AppendLine($"异常日志：{exception.ToStringEx()}");
        }
        var _logger = SerilogEx.GetLogger(logFloderName);
        switch (minLevel)
        {
            case LogEventLevel.Verbose://冗余的
                _logger.Verbose(sb.ToString());
                break;

            case LogEventLevel.Debug://调试
                _logger.Debug(sb.ToString());
                break;

            case LogEventLevel.Information://信息
                _logger.Information(sb.ToString());
                break;

            case LogEventLevel.Warning://警告
                _logger.Warning(sb.ToString());
                break;

            case LogEventLevel.Error://错误
                _logger.Error(sb.ToString());
                break;

            case LogEventLevel.Fatal://致命
                _logger.Fatal(sb.ToString());
                break;

            default:
                break;
        }
    }
}