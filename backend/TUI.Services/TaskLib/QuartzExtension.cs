using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using TUI.Services.Manager;
using System;
using System.Collections.Generic;
using System.Text;

namespace TUI.Services.TaskLib
{
    public static class QuartzExtension
    {
        public static IServiceCollection AddQuartz(this IServiceCollection services)
        {
          
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<IJobFactory, ASPDIJobFactory>();
            services.AddScoped<IQuartzHandleService, QuartzHandleService>();
            services.AddScoped<ClassLibraryJob>();
            return services;

        }
        public static IApplicationBuilder UseQuartz(this IApplicationBuilder builder)
        {
            IServiceProvider services = builder.ApplicationServices;
            using (var serviceScope = services.CreateScope())
            {

                var dd = serviceScope.ServiceProvider.GetService<IQuartzHandleService>();
                dd.InitJobs();
            }

            return builder;
        }
        
    }
}
