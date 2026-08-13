经使用默认发布会把视图（cshtml文件）打包到dll中进行预加载，查阅相关资料和帮助找到分离办法

1.安装Nuget包：Install-Package Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation

2. 在Program.cs中的AddControllersWithViews()之后添加对AddRazorRuntimeCompilation()的调用。也就是builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

 

3.修改项目的csproj文件，在PropertyGroup节点内增加如下两个选项：<MvcRazorCompileOnPublish>false</MvcRazorCompileOnPublish><RazorCompileOnBuild>false</RazorCompileOnBuild>


https://datav-vue3.jiaminghi.com/


https://github.com/imguolao/monaco-vue/blob/main/README.zh-CN.md