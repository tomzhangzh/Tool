// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abc.Utils;
public class RegisterObject
{
    /// <summary>
    /// 
    /// </summary>
    public Type type { get; set; }

    /// <summary>
    /// 方法
    /// </summary>
    /// <example><方法名称，方法信息></example>
    public ConcurrentDictionary<string, MethodInfo> Methods { get; set; } = new();
}
