//using TUI.Services.DBModel;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace TUI.Services.Manager
//{
//    public interface IAppConfigService
//    {

//    }
//    public class AppConfigService:IAppConfigService,ISingletonDependency
//    {
//        private readonly SqlSugar.ISqlSugarClient dbSqlSugar;
//        public AppConfigService(SqlSugar.ISqlSugarClient dbSqlSugar)
//        {
//            this.dbSqlSugar = dbSqlSugar;
//        }
//        public AppConfig GetAppConfig()
//        {
//            var result = new AppConfig();
//        }
//    }
//}
