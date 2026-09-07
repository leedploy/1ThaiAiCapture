@echo off
setlocal enabledelayedexpansion

echo =======================================================
echo    1ThaiAi Capture - Compiling Single File Portable EXE
echo =======================================================
echo.

set "CSC_PATH=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "!CSC_PATH!" (
    set "CSC_PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

if exist "!CSC_PATH!" (
    echo [*] Found Windows C# Compiler: !CSC_PATH!
    echo [*] Compiling 1ThaiAiCapture.exe with embedded icon ...
    "!CSC_PATH!" /nologo /target:winexe /optimize+ /out:1ThaiAiCapture.exe /win32manifest:app.manifest /win32icon:app.ico /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll Program.cs
    
    if !errorlevel! equ 0 (
        echo.
        echo [SUCCESS] Build successful: 1ThaiAiCapture.exe created!
        echo           You can now run 1ThaiAiCapture.exe directly.
    ) else (
        echo.
        echo [ERROR] Compilation failed.
    )
) else (
    echo [*] C# Framework compiler not found. Trying dotnet CLI...
    dotnet build -c Release
)

echo.
pause
