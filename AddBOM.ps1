$utf8Bom = New-Object System.Text.UTF8Encoding $true
$files = Get-ChildItem -Path ".\RoyalD.Web\Views" -Recurse -Filter *.cshtml
foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName)
    [System.IO.File]::WriteAllText($file.FullName, $content, $utf8Bom)
}
Write-Host "BOM added to Views"
