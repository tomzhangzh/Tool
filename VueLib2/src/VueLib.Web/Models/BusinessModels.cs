using SqlSugar;

namespace VueLib.Web.Models;

/// <summary>业务字典类型（业务库）</summary>
[SugarTable("DictTypes")]
[SugarIndex("uk_dict_type_code", nameof(TypeCode), OrderByType.Asc, true)]
public class DictType
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    [SugarColumn(Length = 64)]
    public string TypeCode { get; set; } = "";
    [SugarColumn(Length = 128)]
    public string TypeName { get; set; } = "";
}

/// <summary>业务字典项（业务库，下拉 static/dict/sql 演示用）</summary>
[SugarTable("DictItems")]
public class DictItem
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    [SugarColumn(Length = 64)]
    public string TypeCode { get; set; } = "";
    [SugarColumn(Length = 128)]
    public string Name { get; set; } = "";
    [SugarColumn(Length = 64)]
    public string Value { get; set; } = "";
    public int Sort { get; set; }
}

/// <summary>演示商品（业务库，Grid3 三屏演示用）</summary>
[SugarTable("Products")]
public class Product
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    [SugarColumn(Length = 128)]
    public string Name { get; set; } = "";
    [SugarColumn(Length = 64)]
    public string Category { get; set; } = "";
    [SugarColumn(DecimalDigits = 2)]
    public decimal Price { get; set; }
    public int Stock { get; set; }
    /// <summary>1=上架 0=下架</summary>
    public int Status { get; set; } = 1;
    [SugarColumn(Length = 256, IsNullable = true)]
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>演示客户（业务库，Ajax 下拉演示用）</summary>
[SugarTable("Customers")]
public class Customer
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    [SugarColumn(Length = 64)]
    public string Name { get; set; } = "";
    [SugarColumn(Length = 32)]
    public string Phone { get; set; } = "";
    [SugarColumn(Length = 128)]
    public string Company { get; set; } = "";
    [SugarColumn(Length = 256, IsNullable = true)]
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
