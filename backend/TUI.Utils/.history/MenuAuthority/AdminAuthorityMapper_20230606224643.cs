// MIT License
// 开源地址：https://gitee.com/co1024/TUIMvc
// Copyright (c) 2021-2022 1024
// TUI.Mvc=Furion+EF+SqlSugar+Pear layui admin.

namespace TUI.Utils;

/// <summary>
/// AdminAuthorityMapper
/// </summary>
public class AdminAuthorityMapper : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        //    //.Map(dest => dest.Name, src => src.Name)
        //// AuthorityMenuAttribute -> AuthorityMenuItem
        //config.ForType<AuthorityControllerAttribute, AuthorityMenuItem>()
        //      ;
        //AuthorityFunctionAttribute -> AuthorityFunctionItem
        config.ForType<MaPermissionAttribute, MaMenuItem>()

              ;
    }
}