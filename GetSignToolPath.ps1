param (
    [string]$platForm,
    [string]$resCacheFilePath
)
$LOG_TAG = "CheckSignToolPath"
# Define the search path for the Windows SDK bin folder
$searchPath = "C:\Program Files (x86)\Windows Kits\10\bin"

Write-Host $LOG_TAG": platForm:$platForm"
Write-Host $LOG_TAG": resCacheFilePath:$resCacheFilePath"

# Get the list of directories in the search path
$directories = Get-ChildItem -Path $searchPath -Directory

# Filter the directories to include only those with a version number in the name
$versionedDirectories = $directories | Where-Object { $_.Name -match '^\d+\.\d+\.\d+\.\d+$' }

# Sort the directories by version number in descending order
$sortedDirectories = $versionedDirectories | Sort-Object -Property Name -Descending

# Get the latest directory
$latestDirectory = $sortedDirectories | Select-Object -First 1

Write-Host $LOG_TAG": Found latest folder: $latestDirectory"

# Construct the path to the signtool.exe executable in the x64 folder
$signtoolPath = $latestDirectory.FullName + "\$platForm\signtool.exe"

# Output the signtool.exe path
if (Test-Path -Path $signtoolPath) {
    Write-Host $LOG_TAG": Latest signtool.exe found at: $signtoolPath"
}
else {
    throw "====== $LOG_TAG FATAL =====> signtool.exe not found in $signtoolPath."
}

New-Item -Path $resCacheFilePath -ItemType File -Force
$signtoolPath | Out-File -FilePath $resCacheFilePath

return $signtoolPath