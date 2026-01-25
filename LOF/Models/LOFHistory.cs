using SqlSugar;

namespace LOF.Models
{
    /// <summary>
    /// LOF历史表
    /// </summary>
    [SugarTable("LOFHistory")]
    public class LOFHistory
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// LOF代码
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 价格日期
        /// </summary>
        public DateTime? PriceDate { get; set; }

        /// <summary>
        /// 收盘价
        /// </summary>
        public decimal? ClosePrice { get; set; }

        /// <summary>
        /// 净值日期
        /// </summary>
        public DateTime? NetDate { get; set; }

        /// <summary>
        /// 净值
        /// </summary>
        public decimal? NetValue { get; set; }

        /// <summary>
        /// 估值日期
        /// </summary>
        public DateTime? ValDate { get; set; }

        /// <summary>
        /// 估值
        /// </summary>
        public decimal? ValValue { get; set; }

        /// <summary>
        /// 估值误差
        /// </summary>
        public decimal? ValError { get; set; }

        /// <summary>
        /// 折溢价率
        /// </summary>
        public decimal? PremiumRate { get; set; }

        /// <summary>
        /// 交易量
        /// </summary>
        public decimal? TradeAmount { get; set; }

        /// <summary>
        /// 份额数量
        /// </summary>
        public decimal ShareCount { get; set; }

        /// <summary>
        /// 份额增加
        /// </summary>
        public decimal? ShareAdd { get; set; }

        /// <summary>
        /// 份额变化率
        /// </summary>
        public decimal? ShareChangeRate { get; set; }

        /// <summary>
        /// 指数变化率
        /// </summary>
        public decimal? IndexChangeRate { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }
    }
}