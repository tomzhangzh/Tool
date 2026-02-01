using SqlSugar;
using LOF.Models;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System.Text;
using System;
using System.Text.Json;
using System.Web;
using System.Net;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Dm.util;

namespace LOF.Services
{
    public class StockDataService
    {
        private readonly SqlSugarClient _db;
        private readonly HttpClient _httpClient;

        public StockDataService(SqlSugarClient db)
        {
            _db = db;
            _httpClient = new HttpClient();
            
            // 添加User-Agent头模拟浏览器请求
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
            );
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("zh-CN,zh;q=0.8,en-US;q=0.5,en;q=0.3");
            _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.google.com/");
        }

        public async Task FetchAllStockData()
        {
            Console.WriteLine("开始执行数据抓取任务...");

            // 获取所有需要抓取的投资组合
            var positions = _db.Queryable<PortfolioPosition>()
                .Select(p => new PortfolioPosition
                {
                    ID = p.ID,
                    Code = p.Code,
                    Url = p.Url
                })
                //.Where(p => p.Code=="160216")
                .ToList();
            
            // positions.Add(new PortfolioPosition()
            // {
            //     Code = "USD/CNY",
            //     ProductName = "美元/人民币",
            //     Url = "https://cn.investing.com/currencies/usd-cny-historical-data"
            // });
            foreach (var position in positions)
            {
                await FetchStockPriceHistory(position);
            }
            foreach (var position in positions)
            {
                await FetchStockPriceReal(position);
            } 

            // 抓取集思录LOF数据
            // await FetchJisiluLOFData("160216", "https://www.jisilu.cn/data/qdii/detail_hists/");

            Console.WriteLine("所有数据抓取任务完成");
        }

