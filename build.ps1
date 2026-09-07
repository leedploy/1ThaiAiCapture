# 1ThaiAi Capture - PowerShell Build Script
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host "   1ThaiAi Capture - Compiling Single File Portable EXE" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host ""

$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    $cscPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (Test-Path $cscPath) {
    Write-Host "[*] Using Windows C# Compiler: $cscPath" -ForegroundColor Yellow
    Write-Host "[*] Compiling 1ThaiAiCapture.exe with embedded icon..." -ForegroundColor Yellow
    
    & $cscPath /nologo /target:winexe /optimize+ /out:1ThaiAiCapture.exe /win32manifest:app.manifest /win32icon:app.ico /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll Program.cs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "[SUCCESS] Successfully built 1ThaiAiCapture.exe!" -ForegroundColor Green
        $fileInfo = Get-Item "1ThaiAiCapture.exe"
        Write-Host "File size: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor Green
        Write-Host "Location: $($fileInfo.FullName)" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Compilation failed." -ForegroundColor Red
    }
} else {
    Write-Host "[*] Framework C# Compiler not found. Trying dotnet CLI..." -ForegroundColor Yellow
    dotnet build -c Release
}
