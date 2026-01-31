using SqlSugar;

namespace LOF.Models
{
    /// <summary>
    /// 股票价格历史表
    /// </summary>
    [SugarTable("StockPriceCurrent")]
    public class StockPriceCurrent
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
        /// 当前价格
        /// </summary>
        public decimal? CurrentPrice { get; set; }

        /// <summary>
        /// 当前变化
        /// </summary>
        public decimal? CurrentChange { get; set; }

        /// <summary>
        /// 当前百分比
        /// </summary>
        public decimal? CurrentPercent { get; set; }

        /// <summary>
        /// 实时价格
        /// </summary>
        public decimal? RealPrice { get; set; }

        /// <summary>
        /// 实时变化
        /// </summary>
        public decimal? RealChange { get; set; }

        /// <summary>
        /// 实时百分比
        /// </summary>
        public decimal? RealPercent { get; set; }

        /// <summary>
        /// 实时时间
        /// </summary>
        public decimal? RealTime { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// 涨跌幅
        /// </summary>
        public decimal? ChangePercent { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}