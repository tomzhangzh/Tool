using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LOF.Models;
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
                var lofHistories =  _db.Queryable<LOFHistory>()
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
                    if (dailyItem.LOFHistory == null && currentDate>=maxDate)
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
                        //// 处理美元/人民币汇率
                        //if (USdToCny.ChangePercent != 0)
                        //{
                        //    // 美元/人民币汇率影响净值涨跌幅，需要调整
                        //    result = result+(decimal)USdToCny.ChangePercent ;
                        //}
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




