param(
    [string]$publishDir = "",
    [string]$publishUrl = ""
)

try {
    $publishDir = if ($publishDir) { $publishDir.Trim('"', ' ').TrimEnd('\') } else { "" }
    $publishUrl = if ($publishUrl) { $publishUrl.Trim('"', ' ').TrimEnd('\') } else { "" }

    $targetDir = ""
    if (![string]::IsNullOrWhiteSpace($publishDir) -and (Test-Path $publishDir)) {
        $targetDir = $publishDir
    } elseif (![string]::IsNullOrWhiteSpace($publishUrl) -and (Test-Path $publishUrl)) {
        $targetDir = $publishUrl
    }

    if ([string]::IsNullOrWhiteSpace($targetDir)) {
        Write-Host "[Publish Encryption] No publish directory specified or found."
        exit 0
    }

    $appSettingsPath = Join-Path $targetDir "appsettings.json"
    if (!(Test-Path $appSettingsPath)) {
        Write-Host "[Publish Encryption] appsettings.json not found in $targetDir"
        exit 0
    }

    $content = [System.IO.File]::ReadAllText($appSettingsPath, [System.Text.Encoding]::UTF8)

    # Match Password=... up to semicolon or quote
    $regex = [regex]'Password=([^;"]+)'
    $matches = $regex.Matches($content)

    if ($matches.Count -eq 0) {
        Write-Host "[Publish Encryption] No Password found in connection string."
        exit 0
    }

    $key = [System.Text.Encoding]::UTF8.GetBytes("GCTL#Enterprise#SecretKey#2026!!")
    $iv = [byte[]]@(15, 28, 73, 44, 95, 61, 82, 13, 94, 55, 66, 77, 88, 19, 30, 41)

    $updated = $false
    foreach ($m in $matches) {
        $plain = $m.Groups[1].Value.Trim()

        # Skip if already encrypted
        if ($plain.StartsWith("ENC(", [System.StringComparison]::OrdinalIgnoreCase)) {
            continue
        }

        # Encrypt with AES-256
        $aes = [System.Security.Cryptography.Aes]::Create()
        $aes.Key = $key
        $aes.IV = $iv

        $ms = New-Object System.IO.MemoryStream
        $cs = New-Object System.Security.Cryptography.CryptoStream($ms, $aes.CreateEncryptor(), [System.Security.Cryptography.CryptoStreamMode]::Write)
        $sw = New-Object System.IO.StreamWriter($cs, [System.Text.Encoding]::UTF8)
        $sw.Write($plain)
        $sw.Close()

        $cipher = [Convert]::ToBase64String($ms.ToArray())
        $encPassword = "ENC($cipher)"

        $content = $content.Replace($m.Value, "Password=$encPassword")
        $updated = $true
    }

    if ($updated) {
        [System.IO.File]::WriteAllText($appSettingsPath, $content, [System.Text.Encoding]::UTF8)
        Write-Host "[Publish Encryption] SUCCESS: Database password in published appsettings.json was automatically encrypted."
    } else {
        Write-Host "[Publish Encryption] Password was already encrypted or nothing to update."
    }
}
catch {
    Write-Warning "[Publish Encryption] Error: $($_.Exception.Message)"
}
