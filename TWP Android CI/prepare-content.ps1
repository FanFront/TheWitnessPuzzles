# Prepares MonoGame content for the modern Android project without changing the
# legacy Windows content pipeline file.
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$source = Join-Path $root 'TWP Shared\Content'
$destination = Join-Path $PSScriptRoot 'Content'
if (Test-Path $destination) { Remove-Item $destination -Recurse -Force }
Copy-Item $source $destination -Recurse -Force

$mgcb = Join-Path $destination 'Content.mgcb'
$mgcbText = [IO.File]::ReadAllText($mgcb)
if (-not $mgcbText.Contains('/platform:Windows')) {
    throw "Expected /platform:Windows in $mgcb"
}
[IO.File]::WriteAllText($mgcb, $mgcbText.Replace('/platform:Windows', '/platform:Android'))

# Roboto Mono is bundled with this repository. Use that installed family
# instead of requiring another downloadable Roboto font in CI.
$small = Join-Path $destination 'font\fnt_small.spritefont'
[IO.File]::WriteAllText($small,
    [IO.File]::ReadAllText($small).Replace('<FontName>Roboto</FontName>', '<FontName>Roboto Mono</FontName>'))

$fontRoot = Join-Path $env:WINDIR 'Fonts'
$fontRegistry = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts'
$roboto = Join-Path $source 'font\Roboto Mono.ttf'
$noto = Join-Path $env:RUNNER_TEMP 'NotoSansCJKsc-Regular.otf'

Write-Host 'Downloading open-source Noto Sans CJK SC for the bilingual UI...'
Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/notofonts/noto-cjk/main/Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf' -OutFile $noto

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class FontRegistration {
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    public static extern int AddFontResource(string file);
}
'@

foreach ($font in @(
    @{ Source = $roboto; Name = 'Roboto Mono (TrueType)'; File = 'TWP-RobotoMono.ttf' },
    @{ Source = $noto; Name = 'Noto Sans CJK SC (OpenType)'; File = 'TWP-NotoSansCJKsc.otf' }
)) {
    if (-not (Test-Path $font.Source)) { throw "Font missing: $($font.Source)" }
    $destFont = Join-Path $fontRoot $font.File
    Copy-Item -LiteralPath $font.Source -Destination $destFont -Force
    New-ItemProperty -Path $fontRegistry -Name $font.Name -Value $font.File -PropertyType String -Force | Out-Null
    $registered = [FontRegistration]::AddFontResource($destFont)
    Write-Host "Font registered: $($font.Name), count: $registered"
    if ($registered -lt 1) { throw "Could not load $($font.Name) on this runner" }
}
Write-Host "Android content staged in $destination"
