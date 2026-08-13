using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("ComponentSetting")]
    public class ComponentSetting
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
         [SugarColumn(ColumnName="DefaultOptionsJson"    )]
         public string DefaultOptionsJson { get; set; }
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
         [SugarColumn(ColumnName="OrderNumer"    )]
         public int? OrderNumer { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="QuickInput"    )]
         public string QuickInput { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="IsDeleted"    )]
         public bool IsDeleted { get; set; }
        /// <summary>
        ///  
        ///</summary>
        [SugarColumn(ColumnName = "ComType")]
        public string ComType { get; set; }
        /// <summary>
        ///  
        ///</summary>
        [SugarColumn(ColumnName="ConfigJson"    )]
         public string ConfigJson { get; set; }
        /// <summary>
        ///  
        ///</summary>
        [SugarColumn(ColumnName = "Url")]
        public string Url { get; set; }
        /// <summary>
        ///  
        ///</summary>
        [SugarColumn(ColumnName = "ViewCode")]
        public string ViewCode { get; set; }
    }
}
