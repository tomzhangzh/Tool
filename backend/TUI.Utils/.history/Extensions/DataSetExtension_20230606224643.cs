// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System.Data;

namespace TUI.Utils;

/// <summary>
/// DataSet 扩展
/// </summary>
public static class DataSetExtension
{
    //*********** DataSet

    #region DataSetToIList DataSet 转换为 IList

    /// <summary>
    /// DataSet 转换为 IList
    /// </summary>
    /// <param name="dataSet"></param>
    /// <param name="tableindex">DataSet中Table 的索引 默认为0</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IList<T>? DataSetToIList<T>(this DataSet dataSet, int tableindex = 0)
    {
        if (dataSet == null || dataSet.Tables.Count < 0)

            return null;

        if (tableindex > dataSet.Tables.Count - 1)

            return null;

        if (tableindex < 0)

            tableindex = 0;

        DataTable p_Data = dataSet.Tables[tableindex];

        // 返回值初始化

        IList<T> result = new List<T>();

        for (int j = 0; j < p_Data.Rows.Count; j++)
        {
            T _t = (T)Activator.CreateInstance(typeof(T));

            PropertyInfo[] propertys = _t.GetType().GetProperties();

            foreach (PropertyInfo pi in propertys)
            {
                for (int i = 0; i < p_Data.Columns.Count; i++)
                {
                    // 属性与字段名称一致的进行赋值

                    if (pi.Name.Equals(p_Data.Columns[i].ColumnName))
                    {
                        // 数据库NULL值单独处理

                        if (p_Data.Rows[j][i] != DBNull.Value)

                            pi.SetValue(_t, p_Data.Rows[j][i], null);
                        else

                            pi.SetValue(_t, null, null);

                        break;
                    }
                }
            }

            result.Add(_t);
        }

        return result;
    }

    #endregion DataSetToIList DataSet 转换为 IList

    #region DataSetToIList DataSet 转换为 IList

    /// <summary>
    /// DataSet 转换为 IList
    /// </summary>
    /// <param name="dataset"></param>
    /// <param name="tablename">表名</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static IList<T>? DataSetToIList<T>(this DataSet dataset, string tablename)
    {
        int tableIndex = 0;

        if (dataset == null || dataset.Tables.Count < 0)

            return null;

        if (string.IsNullOrEmpty(tablename))

            return null;

        for (int i = 0; i < dataset.Tables.Count; i++)
        {
            // 获取Table名称在Tables集合中的索引值

            if (dataset.Tables[i].TableName.Equals(tablename))
            {
                tableIndex = i;

                break;
            }
        }

        return DataSetToIList<T>(dataset, tableIndex);
    }

    #endregion DataSetToIList DataSet 转换为 IList
}