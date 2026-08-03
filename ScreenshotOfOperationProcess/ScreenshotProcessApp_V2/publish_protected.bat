@echo off
chcp 65001 >nul
setlocal

echo ============================================
echo  ScreenshotProcessApp 防反编译发布脚本
echo ============================================
echo.

REM 配置 ConfuserEx 路径（请修改为你的实际路径）
set CONFUSER_EXE=tools\ConfuserEx\Confuser.CLI.exe

REM 检查 ConfuserEx 是否存在
if not exist "%CONFUSER_EXE%" (
    echo [警告] 未找到 ConfuserEx: %CONFUSER_EXE%
    echo 请从 https://github.com/mkaring/ConfuserEx/releases 下载
    echo 解压到 tools\ConfuserEx 目录下
    echo.
    echo 将仅执行 .NET 原生发布（不带混淆）
    echo.
    set DO_OBFUSCATE=0
) else (
    set DO_OBFUSCATE=1
)

REM 步骤1: 清理旧输出
echo [1/4] 清理旧的发布文件...
if exist publish rmdir /s /q publish
if exist publish_obfus rmdir /s /q publish_obfus
echo 完成
echo.

REM 步骤2: dotnet 发布（单文件 + ReadyToRun）
echo [2/4] 执行 dotnet publish...
dotnet publish -c Release -r win-x64 --self-contained true -o publish /p:PublishSingleFile=true /p:PublishReadyToRun=true
if errorlevel 1 (
    echo [错误] dotnet publish 失败
    pause
    exit /b 1
)
echo 完成
echo.

REM 步骤3: ConfuserEx 混淆
if "%DO_OBFUSCATE%"=="1" (
    echo [3/4] 执行 ConfuserEx 混淆...
    "%CONFUSER_EXE%" confuserEx.crproj
    if errorlevel 1 (
        echo [错误] ConfuserEx 混淆失败
        pause
        exit /b 1
    )
    echo 完成
    echo.
    echo [4/4] 混淆后的程序位于: publish_obfus 目录
    echo.
    echo ============================================
    echo  发布完成！请分发 publish_obfus 目录中的文件
    echo ============================================
) else (
    echo [3/4] 跳过混淆步骤
    echo.
    echo [4/4] 提示: .NET 原生发布已完成，但未做混淆
    echo.
    echo ============================================
    echo  发布完成（未混淆）！请分发 publish 目录中的文件
    echo  如需混淆，请安装 ConfuserEx 后重新运行本脚本
    echo ============================================
)

echo.
pause
