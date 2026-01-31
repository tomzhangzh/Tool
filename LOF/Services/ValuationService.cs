using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dm;
using LOF.Models;
using Microsoft.Playwright;
using SqlSugar;

namespace LOF.Services
{
    /// <summary>
    /// 估值服务
    /// </summary>
    public class ValuationService
    {
        private readonly SqlSugarClient _db;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="db">数据库客户端</param>
        public ValuationService(SqlSugarClient db)
        {
            _db = db;
        }

        /// <summary>
        /// 计算估值
        /// </summary>
        /// <param name="startDate">开始时间</param>
        /// <param name="endDate">结束时间</param>
        /// <returns>估值结果列表</returns>
        public List<DailyItem> CalculateValuation(DateTime startDate, DateTime endDate)
        {
            var result = new List<DailyItem>();
            try
            {
                Console.WriteLine($"开始计算估值，时间范围：{startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd}");

                // 获取投资组合持仓数据
                var portfolioPositions = _db.Queryable<PortfolioPosition>().ToList();
                Console.WriteLine($"获取到 {portfolioPositions.Count} 条投资组合持仓数据");

                // 获取指定时间范围内的股票价格历史数据
                var stockPriceHistories = _db.Queryable<StockPriceHistory>()
                    .Where(s => s.TradeDate >= startDate && s.TradeDate <= endDate)
                    .OrderBy(s => s.TradeDate)
                    .OrderBy(s => s.Code)
                    .ToList();

                Console.WriteLine($"获取到 {stockPriceHistories.Count} 条股票价格历史数据");

                // 获取LOFHistory数据（不限制时间范围，以便计算净值涨跌幅）
                var lofHistories = _db.Queryable<LOFHistory>()
                    .Where(s => s.PriceDate >= startDate && s.PriceDate <= endDate)
                    .OrderBy(l => l.PriceDate)
                    .ToList();
                Console.WriteLine($"获取到 {lofHistories.Count} 条LOF历史数据");

                // 生成日期列表
                var dateRange = Enumerable.Range(0, (endDate - startDate).Days + 1)
                    .Select(d => startDate.AddDays(d))
                    .ToList();


                foreach (var currentDate in dateRange)
                {

                    var dailyItem = new DailyItem()
                    {
                        Date = currentDate,
                        PortfolioPositions = portfolioPositions,


                    };
                    dailyItem.LOFHistory = lofHistories.Where(l => l.PriceDate == currentDate).FirstOrDefault();
                    dailyItem.PreviousLOFHistory = lofHistories.Where(x => x.PriceDate < currentDate)
                        .OrderByDescending(x => x.PriceDate).FirstOrDefault();
                    dailyItem.StockPriceHistories = stockPriceHistories.Where(x => x.TradeDate == dailyItem.LOFHistory?.NetDate).ToList();
                    //group by trade date
                    var maxDate = stockPriceHistories.Max(x => x.TradeDate);
                    if (dailyItem.LOFHistory == null && currentDate >= maxDate)
                    {
                        var stockPrices = stockPriceHistories
                       .Where(x => x.TradeDate == maxDate).ToList();
                        dailyItem.StockPriceHistories = stockPrices;
                    }
                    //取得上一个LOF净值日期的历史数据
                    dailyItem.PreviousLOFHistory = lofHistories.Where(x => x.NetDate < dailyItem.LOFHistory?.NetDate)
                        .OrderByDescending(x => x.PriceDate).FirstOrDefault();

                    //添加DailyItem到结果列表
                    result.Add(dailyItem);


                }

                Console.WriteLine($"估值计算完成，共处理 {result.Count} 天的数据");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"计算估值失败：{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 估算净值涨跌幅
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>涨跌幅百分比</returns>
        public decimal CalculateNetValueChange(DateTime? date,bool useValue=false)
        {
            if (date == null)
            {
                return 0.0m;
            }
            // 取得所有权重>0的PortfolioPosition记录
            var portfolioPositions = _db.Queryable<PortfolioPosition>()
                .Where(p => p.Weight > 0)
                .ToList();
            
            if (portfolioPositions.Count == 0)
            {
                return 0.0m;
            }
            
            // 遍历每个持仓，计算贡献的涨跌幅
            var totalChange = 0.0m;
            
            foreach (var position in portfolioPositions)
            {
                // 找出TradeDate < date的最大日期
                var latestDate = _db.Queryable<StockPriceHistory>()
                    .Where(s => s.Code == position.Code && s.TradeDate < date)
                    .Max(s => s.TradeDate);
                
                if (latestDate == default(DateTime))
                {
                    continue;
                }
                
                // 找到上一个交易日
                var previousDate = _db.Queryable<StockPriceHistory>()
                    .Where(s => s.Code == position.Code && s.TradeDate < latestDate)
                    .Max(s => s.TradeDate);
                
                if (previousDate == default(DateTime))
                {
                    continue;
                }
                
                // 获取最新日期的涨跌幅
                var latestChangeObj = _db.Queryable<StockPriceHistory>()
                    .Where(s => s.Code == position.Code && s.TradeDate == latestDate)
                    
                    .First();
                if (useValue)
                {
                    var previousDateObj = _db.Queryable<StockPriceHistory>()
                  .Where(s => s.Code == position.Code && s.TradeDate == previousDate).First();
                   // 根据权重计算贡献
                totalChange += (latestChangeObj.ClosePrice- previousDateObj.ClosePrice) / previousDateObj.ClosePrice * (position.Weight / 100);
                }
                else{
// 根据权重计算贡献
                totalChange += latestChangeObj.ChangePercent* (position.Weight / 100);
                }
                
            }
//             var usdToCny = _db.Queryable<StockPriceHistory>()
//                 .Where(l => l.TradeDate == date && l.Code == "USD/CNY")
//                 .First();
//                 if(usdToCny!=null)
//                 {
// totalChange += usdToCny.ChangePercent;
//                 }
            
            return totalChange;
        }
        /// <summary>
        /// 估算净值涨跌幅
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>涨跌幅百分比</returns>
        public decimal CalculateCurrent()
        {
            var findLastNetValue = _db.Queryable<LOFHistory>()
                 .Where(x => x.NetValue != null).OrderByDescending(x => x.NetDate).First();
            if (findLastNetValue == null)
            {
                return 0.0m;
            }
            // 取得所有权重>0的PortfolioPosition记录
            var portfolioPositions = _db.Queryable<PortfolioPosition>()
                .Where(p => p.Weight > 0)
                .ToList();

            if (portfolioPositions.Count == 0)
            {
                return 0.0m;
            }
            var TradeDate=  _db.Queryable<StockPriceHistory>()
            .Where(b=> b.TradeDate < findLastNetValue.NetDate)
                    .Max(s => s.TradeDate);
            var historyList = _db.Queryable<StockPriceHistory>()
            .Where(b => b.TradeDate == TradeDate)
            .ToList();
            var currentList = _db.Queryable<StockPriceCurrent>()
                .OrderByDescending(x => x.UpdateTime)
                .ToList()
                .GroupBy(x => x.Code)
                .Select(g => g.First())
                .ToList();
                
            // 遍历每个持仓，计算贡献的涨跌幅
            var totalChange = 0.0m;

            foreach (var position in portfolioPositions)
            {


                // 获取最新日期的涨跌幅
                var latestChangeObj = currentList
                    .Where(s => s.Code == position.Code)

                    .FirstOrDefault();

                var previousDateObj = historyList
              .Where(s => s.Code == position.Code).First();
                decimal? currentPrice = 0;
                if (latestChangeObj == null || previousDateObj == null)
                {
                    continue;
                }
                currentPrice = latestChangeObj.RealPrice ?? latestChangeObj.CurrentPrice;
                if ((currentPrice ?? 0) == 0) continue;
                if (previousDateObj.ClosePrice == 0) { continue; }

                // 根据权重计算贡献
                totalChange += (currentPrice.Value - previousDateObj.ClosePrice) / previousDateObj.ClosePrice * (position.Weight / 100);

            }
            return totalChange;
        }
        /// <summary>
        /// 每日估值结果
        /// </summary>
        public class DailyItem
        {
            public List<PortfolioPosition> PortfolioPositions { get; set; } = new List<PortfolioPosition>();
            public DateTime? Date { get; set; }
            public LOFHistory LOFHistory { get; set; }= new LOFHistory();
            public List<StockPriceHistory> StockPriceHistories { get; set; } = new List<StockPriceHistory>();
            public LOFHistory? PreviousLOFHistory { get; set; }
            public StockPriceHistory USdToCny { get{
                return StockPriceHistories.Where(x => x.Code == "USD/CNY").FirstOrDefault()?? new StockPriceHistory(){
                    Code = "USD/CNY",
                    ChangePercent = 0,
                };
            } }
            public decimal? EstimatedChangeRate
            {
                get
                {
                    if (this.StockPriceHistories.Count > 0)
                    {
                        decimal result = 0;
                        foreach (var stock in StockPriceHistories)
                        {
                            var weight = this.PortfolioPositions.Where(x => x.Code == stock.Code).FirstOrDefault()?.Weight ?? 0;
                            if (stock.ChangePercent != 0)
                            {
                                result += (decimal)stock.ChangePercent * weight/100;
                            }
                        }
                        // 处理美元/人民币汇率
                        if (USdToCny.ChangePercent != 0)
                        {
                            // 美元/人民币汇率影响净值涨跌幅，需要调整
                            result = result+(decimal)USdToCny.ChangePercent ;
                        }
                        //管理费
                        result=result-0.000041096m;
                        return result;
                    }
                    return null;
                }
            }
            public decimal? EstimatedChangeRate1
            {
                get
                {
                    if (this.StockPriceHistories.Count > 0)
                    {
                        decimal result = 0;
                        foreach (var stock in StockPriceHistories)
                        {
                            // if(stock.Code!="XAG/USD" && stock.Code!="XAU/USD")
                            if(stock.Code!="IAU" && stock.Code!="SLV")
                            {
                                continue;
                            }
                            else
                            {
                                var weight =stock.Code!="SLV"?20m:40m;
                                if (stock.ChangePercent != 0)
                                {
                                    result += (decimal)stock.ChangePercent * weight / 100;
                                }
                            }
                            
                        }
                        // // 处理美元/人民币汇率
                        // if (USdToCny.ChangePercent != 0)
                        // {
                        //     // 美元/人民币汇率影响净值涨跌幅，需要调整
                        //     result = result+(decimal)USdToCny.ChangePercent ;
                        // }
                        // //管理费
                        // result=result-0.000041096m;
                        return result;
                    }
                    return null;
                }
            }
            public decimal? NetValueChangeRate
            {
                get
                {
                    if (PreviousLOFHistory != null && LOFHistory.NetValue != 0)
                    {
                        return (LOFHistory.NetValue - PreviousLOFHistory.NetValue) / PreviousLOFHistory.NetValue;
                    }
                    return null;
                }
            }
        }
    }
}




