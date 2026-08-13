// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Abc.Utils;

/// <summary>
/// 进程
/// </summary>
public class ProcessHelper
{
    #region 判断进程是否存在1（。。。。待验证）

    /// <summary>
    ///  判断进程是否存在
    /// </summary>
    /// <param name="processName"></param>
    /// <returns></returns>
    public bool justisprocess(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        if (processes.Length > 0)
        {
            return true; //表示此进程已存在
        }
        return false;//表示进程不存在
    }

    #endregion 判断进程是否存在1（。。。。待验证）

    #region 判断进程是否存在2（。。。。已验证）

    ///<summary>
    ///判断程序是否正在运行
    ///</summary>
    ///<param name="appId">程序名称</param>
    ///<returns>如果程序是第一次运行返回True,否则返回False</returns>
    public bool IsFirst(string appId)
    {
        bool ret = false;
        if (OpenMutex(0x1F0001, 0, appId) == IntPtr.Zero)
        {
            CreateMutex(IntPtr.Zero, 0, appId);
            ret = true;
        }
        return ret;
    }

    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern IntPtr OpenMutex(
        uint dwDesiredAccess, // access
        int bInheritHandle,    // inheritance option
        string lpName          // object name
        );

    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern IntPtr CreateMutex(
        IntPtr lpMutexAttributes, // SD
        int bInitialOwner,                       // initial owner
        string lpName                            // object name
        );

    #endregion 判断进程是否存在2（。。。。已验证）
}