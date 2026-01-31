using SqlSugar;
using LOF.Models;
using LOF.Services;
using ConsoleTableExt;
using System.Globalization;
using System;

namespace LOF.Services
{
    /// <summary>
    /// 估值服务
    /// </summary>
    public class ConsoleService
    {
        private readonly SqlSugarClient _db;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">数据库客户端</param>
        public ConsoleService(SqlSugarClient db)
        {
            _db = db;
            stockDataService = new StockDataService(_db);
            valuationService = new ValuationService(_db);
        }
        public StockDataService stockDataService = null;
        public ValuationService valuationService = null;
        public async Task ExecuteArg(string arg)
        {
            if (string.IsNullOrEmpty(arg))
            {
                // 显示帮助信息
                ShowHelp();
                
                // 接受用户输入
                Console.Write("请输入要执行的命令: ");
                arg = Console.ReadLine()?.Trim() ?? "";
            }

            if (arg == "1")
            {
                // 执行数据抓取
                await Arg1();
            }
            else if (arg == "0")
            {
                // 只更新实时价格
                await Arg0();
            }
            else if (arg == "a")
            {
                // 循环更新实时价格，每5分钟一次
                await ArgA();
            }
            else if (arg == "2")
            {
                // 循环更新实时价格，每5分钟一次
                await Arg2();
            }
            else
            {
                Console.WriteLine($"未知命令: {arg}");
                ShowHelp();
            }
        }
        
