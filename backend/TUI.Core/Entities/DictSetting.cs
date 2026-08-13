using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("DictSetting")]
    public class DictSetting
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Id" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="TableName"    )]
         public string TableName { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Type"    )]
         public string Type { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Text"    )]
         public string Text { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Value"    )]
         public string Value { get; set; }
    }
}
