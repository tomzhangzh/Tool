// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2023 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Abc.Utils;
public static class DataTableExtension
{
    /// <summary>
    ///  DataTable 转换为 List<IDictionary<string, object>>
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public static List<IDictionary<string, object>> ToListDic(this DataTable dt)
    {
        List<IDictionary<string, object>> datas = new List<IDictionary<string, object>>();

        //表格列名
        var dtColumns = dt.Columns;

        var rowDics = new Dictionary<string, object>();
        foreach (DataRow row in dt.Rows)
        {
            foreach (DataColumn col in dtColumns)
            {
                //跳过重复列
                if (rowDics.ContainsKey(col.ColumnName)) continue;
                rowDics.Add(col.ColumnName, row[col.ColumnName]);
            }

            datas.Add(rowDics);
            rowDics.Clear();
        }
        return datas;
    }

    /// <summary>
    /// 获取DataTable 行索引
    /// </summary>
    /// <param name="datarow"></param>
    /// <param name="columnname"></param>
    /// <returns></returns>
    public static int GetDataRowIndex(this DataRow datarow, string columnname)
    {
        var data = GetDataRowValue(datarow, columnname);

        var state = int.TryParse(data, out var rowIndex);
        if (state) return rowIndex;
        return 0;
    }

    /// <summary>
    /// 获取DataTable 行中某列的值
    /// </summary>
    /// <param name="datarow"></param>
    /// <param name="columnname"></param>
    /// <returns></returns>
    public static string GetDataRowValue(this DataRow datarow, string columnname)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(columnname)) return "";

            var data = datarow[columnname.ToLower()]?.ToString() ?? "";
            return data;
        }
        catch (Exception e)
        {
        }

        return "";
    }



    /// <summary>
    /// DataTable通过反射获取单个像
    /// <param name="prefix">前缀</param>
    /// <param name="ignoreCase">是否忽略大小写，默认不区分</param>
    /// </summary>
    public static T ToSingleModel<T>(this DataTable data, string prefix=null, bool ignoreCase = true) where T : new()
    {
        T t = data.GetList<T>(prefix, ignoreCase).Single();
        return t;
    }

    /// <summary>
    /// DataTable通过反射获取多个对像
    /// </summary>
    /// <param name="prefix">前缀</param>
    /// <param name="ignoreCase">是否忽略大小写，默认不区分</param>
    /// <returns></returns>
    private static List<T> ToListModel<T>(this DataTable data, string prefix=null, bool ignoreCase = true) where T : new()
    {
        List<T> t = data.GetList<T>(prefix, ignoreCase);
        return t;
    }


    /// <summary>
    /// DataTable 转为 List<T>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="prefix">前缀</param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <returns></returns>
    private static List<T> GetList<T>(this DataTable data, string prefix, bool ignoreCase = true) where T : new()
    {
        List<T> t = new List<T>();
        int columnscount = data.Columns.Count;
        if (ignoreCase)
        {
            for (int i = 0; i < columnscount; i++)
                data.Columns[i].ColumnName = data.Columns[i].ColumnName.ToUpper();
        }
        try
        {
            var properties = new T().GetType().GetProperties();

            var rowscount = data.Rows.Count;
            for (int i = 0; i < rowscount; i++)
            {
                var model = new T();
                foreach (var p in properties)
                {
                    var keyName = prefix + p.Name + "";
                    if (ignoreCase)
                        keyName = keyName.ToUpper();
                    for (int j = 0; j < columnscount; j++)
                    {
                        if (data.Columns[j].ColumnName == keyName && data.Rows[i][j] != null)
                        {
                            var pval = data.Rows[i][j]?.ToString();
                            if (!string.IsNullOrEmpty(pval))
                            {
                                try
                                {
                                    // We need to check whether the property is NULLABLE
                                    if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        p.SetValue(model, Convert.ChangeType(data.Rows[i][j], p.PropertyType.GetGenericArguments()[0]), null);
                                    }
                                    else
                                    {
                                        p.SetValue(model, Convert.ChangeType(data.Rows[i][j], p.PropertyType), null);
                                    }
                                }
                                catch (Exception x)
                                {
                                    
                                }
                            }
                            break;
                        }
                    }
                }
                t.Add(model);
            }
        }
        catch (Exception ex)
        {


            
        }


        return t;
    }


    /// <summary>
    /// List泛型转换DataTable.
    /// </summary>
    public static DataTable ModelsToDataTable<T>(List<T> items)
    {
        var tb = new DataTable(typeof(T).Name);

        PropertyInfo[] props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo prop in props)
        {
            Type t = GetCoreType(prop.PropertyType);
            tb.Columns.Add(prop.Name, t);
        }
        foreach (T item in items)
        {
            if (item == null) continue;
            var values = new object[props.Length];

            for (int i = 0; i < props.Length; i++)
            {
                values[i] = props[i].GetValue(item, null);
            }
            tb.Rows.Add(values);
        }
        return tb;
    }

    /// <summary>
    /// 如果类型可空，则返回基础类型，否则返回类型
    /// </summary>
    private static Type GetCoreType(Type t)
    {
        if (t != null && IsNullable(t))
        {
            if (!t.IsValueType)
            {
                return t;
            }
            else
            {
                return Nullable.GetUnderlyingType(t);
            }
        }
        else
        {
            return t;
        }
    }

    /// <summary>
    /// 指定类型是否可为空
    /// </summary>
    private static bool IsNullable(Type t)
    {
        return !t.IsValueType || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
    }

}
