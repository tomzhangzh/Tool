using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
namespace TUI.Core.Entities
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("LogEvents")]
    public class LogEvents
    {
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Id" ,IsPrimaryKey = true ,IsIdentity = true  )]
         public int Id { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Message"    )]
         public string Message { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="MessageTemplate"    )]
         public string MessageTemplate { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Level"    )]
         public string Level { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="TimeStamp"    )]
         public DateTime? TimeStamp { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Exception"    )]
         public string Exception { get; set; }
        /// <summary>
        ///  
        ///</summary>
         [SugarColumn(ColumnName="Properties"    )]
         public string Properties { get; set; }
    }
}
