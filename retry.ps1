 = ""https://royald-web-1.onrender.com/Fix/CustomerName""
for ( = 0;  -lt 15; ++) {
    try {
         = Invoke-WebRequest -Uri  -UseBasicParsing
        if (.StatusCode -eq 200) {
            Write-Host ""Success! ""
            break
        }
    } catch {
        Write-Host ""Attempt  failed: ""
    }
    Start-Sleep -Seconds 60
}
