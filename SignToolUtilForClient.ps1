
# FOR CSPROJ
# <Target Name="ExecuteScript" AfterTargets="DownloadScript">
#     <Exec Command="powershell -File '$(ProjectDir)\SignToolUtilForClient.ps1' -filesToSign '$(TargetDir)ArtWiz.exe,$(TargetDir)ArtWiz.dll'"/>
# </Target>

# FOR PSW
# $filesToSign = "ArtWiz.exe,ArtWiz.dll"
# .\SignToolUtilForClient.ps1 -filesToSign $filesToSign
param (
     [string]$filesToSign,
     [string]$localConfigPath,
     [string]$prvtoken
)
$_TOKEN = $prvtoken
$ISLOCAL = $env:ISLOCAL
if (-not $ISLOCAL) {
     $ISLOCAL = $true
}
 
Write-Host filesToSign: $filesToSign
Write-Host localConfigPath: $localConfigPath
Write-Host isLocal: $ISLOCAL

if (-not $_TOKEN) {
     Write-Host "Assign local variable"

     $localXmlString = Get-Content -Raw -Path $localConfigPath
	
     # Tạo đối tượng XmlDocument và load chuỗi XML vào nó
     $localXmlDoc = New-Object System.Xml.XmlDocument
     $localXmlDoc.PreserveWhitespace = $true
     $localXmlDoc.LoadXml($localXmlString)
     $_TOKEN = $localXmlDoc.configuration.GITHUB_TOKEN

}

if (-not $_TOKEN) {
     throw "GITHUB_TOKEN must not be null "
}

$headers = @{
     Authorization = "token $_TOKEN"
}
$url = "https://raw.githubusercontent.com/TrdHuy/_TrdBuildPlugin/master/SignToolUtil.ps1"
$script = Invoke-RestMethod -Uri $url -Headers $headers

$modifiedScriptContent = "`$filesToSign = `"$filesToSign`"`n" `
     + "`$TOKEN = `"$_TOKEN`"`n" `
     + $script
Write-Host Start signing internal
Invoke-Expression $modifiedScriptContent