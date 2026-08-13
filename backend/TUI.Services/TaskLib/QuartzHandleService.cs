using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using Quartz.Spi;
using TUI.Services.DBModel;
using TUI.Services.Extension;
using TUI.Services.Repository;

namespace TUI.Services.TaskLib
{
    public class BaseResult
    {
        public int Code { get; set; } = 200;
        public string Msg { get; set; }
    }
    public class JobBriefInfoEntity
    {
        /// <summary>
        /// 任务组名
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 任务信息
        /// </summary>
        public List<JobBriefInfo> JobInfoList { get; set; } = new List<JobBriefInfo>();
    }

    public class JobBriefInfo
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 下次执行时间
        /// </summary>
        public DateTime? NextFireTime { get; set; }

        /// <summary>
        /// 上次执行时间
        /// </summary>
        public DateTime? PreviousFireTime { get; set; }

        ///// <summary>
        ///// 上次执行的异常信息
        ///// </summary>
        //public string LastErrMsg { get; set; }

        /// <summary>
        /// 任务状态
        /// </summary>
        public TriggerState TriggerState { get; set; }

        /// <summary>
        /// 显示状态
        /// </summary>
        public string DisplayState
        {
            get
            {
                var state = string.Empty;
                return this.TriggerState.ToString();
               
            }
        }

