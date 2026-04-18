<#
.SYNOPSIS

Gets project version from latest.xml and updates it for dependent files.

.DESCRIPTION

Gets project version from latest.xml and updates it for dependent files. Prints version and update URL.

.EXAMPLE
pwsh ./Update-Version.ps1
Gets project version from latest.xml and updates it for dependent files.
#>

[CmdletBinding()]
param()

#requires -Version 7
Set-StrictMode -Version Latest

$ErrorActionPreference = 'Stop'
$Verbose = $VerbosePreference -ne 'SilentlyContinue'

$PSDefaultParameterValues['*:Verbose']     = $Verbose
$PSDefaultParameterValues['*:ErrorAction'] = $ErrorActionPreference

$ThisFolder = Split-Path (Get-Item (&{ $MyInvocation.ScriptName }))

function Main
{
    $latestFile      = Join-Path $ThisFolder latest.xml
    $buildPropsFile  = Join-Path $ThisFolder Directory.Build.props
    $appManifestFile = Join-Path $ThisFolder Hourglass\Properties\app.manifest
    $bundleWxsFile   = Join-Path $ThisFolder Hourglass.Bundle\Bundle.wxs
    $productWxsFile  = Join-Path $ThisFolder Hourglass.Setup\Product.wxs
    $mitRtfFile      = Join-Path $ThisFolder Hourglass.Bundle\MIT.rtf

    Write-Output "Reading '$latestFile'..."

    $latestXml = ([xml](Get-Content $latestFile)).UpdateInfo
    $latest    = [PSCustomObject]@{
        Version   = $latestXml.LatestVersion
        UpdateUrl = $latestXml.UpdateUrl
    }

    $fieldsFormat =
        @{ n = 'Version';   e = { $_.Version   } },
        @{ n = 'UpdateUrl'; e = { $_.UpdateUrl } }

    Write-Output "`n$(($latest | Format-List $fieldsFormat | Out-String).Trim())`n"

    Update-Content $appManifestFile '(?<=assemblyIdentity\s+version=")[^"]+(?=[\s\S]+?name)',$latest.Version
    Update-Content $buildPropsFile  '(?<=\<Version\>)[^<]+',$latest.Version
    Update-Content $bundleWxsFile   '(?<=\s+Version=")[^"]+',$latest.Version
    Update-Content $productWxsFile  '(?<=\s+Version=")[^"]+',$latest.Version
    Update-Content $mitRtfFile      '(?<=, \d{4}-)\d{4}',((Get-Date).Year)
}

function Update-Content($file, $replace)
{
    Write-Output "Updating '$file'..."

    $reader = [IO.StreamReader]::new($file, [Text.Encoding]::Default, $true)
    [void]$reader.Peek()
    $encoding = $reader.CurrentEncoding
    $reader.Close()

    (Get-Content $file -Raw) -creplace $replace | Set-Content $file -NoNewline -Encoding $encoding
}

. Main
