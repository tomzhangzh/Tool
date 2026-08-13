using Microsoft.Extensions.DependencyInjection;
using TUI.Services.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace TUI.Services.Extension
{
    public static class ServiceCollectionExtension
    {

        /// <summary>
        /// 要扫描的程序集名称
        /// 默认为[^Shop.Utils|^Shop.]多个使用|分隔
        /// </summary>
        public static string MatchAssemblies = "^TUI.Utils|^TUI.";

        public static IServiceCollection AddDataService(this IServiceCollection services)
        {
          //  services.TryAddScoped(typeof(IRepository), typeof(Repository.Repository));
            services.TryAddScoped(typeof(IRespository<>), typeof(Repository.Repository<>));
            services.TryAddScoped(typeof(IService<>), typeof(Repository.Service<>));
           // services.TryAddScoped(typeof(Ipr), typeof(Repository.Service<>));
            #region 依赖注入
            //services.AddScoped<IUserService, UserService>();           
            var baseType = typeof(IDependency);
            var path = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
            var getFiles = Directory.GetFiles(path, "*.dll").Where(Match);  //.Where(o=>o.Match())
            var referencedAssemblies = getFiles.Select(Assembly.LoadFrom).ToList();  //.Select(o=> Assembly.LoadFrom(o))         

            var ss = referencedAssemblies.SelectMany(o => o.GetTypes());

            var types = referencedAssemblies
                .SelectMany(a => a.DefinedTypes)
                .Select(type => type.AsType())
                .Where(x => x != baseType && baseType.IsAssignableFrom(x)).ToList();
            var implementTypes = types.Where(x => x.IsClass).ToList();
            var interfaceTypes = types.Where(x => x.IsInterface).ToList();
            foreach (var implementType in implementTypes)
            {
                if (typeof(IScopeDependency).IsAssignableFrom(implementType))
                {
                    var interfaceType = interfaceTypes.FirstOrDefault(x => x.IsAssignableFrom(typeof(IScopeDependency))==false &&  x.IsAssignableFrom(implementType));
                    
                    if (interfaceType != null)
                        services.AddScoped(interfaceType, implementType);
                }
                else if (typeof(ISingletonDependency).IsAssignableFrom(implementType))
                {
                    var interfaceType = interfaceTypes.FirstOrDefault(x => x.IsAssignableFrom(typeof(ISingletonDependency)) == false && x.IsAssignableFrom(implementType));
                    if (interfaceType != null)
                        services.AddSingleton(interfaceType, implementType);
                }
                else
                {
                    var interfaceType = interfaceTypes.FirstOrDefault(x => x.IsAssignableFrom(typeof(ITransientDependency)) == false && x.IsAssignableFrom(implementType));
                    if (interfaceType != null)
                        services.AddTransient(interfaceType, implementType);
                }
            }
            //foreach (var implementType in implementTypes)
            //{
            //    if (typeof(IScopeDependency).IsAssignableFrom(implementType))
            //    {
            //        var interfaceType = interfaceTypes.FirstOrDefault(x => x.IsAssignableFrom(implementType));
            //        if (interfaceType != null)
            //            services.AddScoped(interfaceType, implementType);
            //    }
            //    else if (typeof(ISingletonDependency).IsAssignableFrom(implementType))
            //    {
            //        var interfaceType = interfaceTypes.FirstOrDefault(x => x.IsAssignableFrom(implementType));
            //        if (interfaceType != null)
            //            services.AddSingleton(interfaceType, implementType);
            //    }
            //    else
            //    {
            //        var interfaceType = interfaceTypes.FirstOrDefault(x => x.IsAssignableFrom(implementType));
            //        if (interfaceType != null)
            //            services.AddTransient(interfaceType, implementType);
            //    }
            //}
            #endregion
            return services;
        }

        /// <summary>
        /// 程序集是否匹配
        /// </summary>
        public static bool Match(string assemblyName)
        {
            assemblyName = Path.GetFileName(assemblyName);
            if (assemblyName.StartsWith($"{AppDomain.CurrentDomain.FriendlyName}.Views"))
                return false;
            if (assemblyName.StartsWith($"{AppDomain.CurrentDomain.FriendlyName}.PrecompiledViews"))
                return false;
            return Regex.IsMatch(assemblyName, MatchAssemblies, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
