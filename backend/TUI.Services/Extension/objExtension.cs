using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace TUI.Services.Extension
{
    public static class ObjExtensions
    {
        //public static dynamic ToExpando(this object obj)
        //{
        //    if (obj.GetType() == typeof(ExpandoObject)) return obj;
        //    var result = new ExpandoObject();
        //    var dict = result as IDictionary<string, object>;
        //    int i = 0;
        //    if (obj.GetType() == typeof(NameValueCollection) || obj.GetType().IsSubclassOf(typeof(NameValueCollection)))
        //    {
        //        var nv = (NameValueCollection)obj;
        //        nv.Cast<string>().Select(key => new KeyValuePair<string, object>(key, nv[key])).ToList().ForEach(f => dict.Add(f));
        //    }
        //    else if (typeof(IEnumerable<dynamic>).IsAssignableFrom(obj.GetType()))
        //        obj.ToEnumerable<dynamic>().ToList().ForEach(f => dict.Add("Item" + (i++).ToString(), f));
        //    else if (typeof(IDictionary<string, object>).IsAssignableFrom(obj.GetType()))
        //        obj.ToDictionary().ToList().ForEach(f => dict.Add(f.Key, f.Value));
        //    else
        //        obj.GetType().GetProperties().ToList().ForEach(f => dict.Add(f.Name, f.GetValue(obj, null)));
        //    return result;
        //}
        //public static IDictionary<string, object> ToDictionary(this object obj)
        //{
        //    if (typeof(Dictionary<string, object>).IsAssignableFrom(obj.GetType()))
        //        return (IDictionary<string, object>)obj;
        //    else
        //        return (IDictionary<string, object>)obj.ToExpando();
        //}
        //public static T To<T>(this object obj)
        //{
        //    if (obj == null)
        //        return (T)default(T);
        //    else
        //        return (T)Convert.ChangeType(obj, typeof(T));
        //}
        //public static IEnumerable<T> ToEnumerable<T>(this object obj)
        //{
        //    return obj as IEnumerable<T>;
        //}
        //public static T[] ToArray<T>(this object obj)
        //{
        //    return ToEnumerable<T>(obj).ToArray();
        //}
    }
}
