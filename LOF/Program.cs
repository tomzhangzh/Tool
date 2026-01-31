using SqlSugar;
using LOF.Models;
using LOF.Services;
using ConsoleTableExt;

// 配置数据库连接字符串
var connectionString = "Data Source=LOF.sqlite3;";

// 创建SqlSugar客户端实例
var db = new SqlSugarClient(new ConnectionConfig
{
    ConnectionString = connectionString,
    DbType = DbType.Sqlite,
    IsAutoCloseConnection = true,
    InitKeyType = InitKeyType.Attribute
});

// 创建数据抓取服务
var stockDataService = new StockDataService(db);
// args = new string[] { "1" };
// 主程序入口
if (args.Length > 0 && args[0] == "1")
{
    // 执行数据抓取
    try
    {
        Console.WriteLine("开始执行数据抓取任务...");

        // 执行数据抓取
        await stockDataService.FetchAllStockData();
        
        // 抓取集思录LOF数据
        await stockDataService.FetchJisiluLOFData("160216", "https://www.jisilu.cn/data/qdii/detail_hists/");

        Console.WriteLine("所有数据抓取任务完成");
    }
    catch (Exception ex)
    {
        Console.WriteLine("数据抓取任务失败：" + ex.Message);
    }
}
else 
{
    // 执行估值计算
    try
    {
        Console.WriteLine("开始执行估值计算任务...");

        // 创建估值服务
        var valuationService = new ValuationService(db);

        // 设置默认时间范围（从明天往前推30天）
        var endDate = DateTime.Today.AddDays(1);
        var startDate = endDate.AddDays(-30);

        // 执行估值计算
        var valuationResults = valuationService.CalculateValuation(startDate, endDate);

        // 输出估值结果
        Console.WriteLine("\n估值计算结果：");

        // 直接输出结果，使用控制台颜色
        Console.WriteLine("+------------+------------+----------+----------+------------+------------+---------+------------+");
        Console.WriteLine("| 当前日期   | 净值日期   | 收盘价   | 净值     | 净值涨跌   | 估值涨跌   | 估算净值| 只算金银   |");
        Console.WriteLine("+------------+------------+----------+----------+------------+------------+---------+------------+");
        
        foreach (var item in valuationResults)
        {
            string date = item.Date?.ToString("yyyy-MM-dd") ?? "-";
            string netDate = item.LOFHistory != null && item.LOFHistory.NetDate !=null ? item.LOFHistory.NetDate.Value.ToString("yyyy-MM-dd") : "-";
            string closePrice = (item.LOFHistory?.ClosePrice ?? 0).ToString("F4");
            string netValue = (item.LOFHistory?.NetValue ?? 0).ToString("F4");
            decimal netValueChange = item.NetValueChangeRate ?? 0;
            decimal estimatedChange = item.EstimatedChangeRate ?? 0;
            string code = item.LOFHistory?.Code ?? "-";
            string premiumRate = (item.LOFHistory?.PremiumRate ?? 0).ToString("P4");
            
            // 格式化每列，确保宽度一致
            string formattedDate = date.PadRight(10);
            string formattedNetDate = netDate.PadRight(10);
            string formattedClosePrice = closePrice.PadLeft(8);
            string formattedNetValue = netValue.PadLeft(8);
            string formattedNetValueChange = netValueChange.ToString("P4").PadLeft(10);
            string formattedEstimatedChange = estimatedChange.ToString("P4").PadLeft(10);
            string formattedCode = ((1+estimatedChange)*item.PreviousLOFHistory?.NetValue??0).ToString("F4").PadLeft(7);
            string EstimatedChangeRate1 = (item.EstimatedChangeRate1??0).ToString("P4").PadLeft(10).PadLeft(7);
            
            // 输出固定部分
            Console.Write($"| {formattedDate} | {formattedNetDate} | {formattedClosePrice} | {formattedNetValue} | ");
            
            // 输出净值涨跌（带颜色）
            ConsoleHelper.WriteColoredValue(netValueChange, "P4");
            Console.Write(" | ");
            
            // 输出估值涨跌（带颜色）
            ConsoleHelper.WriteColoredValue(estimatedChange, "P4");
            
            // 输出剩余部分
            Console.WriteLine($" | {formattedCode} | {EstimatedChangeRate1} |");
        }
        
        Console.WriteLine("+------------+------------+----------+----------+------------+------------+---------+------------+");
        
        // 注释掉原来的表格输出代码
        /*
        var tableData = valuationResults.Select(item => new object[]
        {
            item.Date?.ToString("yyyy-MM-dd") ?? "-",
            item.LOFHistory != null ? item.LOFHistory.NetDate.ToString("yyyy-MM-dd") : "-",
            (item.LOFHistory?.ClosePrice ?? 0).ToString("F4"),
            (item.LOFHistory?.NetValue ?? 0).ToString("F4"),
            (item.NetValueChangeRate ?? 0).ToString("P4"),
            (item.EstimatedChangeRate ?? 0).ToString("P4"),
            item.LOFHistory?.Code ?? "-",
            (item.LOFHistory?.PremiumRate ?? 0).ToString("P4")
        }).ToList();

        // 使用ConsoleTableExt输出表格
        ConsoleTableBuilder
            .From(tableData)
            .WithColumn("当前日期", "净值日期", "收盘价", "净值", "净值涨跌", "估值涨跌", "LOF代码", "折溢价率")
            .WithFormat(ConsoleTableBuilderFormat.Alternative)
            .ExportAndWriteLine();
        */

        Console.WriteLine($"共 {valuationResults.Count} 天的估值计算完成");
        
        // 显示估值范围
        var validItems = valuationResults.Where(x => x.LOFHistory != null && x.LOFHistory.NetValue != null).ToList();
        if (validItems.Any())
        {
            var lastItem = valuationResults.Last();
            var lastCloseItem = valuationResults.Where(x => x.LOFHistory != null && x.LOFHistory.ClosePrice != null).Last();
            
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("**************");
            Console.WriteLine("");
            
            // 显示最新报价
            decimal lastClosePrice = lastCloseItem.LOFHistory?.ClosePrice ?? 0;
            decimal latestNetValue = valuationResults.Where(x=>x.LOFHistory!=null
            && (x.LOFHistory.ValValue>0)).Last().LOFHistory.NetValue.Value;
            decimal estimatedChange = lastItem.EstimatedChangeRate ?? 0;
            decimal estimatedNetValue = (1 + estimatedChange) * latestNetValue;
            
            Console.WriteLine($"最后收盘价:     {lastClosePrice.ToString("F4")}");
            Console.WriteLine($"最新估值:       {estimatedNetValue.ToString("F4")}");
            Console.WriteLine($"最后净值:       {latestNetValue.ToString("F4")}");
            
            // 计算相对最后净值的百分比
            decimal relativeToLastNetValue = (latestNetValue > 0) ? (estimatedNetValue / latestNetValue - 1) : 0;
            Console.Write("相对最后净值:   ");
            if (relativeToLastNetValue > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{relativeToLastNetValue.ToString("P4")}");
            }
            else if (relativeToLastNetValue < 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{relativeToLastNetValue.ToString("P4")}");
            }
            else
            {
                Console.WriteLine($"{relativeToLastNetValue.ToString("P4")}");
            }
            Console.ResetColor();
            
            // 计算相对收盘价的百分比
            decimal premiumRate = (lastClosePrice > 0) ? (estimatedNetValue / lastClosePrice - 1) : 0;
            Console.Write("相对收盘价:     ");
            if (premiumRate > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{premiumRate.ToString("P4")}");
            }
            else if (premiumRate < 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{premiumRate.ToString("P4")}");
            }
            else
            {
                Console.WriteLine($"{premiumRate.ToString("P4")}");
            }
            Console.ResetColor();
            Console.WriteLine("");
            
            // 使用表格显示估值范围（从-2.5%到2.5%，步距0.5%）
            decimal minChange = -0.025m; // -2.5%
            decimal maxChange = 0.025m;  // 2.5%
            decimal step = 0.005m;       // 0.5%步距
            
            // 准备表格数据
            var rangeData = new List<object[]>();
            for (decimal change = minChange; change <= maxChange; change += step)
            {
                decimal rangeValue = (1 + estimatedChange + change) * latestNetValue;
                decimal relativeChange = (rangeValue / lastClosePrice - 1);
                string changeStr = change.ToString("P2");
                string valueStr = rangeValue.ToString("F4");
                
                // 为相对收盘价涨跌幅添加颜色
                string relativeChangeStr = relativeChange.ToString("P4");
                if (relativeChange > 0)
                {
                    relativeChangeStr = $"\u001b[31m{relativeChangeStr}\u001b[0m"; // 红色
                }
                else if (relativeChange < 0)
                {
                    relativeChangeStr = $"\u001b[32m{relativeChangeStr}\u001b[0m"; // 绿色
                }
                
                rangeData.Add(new object[] { changeStr, valueStr, relativeChangeStr });
            }
            
            // 使用ConsoleTableExt输出表格
            Console.WriteLine("估值范围:");
            ConsoleTableBuilder
                .From(rangeData)
                .WithColumn("变化率", "估算净值", "相对收盘价涨跌幅")
                .WithFormat(ConsoleTableBuilderFormat.Alternative)
                .ExportAndWriteLine();
            
            Console.WriteLine("");
            Console.WriteLine("**************");
            Console.ResetColor();
        }
        
        // 输出默认时间范围信息
        Console.WriteLine($"\n默认时间范围（从明天往前推30天）：{startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd}");
    }
    catch (Exception ex)
    {
        Console.WriteLine("估值计算任务失败：" + ex.Message);
    }
}
//else
//{
//    // 测试数据库连接
//    try
//    {
//        db.Ado.ExecuteCommand("SELECT 1");
//        Console.WriteLine("数据库连接成功！");

//        // // 查询所有表
//        // var tables = db.DbMaintenance.GetTableInfoList();
//        // Console.WriteLine($"共发现 {tables.Count} 张表：");
//        // foreach (var table in tables)
//        // {
//        //     Console.WriteLine($"- {table.Name}");
//        // }
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine("数据库连接失败：" + ex.Message);
//    }
//}

public static class ConsoleHelper
{
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
