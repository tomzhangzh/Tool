using Furion.ConfigurableOptions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TUI.Core.Entities;
using TUI.Core.Services;
using TUI.Utils.Extensions;

namespace TUI.Core;

public class AppEx
{
    public static User CurrentUser
    {
        get
        {
            return App.HttpContext?.Session.GetValue<User>(CommonConst.CURRENT_USER_KEY);
        }
        set
        {
            if (App.HttpContext != null && App.HttpContext.Session != null)
            {
                App.HttpContext?.Session.SetValue(CommonConst.CURRENT_USER_KEY, value);
            }
        }
    }
    public static List<string> Custom_Script
    {
        get
        {
            return App.HttpContext?.Session.GetValue<List<string>>(CommonConst.CUSTOM_SCRIPTS);
        }
        set
        {
            if (App.HttpContext != null && App.HttpContext.Session != null)
            {
                App.HttpContext?.Session.SetValue(CommonConst.CUSTOM_SCRIPTS, value);
            }
        }
    }
    public static SqlSugar.ISqlSugarClient dbSqlSugar => App.RootServices.GetService<SqlSugar.ISqlSugarClient>();
    public static IManagerService ManagerService => App.GetRequiredService<IManagerService>();
    public static WebsiteOptions WebsiteOptions => GetOptions<WebsiteOptions>();
    public static TOptions GetOptions<TOptions>() where TOptions : IConfigurableOptions
    {
        var path = typeof(TOptions).Name.TrimEnd("Options");
       var result= App.GetConfig<TOptions>(path, true);
        return result;
    }
}
