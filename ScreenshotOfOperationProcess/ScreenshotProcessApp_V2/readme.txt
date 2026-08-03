=======================================
ScreenshotProcessApp 防反编译发布指南
=======================================

一、发布方式（任选其一）

【方式A】一键发布脚本（推荐）
  双击运行 publish_protected.bat
  - 自动执行 dotnet publish + ConfuserEx 混淆
  - 输出目录: publish_obfus

【方式B】仅 .NET 原生发布（不带混淆）
  cd E:\Tom\Tool\ScreenshotOfOperationProcess\ScreenshotProcessApp_V2
  dotnet publish -c Release -r win-x64 --self-contained true -o publish /p:PublishSingleFile=true /p:PublishReadyToRun=true
  - 输出目录: publish


二、启用完整防反编译（需要 ConfuserEx）

1. 下载 ConfuserEx:
   https://github.com/mkaring/ConfuserEx/releases

2. 解压到项目下:
   tools\ConfuserEx\Confuser.CLI.exe
   tools\ConfuserEx\ConfuserEx.exe (GUI 版本)

3. 运行发布脚本:
   publish_protected.bat

   或手动混淆:
   tools\ConfuserEx\Confuser.CLI.exe confuserEx.crproj


三、防反编译配置说明（已在 csproj 中启用）

1. PublishSingleFile=true
   单文件打包，所有 DLL 嵌入 exe

2. EnableCompressionInSingleFile=true
   压缩单文件内容

3. IncludeNativeLibrariesForSelfExtract=true
   原生库也打包到单文件

4. SelfContained=true
   独立部署，无需用户安装 .NET 运行时

5. PublishReadyToRun=true
   编译为本地机器码（R2R），增加反编译难度
   同时保留部分 IL，兼容性最佳

6. PublishReadyToRunComposite=true
   所有程序集合并编译为本地代码

7. ConfuserEx 混淆（可选）:
   - 控制流混淆: 打乱方法指令顺序
   - 符号重命名: 类名/方法名/字段名打乱
   - 字符串加密: 隐藏代码中的字符串常量
   - 反调试: 检测调试器附加
   - 反篡改: 修改后程序无法运行
   - 资源加密: 加密嵌入资源


四、防反编译效果对照

                          反编译难度    启动速度    兼容性
仅单文件                    低            快          高
+ ReadyToRun               中            较快        高
+ ConfuserEx 混淆          高            较慢        中
NativeAOT (需 .NET 8+)     最高          最快        低


五、注意事项

- ReadyToRun 会增加 exe 体积（约 2-3 倍），但提升启动速度
- ConfuserEx 混淆后启动时间会增加 1-3 秒
- 混淆时排除了 System.Data.SQLite.dll，避免运行时问题
- 如混淆后出现运行异常，可尝试调整 confuserEx.crproj 中的 preset 为 "normal"



dotnet publish ScreenshotProcessApp.csproj -c Release -r win-x64 --self-contained true -o publish /p:PublishSingleFile=true /p:PublishReadyToRun=true 2>&1 | Select-Object -Last 15