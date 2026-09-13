$utf8 = New-Object System.Text.UTF8Encoding $false
$files = Get-ChildItem -Path ".\RoyalD.Web\Views" -Recurse -Filter *.cshtml
foreach ($file in $files) {
    $content = [System.IO.File]::ReadAllText($file.FullName, $utf8)
    $modified = $false

    $content = [regex]::Replace($content, '(<select[^>]*name="salesRep"[^>]*)class="([^"]*)"', {
        param($m)
        if ($m.Groups[2].Value -notmatch 'select2-searchable') { return $m.Groups[1].Value + 'class="' + $m.Groups[2].Value + ' select2-searchable"' }
        return $m.Value
    })
    $content = [regex]::Replace($content, '(<select[^>]*name="rep"[^>]*)class="([^"]*)"', {
        param($m)
        if ($m.Groups[2].Value -notmatch 'select2-searchable') { return $m.Groups[1].Value + 'class="' + $m.Groups[2].Value + ' select2-searchable"' }
        return $m.Value
    })
    $content = [regex]::Replace($content, '(<select[^>]*id="repSelectDropdown"[^>]*)class="([^"]*)"', {
        param($m)
        if ($m.Groups[2].Value -notmatch 'select2-searchable') { return $m.Groups[1].Value + 'class="' + $m.Groups[2].Value + ' select2-searchable"' }
        return $m.Value
    })

    if ($file.Name -eq "WaitingGoods.cshtml") {
        $content = $content.Replace("<th>รหัสสินค้า</th>", "<th style=`"min-width: 140px;`">รหัสสินค้า</th>")
        $content = $content.Replace("<td><code>@item.ProductCode</code></td>", "<td><code style=`"white-space: nowrap;`">@item.ProductCode</code></td>")
    }

    if ($file.Name -eq "Detail.cshtml" -and $file.FullName -match "Debtor") {
        $oldHtml = '<div id="f-baddebt" class="d-none mb-3 p-3 bg-danger bg-opacity-10 rounded border border-danger">
                            <label class="form-label fw-bold small text-danger">⚠️ ยอดตัดหนี้สูญ (บาท)</label>
                            <input type="number" step="0.01" name="badDebtAmount" class="form-control mb-2" value="@(debt?.BadDebtAmount ?? debt?.RemainingAmount)" />
                        </div>'
        $newHtml = '<div id="f-baddebt" class="d-none mb-3 p-3 bg-danger bg-opacity-10 rounded border border-danger">
                            <label class="form-label fw-bold small text-danger">⚠️ ยอดตัดหนี้สูญ (บาท)</label>
                            <input type="number" step="0.01" name="badDebtAmount" class="form-control mb-2" value="@(debt?.BadDebtAmount ?? debt?.RemainingAmount)" />
                            
                            <label class="form-label fw-bold small text-danger mt-2">📅 วันที่บันทึกหนี้สูญ</label>
                            <input type="date" name="badDebtDate" class="form-control mb-2" value="@(debt?.BadDebtDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"))" />
                        </div>'
        
        $oldHtml = $oldHtml -replace "`r`n", "`n"
        $newHtml = $newHtml -replace "`r`n", "`n"
        $content = $content -replace "`r`n", "`n"
        $content = $content.Replace($oldHtml, $newHtml)
    }

    if ($file.Name -eq "_Layout.cshtml") {
        $content = $content.Replace('https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css', 'https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/css/select2.min.css')
        $content = $content.Replace('https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js', 'https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/js/select2.min.js')
        
        $oldInit = '$(".select2-searchable").select2({
            theme: "classic",
            width: "100%"
        });'
        $newInit = '$(".select2-searchable").select2({
            theme: "classic",
            width: "100%"
        }).on(''select2:select'', function(e) {
            var evt = document.createEvent(''HTMLEvents'');
            evt.initEvent(''change'', false, true);
            this.dispatchEvent(evt);
        });'
        
        $oldInit = $oldInit -replace "`r`n", "`n"
        $newInit = $newInit -replace "`r`n", "`n"
        $content = $content -replace "`r`n", "`n"
        $content = $content.Replace($oldInit, $newInit)
    }

    [System.IO.File]::WriteAllText($file.FullName, $content, $utf8)
}

$cFile = ".\RoyalD.Web\Controllers\SalesBillController.cs"
$cContent = [System.IO.File]::ReadAllText($cFile, $utf8)
$cContent = [regex]::Replace($cContent, "decimal\?\s*badDebtAmount,\s*string\[\]\s*waitingProductCodes,", "decimal? badDebtAmount, DateTime? badDebtDate, string[] waitingProductCodes,")
$oldC = 'else if (newStatus == DebtStatus.BadDebt)
            {
                debt.BadDebtAmount = badDebtAmount ?? debt.RemainingAmount;
                debt.RemainingAmount -= (debt.BadDebtAmount ?? 0);
                if (debt.RemainingAmount < 0) debt.RemainingAmount = 0;
            }'
$newC = 'else if (newStatus == DebtStatus.BadDebt)
            {
                debt.BadDebtAmount = badDebtAmount ?? debt.RemainingAmount;
                debt.BadDebtDate = badDebtDate ?? DateTime.Today;
                debt.RemainingAmount -= (debt.BadDebtAmount ?? 0);
                if (debt.RemainingAmount < 0) debt.RemainingAmount = 0;
            }'
$oldC = $oldC -replace "`r`n", "`n"
$newC = $newC -replace "`r`n", "`n"
$cContent = $cContent -replace "`r`n", "`n"
$cContent = $cContent.Replace($oldC, $newC)
[System.IO.File]::WriteAllText($cFile, $cContent, $utf8)

$c2File = ".\RoyalD.Web\Controllers\DebtorController.cs"
$c2Content = [System.IO.File]::ReadAllText($c2File, $utf8)
$oldC2 = 'ViewBag.SalesRep = salesRep;
            ViewBag.SalesReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>'
$newC2 = 'ViewBag.SalesRep = salesRep;
            ViewBag.TotalCount = debts.Count();
            ViewBag.TotalAmount = debts.Sum(d => d.OriginalAmount);
            ViewBag.SalesReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>'
$oldC2 = $oldC2 -replace "`r`n", "`n"
$newC2 = $newC2 -replace "`r`n", "`n"
$c2Content = $c2Content -replace "`r`n", "`n"
$c2Content = $c2Content.Replace($oldC2, $newC2)
[System.IO.File]::WriteAllText($c2File, $c2Content, $utf8)

Write-Host "All done!"
