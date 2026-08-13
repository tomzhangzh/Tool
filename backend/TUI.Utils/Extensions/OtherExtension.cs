
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Utils.Extensions
{
    /// <summary>
    /// 其他扩展方法
    /// </summary>
    public static class OtherExtension
    {
        /// <summary>
        /// 设置值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetValue<T>(this ISession session, string key, T value)
        {
            session.SetString(key, value == null ? "" : value.ToJson());
        }
        /// <summary>
        /// 获取值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="session"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T? GetValue<T>(this ISession session, string key)
        {
            string json = session.GetString(key);
            return json.FromJson<T>();
        }
        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string? GetValue(this HttpRequest request, string key)
        {
            if (!string.IsNullOrEmpty(request.Query[key]))
            {
                return request.Query[key].ToString();
            }
            else if (request.HasFormContentType && request.Form != null && !string.IsNullOrEmpty(request.Form[key]))
            {
                return request.Form[key].ToString();
            }
            else return null;
        }


        public static dynamic ToDynamic(this object obj)
        {
            IDictionary<string, object> expando = new ExpandoObject();
            foreach (var propertyInfo in obj.GetType().GetProperties())
            {
                expando.Add(propertyInfo.Name, propertyInfo.GetValue(obj, null));
            }
            return expando as ExpandoObject;


        }
    }
}


