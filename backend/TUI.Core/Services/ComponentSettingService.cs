using Furion.DependencyInjection;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TUI.Core.Entities;
using TUI.Core.Models;

namespace TUI.Core.Services
{
    public interface IManagerService : IScoped
    {
        List<ComponentSettingNode> GetComponentSettingNodes();
       
    }
    public class ManagerService : IManagerService
    {
        private IService<ComponentSetting> ComponentSettingService;
        public ManagerService(IService<ComponentSetting> ComponentSettingService)
        {
                this.ComponentSettingService = ComponentSettingService;
        }
        public List<ComponentSettingNode> GetComponentSettingNodes()
        {
            var list=this.ComponentSettingService.Queryable().ToList();
            var result = new List<ComponentSettingNode>();
            foreach (var item in list.GroupBy(x=>x.Category))
            {
                var node= new ComponentSettingNode()
                {
                     label = item.Key,
                     value=item.Key,
                     children= item.Select(x=>new ComponentSettingNode() { label=x.Name,value=x.Name}).ToList()
                };
                result.Add(node);
            }
            return result;
        }
        
    }
}
