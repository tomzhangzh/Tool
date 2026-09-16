[Console]::OutputEncoding = [Text.Encoding]::UTF8
Write-Output "=== Program.cs 端口配置 ==="
$prog = "E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\Program.cs"
if (Test-Path $prog) { Select-String -Path $prog -Pattern "Urls|UseUrls|5010|5000|Listen|Kestrel" | ForEach-Object { $_.LineNumber.ToString() + ': ' + $_.Line.Trim().Substring(0, [Math]::Min(120, $_.Line.Trim().Length)) } }
Write-Output "`n=== launchSettings.json ==="
$ls = "E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\Properties\launchSettings.json"
if (Test-Path $ls) { Get-Content $ls -Encoding UTF8 | Select-Object -First 40 }
Write-Output "`n=== 解决方案文件 ==="
Get-ChildItem "E:\Tom\Tool\VueLibV4" -Filter "*.sln" | Select-Object -ExpandProperty FullName
Write-Output "`n=== 最近编译产物 ==="
Get-ChildItem "E:\Tom\Tool\VueLibV4\src\VueLibV4.Web\bin" -Recurse -Filter "VueLibV4.Web.exe" -ErrorAction SilentlyContinue | Select-Object LastWriteTime, FullName
Write-Output "`n=== dotnet SDK 版本 ==="
dotnet --version
