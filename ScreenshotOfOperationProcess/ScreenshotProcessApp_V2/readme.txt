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




## 授权检查功能已添加
### 修改的文件
1. Database.cs — 数据库层

- 新增 AppConfig 表（Key-Value 结构）
- 首次运行自动插入默认授权日期 2030-01-01
- 使用 AES 加密 存储日期（16字节密钥 + 16字节IV）
- 新增方法：
  - GetLicenseExpireDate() — 读取并解密授权日期
  - SetLicenseExpireDate(DateTime) — 加密并保存新日期
  - EncryptValue / DecryptValue — AES 加解密辅助
2. 新增 FormLicenseSetting.cs — 授权设置窗体

- 显示当前授权到期日（已过期显示红色警告）
- DateTimePicker 选择新到期日（不能选过去日期）
- 显示剩余天数或过期警告
- 保存后刷新显示
3. FormMain.cs — 主窗体

- 启动时检查 ：若当前日期超过授权到期日 → 弹窗提示并退出程序
- 菜单显隐控制 ：仅当 zzq.log 文件存在且包含"导入数据"时，显示"导入数据"和"授权设置"两个菜单
- 授权设置入口 ：点击"授权设置"打开 FormLicenseSetting 窗体
- 设置后复查 ：关闭设置窗体后再次检查，若已过期则退出
### 使用流程
普通用户 （无 zzq.log）：

- 看不到"导入数据"和"授权设置"菜单
- 程序在默认到期日 2030-01-01 之前正常使用
管理员 （有 zzq.log 包含"导入数据"）：

- 能看到"导入数据"和"授权设置"菜单
- 可通过"授权设置"修改到期日
- 修改后立即生效，重新启动程序时按新日期校验
### 加密说明
- 算法：AES（对称加密）
- 密钥： Zzq@2026_Key1234 （16字节，硬编码在 Database.cs 中）
- 数据库中存储的是 Base64 编码的密文，无法直接查看明文日期