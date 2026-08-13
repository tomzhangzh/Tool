// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2023 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

public static class ListExtension
{
    #region Random

    /// <summary>
    /// 打乱 List集合
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="inputList"></param>
    /// <returns></returns>
    public static List<T> Random<T>(this List<T> inputList)
    {
        //Copy to a array
        T[] copyArray = new T[inputList.Count];
        inputList.CopyTo(copyArray);

        //Add range
        List<T> copyList = new List<T>();
        copyList.AddRange(copyArray);

        //Set outputList and random
        List<T> outputList = new List<T>();
        Random rd = new Random(DateTime.Now.Millisecond);

        while (copyList.Count > 0)
        {
            //Select an index and item
            int rdIndex = rd.Next(0, copyList.Count - 1);
            T remove = copyList[rdIndex];

            //remove it from copyList and add it to output
            copyList.Remove(remove);
            outputList.Add(remove);
        }
        return outputList;
    }

    #endregion Random

    #region ToList 把字符串按照指定分隔符装成 List 去除重复

    /// <summary>
    /// 把字符串按照指定分隔符装成 List 去除重复
    /// </summary>
    /// <param name="str"></param>
    /// <param name="sepeater">分隔符</param>
    /// <param name="isRemoveRepeat">是否去除重复项 默认为flase</param>
    /// <returns></returns>
    public static List<string> ToList(this string str, string sepeater, bool isRemoveRepeat = false)
    {
        return str.ToList(sepeater.ToCharArray(), isRemoveRepeat);
    }

    /// <summary>
    /// 把字符串按照指定分隔符装成 List 去除重复
    /// </summary>
    /// <param name="str"></param>
    /// <param name="sepeater">分隔符</param>
    /// <param name="isRemoveRepeat">是否去除重复项 默认为flase</param>
    /// <returns></returns>
    public static List<string> ToList(this string str, char[] sepeater, bool isRemoveRepeat = false)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(str)) return list;

        foreach (string item in str.Split(sepeater))
        {
            if (string.IsNullOrEmpty(item)) continue;
            if (!isRemoveRepeat)
            {
                list.Add(item);
            }
            else if (list.All(o => o != item.Trim()))
            {
                list.Add(item);
            }
        }
        return list;
    }

    #endregion ToList 把字符串按照指定分隔符装成 List 去除重复

    /// <summary>
    ///  ToList 把字符串按照指定分隔符装成 List 去除重复
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="sepeater">分隔符,默认英文逗号隔开</param>
    /// <param name="isRemoveRepeat">是否移除重复</param>
    /// <returns></returns>
    public static List<int> ToIntList(this string str, char[] sepeater = null, bool isRemoveRepeat = false)
    {
        if (sepeater == null) sepeater = new char[] { ',' };
        var list = new List<int>();
        if (string.IsNullOrWhiteSpace(str)) return list;
        foreach (string item in str.Split(sepeater))
        {
            if (string.IsNullOrEmpty(item)) continue;
            var state = int.TryParse(item, out var value);
            if (state == false) continue;//不能数字类型，跳过

            if (!isRemoveRepeat)
            {
                list.Add(value);
            }
            else if (list.All(o => o != value))
            {
                list.Add(value);
            }
        }
        return list;
    }

    /// <summary>
    ///  ToList 把字符串按照指定分隔符装成 List 去除重复
    /// </summary>
    /// <param name="str">字符串</param>
    /// <param name="sepeater">分隔符,默认英文逗号隔开</param>
    /// <param name="isRemoveRepeat">是否移除重复</param>
    /// <returns></returns>
    public static List<long> ToLongList(this string str, char[] sepeater = null, bool isRemoveRepeat = false)
    {
        if (sepeater == null) sepeater = new char[] { ',' };
        var list = new List<long>();
        if (string.IsNullOrWhiteSpace(str)) return list;
        foreach (string item in str.Split(sepeater))
        {
            if (string.IsNullOrEmpty(item)) continue;
            var state = Int64.TryParse(item, out var value);
            if (state == false) continue;//不能数字类型，跳过

            if (!isRemoveRepeat)
            {
                list.Add(value);
            }
            else if (list.All(o => o != value))
            {
                list.Add(value);
            }
        }
        return list;
    }

    /// <summary>
    /// list 集合转 object[]
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static object[] ToObjects<T>(this List<T> list)
    {
        object[] result = new object[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            result[i] = list[i];
        }
        return result;
    }

    /// <summary>
    /// List 转 ConcurrentBag
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static ConcurrentBag<T> ToConcurrentBag<T>(this List<T> data)
    {
        var rdata = new ConcurrentBag<T>();
        foreach (var item in data)
        {
            rdata.Add(item);
        }
        return rdata;
    }

    /// <summary>
    /// 把集合添加到集合
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="srcs"></param>
    /// <param name="addDatas">新增数据集合</param>
    public static void AddRange<T>(this ConcurrentBag<T> srcs, List<T> addDatas)
    {
        foreach (var item in addDatas)
        {
            srcs.Add(item);
        }
    }

    /// <summary>
    /// 把集合添加到集合
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="srcs"></param>
    /// <param name="addDatas">新增数据集合</param>
    public static void AddRange<T>(this ConcurrentBag<T> srcs, ConcurrentBag<T> addDatas)
    {
        foreach (var item in addDatas)
        {
            srcs.Add(item);
        }
    }
}