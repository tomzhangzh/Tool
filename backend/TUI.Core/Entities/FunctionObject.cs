using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("FunctionObject")]
    public class FunctionObject
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Id" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="PermissonTag"    )]
         public string PermissonTag { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="FunctionObjectName"    )]
         public string FunctionObjectName { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="FunctionObjectName_En"    )]
         public string FunctionobjectnameEn { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Description"    )]
         public string Description { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ParentFunctionObjectId"    )]
         public int? ParentFunctionObjectId { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="FunctionAvailable"    )]
         public bool FunctionAvailable { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="EditPermissionAvailable"    )]
         public bool EditPermissionAvailable { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="DeletePermissionAvailable"    )]
         public bool DeletePermissionAvailable { get; set; }
    }
}
