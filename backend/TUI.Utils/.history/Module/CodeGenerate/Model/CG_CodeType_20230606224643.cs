// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

public class CG_CodeType
{
    //[SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string Name { get; set; }
    public string CSharepType { get; set; }
    public string DbTypeStr { get; set; }

    //[SugarColumn(ColumnDataType = "text", IsJson = true)]
    public List<DbTypeInfo> DbType { get; set; }

    public int Sort { get; set; }
}

public class DbTypeInfo
{
    public string Name { get; set; }
    public int? Length { get; set; }
    public int? DecimalDigits { get; set; }
}