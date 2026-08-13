using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TUI.Services.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TUI.Services.DBModel
{
    public partial class TUIDbContext
    {
        private List<EntityEntry> addObjectList = new List<EntityEntry>();
        private List<string> AuditEntitySetList = App.SystemSetting.AuditEntitySetList.List;
        public override int SaveChanges()
        {
            this.BeforeSaveChanges(this);
            var result = base.SaveChanges();
            this.AfterSaveChanges();
            return result;
        }

        public void AfterSaveChanges()
        {
            foreach (var item in addObjectList)
            {
                AddAuditData(item, true);
            }
            addObjectList = new List<EntityEntry>();
        }

        private void BeforeSaveChanges(TUIDbContext TUIDbContext)
        {

            TUIDbContext.ChangeTracker.DetectChanges();
            foreach (var entry in TUIDbContext.ChangeTracker.Entries())
            {
                if (entry == null || entry.Entity == null)
                {
                    continue;
                }
                if (this.AuditEntitySetList != null && this.AuditEntitySetList.Any(x => x == entry.Metadata.ClrType.Name))
                {
                    if (entry.State != EntityState.Added)
                    {
                        AddAuditData(entry);
                    }
                    else
                    {
                        this.addObjectList.Add(entry);
                    }

                }

            }
        }

        public void AddAuditData(EntityEntry StateEntry, bool isNew = false)

        {
           
            if (StateEntry == null || StateEntry.Entity == null || StateEntry.Entity is AuditData)
            {
                return;
            }
           
                var entity = StateEntry.Entity;
                string DBName = nameof(TUIDbContext);
                string TableName = entity.GetEntityType().Name;
                var keyValues = this.GetKeys(entity).ToJSON();
                var newObj = new AuditData()
                {
                    AuditType = StateEntry.State.GetHashCode(),
                    DBName = DBName,
                    TableName = TableName,
                    Keys = keyValues,
                    Server = Environment.MachineName,
                    UserID = App.CurrentUser?.ID,
                    LoginName = App.CurrentUser?.UserName,
                    ExecuteTime = DateTime.Now,
                    ExecuteTimeUtc = DateTime.UtcNow,

                };
                if (isNew == false)
                {
                    var changed = GetChanged(StateEntry);

                    newObj.OldValues = changed.Item1.ToJSON();
                    newObj.NewValues = changed.Item2.ToJSON();
                    if (newObj.OldValues == newObj.NewValues)
                    {
                        return;
                    }
                }
                else
                {
                    newObj.AuditType = EntityState.Added.GetHashCode();
                }
            App.dbSqlSugar.Insertable<AuditData>(newObj).ExecuteCommandIdentityIntoEntity();
        }
        private Tuple<Dictionary<string, object>, Dictionary<string, object>> GetChanged(EntityEntry stateEntry)
        {
            var result = new Tuple<Dictionary<string, object>, Dictionary<string, object>>(new Dictionary<string, object>(), new Dictionary<string, object>());
            if (stateEntry.State == EntityState.Deleted)
            {
                for (int i = 0; i < stateEntry.OriginalValues.Properties.Count; i++)
                {
                    var pName = stateEntry.OriginalValues.Properties[i].Name;
                    var oldValue = stateEntry.OriginalValues[pName];
                    result.Item1.Add(pName, oldValue);

                }
            }
            else if (stateEntry.State == EntityState.Modified)
            {
                if (stateEntry.CurrentValues != null && stateEntry.OriginalValues != null)
                {
                    for (int i = 0; i < stateEntry.CurrentValues.Properties.Count; i++)
                    {
                        var pName = stateEntry.CurrentValues.Properties[i].Name;
                        var oldValue = stateEntry.OriginalValues[pName];
                        var newValue = stateEntry.CurrentValues[pName];
                        if ($"{oldValue}" != $"{newValue}")
                        {
                            if (oldValue != null && oldValue.Equals(newValue) == false || newValue != null && newValue.Equals(oldValue) == false)
                            {
                                result.Item1.Add(pName, oldValue);
                                result.Item2.Add(pName, newValue);
                            }

                        }
                    }
                }
            }

            else if (stateEntry.State == EntityState.Added)
            {
                for (int i = 0; i < stateEntry.CurrentValues.Properties.Count; i++)
                {
                    var pName = stateEntry.CurrentValues.Properties[i].Name;
                    var newValue = stateEntry.CurrentValues[pName];
                    result.Item2.Add(pName, newValue);

                }
            }


            return result;
        }

       
       

        
    }
}
