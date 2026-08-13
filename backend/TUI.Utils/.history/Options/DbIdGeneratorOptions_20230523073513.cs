// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using Yitter.IdGenerator;

namespace Abc.Utils;

/// <summary>
/// 数据库 雪花ID配置选项
/// </summary>
public class DbIdGeneratorOptions : IdGeneratorOptions, IConfigurableOptionsListener<DbIdGeneratorOptions>
{
    /// <summary>
    ///  1-漂移算法，2-传统算法
    /// </summary>
    public override short Method { get; set; } = 1;

    public override DateTime BaseTime { get; set; } = new DateTime(2020, 2, 20, 2, 20, 2, 20, DateTimeKind.Utc);

    /// <summary>
    /// 机器码，最重要参数，无默认值，必须 全局唯一（或相同 DataCenterId 内唯一），必须 程序设定，缺省条件（WorkerIdBitLength取默认值）时最大值63，理论最大值 2^WorkerIdBitLength-1（不同实现语言可能会限定在 65535 或 32767，原理同 WorkerIdBitLength 规则）。不同机器或不同应用实例 不能相同，你可通过应用程序配置该值，也可通过调用外部服务获取值。针对自动注册WorkerId需求，本算法提供默认实现：通过 redis 自动注册 WorkerId 的动态库，详见“Tools\AutoRegisterWorkerId”。
    /// </summary>
    public override ushort WorkerId { get; set; } = 1;

    /// <summary>
    /// 机器码位长，决定 WorkerId 的最大值，默认值6，取值范围 [1, 19]，实际上有些语言采用 无符号 ushort (uint16) 类型接收该参数，所以最大值是16，如果是采用 有符号 short (int16)，则最大值为15
    /// </summary>
    public override byte WorkerIdBitLength { get; set; } = 6;

    /// <summary>
    /// 序列数位长，默认值6，取值范围 [3, 21]（建议不小于4），决定每毫秒基础生成的ID个数。规则要求：WorkerIdBitLength + SeqBitLength 不超过 22。
    /// </summary>
    public override byte SeqBitLength { get; set; } = 6;

    public override int MaxSeqNumber { get; set; } = 0;

    public override ushort MinSeqNumber { get; set; } = 5;

    public override int TopOverCostCount { get; set; } = 2000;

    public override uint DataCenterId { get; set; } = 0;

    //public virtual byte DataCenterIdBitLength { get; set; }

    //public virtual byte TimestampType { get; set; }

    public void OnListener(DbIdGeneratorOptions options, IConfiguration configuration)
    {
        //Name = options.Name;  // 实时的最新值
        //Version = options.Version;  // 实时的最新值
        //Company = options.Company;//公司

        Type entityType = GetType();//获得该类的Type

        var dics = options.GetDictionary();
        foreach (var item in dics)
        {
            PropertyInfo propertyInfo = entityType.GetProperty(item.Key);
            propertyInfo.FieldSetValue(item.Key, item.Value as object);
        }
    }

    public void PostConfigure(DbIdGeneratorOptions options, IConfiguration configuration)
    {
    }
}