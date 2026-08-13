// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.
public static class LoggingExtension
{
    //------------------------------------------DEBUG------------------------------------------//

    /// <summary>
    /// 格式化并写入调试日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    public static void Debug(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogDebug(eventId, exception, message, args);
    }

    /// <summary>
    /// 格式化并写入调试日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
    public static void Debug(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string? message = null, params object?[] args)
    {
        logger.LogDebug(eventId, message, args);
    }

    /// <summary>
    /// 格式化并写入调试日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    public static void Debug(this Microsoft.Extensions.Logging.ILogger logger, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogDebug(exception, message, args);
    }

    /// <summary>
    /// 格式化并写入调试日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
    public static void Debug(this Microsoft.Extensions.Logging.ILogger logger, string? message = null, params object?[] args)
    {
        logger.LogDebug(message, args);
    }

    //------------------------------------------TRACE------------------------------------------//

    /// <summary>
    /// 格式化并写入跟踪日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    public static void Trace(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogTrace(eventId, exception, message, args);
    }

    /// <summary>
    /// 格式化并写入跟踪日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
    public static void Trace(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string? message = null, params object?[] args)
    {
        logger.LogTrace(eventId, message, args);
    }

    /// <summary>
    /// 格式化并写入跟踪日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
    public static void Trace(this Microsoft.Extensions.Logging.ILogger logger, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogTrace(exception, message, args);
    }

    /// <summary>
    /// 格式化并写入跟踪日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
    public static void Trace(this Microsoft.Extensions.Logging.ILogger logger, string? message = null, params object?[] args)
    {
        logger.LogTrace(message, args);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    /// <summary>
    /// 格式化并写入信息日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
    public static void Information(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogInformation(eventId, exception, message, args);
    }

    /// <summary>
    /// 格式化并写入信息日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
    public static void Information(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string? message = null, params object?[] args)
    {
        logger.LogInformation(eventId, message, args);
    }

    /// <summary>
    /// 格式化并写入信息日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    public static void Information(this Microsoft.Extensions.Logging.ILogger logger, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogInformation(exception, message, args);
    }

    /// <summary>
    /// 格式化并写入信息日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
    public static void Information(this Microsoft.Extensions.Logging.ILogger logger, string? message = null, params object?[] args)
    {
        logger.LogInformation(message, args);
    }

    //------------------------------------------WARNING------------------------------------------//

    /// <summary>
    /// 格式化并写入警告日志消息.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
    public static void Warning(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogWarning(eventId, exception, message, args);
    }

    /// <summary>
    /// 格式化并写入警告日志消息.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
    public static void Warning(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string? message = null, params object?[] args)
    {
        logger.LogWarning(eventId, message, args);
    }

    /// <summary>
    /// 格式化并写入警告日志消息.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    public static void Warning(this Microsoft.Extensions.Logging.ILogger logger, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogWarning(exception, message, args);
    }

    /// <summary>
    /// 格式化并写入警告日志消息.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
    public static void Warning(this Microsoft.Extensions.Logging.ILogger logger, string? message = null, params object?[] args)
    {
        logger.LogWarning(message, args);
    }

    //------------------------------------------ERROR------------------------------------------//

    /// <summary>
    /// 格式化并写入错误日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
    public static void Error(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogError(eventId, exception, message, args);
    }

    /// <summary>
    /// 格式化并写入错误日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
    public static void Error(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string? message = null, params object?[] args)
    {
        logger.LogError(eventId, message, args);
    }

    /// <summary>
    /// 格式化并写入错误日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    public static void Error(this Microsoft.Extensions.Logging.ILogger logger, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogError(exception, message, args);
    }

    /// <summary>
    /// 格式化并写入错误日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogError("Processing request from {Address}", address)</example>
    public static void Error(this Microsoft.Extensions.Logging.ILogger logger, string? message = null, params object?[] args)
    {
        logger.LogError(message, args);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    /// <summary>
    /// 格式化并写入关键日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
    public static void Critical(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogCritical(eventId, exception, message, args);
    }

    /// <summary>
    /// 格式化并写入关键日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">与日志关联的事件id</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
    public static void Critical(this Microsoft.Extensions.Logging.ILogger logger, EventId eventId, string? message = null, params object?[] args)
    {
        logger.LogCritical(eventId, message, args);
    }

    /// <summary>
    /// 格式化并写入关键日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">异常日志</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    public static void Critical(this Microsoft.Extensions.Logging.ILogger logger, Exception? exception, string? message = null, params object?[] args)
    {
        logger.LogCritical(exception, message, args);
    }

    /// <summary>
    /// 格式化并写入关键日志消息
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">消息内容，支持格式化。例如: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">包含零个或多个要格式化的对象的对象数组.</param>
    /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
    public static void Critical(this Microsoft.Extensions.Logging.ILogger logger, string? message = null, params object?[] args)
    {
        logger.LogCritical(message, args);
    }
}