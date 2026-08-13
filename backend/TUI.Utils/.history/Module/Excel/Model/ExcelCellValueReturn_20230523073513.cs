// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abc.Utils;
/// <summary>
/// Excel 单元格获取的值返回对象
/// </summary>
public class ExcelCellValueReturn
{
    /// <summary>
    /// 是否获取值成功
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 值
    /// </summary>
    public object? ObjValue { get; set; }
    /// <summary>
    /// 错误
    /// </summary>
    public string Error { get; set; }
}
