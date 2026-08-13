// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Microsoft.Extensions.Internal;

public class SystemClockEx : ISystemClock
{
    public DateTimeOffset UtcNow => DateTime.Now;

    DateTimeOffset ISystemClock.UtcNow => DateTime.Now;
}