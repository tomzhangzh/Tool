// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

public static class NLogEx
{
    #region NLog

    //private static readonly ConcurrentDictionary<string, NLog.Logger> NLoggers = new();
    //private static readonly Dictionary<string, string> dictionary = new();

    /////// <summary>
    /////// 全局属性参数【初始化设定】
    /////// </summary>
    ////public static Dictionary<string, string> GlobalProperties = dictionary;

    ///// <summary>
    ///// 获取Logger ，按 logFloder 昨晚Key缓存
    ///// </summary>
    ///// <param name="logFloder"></param>
    ///// <param name="filename"></param>
    ///// <param name="withPropertys">字典</param>
    ///// <returns></returns>
    //public static NLog.Logger GetNLogger(
    //    string logFloder = "default",
    //    string filename = "",
    //    Dictionary<string, string> withPropertys = null)
    //{
    //    if (string.IsNullOrWhiteSpace(logFloder))
    //    {
    //        logFloder = "default";
    //    }
    //    Logger logger;
    //    //从缓存中取出日志对象 logger
    //    if (NLoggers.ContainsKey(logFloder))
    //    {
    //        _ = NLoggers.TryGetValue(logFloder, out logger);
    //    }
    //    else
    //    {
    //        //创建一个新的日志对象 logger
    //        //logger = LogManager.GetCurrentClassLogger();
    //        logger = LogManager.GetLogger(logFloder);
    //        //if (string.IsNullOrWhiteSpace(logFloder)) logFloder = "default";
    //        PropertiesAdd(logger, "logfloder", logFloder.Trim());
    //        if (!string.IsNullOrWhiteSpace(filename)) PropertiesAdd(logger, "filename", $"{filename.Trim()}-");

    //        if (withPropertys != null)
    //        {
    //            foreach (var withProperty in withPropertys)
    //            {
    //                if (string.IsNullOrWhiteSpace(withProperty.Key)) continue;
    //                PropertiesAdd(logger, withProperty.Key.Trim(), withProperty.Value?.Trim() ?? "");
    //            }
    //        }
    //        NLoggers.TryAdd(logFloder, logger);
    //    }
    //    return logger;
    //}

    //private static NLog.Logger PropertiesAdd(NLog.Logger logger, string key, object value)
    //{
    //    if (logger.Properties.ContainsKey(key)) return logger;
    //    logger.Properties.Add(key, value);
    //    return logger;
    //}

    #endregion NLog
}