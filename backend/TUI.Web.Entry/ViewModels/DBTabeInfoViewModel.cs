using SqlSugar;

namespace TUI.Web.Entry.ViewModels
{
    public class DBTabeInfoViewModel
    {
        public DbTableInfo  Table { get; set; }=new DbTableInfo();
        public List<DbColumnInfo> Columns { get; set; }= new List<DbColumnInfo>();
        public string TableName { get; set; }
    }
}
