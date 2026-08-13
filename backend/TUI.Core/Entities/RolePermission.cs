using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("RolePermission")]
    public class RolePermission
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Id" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="FunctionObjectId"    )]
         public int FunctionObjectId { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Enable"    )]
         public bool Enable { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Editable"    )]
         public bool Editable { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Deleteable"    )]
         public bool Deleteable { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="RoleId"    )]
         public int? RoleId { get; set; }
    }
}
