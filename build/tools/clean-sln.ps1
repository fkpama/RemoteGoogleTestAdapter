#$dir = Test-Path -FullPath ($PSScriptRoot + "\\..\\..")
Get-ChildItem -Path "$PSScriptRoot\..\.." -Recurse -Directory -Filter obj | Remove-Item -Force -Recurse
Get-ChildItem -Path "$PSScriptRoot\..\.." -Recurse -Directory -Filter bin | Remove-Item -Force -Recurse
Get-ChildItem -Path "$PSScriptRoot\..\.." -Recurse -Filter msbuild.binlog | Remove-Item -Force -Recurse
if (Test-Path "$PSScriptRoot\..\..\TestResults") {
	Remove-Item "$PSScriptRoot\..\..\TestResults" -Force -Recurse
}
if (Test-Path "$PSScriptRoot\..\..\.vs") {
	Remove-Item "$PSScriptRoot\..\..\.vs" -Force -Recurse
}
