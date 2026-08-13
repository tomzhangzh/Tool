// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

//public class TenantEntity : TentantEntity<string>
//{
//}

public class TentantEntity : ITenantEntity
{
    /// <summary>
    /// 租户id
    /// </summary>
    public long TenantId { get; set; }
}