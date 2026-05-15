$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "ShuiyuanHtmlPrivacyCleaner.csproj"
$dist = Join-Path $root "dist"
$assetScript = Join-Path $root "tools\GenerateBrandAssets.ps1"
$windowsPowerShell = "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"

New-Item -ItemType Directory -Force -Path $dist | Out-Null
& $windowsPowerShell -STA -ExecutionPolicy Bypass -File $assetScript
if ($LASTEXITCODE -ne 0) {
  throw "Asset generation failed with exit code $LASTEXITCODE"
}

$rids = @("win-x64", "win-x86", "win-arm64")
foreach ($rid in $rids) {
  $out = Join-Path $dist $rid
  & dotnet publish $project `
    -c Release `
    -r $rid `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -o $out
  if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for $rid with exit code $LASTEXITCODE"
  }
}

Get-ChildItem -Path $dist -Filter "ShuiyuanHtmlPrivacyCleaner.exe" -Recurse |
  Sort-Object FullName |
  ForEach-Object {
    $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
    $relative = $_.FullName.Substring($root.Length + 1)
    "$($hash.Hash)  $relative"
  } | Set-Content -LiteralPath (Join-Path $root "CHECKSUMS-SHA256.txt") -Encoding UTF8

Write-Host "Done. EXE files are under $dist"
