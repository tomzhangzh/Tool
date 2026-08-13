using Microsoft.Extensions.Logging;
using TUI.Services.DBModel;
using TUI.Services.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.TaskLib
{
    public class BaseTaskService 
    {
        protected TUIDbContext dbContext;
        protected ILogger<ClassLibraryJob> logger { get; set; }
        protected readonly IService<ScheduleTask> ScheduleTaskService;
        protected readonly IService<ScheduleTaskLog> ScheduleTaskLogService;
        public BaseTaskService(
            TUIDbContext dbContext
            , IService<ScheduleTask> ScheduleTaskService
            , IService<ScheduleTaskLog> ScheduleTaskLogService
            , ILogger<ClassLibraryJob> logger)
        {
            this.dbContext = dbContext;
            this.ScheduleTaskService = ScheduleTaskService;
            this.ScheduleTaskLogService = ScheduleTaskLogService;
            this.logger = logger;
        }
       
  
}
}
