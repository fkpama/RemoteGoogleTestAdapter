$ErrorActionPreference='Stop'
if (!(Test-Path Env:VSINSTALLDIR)) {
  Write-Output "ERROR: must run in Developer Command Prompt for Visual Studio"
  exit -127
}

$googleTestAdapterDir="$PSScriptRoot\..\third-party\GoogleTestAdapter\GoogleTestAdapter"
$vsLocation=$env:VSINSTALLDIR
$diaSdk="$vsLocation\DIA SDK\bin"

$diaResolverDir="$googleTestAdapterDir\DiaResolver"

foreach($platform in ('x86', 'x64', 'arm64', 'arm')) {
    switch($platform)
    {
        'x64' { $dllFolder = "$diaSdk\amd64" }
        'x86' { $dllFolder = $diaSdk }
        default { $dllFolder = "$diaSdk\$platform" }
    }
    New-Item -ItemType Directory -Force "$diaResolverDir\$platform" | Out-Null
    Copy-Item "$dllFolder\msdia140.dll" "$diaResolverDir\$platform"
}

echo "##[command]cmd /c `"set VSCMD_DEBUG=3 && CALL `"$env:VSINSTALLDIR\VC\Auxiliary\Build\vcvars64.bat`" && cd `"$diaResolverDir\dia2`" && powershell -f compile_typelib.ps1"
cmd /c "set VSCMD_DEBUG=3 && CALL `"$env:VSINSTALLDIR\VC\Auxiliary\Build\vcvars64.bat`" && cd `"$diaResolverDir\dia2`" && powershell -f compile_typelib.ps1"