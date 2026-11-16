[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

$passwords = @('123456', '123', 'admin', 'zohan', 'Zohan@123', '123qwe', '12345678')
foreach ($pwd in $passwords) {
    try {
        $body = @{ userNameOrEmailAddress = 'Zohan'; password = $pwd; rememberClient = $true } | ConvertTo-Json
        $headers = @{ "Abp.TenantId" = "2005" }
        $login = Invoke-RestMethod -Uri 'https://localhost:44311/api/TokenAuth/Authenticate' -Method Post -Body $body -Headers $headers -ContentType 'application/json'
        if ($login.result.accessToken) {
            Write-Output "Found password: $pwd"
            $token = $login.result.accessToken
            $authHeaders = @{ Authorization = "Bearer $token"; "Abp.TenantId" = "2005" }
            $branch = Invoke-RestMethod -Uri 'https://localhost:44311/api/services/app/Branch/Get?Id=1020' -Method Get -Headers $authHeaders
            Write-Output ("Initial Branch 1020: Name=" + $branch.result.name + " Lat=" + $branch.result.latitude + " Long=" + $branch.result.longitude)
            $branch.result.latitude = 31.4869
            $branch.result.longitude = 74.2316
            $updatePayload = $branch.result | ConvertTo-Json -Depth 5
            $updated = Invoke-RestMethod -Uri 'https://localhost:44311/api/services/app/Branch/Update' -Method Put -Body $updatePayload -ContentType 'application/json' -Headers $authHeaders
            Write-Output ("Update Result: Lat=" + $updated.result.latitude + " Long=" + $updated.result.longitude)
            $fresh = Invoke-RestMethod -Uri 'https://localhost:44311/api/services/app/Branch/Get?Id=1020' -Method Get -Headers $authHeaders
            Write-Output ("Fresh Get After Update: Lat=" + $fresh.result.latitude + " Long=" + $fresh.result.longitude)
            break
        }
    } catch {
        # continue
    }
}
