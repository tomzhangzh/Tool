namespace VueLibV4.Services.Dependency;

/// <summary>
/// 依赖注入标记接口基类：实现该接口（或其子接口）的类会在 Web 启动时被程序集扫描自动注册到 IoC 容器。
/// 参考 Sunshine.Services 的 IDependency 体系。
/// </summary>
public interface IDependency
{
}

/// <summary>实现该接口将自动注册，生命周期为 Scoped（每个请求/作用域一个实例）</summary>
public interface IScopeDependency : IDependency
{
}

/// <summary>实现该接口将自动注册，生命周期为 Singleton（全局单例）</summary>
public interface ISingletonDependency : IDependency
{
}

/// <summary>实现该接口将自动注册，生命周期为 Transient（每次解析都创建新实例）</summary>
public interface ITransientDependency : IDependency
{
}
