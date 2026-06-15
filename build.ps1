# UO Launcher - Build Script (PowerShell)
Write-Host "=== UO Launcher Build ===" -ForegroundColor Cyan
Write-Host

# Locate csc.exe
$cscCandidates = @(
    "$env:SystemRoot\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:SystemRoot\Microsoft.NET\Framework\v4.0.30319\csc.exe",
    "csc.exe"
)

$csc = $null
foreach ($c in $cscCandidates) {
    if (Test-Path $c) { $csc = $c; break }
}

if (-not $csc) {
    Write-Host "ERROR: No se encuentra csc.exe (.NET Framework SDK)" -ForegroundColor Red
    pause
    exit 1
}

$references = @(
    "System.dll",
    "System.Windows.Forms.dll",
    "System.Drawing.dll",
    "System.Core.dll",
    "System.Net.dll"
)

$refArg = ($references | ForEach-Object { "/reference:$_" }) -join " "

$sources = @(
    "Program.cs",
    "MainForm.cs",
    "Patcher.cs",
    "ManifestGenerator.cs"
)

Write-Host "Compilando con: $csc" -ForegroundColor Yellow
Write-Host "Fuentes: $($sources -join ', ')" -ForegroundColor Gray
Write-Host

$args = @(
    "/nologo",
    "/target:winexe",
    "/out:UOLauncher.exe",
    $refArg
) + $sources

$proc = Start-Process -FilePath $csc -ArgumentList $args -NoNewWindow -Wait -PassThru

if ($proc.ExitCode -eq 0) {
    Write-Host
    Write-Host "OK: UOLauncher.exe generado." -ForegroundColor Green

    if (Test-Path "app.ico") {
        Write-Host "Icono incluido." -ForegroundColor Gray
    }
} else {
    Write-Host
    Write-Host "ERROR: Compilacion fallida (exit code $($proc.ExitCode))." -ForegroundColor Red
    pause
    exit 1
}

Write-Host
Write-Host "Para generar manifest.json del cliente UO:" -ForegroundColor Cyan
Write-Host "  UOLauncher.exe --generate-manifest ""C:\ruta\al\cliente\UO""" -ForegroundColor White
