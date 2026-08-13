using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.WebPortal
{
    public class Program
    {
        public static void Main(string[] args)
        {
           // try
           // {
           //     var configuration = new ConfigurationBuilder()
           //.SetBasePath(Directory.GetCurrentDirectory())
           //.AddJsonFile("appsettings.json")
           //.Build();
           //     Log.Logger = new LoggerConfiguration()
           //    .WriteTo.MSSqlServer(configuration.GetConnectionString("TUISession"), sinkOptions: new MSSqlServerSinkOptions { TableName = "LogEvents", AutoCreateSqlTable = true }
           //    ).CreateLogger();
                CreateHostBuilder(args).Build().Run();
            //}
            //catch (Exception ex)
            //{
            //    Log.Fatal(ex, "Get Error");
            //}
            //finally
            //{
            //    Log.CloseAndFlush();
            //}
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                }).UseSerilog((context, con) =>
             {
                 string date = DateTime.Now.ToString("yyyy-MM-dd");//��ʱ�䴴���ļ���
                 string outputTemplate = "{NewLine}��{Level:u3}��{Timestamp:yyyy-MM-dd HH:mm:ss.fff}" +
                 "{NewLine}#Msg#{Message:lj}" +
                 "{NewLine}#Pro #{Properties:j}" +
                 "{NewLine}#Exc#{Exception}" +
                 new string('-', 50);//���ģ��
                 // ���������ļ�
                 var config = con
                 .ReadFrom.Configuration(context.Configuration)
                 .Enrich.FromLogContext();

                 // �ж��Ƿ����������
                 var hasWriteTo = context.Configuration["Serilog:WriteTo:0:Name"];
                 if (hasWriteTo == null)
                 {
                     var columnOption = new ColumnOptions();
                     columnOption.Store.Remove(StandardColumn.MessageTemplate);
                     config.WriteTo.Console(outputTemplate: outputTemplate)
                     #region 2.��LogEventLevel.�����������/���ļ�

        ///2.1����� LogEventLevel.Debug ����
        .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(evt => evt.Level < LogEventLevel.Error)//ɸѡ����
            .WriteTo.File($"{AppContext.BaseDirectory}/logs/{date}/{LogEventLevel.Debug}.log",
                outputTemplate: outputTemplate,
                //rollingInterval: RollingInterval.Day,//��־���ձ��棬���������ļ����ƺ��Զ��������ں�׺
                encoding: Encoding.UTF8            // �ļ��ַ�����
             )
         )

        ///2.2����� LogEventLevel.Error ����
        .WriteTo.Logger(lg => lg.Filter.ByIncludingOnly(evt => evt.Level >= LogEventLevel.Error)//ɸѡ����
            .WriteTo.File($"{AppContext.BaseDirectory}/logs/{date}/{LogEventLevel.Error}.log",
                outputTemplate: outputTemplate,
                //rollingInterval: RollingInterval.Day,//��־���ձ��棬���������ļ����ƺ��Զ��������ں�׺
                encoding: Encoding.UTF8            // �ļ��ַ�����
             )
         ).WriteTo.MSSqlServer(context.Configuration.GetConnectionString("TUIDatabase"),restrictedToMinimumLevel:LogEventLevel.Error, sinkOptions: new MSSqlServerSinkOptions { TableName = "LogEvents", AutoCreateSqlTable = true });

                     #endregion ��LogEventLevel ��������/���ļ�

                 }
             });
    }
}
