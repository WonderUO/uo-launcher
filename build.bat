@echo off
chcp 65001 >nul
title UO Launcher - Build

:: Buscar csc.exe en .NET Framework
set CSC=csc.exe
if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" (
    set CSC="%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
) else if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" (
    set CSC="%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
) else (
    echo ERROR: No se encuentra csc.exe (.NET Framework SDK)
    pause
    exit /b 1
)

set REFERENCES=
set REFS=System.dll;System.Windows.Forms.dll;System.Drawing.dll;System.Core.dll;System.Net.dll

echo.
echo === UO Launcher Build ===
echo.
echo Compilando...

%CSC% /nologo /target:winexe /out:UOLauncher.exe /reference:%REFS% /win32icon:app.ico Program.cs MainForm.cs Patcher.cs ManifestGenerator.cs

if errorlevel 1 (
    echo.
    echo ERROR: Compilacion fallida.
    pause
    exit /b 1
)

echo.
echo OK: UOLauncher.exe generado.
echo.

:: Copiar appsettings.json si no existe
if not exist UOLauncher.exe.config (
    echo No se necesita config.
)

pause
