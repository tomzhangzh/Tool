using SqlSugar;
using LOF.Models;
using LOF.Services;
using ConsoleTableExt;
using System.Globalization;
using System;
using System.Text;

// 配置数据库连接字符串
var connectionString = "Data Source=LOF.sqlite3;";
Console.OutputEncoding = Encoding.UTF8;
// 创建SqlSugar客户端实例
var db = new SqlSugarClient(new ConnectionConfig
{
    ConnectionString = connectionString,
    DbType = DbType.Sqlite,
    IsAutoCloseConnection = true,
    InitKeyType = InitKeyType.Attribute
});

var consoleService = new ConsoleService(db);
var stockDataService = new StockDataService(db);

try
{
    consoleService.ExecuteArg(args.Length > 0 ? args[0] : null);
}
finally
{
    // 释放资源
    stockDataService.Dispose();
}


public static class ConsoleHelper
{
    public static string CalType => "1";
    /// <summary>
    /// 输出带颜色的数值
    /// </summary>
    /// <param name="value">数值</param>
    /// <param name="format">格式字符串</param>
    public static void WriteColoredValue(decimal value, string format = "P4")
    {
        string formattedValue = value.ToString(format);
        if (value > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(formattedValue.PadLeft(10));
        }
        else if (value < 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(formattedValue.PadLeft(10));
        }
        else
        {
            Console.Write(formattedValue.PadLeft(10));
        }
        Console.ResetColor();
    }
}
