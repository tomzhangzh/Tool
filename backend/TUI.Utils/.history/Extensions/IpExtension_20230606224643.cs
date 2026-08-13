// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System.Net;
using System.Numerics;

namespace TUI.Utils;

public static class IpExtension
{
    #region IpToLong IP 转换为 long

    /// <summary>
    /// IP 转换为 long
    /// </summary>
    /// <param name="ip"></param>
    /// <returns></returns>
    public static long IpToLong(this string ip)
    {
        char[] separator = new char[] { '.' };
        string[] items = ip.Split(separator);

        return long.Parse(items[0]) << 24

                | long.Parse(items[1]) << 16

                | long.Parse(items[2]) << 8

                | long.Parse(items[3]);
    }

    #endregion IpToLong IP 转换为 long

    #region LongToIP 数字转换为IP

    /// <summary>
    /// 数字转换为IP
    /// </summary>
    /// <param name="ipInt"></param>
    /// <returns></returns>
    public static string LongToIP(this long ipInt)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append((ipInt >> 24) & 0xFF).Append(".");
        sb.Append((ipInt >> 16) & 0xFF).Append(".");
        sb.Append((ipInt >> 8) & 0xFF).Append(".");
        sb.Append(ipInt & 0xFF);
        return sb.ToString();
    }

    #endregion LongToIP 数字转换为IP

    #region IPV6

    public static BigInteger IpV6ToLong(this string ipStr)
    {
        // 测试：2400:A480:aaaa:400:a1:b2:c3:d4

        IPAddress ip = IPAddress.Parse(ipStr);
        List<Byte> ipFormat = ip.GetAddressBytes().ToList();
        ipFormat.Reverse();
        ipFormat.Add(0);
        BigInteger ipAsInt = new BigInteger(ipFormat.ToArray());
        return ipAsInt;
    }

    /// <summary>
    /// 将IP地址转换为等效的UInt64[2]。
    /// 对于IPv4地址，第一个元素将是0，
    /// 第二个是四个字节的UInt32表示。
    /// 对于IPv6地址，第一个元素将是UInt64
    /// 表示前八个字节，第二个将是
    /// 最后八个字节。
    /// </summary>
    /// <param name="ipAddress">IP地址转换</param>
    /// <returns></returns>
    private static ulong[] IpToUInt64Array(this string ipAddress)
    {
        byte[] addrBytes = System.Net.IPAddress.Parse(ipAddress).GetAddressBytes();
        if (System.BitConverter.IsLittleEndian)
        {
            //little-endian machines store multi-byte integers with the
            //least significant byte first. this is a problem, as integer
            //values are sent over the network in big-endian mode. reversing
            //the order of the bytes is a quick way to get the BitConverter
            //methods to convert the byte arrays in big-endian mode.
            //不同的计算机体系结构使用不同的字节顺序存储数据。 “Big-endian”表示最重要的字节位于单词的左端。 “Little-endian”表示最重要的字节位于单词的右端。

            System.Collections.Generic.List<byte> byteList = new System.Collections.Generic.List<byte>(addrBytes);
            byteList.Reverse(); //反转数组
            addrBytes = byteList.ToArray();
        }
        ulong[] addrWords = new ulong[2];
        if (addrBytes.Length > 8)
        {
            addrWords[0] = System.BitConverter.ToUInt64(addrBytes, 8);
            addrWords[1] = System.BitConverter.ToUInt64(addrBytes, 0);
        }
        else
        {
            addrWords[0] = 0;
            addrWords[1] = System.BitConverter.ToUInt32(addrBytes, 0);
        }
        return addrWords;
    }

    #endregion IPV6

    //IPV6 https://blog.csdn.net/qq_29229567/article/details/88739294

    //       /**
    //* 将 IPv6 地址转为 long 数组，只支持冒分十六进制表示法
    //*/
    //       public static long[] ipv6ToLongs(String ipv6_string)
    //       {
    //           if (string.IsNullOrWhiteSpace(ipv6_string))
    //           {
    //              // throw new IllegalArgumentException("ipv6_string cannot be null.");
    //              throw new Exception("ipv6_string cannot be null.");
    //           }

    //           String[] ipSlices = ipv6_string.Split(":");
    //           if (ipSlices.Length != 8)
    //           {
    //               //throw new IllegalArgumentException(ipv6_string + " is not an ipv6 address.");
    //               throw new Exception(ipv6_string + " is not an ipv6 address.");
    //           }

    //           long[] ipv6 = new long[2];
    //           for (int i = 0; i < 8; i++)
    //           {
    //               String slice = ipSlices[i];

    //               // 以 16 进制解析
    //               //long num = Long.parseLong(slice, 16);
    //               long num = Convert.ToInt64(slice);

    //               // 每组 16 位
    //               long right = num << (16 * i);

    //               // 每个 long 保存四组，i >> 2 等于 i / 4
    //               int length = i >> 2;//即int length=i / 4;
    //               ipv6[length] = ipv6[length] | right;
    //           }

    //           return ipv6;
    //       }

    //       /**
    //* 将 long 数组转为冒分十六进制表示法的 IPv6 地址
    //*/
    //       public static String longsToIpv6(long[] numbers)
    //       {
    //           if (numbers == null || numbers.Length != 2)
    //           {
    //               //throw new IllegalArgumentException(Arrays.toString(numbers) + " is not an IPv6 address.");
    //               throw new Exception(Arrays.toString(numbers) + " is not an IPv6 address.");
    //           }

    //           StringBuilder sb = new StringBuilder(32);
    //           for (long numSlice : numbers)
    //           {
    //               // 每个 long 保存四组
    //               for (int j = 0; j < 4; j++)
    //               {
    //                   // 取最后 16 位
    //                   long current = numSlice & 0xFFFF;
    //                   sb.append(Long.toString(current, 16)).append(":");
    //                   // 右移 16 位，即去除掉已经处理过的 16 位
    //                   numSlice >>= 16;
    //               }
    //           }

    //           // 去掉最后的 :
    //           return sb.ToString().Substring(0, sb.Length - 1);
    //       }
}