using SqlSugar;

namespace LOF.Models
{
    /// <summary>
    /// 股票价格历史表
    /// </summary>
    [SugarTable("StockPriceHistory")]
    public class StockPriceHistory
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 股票代码
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 交易日期
        /// </summary>
        public DateTime TradeDate { get; set; }

        /// <summary>
        /// 开盘价
        /// </summary>
        public decimal OpenPrice { get; set; }

        /// <summary>
        /// 最高价
        /// </summary>
        public decimal HighPrice { get; set; }

        /// <summary>
        /// 最低价
        /// </summary>
        public decimal LowPrice { get; set; }

        /// <summary>
        /// 收盘价
        /// </summary>
        public decimal ClosePrice { get; set; }

        /// <summary>
        /// 成交量
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// 涨跌幅
        /// </summary>
        public decimal ChangePercent { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}