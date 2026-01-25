using SqlSugar;

namespace LOF.Models
{
    /// <summary>
    /// 投资组合持仓表
    /// </summary>
    [SugarTable("UsdToCny")]
    public class UsdToCny
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int ID { get; set; }

        /// <summary>
        /// 代码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 产品名称
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// ISIN编码
        /// </summary>
        public string? ISIN { get; set; }

        /// <summary>
        /// 权重
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>
        /// 最新价格
        /// </summary>
        public decimal? LatestPrice { get; set; }

        /// <summary>
        /// 涨跌幅
        /// </summary>
        public decimal? ChangePercent { get; set; }

        /// <summary>
        /// 持仓日期
        /// </summary>
        public DateTime? PositionDate { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// 数据抓取URL
        /// </summary>
        public string? Url { get; set; }
    }
}