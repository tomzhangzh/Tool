// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

public static class SerilogExtension
{
    /// <summary>
    /// 调试日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="ex"></param>
    public static void Debug(this Serilog.ILogger logger, Exception ex)
    {
        logger.Debug(ex.ToStringEx());
    }

    /// <summary>
    /// 错误日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="ex"></param>
    public static void Error(this Serilog.ILogger logger, Exception ex)
    {
        logger.Error(ex.ToStringEx());
    }

    /// <summary>
    /// 致命日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="ex"></param>
    public static void Fatal(this Serilog.ILogger logger, Exception ex)
    {
        logger.Fatal(ex.ToStringEx());
    }

    /// <summary>
    /// 信息日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="msg"></param>
    public static void Info(this Serilog.ILogger logger, string msg)
    {
        logger.Information(msg);
    }

    /// <summary>
    /// 信息日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="ex"></param>
    public static void Info(this Serilog.ILogger logger, Exception ex)
    {
        logger.Information(ex.ToStringEx());
    }

    /// <summary>
    /// 冗余日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="ex"></param>
    public static void Verbose(this Serilog.ILogger logger, Exception ex)
    {
        logger.Verbose(ex.ToStringEx());
    }

    /// <summary>
    /// 冗余日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="ex"></param>
    public static void Trace(this Serilog.ILogger logger, Exception ex)
    {
        logger.Verbose(ex.ToStringEx());
    }

    /// <summary>
    /// 警告日志
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="ex"></param>
    public static void Warning(this Serilog.ILogger logger, Exception ex)
    {
        logger.Warning(ex.ToStringEx());
    }
}