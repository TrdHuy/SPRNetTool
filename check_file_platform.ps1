param (
    [string]$dllPath
)

function Get-PeHeaderType {
    param (
        [string]$Path
    )
    # Open the file for reading
    $fileStream = [System.IO.File]::OpenRead($Path)
    $binaryReader = New-Object System.IO.BinaryReader($fileStream)

    # Read the DOS header
    $dosHeader = $binaryReader.ReadBytes(64)
    $e_lfanew = [BitConverter]::ToUInt32($dosHeader, 60)

    # Move to the NT headers
    $fileStream.Seek($e_lfanew, [System.IO.SeekOrigin]::Begin) > $null
    $peHeader = $binaryReader.ReadUInt32()

    if ($peHeader -ne 0x00004550) {
        $fileStream.Close()
        throw "Invalid PE header."
    }

    # Read the file header
    $machineType = $binaryReader.ReadUInt16()

    $fileStream.Close()

    return $machineType
}

try {
    $machineType = Get-PeHeaderType -Path $dllPath

    switch ($machineType) {
        0x014c { Write-Output "x86" }
        0x8664 { Write-Output "x64" }
        default { Write-Output "Unknown" }
    }
}
catch {
    Write-Output "Error: $_"
    exit 1
}
