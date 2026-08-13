using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Core.Models
{
    public class SummaryPageInfo<TFilter, TSummary>: BasePageInfo where TFilter : class, new()
    {
        public TFilter Filter { get; set; }= new TFilter();
        public PageInfo PageInfo { get; set; }= new PageInfo();
        public List<TSummary> SummaryData { get; set; }= new List<TSummary>();

    }
    public class SummaryPageInfo<TFilter> : BasePageInfo where TFilter : class, new()
    {
        public TFilter Filter { get; set; }=new TFilter();
        public PageInfo PageInfo { get; set; } = new PageInfo();

    }
}
