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
            arg="3.a";
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
            else if (arg == "3" || arg == "3.a")
            {
                // 每隔1分钟计算实时报价并输出表格
                Arg3a();
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
            Console.WriteLine("  3.a - 每隔1分钟计算实时报价并输出表格");
            Console.WriteLine("  exit - 退出程序\n");
        }
        
        private async Task<decimal> GetClosePrice()
        {
            // 实现获取收盘价格的逻辑
            return 0.0m;
        }
        
        private void PrintTableHeader(string[] headers, int columnWidth = 13)
        {
            // 打印顶部边框
            Console.WriteLine("+" + string.Join("+", headers.Select(h => new string('-', columnWidth + 2))) + "+");
            
            // 打印表头
            Console.WriteLine("| " + string.Join(" | ", headers.Select(h => PadString(h, columnWidth))) + " |");
            
            // 打印分隔线
            Console.WriteLine("+" + string.Join("+", headers.Select(h => new string('-', columnWidth + 2))) + "+");
        }
        
        private string PadString(string str, int columnWidth)
        {
            // 处理空输入
            if (str == null)
            {
                return new string(' ', columnWidth);
            }

            // 计算字符串显示宽度（全角字符占2位，半角字符占1位）
            int displayWidth = 0;
            foreach (char c in str)
            {
                if (char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter || char.GetUnicodeCategory(c) == UnicodeCategory.Surrogate)
                {
                    displayWidth += 2;
                }
                else
                {
                    displayWidth += 1;
                }
            }

            // 如果显示宽度超过列宽，截断字符串以适配并添加省略号
            if (displayWidth > columnWidth)
            {
                int currentWidth = 0;
                System.Text.StringBuilder truncated = new System.Text.StringBuilder();
                foreach (char c in str)
                {
                    int charWidth = (char.GetUnicodeCategory(c) == UnicodeCategory.OtherLetter || char.GetUnicodeCategory(c) == UnicodeCategory.Surrogate) ? 2 : 1;
                    if (currentWidth + charWidth > columnWidth)
                    {
                        break;
                    }
                    truncated.Append(c);
                    currentWidth += charWidth;
                }

                // 添加省略号表示截断，确保总宽度不超过列宽
                if (truncated.Length < str.Length)
                {
                    // 调整截断内容以容纳省略号（占3个半角字符宽度）
                    while (currentWidth +3 > columnWidth && truncated.Length > 0)
                    {
                        char lastChar = truncated[truncated.Length -1];
                        int charWidth = (char.GetUnicodeCategory(lastChar) == UnicodeCategory.OtherLetter || char.GetUnicodeCategory(lastChar) == UnicodeCategory.Surrogate) ?2 :1;
                        truncated.Remove(truncated.Length-1,1);
                        currentWidth -= charWidth;
                    }
                    str = truncated.ToString() + "...";
                    displayWidth = currentWidth +3;
                }
                else
                {
                    str = truncated.ToString();
                    displayWidth = currentWidth;
                }
            }

            int padCount = columnWidth - displayWidth;

            // 数字和百分比右对齐，其他内容左对齐
            if (decimal.TryParse(str, out _) || double.TryParse(str, out _) || str.Contains('%'))
            {
                return new string(' ', padCount) + str;
            }
            else
            {
                return str + new string(' ', padCount);
            }
        }
        
        private void PrintTableRow(object[] values, int columnWidth = 17)
        {
            // 打印数据行，数字右对齐，文本左对齐
            var formattedValues = values.Select(v => 
            {
                string str;
                
                if (v is decimal d)
                {
                    str = d.ToString("F4");
                }
                else if (v is double db)
                {
                    str = db.ToString("F4");
                }
                else if (v is float f)
                {
                    str = f.ToString("F4");
                }
                // else if (v is int i)
                // {
                //     str = i.ToString();
                // }
                // else if (v is decimal? dNullable && dNullable.HasValue)
                // {
                //     str = dNullable.Value.ToString("F4");
                // }
                // else if (v is double? dbNullable && dbNullable.HasValue)
                // {
                //     str = dbNullable.Value.ToString("F4");
                // }
                // else if (v is float? fNullable && fNullable.HasValue)
                // {
                //     str = fNullable.Value.ToString("F4");
                // }
                // else if (v is int? iNullable && iNullable.HasValue)
                // {
                //     str = iNullable.Value.ToString();
                // }
                else
                {
                    str = $"{v}";
                }
                
                return PadString(str, columnWidth);
            });
            Console.WriteLine("| " + string.Join(" | ", formattedValues) + " |");
        }
        
        public void Arg3a()
        {
            Console.WriteLine($"每月28-30是交割日：\u001b[31m{(DateTime.Today.Day>=26?"注意":"正常")}\u001b[0m");
            Console.WriteLine($"\u001b[31m{(DateTime.Today.Day>=26?"注意注意注意注意注意注意注意注意注意注意注意注意":"正常")}\u001b[0m");
            Console.WriteLine("开始执行实时报价计算任务，每隔1分钟更新一次...");
            
            string[] headers = { "当前时间", "当前估价", "当前报价", "收盘价格", "实时报价对收盘价%", "当前报价对收盘%","买卖信号" };
            PrintTableHeader(headers, 17);
            while (true)
            {
                try
                {
                    
                    // 获取最新的实时价格和收盘价
                    var realTimePrice = valuationService.GetRealTimePrice();
                    var closePrice = valuationService.GetClosePrice();
                    var currentPrice = valuationService.GetCurrentPrice();
                    
                    // 计算百分比
                    decimal realTimeToClosePercent = closePrice > 0 ? (realTimePrice / closePrice - 1) * 100 : 0;
                    decimal currentToClosePercent = closePrice > 0 ? (currentPrice / closePrice - 1) * 100 : 0;
                    
                  
                    
             
                    
                    // 计算买卖信号
                    decimal priceDiffPercent = (realTimePrice - currentPrice) / currentPrice * 100;
                    string signal = "";
                    
                    if (priceDiffPercent >= 1m)
                    {
                        int sellCount = (int)(priceDiffPercent / 0.5m);
                        signal = $"卖[{sellCount}]{priceDiffPercent:F2}%";
                    }
                    else if (priceDiffPercent <= -1m)
                    {
                        int buyCount = (int)(-priceDiffPercent / 0.5m);
                        signal = $"买[{buyCount}]{priceDiffPercent:F2}%";
                    }
                    
                    object[] values = { 
                        DateTime.Now.ToString("HH:mm:ss"),
                        realTimePrice.ToString("F4"),
                        currentPrice.ToString("F4"),
                        closePrice.ToString("F4"),
                        $"{realTimeToClosePercent:F2}%",
                        $"{currentToClosePercent:F2}%",
                        signal
                    };
                    PrintTableRow(values, 17);
                    
                    
                    
                    Thread.Sleep(1000*30);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"实时报价计算任务失败：{ex.Message}");
                }
            }
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




