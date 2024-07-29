param (
     [string]$prvtoken,
     [string]$assemblyfilters,
     [string]$coverageOutDir,
     [string]$config,
     [string]$platform,
     [bool]$isUploadArtifactToGitHubPage = $false,
     [string]$ownerRepoToUploadArtifact,
     [string]$branchRepoToUploadArtifact,
     [string]$rootFolderToUploadArtifact,
     [string]$projectIdToUploadArtifact,
     [string]$reportSHAToUploadArtifact,
     [string]$gitHubPageRepoToUploadArtifact,
     [Int16]$coverageRetentionHours = 24
)

function UploadFolderToGitHub {
     param (
          [string]$token,
          [string]$user,
          [string]$repo,
          [string]$branch,
          [string]$localFolderPath,
          [string]$remoteFolderPath,
          [string]$commitMessage
     )
 
     # URL API cơ bản
     $baseApiUrl = "https://api.github.com/repos/$user/$repo"
 
     # Lấy SHA của nhánh master
     $refApiUrl = "$baseApiUrl/git/ref/heads/$branch"
     $refResponse = Invoke-RestMethod -Uri $refApiUrl -Headers @{Authorization = "token $token" } -Method Get
     $latestCommitSha = $refResponse.object.sha
 
     # Lấy SHA của cây (tree) hiện tại
     $commitApiUrl = "$baseApiUrl/git/commits/$latestCommitSha"
     $commitResponse = Invoke-RestMethod -Uri $commitApiUrl -Headers @{Authorization = "token $token" } -Method Get
     $baseTreeSha = $commitResponse.tree.sha
 
     # Tạo một danh sách các tệp cần upload
     $files = Get-ChildItem -Recurse -File -Path $localFolderPath
     $blobs = @()
     $treeItems = @()
 
     # Tổng số tệp cần upload
     $totalFiles = $files.Count
     $currentFileIndex = 0
 
     foreach ($file in $files) {
          $currentFileIndex++
 
          # Cập nhật tiến độ
          Write-Progress -Activity "Uploading files to GitHub" -Status "Processing file $currentFileIndex of $totalFiles" -PercentComplete (($currentFileIndex / $totalFiles) * 100)
 
          $filePath = $file.FullName
          $relativePath = $file.FullName.Substring($localFolderPath.Length + 1).Replace("\", "/")
          $remotePath = "$remoteFolderPath/$relativePath"
          $remotePath = $remotePath.Replace("\", "/")
          $fileContent = [System.IO.File]::ReadAllBytes($filePath)
          $fileContentBase64 = [Convert]::ToBase64String($fileContent)
 
          # Tạo blob
          $blobResponse = Invoke-RestMethod -Uri "$baseApiUrl/git/blobs" -Headers @{Authorization = "token $token" } -Method Post -Body (@{content = $fileContentBase64; encoding = "base64" } | ConvertTo-Json)
          $blobSha = $blobResponse.sha
 
          # Thêm vào tree
          $treeItems += @{
               path = $remotePath
               mode = "100644"
               type = "blob"
               sha  = $blobSha
          }
     }
 
     # Tạo tree mới
     $newTreeResponse = Invoke-RestMethod -Uri "$baseApiUrl/git/trees" -Headers @{Authorization = "token $token" } -Method Post -Body (@{base_tree = $baseTreeSha; tree = $treeItems } | ConvertTo-Json)
     $newTreeSha = $newTreeResponse.sha
 
     # Tạo commit mới
     $newCommitResponse = Invoke-RestMethod -Uri "$baseApiUrl/git/commits" -Headers @{Authorization = "token $token" } -Method Post -Body (@{message = $commitMessage; parents = @($latestCommitSha); tree = $newTreeSha } | ConvertTo-Json)
     $newCommitSha = $newCommitResponse.sha
 
     # Cập nhật ref để trỏ tới commit mới
     $updateRefResponse = Invoke-RestMethod -Uri "$baseApiUrl/git/refs/heads/$branch" -Headers @{
          Authorization  = "token $token"
          "Content-Type" = "application/json"
     } -Method Patch -Body (@{sha = $newCommitSha; force = $true } | ConvertTo-Json)

     Write-Output "Commit created successfully with SHA: $newCommitSha"
}
 

$SCRIPT_ROOT = (Get-Location).Path
$LOG_TAG = "UNITTESTUTIL"

if (-not $prvtoken) {
     Write-Host "Assign local variable"
     $localXmlString = Get-Content -Raw -Path "local.xml"
	
     # Tạo đối tượng XmlDocument và load chuỗi XML vào nó
     $localXmlDoc = New-Object System.Xml.XmlDocument
     $localXmlDoc.PreserveWhitespace = $true
     $localXmlDoc.LoadXml($localXmlString)
     $prvtoken = $localXmlDoc.configuration.GITHUB_TOKEN
     $platform = $localXmlDoc.configuration.PLATFORM
     $assemblyfilters = $localXmlDoc.configuration.ASSEMBLY_FILTER
     $coverageOutDir = $localXmlDoc.configuration.COVERAGE_OURDIR
     $config = $localXmlDoc.configuration.CONFIG
     $isUploadArtifactToGitHubPage = [bool]::Parse($localXmlDoc.configuration.IS_UPLOAD_ARTIFACT_TO_GIT_PAGE)
     $ownerRepoToUploadArtifact = $localXmlDoc.configuration.OWNER_TO_UPLOAD_ARTIFACT
     $branchRepoToUploadArtifact = $localXmlDoc.configuration.BRANCH_TO_UPLOAD_ARTIFACT
     $rootFolderToUploadArtifact = $localXmlDoc.configuration.ROOT_FOLDER_TO_UPLOAD_ARTIFACT
     $projectIdToUploadArtifact = $localXmlDoc.configuration.PROJECT_ID_TO_UPLOAD_ARTIFACT
     $reportSHAToUploadArtifact = $localXmlDoc.configuration.REPORT_SHA_TO_UPLOAD_ARTIFACT
     $gitHubPageRepoToUploadArtifact = $localXmlDoc.configuration.GIT_REPO_TO_UPLOAD_ARTIFACT
     $coverageRetentionHours = 24
}

if (-not $prvtoken) {
     throw "======= $LOG_TAG FATAL =====> prvtoken must not be null."
}
if (-not $coverageOutDir) {
     throw "======= $LOG_TAG  FATAL =====> coverageOutDir must not be null."
}
if (-not $platform -or ($platform -ne "x86" -and $platform -ne "x64")) {
     throw "======= $LOG_TAG FATAL =====> platform must not be x86 or x64"
}
if (-not $config -or ($config -ne "Release" -and $config -ne "Debug")) {
     throw "======= $LOG_TAG FATAL =====> config must not be Release or Debug."
}
if (-not $assemblyfilters) {
     throw "======= $LOG_TAG FATAL =====> assemblyfilters must not be null."
}
if ($isUploadArtifactToGitHubPage -eq $true) {
     if (-not $ownerRepoToUploadArtifact) {
          throw "======= $LOG_TAG FATAL =====> ownerRepoToUploadArtifact must not be null."
     }
     if (-not $projectIdToUploadArtifact) {
          throw "======= $LOG_TAG FATAL =====> projectIdToUploadArtifact must not be null."
     }
     if (-not $reportSHAToUploadArtifact) {
          throw "======= $LOG_TAG FATAL =====> reportSHAToUploadArtifact must not be null."
     }
     if (-not $gitHubPageRepoToUploadArtifact) {
          throw "======= $LOG_TAG FATAL =====> gitHubPageRepoToUploadArtifact must not be null."
     }
     if (-not $branchRepoToUploadArtifact) {
          throw "======= $LOG_TAG FATAL =====> branchRepoToUploadArtifact must not be null."
     }
     if (-not $rootFolderToUploadArtifact) {
          throw "======= $LOG_TAG FATAL =====> rootFolderToUploadArtifact must not be null."
     }
     if ($coverageRetentionHours -gt 24 * 7) {
          throw "======= $LOG_TAG FATAL =====> retentionPeriod must be less than 7 days."
     }
}


if ([System.IO.Path]::IsPathRooted($coverageOutDir)) {
}
else {
     if (Test-Path -Path $coverageOutDir -PathType Container) {
     }
     else {
          New-Item -Path $coverageOutDir -ItemType Directory | Out-Null
     }
     $coverageOutDir = Convert-Path -Path $coverageOutDir
}
$coverageOutDir = "$coverageOutDir\$platform\$config"

# $outputFilePath = "$coverageOutDir\properties.json"
# $jsonContent = Get-Content -Path $outputFilePath -Raw
# $object = $jsonContent | ConvertFrom-Json
# $currentTime = Get-Date
# $creationTime = [datetime]$object.CreationDate
# $retentionPeriod = [timespan]::FromHours($object.RetentionPeriodHours)
# $expirationTime = $creationTime + $retentionPeriod
# if ($currentTime -ge $expirationTime) {
#      Write-Output "The retention period has expired."
# }
# else {
#      Write-Output "The retention period has not yet expired."
# }


# Run dotnet test and capture output
dotnet test --collect:"XPlat Code Coverage" `
     --configuration $config -p:Platform=$platform `
     -p:GitToken=$prvtoken | Tee-Object -Variable output

# Extract the coverage file path

# Iterate through the output lines from the end to find the last coverage file path
for ($i = $output.Count - 1; $i -ge 0; $i--) {
     if ($output[$i] -match "Attachments:") {
          $coverageFilePath = $output[$i + 1] -replace "^\s*|\s*$", ""
          break
     }
}

if ($coverageFilePath) {
     reportgenerator -reports:$coverageFilePath `
          -targetdir:$coverageOutDir `
          -reporttypes:"HtmlInline;Cobertura" `
          -verbosity:Verbose `
          -assemblyfilters:"$assemblyfilters"
     if (Test-Path -Path $coverageOutDir -PathType Container) {
          if ($isUploadArtifactToGitHubPage -eq $true) {
               $nowTicks = ([DateTime]::Now).Ticks
               $localFolderPath = $coverageOutDir
               $remoteArtifactFolderPath = "$rootFolderToUploadArtifact\$projectIdToUploadArtifact\$nowTicks"
               #Create properties.json
               $properties = @{
                    CreationDate         = Get-Date
                    NowTicks             = $nowTicks
                    RetentionPeriodHours = $coverageRetentionHours
                    SHA                  = $reportSHAToUploadArtifact
                    ModuleId             = $projectIdToUploadArtifact
                    Platform             = $platform
                    AssemblyFilters      = $assemblyfilters
                    RepoContainer        = $gitHubPageRepoToUploadArtifact
                    RepoContainerOwner   = $ownerRepoToUploadArtifact
                    RootArtifactFolder   = $rootFolderToUploadArtifact
               }
               $json = $properties | ConvertTo-Json
               $outputFilePath = "$coverageOutDir\properties.json"
               $json | Out-File -FilePath $outputFilePath -Encoding utf8
               Write-Output "$LOG_TAG properties has been saved to $outputFilePath"

               UploadFolderToGitHub -token $prvtoken `
                    -user $ownerRepoToUploadArtifact `
                    -repo $gitHubPageRepoToUploadArtifact `
                    -branch $branchRepoToUploadArtifact `
                    -localFolderPath $localFolderPath `
                    -remoteFolderPath $remoteArtifactFolderPath `
                    -commitMessage "[#1] Upload coverage report for: $projectIdToUploadArtifact_$nowTicks"
          }
     }
    
}
else {
     Write-Output "$LOG_TAG Coverage file path not found in the test output."
}