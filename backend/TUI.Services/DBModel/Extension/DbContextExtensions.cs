using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TUI.Services.DBModel.Extension
{
    public static class DbContextExtensions
    {

        public static string[] GetEntityKeyNames<TEntity>(this DbContext dbContext) where TEntity : class
        {
            if (dbContext == null)
                throw new ArgumentNullException("dbContext");

            var keyNames = dbContext.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey().Properties.Select(x => x.Name).ToArray();
            return keyNames;
        }
        public static IEnumerable<object> GetEntityKeys<TEntity>(this DbContext dbContext, TEntity entity)
          where TEntity : class
        {
            if (dbContext == null)
                throw new NullReferenceException("dbContext");

            var entry = dbContext.Entry(entity);
            return entry.Metadata.FindPrimaryKey().Properties.Select(p => entry.Property(p.Name).CurrentValue);
        }

        public static IEnumerable<object> GetKeys(this DbContext dbContext, object entity)
        {
            return dbContext.GetEntityKeys(entity);
        }
    }
}
