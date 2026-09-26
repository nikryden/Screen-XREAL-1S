<#
.SYNOPSIS
  Builds a signed, self-contained XrealScreen MSIX (x64) plus the install scripts into artifacts\installer.

.DESCRIPTION
  - Creates (once) a self-signed code-signing certificate "CN=Niklas Ryden" (must equal the manifest
    Publisher) in CurrentUser\My and exports the public part (.cer). The private key (.pfx) stays in
    installer\.signing (git-ignored). For a public release replace it with a trusted certificate (ADR-0005).
  - Publishes the app self-contained (.NET + Windows App SDK inside the package): the target PC needs
    no runtimes or developer tools.
  - Driver: NOT included (ADR-0008/0005 phase 1). Install-XrealScreen.ps1 checks for it.
#>
param(
    [string]$Configuration = 'Release',
    [string]$Version = ''
)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..')
$project = Join-Path $repo 'src\XrealScreen.App\XrealScreen.App.csproj'
$signingDir = Join-Path $PSScriptRoot '.signing'
$out = Join-Path $repo 'artifacts\installer'
$subject = 'CN=Niklas Ryden'
New-Item -ItemType Directory -Force $signingDir, $out | Out-Null

# 1. Certificate (created once, reused)
$pfx = Join-Path $signingDir 'XrealScreen-signing.pfx'
$pwdFile = Join-Path $signingDir 'pfx-password.txt'
if (-not (Test-Path $pfx)) {
    $password = [Guid]::NewGuid().ToString('N')
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $subject -FriendlyName 'XrealScreen test signing' `
        -CertStoreLocation Cert:\CurrentUser\My -KeyUsage DigitalSignature -KeyExportPolicy Exportable `
        -NotAfter (Get-Date).AddYears(3) -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
    Export-PfxCertificate -Cert $cert -FilePath $pfx -Password (ConvertTo-SecureString $password -AsPlainText -Force) | Out-Null
    Set-Content -Path $pwdFile -Value $password -NoNewline
    Write-Host "created signing certificate $($cert.Thumbprint)"
}
$password = Get-Content $pwdFile -Raw
$certObj = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfx, $password)
[System.IO.File]::WriteAllBytes((Join-Path $out 'XrealScreen.cer'), $certObj.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))

# 2. Optional version stamp (manifest Identity Version)
$manifest = Join-Path $repo 'src\XrealScreen.App\Package.appxmanifest'
$originalManifest = $null
if ($Version) {
    $originalManifest = Get-Content $manifest -Raw
    ($originalManifest -replace '(<Identity[^>]*Version=")[^"]+(")', "`${1}$Version`$2") | Set-Content $manifest -NoNewline -Encoding utf8
}

# 3. Publish the signed MSIX
$pkgDir = Join-Path $repo 'artifacts\msix\'
try {
    dotnet publish $project -c $Configuration -p:Platform=x64 -r win-x64 `
        -p:GenerateAppxPackageOnBuild=true -p:AppxPackageDir=$pkgDir -p:AppxBundle=Never `
        -p:UapAppxPackageBuildMode=SideloadOnly -p:AppxPackageSigningEnabled=true `
        -p:PackageCertificateKeyFile=$pfx -p:PackageCertificatePassword=$password `
        -p:WindowsAppSDKSelfContained=true -p:SelfContained=true -p:AppxSymbolPackageEnabled=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed ($LASTEXITCODE)" }
}
finally {
    if ($originalManifest) { Set-Content $manifest -Value $originalManifest -NoNewline -Encoding utf8 }
}

$msix = Get-ChildItem $pkgDir -Recurse -Filter *.msix | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $msix) { throw 'no .msix produced' }
Copy-Item $msix.FullName (Join-Path $out 'XrealScreen.msix') -Force
Copy-Item (Join-Path $PSScriptRoot 'Install-XrealScreen.ps1'), (Join-Path $PSScriptRoot 'Uninstall-XrealScreen.ps1'), (Join-Path $PSScriptRoot 'README.md') $out -Force

$sig = Get-AuthenticodeSignature (Join-Path $out 'XrealScreen.msix')
Write-Host "package: $out\XrealScreen.msix  ($([math]::Round($msix.Length / 1MB, 1)) MB)  signer: $($sig.SignerCertificate.Subject)"
