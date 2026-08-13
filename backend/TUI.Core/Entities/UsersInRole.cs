using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("UsersInRole")]
    public class UsersInRole
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Id" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="UserId"    )]
         public int UserId { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="RoleId"    )]
         public int RoleId { get; set; }
    }
}
