// MIT License
// 开源地址：https://gitee.com/co1024/AbcMvc
// Copyright (c) 2021-2022 1024
// Abc.Mvc=Furion+EF+SqlSugar+Pear layui admin.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Abc.Utils;
/// <summary>
/// 通过反射注册方法Helper
/// 全称：MethodRegisterExecute
/// </summary>
public static class MreHelper
{
    /// <summary>
    /// 注册方法集合
    /// </summary>
    /// <example><接口的type，<接口实现类type，RegisterObject>></example>
    public static ConcurrentDictionary<Type, ConcurrentDictionary<Type, RegisterObject>> RegisterMethodInfos = new();

    /// <summary>
    /// 注册方法
    /// </summary>
    /// <param name="type">接口interface 的type</param>
    /// <param name="methodName">方法名</param>
    public static void Register(Type type, string methodName)
    {
        Register(type, new List<string>() { methodName });
    }

    /// <summary>
    /// 注册方法
    /// </summary>
    /// <param name="type">接口interface 的type</param>
    /// <param name="methodNames">方法名列表</param>
    public static void Register(Type type, List<string> methodNames)
    {
        try
        {
            //查找所有实现了 type（接口）的类
            var apps = App.EffectiveTypes.Where(u => u.GetInterfaces()
                         .Any(i => i.HasImplementedRawGeneric(type)));
            //实现类的方法信息
            var appRegister = new ConcurrentDictionary<Type, RegisterObject>();

            //遍历找到的所有类
            foreach (var app in apps)
            {
                var registerObject = new RegisterObject();
                //遍历方法
                foreach (var methodName in methodNames)
                {
                    var method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                .Where(u => u.Name == methodName && u.GetParameters().Length > 0)
                                .FirstOrDefault();
                    if (method == null) continue;
                    //添加到方法集合
                    registerObject.Methods.TryAdd(methodName, method);
                }
                appRegister.TryAdd(app, registerObject);


            }
            if (RegisterMethodInfos.ContainsKey(type))
            {
                RegisterMethodInfos.TryRemove(type, out _);
            }

            RegisterMethodInfos.TryAdd(type, appRegister);
        }
        catch (Exception ex)
        {
            LogEx.Error($"type:{type.FullName} methodName:{methodNames.ToJson()}，\r\n异常：{ex.ToStringEx()}", "Abc\\Mre\\Register");
        }
    }




    /// <summary>
    /// 根据 Interface 的type 获取监听方法
    /// </summary>
    /// <param name="type">Interface 的type </param>
    /// <returns></returns>
    public static ConcurrentDictionary<Type, RegisterObject> GetMethods(Type type)
    {
        if (RegisterMethodInfos.TryGetValue(type, out var methods))
        {
            return methods;
        }
        else
        {
            return new ConcurrentDictionary<Type, RegisterObject>();
        }
    }
    /// <summary>
    /// 执行注册的方法
    /// </summary>
    /// <param name="type">Interface 的type</param>
    /// <param name="methodName">方法名,区分大小写</param>
    /// <param name="args">参数</param>
    public static void Execute(Type type, string methodName, object[] args)
    {
        //如果方法名为空，则不执行
        if (string.IsNullOrWhiteSpace(methodName))
        {
            LogEx.Error($"type:{type.FullName} methodName:{methodName} 为空，\r\n参数：{args.ToJson2()}", "Abc\\Mre\\Execute");
            return;
        }
        //遍历实现类
        foreach (var item in GetMethods(type))
        {
            //遍历方法集合
            foreach (var methodItem in item.Value.Methods)
            {
                if (methodItem.Key == methodName)
                {
                    try
                    {
                        var instance = Activator.CreateInstance(item.Key);
                        methodItem.Value.Invoke(instance, args);
                    }
                    catch (Exception ex)
                    {
                        LogEx.Error(ex, "Abc\\Mre\\Execute");
                    }
                }
            }

        }
    }
}
