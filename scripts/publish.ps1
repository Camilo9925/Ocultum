<#
.SYNOPSIS
  Gera o pacote de entrega do Ocultum (win-x64, self-contained) com o Steghide e os exemplos incluídos.
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File scripts/publish.ps1
#>
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'artifacts\Ocultum-win-x64'
$zip = Join-Path $root 'artifacts\Ocultum-win-x64.zip'

if (Test-Path $output) { Remove-Item $output -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }

dotnet publish (Join-Path $root 'src\Ocultum.Web\Ocultum.Web.csproj') `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output $output
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish falhou.' }

Copy-Item (Join-Path $root 'README.md') $output -ErrorAction SilentlyContinue
Remove-Item (Join-Path $output '*.pdb') -Force -ErrorAction SilentlyContinue

Compress-Archive -Path (Join-Path $output '*') -DestinationPath $zip
Write-Host "Pacote gerado em $output"
Write-Host "Zip: $zip"
