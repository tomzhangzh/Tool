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
/// Excel读取实体基类，包含excle行索引和错误信息字段
/// </summary>
public class ExcelReadBaseModel
{
    /// <summary>
    /// 表格中的行索引(从0开始计数)
    /// </summary>
    public int RowIndex { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string Error { get; set; }
}
