using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Dynamic.Core;
namespace TUI.Services.Models
{
    public class PaginationInfo
    {
        public PaginationInfo()
        {
            this.CurrentPage = 1;
            this.PageSize = 10;
            this.MaxSize = 7;
            //this.SortDir = "ASC";

        }
        private int _TotalCount;
        public int TotalCount
        {
            get
            {
                return _TotalCount;
            }
            set
            {
                _TotalCount = value;
                if (this.CurrentPage > this.PageCount)
                {
                    this.CurrentPage = this.PageCount;
                }
            }
        }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int MaxSize { get; set; }
        private string sortName;
        public string SortName
        {
            get
            {
                if (string.IsNullOrEmpty(sortName))
                {
                    return this.DefaultSortName;
                }
                else
                {
                    return sortName;
                }
            }
            set { this.sortName = value; }
        }
        public string SortDir { get; set; }
        // public SqlSugar.OrderByType SortEnum => this.SortDir == "ASC" ? SqlSugar.OrderByType.Asc : SqlSugar.OrderByType.Desc;
        public string OrderFields => $"{sortName} {SortDir}";
        public string DefaultSortName { get; set; }
        public int PageCount
        {
            get
            {
                var totalPageCount = (this.TotalCount / this.PageSize);
                if (this.TotalCount % this.PageSize != 0)
                {
                    totalPageCount++;
                }
                if (totalPageCount == 0) { totalPageCount = 1; }
                return totalPageCount;
            }
        }
        public int Start { get { return (this.CurrentPage - 1) * this.PageSize; } }
        public IQueryable<T> OrderBy<T>(IQueryable<T> query)
        {
            return query.OrderBy(this.SortName + " " + this.SortDir);
        }

        public IQueryable<T> GetQuery<T>(IQueryable<T> query)
        {
            this.TotalCount = query.Count();
            return query.OrderBy(this.SortName + " " + this.SortDir).Skip(this.Start).Take(this.PageSize);
        }

    }
}
