using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("User")]
    public class User
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Id" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="UserName"    )]
         public string UserName { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Password"    )]
         public string Password { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Email"    )]
         public string Email { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Name"    )]
         public string Name { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Status"    )]
         public int Status { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="AttachmentID"    )]
         public int? AttachmentID { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="IsActive"    )]
         public bool IsActive { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="IsDeleted"    )]
         public bool IsDeleted { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="LocationIDs"    )]
         public string LocationIDs { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="LastLocationID"    )]
         public int? LastLocationID { get; set; }
    }
}