        private void ShowHelp()
        {
            Console.WriteLine("\n可用命令:");
            Console.WriteLine("  1 - 执行完整数据抓取");
            Console.WriteLine("  0 - 只更新实时价格");
            Console.WriteLine("  a - 自动循环更新实时价格（每2分钟一次）");
            Console.WriteLine("  2 - 执行估值计算");
            Console.WriteLine("  exit - 退出程序\n");
        }
        /// <summary>
        /// 执行数据抓取任务
        /// </summary>
        public async Task Arg1()
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
        public async Task Arg0()
        {
            // 只更新实时价格
            try
            {
                Console.WriteLine("开始执行实时价格更新任务...");

                // 执行实时价格更新
                await stockDataService.FetchStockPriceRealAll();

                Console.WriteLine("实时价格更新任务完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine("实时价格更新任务失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 循环更新实时价格，每5分钟一次
        /// </summary>
        public async Task ArgA()
        {
            try
            {
                Console.WriteLine("\x1b[1;32m[开始执行循环实时价格更新任务...\x1b[0m");
                while (true)
                {
                    Console.WriteLine($"\x1b[1;32m[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 实时数据更新开始\x1b[0m");
                    await stockDataService.FetchStockPriceRealAll();
                    Console.WriteLine($"\x1b[1;32m[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 实时数据更新完成，2分钟后再次更新...\x1b[0m");
                    await Task.Delay(TimeSpan.FromMinutes(2));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("循环实时价格更新任务失败：" + ex.Message);
            }
        }
        /// <summary>
        /// 执行估值计算任务
        /// </summary>
        public async Task Arg2()
        {
            try
            {


                Console.WriteLine("开始执行估值计算任务...");


                // 设置默认时间范围（从明天往前推30天）
                var endDate = DateTime.Today.AddDays(1);
                var startDate = endDate.AddDays(-30);

                // 执行估值计算
                var valuationResults = valuationService.CalculateValuation(startDate, endDate);

                // 输出估值结果
                Console.WriteLine("\n估值计算结果：");

                // 直接输出结果，使用控制台颜色
                Console.WriteLine("+------------+------------+----------+----------+------------+------------+---------+------------+");
                Console.WriteLine("| 当前日期   | 净值日期   | 收盘价   | 净值     | 净值涨跌   | 估值涨跌   | 估算净值| 估算百分比   |估算按照价格   |");
                Console.WriteLine("+------------+------------+----------+----------+------------+------------+---------+------------+");

                foreach (var item in valuationResults)
                {
                    string date = item.Date?.ToString("yyyy-MM-dd") ?? "-";
                    string netDate = item.LOFHistory != null && item.LOFHistory.NetDate != null ? item.LOFHistory.NetDate.Value.ToString("yyyy-MM-dd") : "-";
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
                    string formattedCode = ((1 + estimatedChange) * item.PreviousLOFHistory?.NetValue ?? 0).ToString("F4").PadLeft(7);
                    string EstimatedChangeRate1 = (item.EstimatedChangeRate1 ?? 0).ToString("P4").PadLeft(10).PadLeft(7);

                    // 输出固定部分
                    Console.Write($"| {formattedDate} | {formattedNetDate} | {formattedClosePrice} | {formattedNetValue} | ");

                    // 输出净值涨跌（带颜色）
                    ConsoleHelper.WriteColoredValue(netValueChange, "P4");
                    Console.Write(" | ");

                    // 输出估值涨跌（带颜色）
                    ConsoleHelper.WriteColoredValue(estimatedChange, "P4");

                    // 输出剩余部分
                    Console.WriteLine($" | {formattedCode} | {valuationService.CalculateNetValueChange(item.Date).ToString("P4").PadLeft(10)}| {valuationService.CalculateNetValueChange(item.Date, true).ToString("P4").PadLeft(10)}");
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
                    decimal latestNetValue = valuationResults.Where(x => x.LOFHistory != null
                    && (x.LOFHistory.ValValue > 0)).Last().LOFHistory.NetValue.Value;
                    decimal estimatedChange = lastItem.EstimatedChangeRate ?? 0;
                    decimal estimatedNetValue = (1 + estimatedChange) * latestNetValue;
                    var 实时百分比 = valuationService.CalculateCurrent();
                    Console.WriteLine($"\x1b[1m\x1b[31m最后收盘价:     {lastClosePrice.ToString("F4")}\x1b[0m");
                    Console.WriteLine($"最新估值:     \u001b[31m   {estimatedNetValue.ToString("F4")} 根据上次交易日 最有可能今天的收盘价 \u001b[0m");
                    Console.WriteLine($"最新估值%:       {valuationService.CalculateNetValueChange(DateTime.Today).ToString("P4")} 根据上次交易日");
                    Console.WriteLine($"最后净值:       {latestNetValue.ToString("F4")} 别人看到的");
                    Console.WriteLine($"实时估值:     \u001b[31m{((1 + 实时百分比) * latestNetValue).ToString("F4")} ({实时百分比.ToString("P4")})\u001b[0m  ");
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
                    decimal premiumRate = (lastClosePrice > 0) ? (lastClosePrice / estimatedNetValue - 1) : 0;
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

                    // 显示最新报价
                    // decimal lastClosePrice = lastCloseItem.LOFHistory?.ClosePrice ?? 0;
                    // decimal latestNetValue = valuationResults.Where(x=>x.LOFHistory!=null
                    // && (x.LOFHistory.ValValue>0)).Last().LOFHistory.NetValue.Value;
                    // decimal estimatedChange = lastItem.EstimatedChangeRate ?? 0;
                    // decimal estimatedNetValue = (1 + estimatedChange) * latestNetValue;
                    // var 实时百分比 = valuationService.CalculateCurrent();


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
                    #region 最新估值信息
                    // 输出表格标题
                    Console.WriteLine("\x1b[1m最新估值信息:\x1b[0m");
                    Console.WriteLine("+---------------+---------------+---------------+---------------+---------------+");
                    Console.WriteLine("|   最后收盘价  |   最后净值    |   最新估值    |   实时估值    |  实时估值%   |");
                    Console.WriteLine("+---------------+---------------+---------------+---------------+---------------+");

                    // 输出数据行
                    var realTimeValue = ((1 + 实时百分比) * latestNetValue);
                    var realTimePercent = 实时百分比.ToString("P4");

                    // 根据实时百分比设置颜色
                    var colorCode = 实时百分比 > 0 ? "1;31m" : "1;32m";

                    Console.WriteLine($"| {lastClosePrice.ToString("F4").PadLeft(13)} | {latestNetValue.ToString("F4").PadLeft(13)} | {estimatedNetValue.ToString("F4").PadLeft(13)} | \x1b[{colorCode}{realTimeValue.ToString("F4").PadLeft(13)}\x1b[0m | \x1b[{colorCode}{realTimePercent.PadLeft(13)}\x1b[0m |");
                    Console.WriteLine("+---------------+---------------+---------------+---------------+---------------+");
                    #endregion
                }

                // 输出默认时间范围信息
                Console.WriteLine($"\n默认时间范围（从明天往前推30天）：{startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd}");


            }
            catch (Exception ex)
            {
                Console.WriteLine("估值计算任务失败：" + ex.Message);
            }

        }
    }
}