        ///// <summary>
        ///// 已经执行次数
        ///// </summary>
        //public long RunNumber { get; set; }
    }
    public interface IQuartzHandleService: IScopeDependency
    {
        void InitJobs();
        Task<BaseResult> Pause(ScheduleTask scheduleTask);

        Task<BaseResult> Resume(ScheduleTask scheduleTask);
        Task<BaseResult> TriggerJob(ScheduleTask scheduleTask);
        Task<List<JobBriefInfoEntity>> GetAllJobBriefInfo();
        Task<JobBriefInfo> GetJobInfo(ScheduleTask scheduleTask);
    }
    public class QuartzHandleService: IQuartzHandleService
    {
        private readonly IService<ScheduleTask> ScheduleTaskService;
        private readonly IService<ScheduleTaskLog> ScheduleTaskLogService;
        private readonly ISchedulerFactory schedulerFactory;
        private ILogger<ClassLibraryJob> logger { get; set; }
        private IJobFactory jobFactory;
        public QuartzHandleService(IService<ScheduleTask> ScheduleTaskService
            , IService<ScheduleTaskLog> ScheduleTaskLogService
            , IJobFactory jobFactory
            , ILogger<ClassLibraryJob> logger
            , ISchedulerFactory schedulerFactory)
        {
            this.ScheduleTaskLogService = ScheduleTaskLogService;
            this.ScheduleTaskService = ScheduleTaskService;
            this.jobFactory = jobFactory;
            this.schedulerFactory = schedulerFactory;
            this.logger = logger;
        }
        public async void InitJobs()
        {

            var jobs = this.ScheduleTaskService.List();
            IScheduler scheduler = await this.schedulerFactory.GetScheduler();
            foreach (var item in jobs)
            {
                var Log = new ScheduleTaskLog()
                {
                    ScheduleTaskID = item.ID,
                    BeginDate = DateTime.Now,

                };
                try
                {
                    IJobDetail job = null;
                    job = JobBuilder.Create<ClassLibraryJob>()
                       .WithIdentity(item.TaskName, item.GroupName)
                       .Build();
                    ITrigger trigger = TriggerBuilder.Create()
                       .WithIdentity(item.TaskName, item.GroupName)
                       .WithDescription(item.Description)
                       .WithCronSchedule(item.Interval)
                       .Build();



                    if (this.jobFactory != null)
                    {
                        scheduler.JobFactory = this.jobFactory;
                    }


                    if (item.Status == 1)
                    {
                        await scheduler.ScheduleJob(job, trigger);
                        this.addLog(Log,  $"Task initialization started successfully:{ item.Status}");
                    }
                    else
                    {
                        await scheduler.ScheduleJob(job, trigger);
                        await Pause(item);
                        this.addLog(Log, $"Task initialization, not started, status:{item.Status}");
              
                    }
                }
                catch (Exception ex)
                {
                    this.addError(Log, ex);
                    continue;
                }
                await scheduler.Start();
            }




        }
        public async Task<BaseResult> TriggerJob(ScheduleTask scheduleTask)
        {

            IScheduler scheduler = await this.schedulerFactory.GetScheduler();
            BaseResult result = new BaseResult();
            var Log = new ScheduleTaskLog()
            {
                ScheduleTaskID = scheduleTask.ID,
                BeginDate = DateTime.Now,

            };
            try
            {
                var jobKey = new JobKey(scheduleTask.TaskName, scheduleTask.GroupName);
                await scheduler.TriggerJob(jobKey);
                addLog(Log, string.Format("Task '{0}' Trigger", scheduleTask.TaskName));
            }

            catch (Exception ex)
            {
                result.Msg = "Failed to Trigger Job!";
                result.Code = 500;
                addError(Log, ex);
            }
            finally
            {
                this.ScheduleTaskLogService.AddOrUpdate(Log);
            }
            return result;
        }
        public async Task<BaseResult> Pause(ScheduleTask scheduleTask)
        {

            IScheduler scheduler = await this.schedulerFactory.GetScheduler();
            BaseResult result = new BaseResult();
            var Log = new ScheduleTaskLog()
            {
                ScheduleTaskID = scheduleTask.ID,
                BeginDate = DateTime.Now,

            };
            try
            {
                var jobKey = new JobKey(scheduleTask.TaskName, scheduleTask.GroupName);
                await scheduler.PauseJob(new JobKey(scheduleTask.TaskName, scheduleTask.GroupName));
                addLog(Log, string.Format("Task '{0}' Pause", scheduleTask.TaskName));
            }

            catch (Exception ex)
            {
                result.Msg = "Failed to Pause task plan!";
                result.Code = 500;
                addError(Log, ex);
            }
            finally
            {
                this.ScheduleTaskLogService.AddOrUpdate(Log);
            }
            return result;
        }
        public async Task<BaseResult> Resume(ScheduleTask scheduleTask)
        {
            IScheduler scheduler = await this.schedulerFactory.GetScheduler();
            BaseResult result = new BaseResult();
            var Log = new ScheduleTaskLog()
            {
                ScheduleTaskID = scheduleTask.ID,
                BeginDate = DateTime.Now,

            };
            try
            {
                //检查任务是否存在
                var jobKey = new JobKey(scheduleTask.TaskName, scheduleTask.GroupName);
                if (await scheduler.CheckExists(jobKey))
                {
                    var jobDetail = await scheduler.GetJobDetail(jobKey);
                    var endTime = jobDetail.JobDataMap.GetString("EndAt");
                    if (!string.IsNullOrWhiteSpace(endTime) && DateTime.Parse(endTime) <= DateTime.Now)
                    {
                        result.Code = 500;
                        result.Msg = "The end time of the job has expired.";
                    }
                    else
                    {
                        //任务已经存在则暂停任务
                        await scheduler.ResumeJob(jobKey);
                        result.Msg = "Resume task plan succeeded!";
                        addLog(Log,string.Format("Task '{0}' resumed", scheduleTask.TaskName));
                    }
                }
                else
                {
                    result.Code = 500;
                    result.Msg = "Task does not exist";
                }
            }
            catch (Exception ex)
            {
                result.Msg = "Failed to Resume task plan!";
                result.Code = 500;
                addError(Log, ex);
            }
            finally
            {
                this.ScheduleTaskLogService.AddOrUpdate(Log);
            }
            return result;
        }
        public async Task<JobBriefInfo> GetJobInfo(ScheduleTask scheduleTask)
        {
            IScheduler scheduler = await this.schedulerFactory.GetScheduler();
            var jobKey = new JobKey(scheduleTask.TaskName, scheduleTask.GroupName);
            var jobDetail = await scheduler.GetJobDetail(jobKey);
            var triggersList = await scheduler.GetTriggersOfJob(jobKey);
            var triggers = triggersList.AsEnumerable().FirstOrDefault();
            return new JobBriefInfo()
            {
                Name = jobKey?.Name,
                TriggerState=await scheduler.GetTriggerState(triggers.Key),
                PreviousFireTime = triggers.GetPreviousFireTimeUtc()?.LocalDateTime,
                NextFireTime = triggers.GetNextFireTimeUtc()?.LocalDateTime,
            };
        }
        public async Task<List<JobBriefInfoEntity>> GetAllJobBriefInfo()
        {
            IScheduler scheduler = await this.schedulerFactory.GetScheduler();
            List<JobKey> jboKeyList = new List<JobKey>();
            List<JobBriefInfoEntity> jobInfoList = new List<JobBriefInfoEntity>();
            var groupNames = await scheduler.GetJobGroupNames();
            foreach (var groupName in groupNames.OrderBy(t => t))
            {
                jboKeyList.AddRange(await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(groupName)));
                jobInfoList.Add(new JobBriefInfoEntity() { GroupName = groupName });
            }
            foreach (var jobKey in jboKeyList.OrderBy(t => t.Name))
            {
                var jobDetail = await scheduler.GetJobDetail(jobKey);
                var triggersList = await scheduler.GetTriggersOfJob(jobKey);
                var triggers = triggersList.AsEnumerable().FirstOrDefault();

                foreach (var jobInfo in jobInfoList)
                {
                    if (jobInfo.GroupName == jobKey.Group)
                    {
                        jobInfo.JobInfoList.Add(new JobBriefInfo()
                        {
                            Name = jobKey.Name,
                            //LastErrMsg = jobDetail?.JobDataMap.GetString("Exception"),
                            TriggerState = await scheduler.GetTriggerState(triggers.Key),
                            PreviousFireTime = triggers.GetPreviousFireTimeUtc()?.LocalDateTime,
                            NextFireTime = triggers.GetNextFireTimeUtc()?.LocalDateTime,
                           //RunNumber = jobDetail?.JobDataMap.GetLong("RunNumber") ?? 0
                        });
                        continue;
                    }
                }
            }
            return jobInfoList;
        }
        private void addLog(ScheduleTaskLog log, string v)
        {
            
            log.LogInfo = $@"{(log.LogInfo.IsNullOrEmpty()?"": $"{log.LogInfo}\n")}{DateTime.Now}
{v}";
            logger.LogInformation(v);
        }
        private void addError(ScheduleTaskLog log, Exception exception)
        {
            log.ErrorMessage = $@"{(log.ErrorMessage.IsNullOrEmpty() ? "" : $"{log.ErrorMessage}\n")}{DateTime.Now}
Message:{exception.Message}
StackTrace:{exception.StackTrace}";
            logger.LogError(exception, exception.Message);
        }
    }
}
