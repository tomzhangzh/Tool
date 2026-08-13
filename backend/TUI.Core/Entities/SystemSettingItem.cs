using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("SystemSettingItem")]
    public class SystemSettingItem
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ID" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Type"    )]
         public string Type { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Category"    )]
         public string Category { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Json"    )]
         public string Json { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="TypeFullName"    )]
         public string TypeFullName { get; set; }
    }
}
