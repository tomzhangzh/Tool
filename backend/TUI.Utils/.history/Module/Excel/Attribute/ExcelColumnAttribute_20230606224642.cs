// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Utils;
public class ExcelColumnAttribute:Attribute
{
    /// <summary>
    /// Excel的列明
    /// </summary>
    public string ColumnName { get; set; }

    //public Type type { get; set; } = typeof(Int32);
}
