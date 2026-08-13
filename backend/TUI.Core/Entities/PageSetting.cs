using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("PageSetting")]
    public class PageSetting
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Id" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Name"    )]
         public string Name { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ApiBaseUrl"    )]
         public string ApiBaseUrl { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Category"    )]
         public string Category { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ConfigJson"    )]
         public string ConfigJson { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Icon"    )]
         public string Icon { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Description"    )]
         public string Description { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="DefaultValueJson"    )]
         public string DefaultValueJson { get; set; }
        /// <summary>
        ///  
        /// 默认值: ((0))
        ///</summary>
         [SugarColumn(ColumnName="IsDeleted"    )]
         public bool IsDeleted { get; set; }
    }
}
