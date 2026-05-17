# Ollama Installation Script - Custom location D:\Ollama
# Run as Administrator

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Ollama Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$installPath = "D:\Ollama"
if (-not (Test-Path $installPath)) {
    Write-Host "[1/4] Creating directory: $installPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $installPath -Force | Out-Null
} else {
    Write-Host "[1/4] Directory exists: $installPath" -ForegroundColor Green
}

Write-Host "[2/4] Configuring environment variables..." -ForegroundColor Yellow
$env:OLLAMA_MODELS = "$installPath\models"
$env:OLLAMA_HOST = "127.0.0.1:11434"
[Environment]::SetEnvironmentVariable("OLLAMA_MODELS", "$installPath\models", "User")
[Environment]::SetEnvironmentVariable("OLLAMA_HOST", "127.0.0.1:11434", "User")
Write-Host "  Environment variables set" -ForegroundColor Green

Write-Host "[3/4] Checking package manager..." -ForegroundColor Yellow
try {
    $winget = Get-Command winget -ErrorAction Stop
    Write-Host "  winget available" -ForegroundColor Green

    Write-Host "[4/4] Installing Ollama..." -ForegroundColor Yellow
    Write-Host "  Please select install location: $installPath" -ForegroundColor Magenta

    $result = winget install Ollama.Ollama --location $installPath --accept-package-agreements --accept-source-agreements

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "  Ollama installed successfully!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:"
        Write-Host "1. Open new terminal"
        Write-Host "2. Run: ollama serve"
        Write-Host "3. Run: ollama pull nomic-embed-text"
        Write-Host "4. Run: ollama pull llama3"
    } else {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Yellow
        Write-Host "  Manual installation required" -ForegroundColor Yellow
        Write-Host "========================================" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Due to network issues, please:"
        Write-Host "1. Visit: https://github.com/ollama/ollama/releases"
        Write-Host "2. Download OllamaSetup.exe"
        Write-Host "3. Run installer, select: D:\Ollama"
        Write-Host "4. Restart terminal after install"
    }
} catch {
    Write-Host "  winget not available" -ForegroundColor Red
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "  Manual installation required" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Due to network issues, please:"
    Write-Host "1. Visit: https://github.com/ollama/ollama/releases"
    Write-Host "2. Download OllamaSetup.exe"
    Write-Host "3. Run installer, select: D:\Ollama"
    Write-Host "4. Restart terminal after install"
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
