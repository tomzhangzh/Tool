
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
    public static class JsonExtension
    {
        /// <summary>
        /// 将json字符串转换为指定类型的对象
        /// </summary>
        /// <typeparam name="T">指定类型</typeparam>
        /// <param name="json">json字符串</param>
        /// <returns>指定类型的对象</returns>
        public static T? FromJson<T>(this string json)
        {
            json = (json ?? "").Replace("&nbsp;", "");
            return json.IsEmpty() ? default(T) : JsonConvert.DeserializeObject<T>(json);
        }
        /// <summary>
        /// 将json字符串转换为object对象
        /// </summary>
        /// <param name="Json">json字符串</param>
        /// <returns>object对象</returns>
        public static object? FromJson(this string Json)
        {
            return string.IsNullOrEmpty(Json) ? null : JsonConvert.DeserializeObject(Json);
        }
        /// <summary>
        /// 将对象转换为json字符串
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>json字符串</returns>
        public static string ToJson(this object obj)
        {
            return obj == null ? string.Empty : JsonConvert.SerializeObject(obj);
        }


        public static Dictionary<string, object> OnlyPropertiesToDictionary(this object obj, List<string> properties, bool ignoreCase = true)
        {
            if (obj == null) return new Dictionary<string, object>();
            var jObj = JObject.FromObject(obj);
            var newJObj = new Dictionary<string, object>();
            foreach (var property in properties)
            {
                StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (jObj.TryGetValue(property, comparison, out var value))

                {
                    newJObj.Add(property, value);
                }
            }
            return newJObj;

        }

    }
     
}


