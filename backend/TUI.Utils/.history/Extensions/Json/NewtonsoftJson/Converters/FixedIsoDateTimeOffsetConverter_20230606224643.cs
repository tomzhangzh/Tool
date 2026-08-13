// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Newtonsoft.Json.Converters;
using System.Globalization;

namespace TUI.Utils;

public class FixedIsoDateTimeOffsetConverter : IsoDateTimeConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DateTimeOffset) || objectType == typeof(DateTimeOffset?);
    }

    public FixedIsoDateTimeOffsetConverter() : base()
    {
        this.DateTimeStyles = DateTimeStyles.AssumeUniversal;
    }
}