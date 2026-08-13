using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("ScheduleTask")]
    public class ScheduleTask
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ID" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="GroupName"    )]
         public string GroupName { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="TaskName"    )]
         public string TaskName { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Interval"    )]
         public string Interval { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Parameter"    )]
         public string Parameter { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Description"    )]
         public string Description { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="LastRunTime"    )]
         public DateTime? LastRunTime { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Status"    )]
         public int? Status { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ClassName"    )]
         public string ClassName { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Result"    )]
         public string Result { get; set; }
    }
}
