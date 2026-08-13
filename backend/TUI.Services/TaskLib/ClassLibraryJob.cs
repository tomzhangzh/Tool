using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Triggers;
using TUI.Services.DBModel;
using TUI.Services.Extension;
using TUI.Services.Manager;
using TUI.Services.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Services.TaskLib
{
    //public interface IJobService: IScopeDependency
    //{
    //    string ExecuteService(string parameter);
    //}
    public class ClassLibraryJob : IJob
    {
        private IServiceProvider serviceProvider;
        private ILogger<ClassLibraryJob> logger { get; set; }
        private readonly IService<ScheduleTask> ScheduleTaskService;
        private readonly IService<ScheduleTaskLog> ScheduleTaskLogService;
        //public ClassLibraryJob()
        //{

        //}
        /// <summary>
        /// 2020.05.31增加构造方法
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="httpClientFactory"></param>
        public ClassLibraryJob(IServiceProvider serviceProvider
            , IService<ScheduleTask> ScheduleTaskService
            , IService<ScheduleTaskLog> ScheduleTaskLogService
            ,  ILogger<ClassLibraryJob> logger)
        {
            this.ScheduleTaskService = ScheduleTaskService;
            this.ScheduleTaskLogService = ScheduleTaskLogService;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            //serviceProvider.GetService()
        }
        public async Task Execute(IJobExecutionContext context)
        {
           // DateTime dateTime = DateTime.Now;
           
            AbstractTrigger trigger = (context as JobExecutionContextImpl).Trigger as AbstractTrigger;

            ScheduleTask sTask = ScheduleTaskService.Queryable().Where(x => x.TaskName == trigger.Name && x.GroupName == trigger.Group).FirstOrDefault();
           
            if (sTask == null)
            {

                sTask = ScheduleTaskService.Queryable().Where(x => x.TaskName == trigger.JobName && x.GroupName == trigger.JobGroup).FirstOrDefault();

            }
            if (sTask == null)
            {
                logger.LogError($"Group:{trigger.Group},Name:{trigger.Name},can not find task.");
                return;
            }
            sTask.LastRunTime = DateTime.Now;
            this.ScheduleTaskService.AddOrUpdate(sTask);
            logger.LogInformation($"Group:{trigger.Group},Name:{trigger.Name},start,on:{DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss")}");
            var Log = new ScheduleTaskLog()
            {
                ScheduleTaskID = sTask.ID,
                BeginDate = DateTime.Now,
                 
            };
            //this.ScheduleTaskLogService.AddOrUpdate(Log);
            if (string.IsNullOrEmpty(sTask.ClassName))
            {
                this.addLog(Log, $"Group:{trigger.Group},Name:{trigger.Name},can not find class.");
                return;
            }

            try
            {
                var services = App.RootServices.CreateScope().ServiceProvider.GetServices<IJobService>();
               // var services = App.RootServices.GetServices<IJobService>();
                var service = services.Where(a => a.GetType().Name == sTask.ClassName).FirstOrDefault();
                if (service != null)
                {
                    var result = service.Execute(sTask,Log, sTask.Parameter);
                    Log.Result = result;
                    Log.Status = "Complete";
                }
                else
                {
                    throw new Exception($"Can not find {sTask.ClassName}. Please check the code.");
                }


            }
            catch (Exception ex)
            {
                Log.Status = "Error";
                addError(Log,ex);
            }
            Log.EndDate = DateTime.Now;
            try
            {
                this.ScheduleTaskLogService.AddOrUpdate(Log);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
               
            }
            
            return;
        }

        private void addLog(ScheduleTaskLog log, string v)
        {
            log.LogInfo = $@"{(log.LogInfo.IsNullOrEmpty() ? "" : $"{log.LogInfo}\n")}{DateTime.Now}
{v}";
            logger.LogInformation(v);
        }
        private void addError(ScheduleTaskLog log,Exception exception)
        {
            log.ErrorMessage = $@"{(log.ErrorMessage.IsNullOrEmpty() ? "" : $"{log.ErrorMessage}\n")}{DateTime.Now}
Message:{exception.Message}
StackTrace:{exception.StackTrace}";
            logger.LogError(exception,exception.Message);
        }
    }
}
