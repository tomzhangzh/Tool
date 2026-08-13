// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

public class LogCacheItem<T>
{
    public LogCacheItem()
    {
        Date = $"{DateTimeOffset.Now:yyyy-MM-dd}";
    }

    /// <summary>
    /// 日志对象
    /// </summary>
    public T Logger { get; set; }

    /// <summary>
    /// 日期（2022-01-01）
    /// 用来记录日志对象是什么时间创建的
    /// </summary>
    public string Date { get; set; }
}