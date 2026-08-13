using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using TUI.Services.DBModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace TUI.Services
{
    public class IngoreLazyLoadResolver : DefaultContractResolver
    {

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);
            return properties.Where(o => !o.PropertyName.Equals("lazyLoader") && o.PropertyType.FullName.Contains(".DBModel.")==false).ToList();
        }
    }
    //public class NonLazyloaderContractResolver : DefaultContractResolver
    //{
    //   // public new static readonly NonLazyloaderContractResolver Instance = new NonLazyloaderContractResolver();
        
    //    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    //    {
    //        JsonProperty property = base.CreateProperty(member, memberSerialization);

    //        if (property.PropertyName == "LazyLoader")
    //        {
    //            property.ShouldSerialize = i => false;
    //        }

    //        return property;
    //    }
    //}
    public static class JSONHelper
    {
        public static object DeserializeObject(this string jsonString, Type type = null)
        {
            if (type == null)
            {
                return JsonConvert.DeserializeObject(jsonString);
            }
            else
            {
                return JsonConvert.DeserializeObject(jsonString, type);
            }
        }
        public static object DeserializeObjectReplace(this string jsonString, Type type = null)
        {
            if (type == null)
            {
                return JsonConvert.DeserializeObject(jsonString, new JsonSerializerSettings()
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace

                });
            }
            else
            {
                return JsonConvert.DeserializeObject(jsonString, type, new JsonSerializerSettings()
                {
                    ObjectCreationHandling = ObjectCreationHandling.Replace

                });
            }
        }
        public static T Deserialize<T>(this string jsonString)
        {
           return JsonConvert.DeserializeObject<T>(jsonString);
        }
        public static string ToJSONEF(this object obj)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.MissingMemberHandling = MissingMemberHandling.Ignore;
            settings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            settings.NullValueHandling = NullValueHandling.Ignore;
            //var x = App.HttpContext.RequestServices.GetService<TUIDbContext>();
            //x.ChangeTracker.LazyLoadingEnabled = false;
            settings.ContractResolver = new IngoreLazyLoadResolver();

            return JsonConvert.SerializeObject(obj, settings);
        }

        public static string ToJSON(this object obj,bool format=false, bool isSystemJson = false)
        {
            if (isSystemJson == false)
                if (format==false)
                {
                    return JsonConvert.SerializeObject(obj);
                }
                else
                {
                     return JsonConvert.SerializeObject(obj, Formatting.Indented);
                }
            else
                return System.Text.Json.JsonSerializer.Serialize(obj);
        }

        public static string ToJSONWithoutNull(this object obj)
        {
            IsoDateTimeConverter timeConverter = new IsoDateTimeConverter();
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
            var result = JsonConvert.SerializeObject(obj,
             new JsonSerializerSettings()
             {
                 Converters = new[] { timeConverter },
                 NullValueHandling = NullValueHandling.Ignore,
             });
            return result;
        }
        public static void SetValue<T>(this ISession session, string key, T value)
        {
            session.SetString(key, value == null ? "" : value.ToJSONEF());
        }
        public static T GetValue<T>(this ISession session, string key)
        {
            string json = session.GetString(key);
            if (string.IsNullOrEmpty(json)) return default;
            else return json.Deserialize<T>();
        }

        public static List<Dictionary<string, object>> GetListDictionaryFromTable(DataTable table)
        {
            List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();

                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                list.Add(dict);
            }
            return list;
        }
        
    }
}
