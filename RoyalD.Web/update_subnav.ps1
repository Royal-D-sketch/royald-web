 = @(
    "Views\SalesReport\Summary.cshtml",
    "Views\SalesReport\Charts.cshtml",
    "Views\SalesReport\ProductDetails.cshtml",
    "Views\Report\Sales.cshtml"
)

 = '(?s)<!-- Sub-Tabs Navigation Header -->.*?</div>'

 = @'
@{
    bool isSRep = User.HasClaim(c => c.Type == "Position" && (c.Value.Contains("ผู้แทน") || c.Value.Contains("พนักงานขาย"))) && !User.HasClaim(c => c.Type == "Role" && c.Value == "Admin");
}
<!-- Sub-Tabs Navigation Header -->
<div class="sales-subnav no-print">
    <a href="/SalesReport/Summary" class="subnav-item @(ViewContext.RouteData.Values["action"]?.ToString() == "Summary" || ViewContext.RouteData.Values["action"]?.ToString() == "Sales" ? "active" : "")">
        <i class="bi bi-table me-2"></i> 1. สรุปยอดขาย-ยอดเก็บเงิน (Pivot Matrix)
    </a>
    <a href="/SalesReport/Charts" class="subnav-item @(ViewContext.RouteData.Values["action"]?.ToString() == "Charts" ? "active" : "")">
        <i class="bi bi-bar-chart-line me-2"></i> 2. กราฟผลงานรายผู้แทน (Charts)
    </a>
    <a href="/SalesReport/ProductDetails" class="subnav-item @(ViewContext.RouteData.Values["action"]?.ToString() == "ProductDetails" ? "active" : "")">
        <i class="bi bi-box-seam me-2"></i> 3. รายละเอียดสินค้าที่ขาย (Product Details)
    </a>
    @if (!isSRep)
    {
        <a href="/SalesReport/Compare" class="subnav-item @(ViewContext.RouteData.Values["action"]?.ToString() == "Compare" ? "active" : "")">
            <i class="bi bi-people me-2"></i> 4. กราฟเปรียบเทียบผู้แทนขาย
        </a>
    }
</div>
'@

foreach ($f in $files) {
    if (Test-Path $f) {
        $c = [IO.File]::ReadAllText($f, [System.Text.Encoding]::UTF8)
        $c = $c -replace $subnavRegex, $newSubnav
        [IO.File]::WriteAllText($f, $c, [System.Text.Encoding]::UTF8)
        Write-Host "Updated $f"
    }
}
