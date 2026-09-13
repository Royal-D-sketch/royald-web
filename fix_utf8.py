import os
import re

views_dir = r'.\RoyalD.Web\Views'
for root, dirs, files in os.walk(views_dir):
    for name in files:
        if name.endswith('.cshtml'):
            path = os.path.join(root, name)
            with open(path, 'r', encoding='utf-8') as f:
                content = f.read()
            
            modified = False
            
            # 1. Select2 searchable
            new_content = re.sub(r'(<select[^>]*name="salesRep"[^>]*)class="([^"]*)"', 
                lambda m: m.group(0) if 'select2-searchable' in m.group(2) else f'{m.group(1)}class="{m.group(2)} select2-searchable"', 
                content)
            if new_content != content: modified = True; content = new_content
            
            new_content = re.sub(r'(<select[^>]*name="rep"[^>]*)class="([^"]*)"', 
                lambda m: m.group(0) if 'select2-searchable' in m.group(2) else f'{m.group(1)}class="{m.group(2)} select2-searchable"', 
                content)
            if new_content != content: modified = True; content = new_content
            
            new_content = re.sub(r'(<select[^>]*id="repSelectDropdown"[^>]*)class="([^"]*)"', 
                lambda m: m.group(0) if 'select2-searchable' in m.group(2) else f'{m.group(1)}class="{m.group(2)} select2-searchable"', 
                content)
            if new_content != content: modified = True; content = new_content
            
            # 2. WaitingGoods width
            if name == 'WaitingGoods.cshtml':
                new_content = content.replace('<th>รหัสสินค้า</th>', '<th style="min-width: 140px;">รหัสสินค้า</th>')
                new_content = new_content.replace('<td><code>@item.ProductCode</code></td>', '<td><code style="white-space: nowrap;">@item.ProductCode</code></td>')
                if new_content != content: modified = True; content = new_content
            
            # 3. Detail.cshtml BadDebt Date
            if name == 'Detail.cshtml' and 'Debtor' in root:
                old_html = '''<div id="f-baddebt" class="d-none mb-3 p-3 bg-danger bg-opacity-10 rounded border border-danger">
                            <label class="form-label fw-bold small text-danger">⚠️ ยอดตัดหนี้สูญ (บาท)</label>
                            <input type="number" step="0.01" name="badDebtAmount" class="form-control mb-2" value="@(debt?.BadDebtAmount ?? debt?.RemainingAmount)" />
                        </div>'''
                new_html = '''<div id="f-baddebt" class="d-none mb-3 p-3 bg-danger bg-opacity-10 rounded border border-danger">
                            <label class="form-label fw-bold small text-danger">⚠️ ยอดตัดหนี้สูญ (บาท)</label>
                            <input type="number" step="0.01" name="badDebtAmount" class="form-control mb-2" value="@(debt?.BadDebtAmount ?? debt?.RemainingAmount)" />
                            
                            <label class="form-label fw-bold small text-danger mt-2">📅 วันที่บันทึกหนี้สูญ</label>
                            <input type="date" name="badDebtDate" class="form-control mb-2" value="@(debt?.BadDebtDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"))" />
                        </div>'''
                if old_html in content:
                    new_content = content.replace(old_html, new_html)
                    if new_content != content: modified = True; content = new_content

            # 4. _Layout.cshtml Select2
            if name == '_Layout.cshtml':
                content = content.replace('https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css', 'https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/css/select2.min.css')
                content = content.replace('https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js', 'https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/js/select2.min.js')
                
                old_init = '''.select2-searchable.select2({
            theme: "classic",
            width: "100%"
        });'''
                new_init = '''.select2-searchable.select2({
            theme: "classic",
            width: "100%"
        }).on('select2:select', function(e) {
            var evt = document.createEvent('HTMLEvents');
            evt.initEvent('change', false, true);
            this.dispatchEvent(evt);
        });'''
                if old_init in content:
                    content = content.replace(old_init, new_init)
                modified = True

            if modified:
                with open(path, 'w', encoding='utf-8') as f:
                    f.write(content)

# Update Controllers
c_file = r'.\RoyalD.Web\Controllers\SalesBillController.cs'
with open(c_file, 'r', encoding='utf-8') as f: content = f.read()
content = re.sub(r'decimal\?\s*badDebtAmount,\s*string\[\]\s*waitingProductCodes,', 'decimal? badDebtAmount, DateTime? badDebtDate, string[] waitingProductCodes,', content)
old_c = '''else if (newStatus == DebtStatus.BadDebt)
            {
                debt.BadDebtAmount = badDebtAmount ?? debt.RemainingAmount;
                debt.RemainingAmount -= (debt.BadDebtAmount ?? 0);
                if (debt.RemainingAmount < 0) debt.RemainingAmount = 0;
            }'''
new_c = '''else if (newStatus == DebtStatus.BadDebt)
            {
                debt.BadDebtAmount = badDebtAmount ?? debt.RemainingAmount;
                debt.BadDebtDate = badDebtDate ?? DateTime.Today;
                debt.RemainingAmount -= (debt.BadDebtAmount ?? 0);
                if (debt.RemainingAmount < 0) debt.RemainingAmount = 0;
            }'''
content = content.replace(old_c, new_c)
with open(c_file, 'w', encoding='utf-8') as f: f.write(content)

c2_file = r'.\RoyalD.Web\Controllers\DebtorController.cs'
with open(c2_file, 'r', encoding='utf-8') as f: content = f.read()
old_c2 = '''ViewBag.SalesRep = salesRep;
            ViewBag.SalesReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>'''
new_c2 = '''ViewBag.SalesRep = salesRep;
            ViewBag.TotalCount = debts.Count();
            ViewBag.TotalAmount = debts.Sum(d => d.OriginalAmount);
            ViewBag.SalesReps = await _cache.GetOrCreateAsync("all_debtor_reps", async entry =>'''
content = content.replace(old_c2, new_c2)
with open(c2_file, 'w', encoding='utf-8') as f: f.write(content)

print("Done python script")
