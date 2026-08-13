
/// <summary>
/// JsonExtension类
/// </summary>
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Utils.Extensions
{
    public static class DynamicExtension
    {
        /// <summary>
        /// 去除字典中的properties，有个参数忽略大小写
        /// </summary>
        /// <param name="dict">字典</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns>新字典</returns>
        public static Dictionary<string, object> RemoveProperties(this Dictionary<string, object> dict, List<string> properties, bool ignoreCase = false)
        {
            var newDict = new Dictionary<string, object>(ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach (var kvp in dict)
            {
                if (!properties.Contains(kvp.Key, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal))
                {
                    newDict.Add(kvp.Key, kvp.Value);
                }
            }
            return newDict;
        }
         /// <summary>
        /// 去除字典中的ID
        /// </summary>
        /// <param name="dict">字典</param>
        /// <returns>新字典</returns>
        public static Dictionary<string, object> RemoveId(this Dictionary<string, object> dict)
        {
           return dict.RemoveProperties(new List<string> { "Id" },true);
        }
    }

    
       
    }



