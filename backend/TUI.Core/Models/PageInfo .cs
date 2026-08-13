using Microsoft.CodeAnalysis;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Core.Models
{
    public class PageInfo
    {
        public PageInfo()
        {
         
        }
        public int Limit { get; set; } = 10;
        public int CurrentPage { get; set; } = 1;
        public int  Count { get; set; }

        public string SortDir { get; set; } = "Asc";
        public string SortName { get; set; } = "Id";
        public List<T> GetPageData<T>(ISugarQueryable<T> query)
        {
            if (this.SortName.IsNotNullOrEmpty() == false)
            {
                query = query.OrderBy($"{this.SortName},{this.SortDir}");
            }
            int totlNumber = 0;
            var result = query.ToPageList(this.CurrentPage, this.Limit,ref totlNumber);
            this.Count = totlNumber;
            return result;
        }
        public int Skip { get { return (this.CurrentPage - 1) * this.Limit; } }
    }
}
