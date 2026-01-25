@echo off
chcp 65001 >nul 2>&1
setlocal

:: 配置项（直接修改这里）
set "TASK_NAME=同步LOFTask"
set "EXE_PATH=E:\Tom\Tool\LOF\bin\Debug\net6.0\LOF.exe"
set "ARGUMENT=1"

:: 1. 校验EXE是否存在
if not exist "%EXE_PATH%" (
    echo ❌ 错误：EXE文件不存在 → %EXE_PATH%
    pause
    exit /b 1
)

:: 2. 清理原有任务
echo ℹ️ 正在清理原有任务...
schtasks /delete /tn "%TASK_NAME%" /f >nul 2>&1

:: 3. 创建定时任务（无换行，避免格式错误）
echo ℹ️ 正在创建定时任务...
schtasks /create /sc hourly /mo 1 /tn "%TASK_NAME%" /tr "\"%EXE_PATH%\" %ARGUMENT%" /f /rl highest /ru SYSTEM

:: 4. 验证结果
if %errorlevel% equ 0 (
    echo ✅ 定时任务创建成功！
    echo 📋 任务：%TASK_NAME%
    echo 📂 执行文件：%EXE_PATH%
    echo ⚙️ 参数：%ARGUMENT%
    echo ⏰ 频率：每小时
) else (
    echo ❌ 定时任务创建失败！
)

pause
endlocal