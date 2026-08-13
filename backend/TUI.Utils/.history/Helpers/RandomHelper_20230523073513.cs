// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace Abc.Utils;

/// <summary>
/// 使用Random类生成伪随机数
/// </summary>
public static class RandomHelper
{
    public static Random GetRandom()
    {
        byte[] buffer = Guid.NewGuid().ToByteArray();
        int iSeed = BitConverter.ToInt32(buffer, 0);
        //随机数对象
        return new(iSeed);
    }

    #region 生成一个指定范围的随机整数

    /// <summary>
    /// 生成一个指定范围的随机整数，该随机数范围包括最小值，但不包括最大值
    /// </summary>
    /// <param name="minNum">最小值</param>
    /// <param name="maxNum">最大值</param>
    public static int GetInt(int minNum, int maxNum)
    {
        Random _random = GetRandom();
        return _random.Next(minNum, maxNum);
    }

    #endregion 生成一个指定范围的随机整数

    #region 生成一个0.0到1.0的随机小数

    /// <summary>
    /// 生成一个0.0到1.0的随机小数
    /// </summary>
    public static double GetDouble()
    {
        Random _random = GetRandom();
        return _random.NextDouble();
    }

    #endregion 生成一个0.0到1.0的随机小数

    #region GetString 产生随机数

    /// <summary>
    /// 从字符串里随机得到，规定个数的字符串.
    /// </summary>
    /// <param name="lenght"></param>
    /// <param name="randtype">随机类型（数字，字母，数字+字母）</param>
    /// <param name="chars">随机用的字符</param>
    /// <returns></returns>
    public static string GetString(int lenght, RandType randtype = RandType.Normarl, string chars = "")
    {
        var allChar = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        if (!string.IsNullOrWhiteSpace(chars))
        {
            allChar = chars;
        }
        else
        {
            switch (randtype)
            {
                case RandType.NumAndStr:
                    allChar = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                    break;

                case RandType.Num:
                    allChar = "0123456789";
                    break;

                case RandType.Str:
                    allChar = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                    break;
            }
        }
        var randomCode = "";
        var temp = -1;
        Random _random = GetRandom(); ;
        for (var i = 0; i < lenght; i++)
        {
            if (temp != -1)
            {
                _random = new Random(temp * i * ((int)DateTime.Now.Ticks));
            }

            var t = _random.Next(allChar.Length - 1);

            while (temp == t)
            {
                t = _random.Next(allChar.Length - 1);
            }

            temp = t;
            // randomCode += allCharArray[t];
            randomCode += allChar.Substring(t, 1);
        }
        return randomCode;
    }

    #endregion GetString 产生随机数

    /// <summary>
    /// 使用Guid生成种子
    /// </summary>
    /// <returns></returns>
    private static int GetRandomSeedbyGuid()
    {
        return new Guid().GetHashCode();
    }

    public static string GetRandomByGuid(int length)
    {
        Random random = new Random(GetRandomSeedbyGuid());
        //for (int i = 0; i < len; i++)
        //{
        //    array[i] = random.Next(0, len);
        //}
        return random.Next(0, length).ToString();
    }
}

/// <summary>
/// 随机类型
/// </summary>
public enum RandType
{
    Normarl,

    /// <summary>
    /// 数字
    /// </summary>
    Num,

    /// <summary>
    /// 字母
    /// </summary>
    Str,

    /// <summary>
    /// 数字+字母
    /// </summary>
    NumAndStr
}