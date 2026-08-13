using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("ScheduleTaskLog")]
    public class ScheduleTaskLog
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ID" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ScheduleTaskID"    )]
         public int? ScheduleTaskID { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Name"    )]
         public string Name { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="BeginDate"    )]
         public DateTime? BeginDate { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="EndDate"    )]
         public DateTime? EndDate { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Status"    )]
         public string Status { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="ErrorMessage"    )]
         public string ErrorMessage { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="LogInfo"    )]
         public string LogInfo { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Result"    )]
         public string Result { get; set; }
    }
}