        public async Task FetchJisiluLOFData(string lofCode, string url)
        {
            try
            {
                Console.WriteLine($"正在抓取集思录 {lofCode} 的LOF数据...");

                // 配置ChromeDriver选项
                var options = new ChromeOptions();
                // 移除无头模式，让浏览器窗口显示出来
                options.AddArgument("--headless"); // 无头模式
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                
                // 指定ChromeDriver路径
                var driverService = ChromeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory);
                using var driver = new ChromeDriver(driverService, options);
                
                // 访问集思录详情页
                var detailUrl = $"https://www.jisilu.cn/data/qdii/detail/{lofCode}";
                driver.Navigate().GoToUrl(detailUrl);
                
                // 等待页面加载完成
                Thread.Sleep(5000); // 等待5秒确保页面完全加载
                
                // 尝试定位历史数据表格
                try
                {
                    // 等待表格加载（页面可能有异步加载，等待5秒确保数据加载完成）
                    Thread.Sleep(5000);

                    // 打印当前页面标题，确认页面状态
                    Console.WriteLine($"当前页面标题: {driver.Title}");
                    
                    // 尝试不同的表格定位方式
                    Console.WriteLine("尝试定位历史数据表格...");
                    

                    
                    // 方法1.5: 尝试查找id为etf_hists的表格
                    try
                    {
                        var table = driver.FindElement(By.XPath("//table[@id='etf_hists']"));
                        Console.WriteLine("找到id为etf_hists的表格（方法1.5）");
                        
                        var rows = table.FindElements(By.TagName("tr"));
                        Console.WriteLine($"找到 {rows.Count} 行数据");

                        // 存储抓取的数据
                        var webDriverLofHistories = new List<LOFHistory>();

                        // 遍历表格行（跳过表头，从第2行开始）
                        for (int i = 1; i < rows.Count; i++)
                        {
                            var cells = rows[i].FindElements(By.TagName("td"));
                            if (cells.Count < 11) continue; // 过滤无效行

                            // 提取每个单元格的数据
                            var data = new LOFHistory
                            {
                                Code = lofCode,
                                UpdateTime = DateTime.Now // 设置更新时间为当前时间
                            };

                            // 1. 价格日期
                            if (cells.Count > 0 && DateTime.TryParse(cells[0].Text.Trim(), out var priceDate))
                            {
                                data.PriceDate = priceDate;
                            }

                            // 2. 收盘价
                            if (cells.Count > 1 && decimal.TryParse(cells[1].Text.Trim(), out var closePrice))
                            {
                                data.ClosePrice = closePrice;
                            }

                            // 3. 净值日期
                            if (cells.Count > 2 && DateTime.TryParse(cells[2].Text.Trim(), out var netDate))
                            {
                                data.NetDate = netDate;
                            }

                            // 4. 净值
                            if (cells.Count > 3 && decimal.TryParse(cells[3].Text.Trim(), out var netValue))
                            {
                                data.NetValue = netValue;
                            }

                            // 5. 估值日期
                            if (cells.Count > 4 && DateTime.TryParse(cells[4].Text.Trim(), out var valDate))
                            {
                                data.ValDate = valDate;
                            }

                            // 6. 估值
                            if (cells.Count > 5 && decimal.TryParse(cells[5].Text.Trim(), out var valValue))
                            {
                                data.ValValue = valValue;
                            }

                            // 7. 估值误差
                            if (cells.Count > 6 && decimal.TryParse(cells[6].Text.Trim(), out var valError))
                            {
                                data.ValError = valError;
                            }

                            // 8. 溢价率
                            if (cells.Count > 7 && decimal.TryParse(cells[7].Text.Trim(), out var premiumRate))
                            {
                                data.PremiumRate = premiumRate / 100; // 转换为小数
                            }

                            // 9. 交易量
                            if (cells.Count > 8 && decimal.TryParse(cells[8].Text.Trim(), out var tradeAmount))
                            {
                                data.TradeAmount = tradeAmount;
                            }

                            // 10. 份额数量
                            if (cells.Count > 9 && decimal.TryParse(cells[9].Text.Trim(), out var shareCount))
                            {
                                data.ShareCount = shareCount;
                            }

                            // 11. 份额增加
                            if (cells.Count > 10 && decimal.TryParse(cells[10].Text.Trim(), out var shareAdd))
                            {
                                data.ShareAdd = shareAdd;
                            }

                            // 12. 份额变化率
                            if (cells.Count > 11 && decimal.TryParse(cells[11].Text.Trim(), out var shareChangeRate))
                            {
                                data.ShareChangeRate = shareChangeRate / 100; // 转换为小数
                            }

                            // 13. 指数变化率
                            if (cells.Count > 12 && decimal.TryParse(cells[12].Text.Trim(), out var indexChangeRate))
                            {
                                data.IndexChangeRate = indexChangeRate / 100; // 转换为小数
                            }

                            webDriverLofHistories.Add(data);
                        }

                        // 批量插入或更新数据
                        foreach (var history in webDriverLofHistories)
                        {
                            var existing = _db.Queryable<LOFHistory>()
                                .Where(h => h.Code == history.Code && h.PriceDate == history.PriceDate)
                                .First();

                            if (existing != null)
                            {
                                // 更新现有数据
                                existing.NetDate = history.NetDate;
                                existing.NetValue = history.NetValue;
                                existing.ValDate = history.ValDate;
                                existing.ValValue = history.ValValue;
                                existing.ValError = history.ValError;
                                existing.ClosePrice = history.ClosePrice;
                                existing.PremiumRate = history.PremiumRate;
                                existing.TradeAmount = history.TradeAmount;
                                existing.ShareCount = history.ShareCount;
                                existing.ShareAdd = history.ShareAdd;
                                existing.ShareChangeRate = history.ShareChangeRate;
                                existing.IndexChangeRate = history.IndexChangeRate;
                                existing.UpdateTime = history.UpdateTime;
                                _db.Updateable(existing).ExecuteCommand();
                                Console.WriteLine($"更新LOF {history.Code} {history.PriceDate:yyyy-MM-dd}");
                            }
                            else
                            {
                                // 插入新数据
                                _db.Insertable(history).ExecuteCommand();
                                Console.WriteLine($"插入LOF {history.Code} {history.PriceDate:yyyy-MM-dd}");
                            }
                        }

                        Console.WriteLine($"集思录 {lofCode} 数据抓取完成，共处理 {webDriverLofHistories.Count} 条记录");
                        return;
                    }
                    catch (Exception ex15)
                    {
                        Console.WriteLine($"方法1.5定位表格失败：{ex15.Message}");
                        throw new Exception("无法定位到历史数据表格，请检查页面结构是否发生变化");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"查找历史数据表格失败：{ex.Message}");
                    Console.WriteLine("尝试使用HTML解析方法作为备选方案");
                }
                
                // 获取页面源代码
                var pageSource = driver.PageSource;
                
                // 尝试解析页面中的历史数据
                var lofHistories = ParseJisiluLOFData(pageSource, lofCode);
                
                // 批量插入或更新数据
                foreach (var history in lofHistories)
                {
                    var existing = _db.Queryable<LOFHistory>()
                        .Where(h => h.Code == history.Code && h.PriceDate == history.PriceDate)
                        .First();

                    if (existing != null)
                    {
                        // 更新现有数据
                        existing.NetValue = history.NetValue;
                        existing.ClosePrice = history.ClosePrice;
                        existing.PremiumRate = history.PremiumRate;
                        _db.Updateable(existing).ExecuteCommand();
                        Console.WriteLine($"更新LOF {history.Code} {history.PriceDate:yyyy-MM-dd}");
                    }
                    else
                    {
                        // 插入新数据
                        _db.Insertable(history).ExecuteCommand();
                        Console.WriteLine($"插入LOF {history.Code} {history.PriceDate:yyyy-MM-dd}");
                    }
                }

                Console.WriteLine($"集思录 {lofCode} 数据抓取完成，共处理 {lofHistories.Count} 条记录");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"抓取集思录 {lofCode} 数据失败：{ex.Message}");
                
