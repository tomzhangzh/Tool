using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("MenuSetting")]
    public class MenuSetting
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
         [SugarColumn(ColumnName="Title"    )]
         public string Title { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="URL"    )]
         public string Url { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ImagePath"    )]
         public string ImagePath { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Parameters"    )]
         public string Parameters { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Description"    )]
         public string Description { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Options"    )]
         public string Options { get; set; }
    }
}
