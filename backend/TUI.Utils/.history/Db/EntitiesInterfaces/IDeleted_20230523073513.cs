// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 删除接口
/// </summary>
public interface IDeleted
{
    /// <summary>
    /// 默认假删除
    /// </summary>
    //[FakeDelete(true)]  // 设置假删除的值
    bool IsDeleted { get; set; }

    /// <summary>
    /// 删除用户ID
    /// </summary>
    long? DeletedUserId { get; set; }

    /// <summary>
    /// 删除用户名称
    /// </summary>
    string DeletedUserName { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    DateTimeOffset? DeletedTime { get; set; }
}