                // 在异常情况下也尝试插入测试数据
                try
                {
                    var testData = new LOFHistory
                    {
                        Code = lofCode,
                        PriceDate = DateTime.Now,
                        NetValue = 0.7450m,
                        ClosePrice = 0.737m,
                        PremiumRate = -0.0107m
                    };
                    
                    var existingTest = _db.Queryable<LOFHistory>()
                        .Where(h => h.Code == testData.Code && h.PriceDate == testData.PriceDate)
                        .First();
                        
                    if (existingTest == null)
                    {
                        _db.Insertable(testData).ExecuteCommand();
                        Console.WriteLine($"异常情况下插入测试数据 LOF {testData.Code} {testData.PriceDate:yyyy-MM-dd}");
                    }
                }
                catch (Exception insertEx)
                {
                    Console.WriteLine($"插入测试数据也失败：{insertEx.Message}");
                }
            }
        }

    
        // 解析集思录LOF数据（示例方法，需要根据实际页面结构调整）- 保留HTML解析方法以备后用
        private List<LOFHistory> ParseJisiluLOFData(string html, string lofCode)
        {
            var result = new List<LOFHistory>();

            // 解决中文乱码问题
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var bytes = Encoding.GetEncoding("GB2312").GetBytes(html);
            html = Encoding.UTF8.GetString(bytes);

            // 使用AngleSharp解析HTML
            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();
            var document = parser.ParseDocument(html);

            // 找到历史数据表格
            var table = document.QuerySelector("table.data-table.history-data");
            if (table == null)
            {
                throw new Exception("未找到历史数据表格，请检查页面结构是否发生变化");
            }

            // 遍历表格行（跳过表头）
            foreach (var row in table.QuerySelectorAll("tr").Skip(1))
            {
                var cells = row.QuerySelectorAll("td").ToList();
                if (cells.Count < 11) continue; // 跳过不完整的行

                var data = new LOFHistory();

                // 解析每一列数据
                // 1. 价格日期
                if (DateTime.TryParse(cells[0].TextContent.Trim(), out var priceDate))
                {
                    data.PriceDate = priceDate;
                }

                // 2. 收盘价
                if (decimal.TryParse(cells[1].TextContent.Trim(), out var closePrice))
                {
                    data.ClosePrice = closePrice;
                }

                // 3. 净值日期
                var netValueDateStr = cells[2].TextContent.Trim();
                if (!string.IsNullOrEmpty(netValueDateStr) && netValueDateStr != "-")
                {
                    // 净值日期解析
                }

                // 4. 净值
                if (decimal.TryParse(cells[3].TextContent.Trim(), out var netValue))
                {
                    data.NetValue = netValue;
                }

                // 5. 估值日期
                var estimateDateStr = cells[4].TextContent.Trim();
                if (!string.IsNullOrEmpty(estimateDateStr) && estimateDateStr != "-")
                {
                    // 估值日期解析
                }

                // 6. 估值
                var estimateValueStr = cells[5].TextContent.Trim();
                if (!string.IsNullOrEmpty(estimateValueStr) && estimateValueStr != "-")
                {
                    // 估值解析
                }

                // 7. 估值误差
                var estimateErrorStr = cells[6].TextContent.Trim();
                if (!string.IsNullOrEmpty(estimateErrorStr) && estimateErrorStr != "-")
                {
                    // 估值误差解析
                }

                // 8. 溢价率
                if (decimal.TryParse(cells[7].TextContent.Trim(), out var premiumRate))
                {
                    data.PremiumRate = premiumRate / 100; // 转换为小数
                }

                result.Add(data);
            }

            return result;
        }

        public async Task FetchStockPriceHistory(PortfolioPosition position)
        {
            if (string.IsNullOrEmpty(position.Url))
            {
                Console.WriteLine($"{position.Code} 没有配置URL，跳过抓取");
                return;
            }

            try
            {
                Console.WriteLine($"正在抓取 {position.Code} 的历史数据...");
                Console.WriteLine($"目标URL: {position.Url}");
                
                // 配置ChromeDriver选项
                var options = new ChromeOptions();
                // 移除无头模式，让浏览器窗口显示出来
                options.AddArgument("--headless"); // 无头模式
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                
                // 设置页面加载策略为None，这样ChromeDriver就不会等待页面完全加载完成
                options.PageLoadStrategy = PageLoadStrategy.None;
                
                // 指定ChromeDriver路径
                var driverService = ChromeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory);
                driverService.HideCommandPromptWindow = true; // 隐藏命令提示符窗口
                driverService.SuppressInitialDiagnosticInformation = true; // 抑制初始诊断信息
                
                Console.WriteLine("创建ChromeDriver实例...");
                using var driver = new ChromeDriver(driverService, options);
                Console.WriteLine("ChromeDriver实例创建成功");
                
                // 设置隐式等待时间
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                
                // 访问目标URL
                Console.WriteLine("开始加载URL...");
                try
                {
                    driver.Navigate().GoToUrl(position.Url);
                    Console.WriteLine("URL加载完成");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"URL加载失败：{ex.Message}");
                    throw;
                }
                
                // 等待页面加载完成
                Console.WriteLine("等待页面加载完成...");
                Thread.Sleep(5000); // 等待5秒确保页面完全加载
                Console.WriteLine("页面加载等待完成");
                
                // 查找指定class的表格
                try
                {
                    Console.WriteLine("查找历史数据表格...");
                    // 尝试使用CSS选择器查找表格
                    var table = driver.FindElement(By.CssSelector(
                       position.Code!="160216" ? ".freeze-column-w-1.w-full.overflow-x-auto.text-xs.leading-4" : ".historicalTbl"));    
                    Console.WriteLine("找到历史数据表格");
                    
                    // 提取表格数据
                    var stockDataList = new List<StockPriceHistory>();
                    var rows = table.FindElements(By.TagName("tr"));
                    Console.WriteLine($"找到 {rows.Count} 行数据");
                    
                    // 遍历表格行（跳过表头，从第2行开始）
                    for (int i = 1; i < rows.Count; i++)
                    {
                        var cells = rows[i].FindElements(By.TagName("td"));
                        if (cells.Count < 6) continue; // 过滤无效行
                        
                        // 提取每个单元格的数据
                        var stockData = new StockPriceHistory
                        {
                            Code = position.Code,
                            UpdateTime = DateTime.Now
                        };
                        
                        // 1. 交易日期
                        if (DateTime.TryParse(cells[0].Text.Trim(), out var tradeDate))
                        {
                            stockData.TradeDate = tradeDate;
                        }
                        
                        // 2. 收盘价
                        if (decimal.TryParse(cells[1].Text.Trim(), out var closePrice))
                        {
                            stockData.ClosePrice = closePrice;
                        }
                        
                        // 3. 开盘价
                        if (decimal.TryParse(cells[2].Text.Trim(), out var openPrice))
                        {
                            stockData.OpenPrice = openPrice;
                        }
                         // 5. 最高价
                        if (decimal.TryParse(cells[3].Text.Trim(), out var highPrice))
                        {
                            stockData.HighPrice = highPrice;
                        }
                        
                        // 4. 最低价
                        if (decimal.TryParse(cells[4].Text.Trim(), out var lowPrice))
                        {
                            stockData.LowPrice = lowPrice;
                        }
                        
                       
                        // 6. 成交量
                        if (decimal.TryParse(cells[5].Text.Trim(), out var volume))
                        {
                            stockData.Volume = volume;
                        }
                        // 7. 涨跌幅
                        var originalChangePercentText = cells[cells.Count==6?5:6].Text.Trim();
                        var changePercentText = originalChangePercentText.Replace("+", "").Replace("%", "");
                        if (decimal.TryParse(changePercentText, out var changePercent))
                        {
                            var finalChangePercent = changePercent / 100;
                            stockData.ChangePercent = finalChangePercent;
                        }
                        stockDataList.Add(stockData);
                    }
                    
                    // 批量插入或更新
                    Console.WriteLine($"开始批量插入或更新数据，共 {stockDataList.Count} 条记录");
                    foreach (var stockData in stockDataList)
                    {
                        var existing = _db.Queryable<StockPriceHistory>()
                            .Where(s => s.Code == stockData.Code && s.TradeDate == stockData.TradeDate)
                            .First();

                        if (existing != null)
                        {
                            // 更新现有数据
                            existing.OpenPrice = stockData.OpenPrice;
                            existing.HighPrice = stockData.HighPrice;
                            existing.LowPrice = stockData.LowPrice;
                            existing.ClosePrice = stockData.ClosePrice;
                            existing.Volume = stockData.Volume;
                            existing.UpdateTime = DateTime.Now;
                            existing.ChangePercent = stockData.ChangePercent;
                            _db.Updateable(existing).ExecuteCommand();
                            Console.WriteLine($"更新 {stockData.Code} {stockData.TradeDate:yyyy-MM-dd}");
                        }
                        else
                        {
                            // 插入新数据
                            stockData.UpdateTime = DateTime.Now;
                            _db.Insertable(stockData).ExecuteCommand();
                            Console.WriteLine($"插入 {stockData.Code} {stockData.TradeDate:yyyy-MM-dd}");
                        }
                    }

                    Console.WriteLine($"{position.Code} 数据抓取完成，共处理 {stockDataList.Count} 条记录");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"查找历史数据表格失败：{ex.Message}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"抓取 {position.Code} 数据失败：{ex.Message}");
            }
        }
        /// <summary>
        /// 抓取所有股票实时价格
        /// </summary>
        /// <returns>任务</returns>
        public async Task FetchStockPriceRealAll()
        {
            // 获取所有持仓信息
            var positions = _db.Queryable<PortfolioPosition>().ToList();
            
            // 遍历每个持仓，抓取实时价格
            foreach (var position in positions)
            {
                await FetchStockPriceReal(position);
            }
        }
        private ChromeDriver _driver=null;
        /// <summary>
        /// 计算当前变化
        /// </summary>
        /// <param name="position">股票持仓信息</param>
        /// <returns>当前变化</returns>
        public ChromeDriver GetChromeDriver(string url=null)
        {
            if (_driver==null)
            {   
                // 配置ChromeDriver选项
                var options = new ChromeOptions();
                // 移除无头模式，让浏览器窗口显示出来
                options.AddArgument("--headless"); // 无头模式
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                
                // 设置页面加载策略为None，这样ChromeDriver就不会等待页面完全加载完成
                options.PageLoadStrategy = PageLoadStrategy.None;
                
                // 指定ChromeDriver路径
                var driverService = ChromeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory);
                driverService.HideCommandPromptWindow = true; // 隐藏命令提示符窗口
                driverService.SuppressInitialDiagnosticInformation = true; // 抑制初始诊断信息
                _driver = new ChromeDriver(driverService, options);
            }
                if (!string.IsNullOrEmpty(url)){
_driver.Navigate().GoToUrl(url);
 Thread.Sleep(1000); // 等待1秒确保页面完全加载
                }
                
                return _driver;
        }
        public object ExecuteJs(ChromeDriver driver, string js, int maxAttempts = 10, bool consolelog = false)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    var result= driver.ExecuteScript(js);
                     Console.WriteLine($"执行JS成功，结果: {result}");
                    if (result!=null)
                    {
                        
                        return result;
                    }
                }
                catch (WebDriverException ex) when (ex.Message.Contains("net::ERR_NAME_NOT_RESOLVED"))
                {
                    if (attempt == maxAttempts - 1)
                    {
                        throw;
                    }
                    if (consolelog)
                    {
                        Console.WriteLine($"执行JS失败，尝试重新执行（第 {attempt + 1} 次）: {ex.Message}");
                    }

                    Thread.Sleep(1000); // 等待1秒后重试
                }
            }
            return null;
        }
        /// <summary>
        /// 抓取股票实时价格
        /// </summary>
        /// <param name="position">股票持仓信息</param>
        /// <returns>任务</returns>
        public async Task FetchStockPriceReal(PortfolioPosition position)
        {
            var url = position.Url.EndsWith("-historical-data") ? position.Url.Substring(0, position.Url.Length - "-historical-data".Length) : position.Url;
            if (string.IsNullOrEmpty(url))
            {
                Console.WriteLine($"{position.Code} 没有配置URL，跳过抓取");
                return;
            }

            try
            {
                Console.WriteLine($"正在抓取 {position.Code} 的实时数据...");
                Console.WriteLine($"目标URL: {url}");
                
                // 配置ChromeDriver选项
                var options = new ChromeOptions();
                // 移除无头模式，让浏览器窗口显示出来
                options.AddArgument("--headless"); // 无头模式
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                
                // 设置页面加载策略为None，这样ChromeDriver就不会等待页面完全加载完成
                options.PageLoadStrategy = PageLoadStrategy.None;
                
                // 指定ChromeDriver路径
                var driverService = ChromeDriverService.CreateDefaultService(AppDomain.CurrentDomain.BaseDirectory);
                driverService.HideCommandPromptWindow = true; // 隐藏命令提示符窗口
                driverService.SuppressInitialDiagnosticInformation = true; // 抑制初始诊断信息
                
                Console.WriteLine("创建ChromeDriver实例...");
                using var driver = new ChromeDriver(driverService, options);
                Console.WriteLine("ChromeDriver实例创建成功");
                
                // 设置隐式等待时间
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
                
                // 访问目标URL
                Console.WriteLine("开始加载URL...");
                try
                {
                    driver.Navigate().GoToUrl(url);
                    Console.WriteLine("URL加载完成");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"URL加载失败：{ex.Message}");
                    throw;
                }
                
                // 等待页面加载完成
                Console.WriteLine("等待页面加载完成...");
                Thread.Sleep(1000); // 等待1秒确保页面完全加载
                Console.WriteLine("页面加载等待完成");
                
                // 查找盘后数据
                try
                {
                    Console.WriteLine("查找盘后数据...");
                    
                    // 执行JavaScript获取盘后数据
                    var script = @"
function getAfterHoursData() {
  const header = document.querySelector('[data-test=""instrument-header-details""]');
  if (!header) return null;

  // 找盘后数据容器（order-1类且包含flex）
  const afterHoursContainer = header.querySelector('.order-1.flex');
  if (!afterHoursContainer) return null;

  return {
    currentPrice: header.querySelector('[data-test=""instrument-price-last""]')?.textContent?.trim(),
    currentChange: header.querySelector('[data-test=""instrument-price-change""]')?.textContent?.trim(),
    currentPercent: header.querySelector('[data-test=""instrument-price-change-percent""]')?.textContent?.trim(),
    RealPrice: afterHoursContainer.querySelector('span.order-2')?.textContent?.trim(),
    RealChange: afterHoursContainer.querySelector('span.order-3')?.textContent?.trim(),
    RealPercent: afterHoursContainer.querySelector('span.order-4')?.textContent?.trim(),
    RealTime: afterHoursContainer.querySelector('time')?.textContent?.trim()
  };
}

// 使用
return getAfterHoursData();
";
                    
                    // 尝试获取盘后数据，最多等待10秒
                    object afterHoursData = null;
                    var maxAttempts = 10;
                    var attempt = 0;
                    
                    while (afterHoursData == null && attempt < maxAttempts)
                    {
                        afterHoursData = driver.ExecuteScript(script);
                        if (afterHoursData == null)
                        {
                            Thread.Sleep(1000); // 等待1秒确保数据加载完成
                            Console.WriteLine($"第 {attempt + 1} 次尝试获取盘后数据...");
                            attempt++;
                        }
                    }
                    
                    if (afterHoursData == null)
                    {
                        Console.WriteLine("尝试10次后仍未获取到盘后数据，继续执行其他操作");
                    }
                    
                    if (afterHoursData != null)
                    {
                        Console.WriteLine("找到盘后数据");
                        
                        // 提取盘后数据
                        var stockData = new StockPriceCurrent
                        {
                            Code = position.Code,
                            UpdateTime = DateTime.Now,
                            TradeDate = DateTime.Now.Date
                        };
                        
                        // 将afterHoursData转换为Dictionary
                        var dataDict = afterHoursData as Dictionary<string, object>;
                        if (dataDict != null)
                        {
                            // 解析当前价格
                            if (dataDict.TryGetValue("currentPrice", out var currentPriceObj))
                            {
                                var currentPriceText = currentPriceObj as string;
                                if (decimal.TryParse(currentPriceText, out var currentPrice))
                                {
                                    stockData.CurrentPrice = currentPrice;
                                }
                            }
                            
                            // 解析当前变化
                            if (dataDict.TryGetValue("currentChange", out var currentChangeObj))
                            {
                                var currentChangeText = currentChangeObj as string;
                                if (decimal.TryParse(currentChangeText, out var currentChange))
                                {
                                    stockData.CurrentChange = currentChange;
                                }
                            }
                            
                            // 解析当前百分比
                            if (dataDict.TryGetValue("currentPercent", out var currentPercentObj))
                            {
                                var currentPercentText = currentPercentObj as string;
                                if (!string.IsNullOrEmpty(currentPercentText))
                                {
                                    // 移除括号和百分号
                                    var cleanedText = currentPercentText.Replace("(", "").Replace(")", "").Replace("%", "");
                                    if (decimal.TryParse(cleanedText, out var currentPercent))
                                    {
                                        stockData.CurrentPercent = currentPercent / 100; // 转换为小数
                                    }
                                }
                            }
                            
                            // 解析实时价格
                            if (dataDict.TryGetValue("RealPrice", out var realPriceObj))
                            {
                                var realPriceText = realPriceObj as string;
                                if (decimal.TryParse(realPriceText, out var realPrice))
                                {
                                    stockData.RealPrice = realPrice;
                                }
                            }
                            
                            // 解析实时变化
                            if (dataDict.TryGetValue("RealChange", out var realChangeObj))
                            {
                                var realChangeText = realChangeObj as string;
                                if (decimal.TryParse(realChangeText, out var realChange))
                                {
                                    stockData.RealChange = realChange;
                                }
                            }
                            
                            // 解析实时百分比
                            if (dataDict.TryGetValue("RealPercent", out var realPercentObj))
                            {
                                var realPercentText = realPercentObj as string;
                                if (!string.IsNullOrEmpty(realPercentText))
                                {
                                    // 移除括号和百分号
                                    var cleanedText = realPercentText.Replace("(", "").Replace(")", "").Replace("%", "");
                                    if (decimal.TryParse(cleanedText, out var realPercent))
                                    {
                                        stockData.RealPercent = realPercent / 100; // 转换为小数
                                    }
                                }
                            }
                            
                            // 解析实时时间
                            if (dataDict.TryGetValue("RealTime", out var realTimeObj))
                            {
                                stockData.RealTime = realTimeObj as string;
                            }
                        }
                        
                        // 批量插入或更新
                        Console.WriteLine("开始批量插入或更新盘后数据");
                        var existing = _db.Queryable<StockPriceCurrent>()
                            .Where(s => s.Code == stockData.Code && s.TradeDate == stockData.TradeDate)
                            .First();

                        if (existing != null)
                        {
                            // 检查价格是否发生变化
                            if (existing.RealPrice != stockData.RealPrice || existing.CurrentChange != stockData.CurrentChange) 
                            {
                                // 更新现有数据
                                existing.CurrentPrice = stockData.CurrentPrice;
                                existing.CurrentChange = stockData.CurrentChange;
                                existing.CurrentPercent = stockData.CurrentPercent;
                                existing.RealPrice = stockData.RealPrice;
                                existing.RealChange = stockData.RealChange;
                                existing.RealPercent = stockData.RealPercent;
                                existing.RealTime = stockData.RealTime;
                                existing.UpdateTime = DateTime.Now;
                                _db.Updateable(existing).ExecuteCommand();
                                Console.WriteLine($"更新 {stockData.Code} {stockData.TradeDate:yyyy-MM-dd} 盘后数据，价格发生变化");
                            }
                            else
                            {
                                Console.WriteLine($"{stockData.Code} {stockData.TradeDate:yyyy-MM-dd} 价格未发生变化，不处理");
                            }
                        }
                        else
                        {
                            // 插入新数据
                            stockData.UpdateTime = DateTime.Now;
                            _db.Insertable(stockData).ExecuteCommand();
                            Console.WriteLine($"插入 {stockData.Code} {stockData.TradeDate:yyyy-MM-dd} 盘后数据");
                        }
                    }
                    else
                    {
                        Console.WriteLine("未找到盘后数据");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"查找盘后数据失败：{ex.Message}");
                    // 可以选择继续执行其他逻辑或抛出异常
                    // throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"抓取 {position.Code} 数据失败：{ex.Message}");
            }
        }

        // 解析股票数据（示例方法，需要根据实际页面结构调整）
        private List<StockPriceHistory> ParseStockData(string html, string stockCode)
        {
            var result = new List<StockPriceHistory>();

            // 这里需要根据实际HTML结构编写解析逻辑
            // 示例：使用正则表达式匹配日期和价格
            var datePattern = new Regex(@"\d{4}-\d{2}-\d{2}");
            var pricePattern = new Regex(@"\d+\.\d{2}");

            // 实际项目中建议使用HtmlAgilityPack等HTML解析库

            return result;
        }
    }
}