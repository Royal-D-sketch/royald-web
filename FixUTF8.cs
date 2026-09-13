using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

class Program {
    static void Main() {
        var utf8 = new UTF8Encoding(false); // No BOM
        string[] files = Directory.GetFiles(@"RoyalD.Web\Views", "*.cshtml", SearchOption.AllDirectories);
        foreach (var file in files) {
            string content = File.ReadAllText(file, Encoding.UTF8);
            bool modified = false;

            string newContent = Regex.Replace(content, "(<select[^>]*name=\"salesRep\"[^>]*)class=\"([^\"]*)\"", m => {
                if (!m.Groups[2].Value.Contains("select2-searchable")) return m.Groups[1].Value + "class=\"" + m.Groups[2].Value + " select2-searchable\"";
                return m.Value;
            });
            if (newContent != content) { modified = true; content = newContent; }

            newContent = Regex.Replace(content, "(<select[^>]*name=\"rep\"[^>]*)class=\"([^\"]*)\"", m => {
                if (!m.Groups[2].Value.Contains("select2-searchable")) return m.Groups[1].Value + "class=\"" + m.Groups[2].Value + " select2-searchable\"";
                return m.Value;
            });
            if (newContent != content) { modified = true; content = newContent; }

            newContent = Regex.Replace(content, "(<select[^>]*id=\"repSelectDropdown\"[^>]*)class=\"([^\"]*)\"", m => {
                if (!m.Groups[2].Value.Contains("select2-searchable")) return m.Groups[1].Value + "class=\"" + m.Groups[2].Value + " select2-searchable\"";
                return m.Value;
            });
            if (newContent != content) { modified = true; content = newContent; }

            if (file.EndsWith("WaitingGoods.cshtml")) {
                newContent = content.Replace("<th>รหัสสินค้า</th>", "<th style=\"min-width: 140px;\">รหัสสินค้า</th>")
                                    .Replace("<td><code>@item.ProductCode</code></td>", "<td><code style=\"white-space: nowrap;\">@item.ProductCode</code></td>");
                if (newContent != content) { modified = true; content = newContent; }
            }

            if (file.EndsWith("Detail.cshtml") && file.Contains("Debtor")) {
                string oldHtml = @"<div id=""f-baddebt"" class=""d-none mb-3 p-3 bg-danger bg-opacity-10 rounded border border-danger"">
                            <label class=""form-label fw-bold small text-danger"">⚠️ ยอดตัดหนี้สูญ (บาท)</label>
                            <input type=""number"" step=""0.01"" name=""badDebtAmount"" class=""form-control mb-2"" value=""@(debt?.BadDebtAmount ?? debt?.RemainingAmount)"" />
                        </div>".Replace("\r\n", "\n");
                string newHtml = @"<div id=""f-baddebt"" class=""d-none mb-3 p-3 bg-danger bg-opacity-10 rounded border border-danger"">
                            <label class=""form-label fw-bold small text-danger"">⚠️ ยอดตัดหนี้สูญ (บาท)</label>
                            <input type=""number"" step=""0.01"" name=""badDebtAmount"" class=""form-control mb-2"" value=""@(debt?.BadDebtAmount ?? debt?.RemainingAmount)"" />
                            
                            <label class=""form-label fw-bold small text-danger mt-2"">📅 วันที่บันทึกหนี้สูญ</label>
                            <input type=""date"" name=""badDebtDate"" class=""form-control mb-2"" value=""@(debt?.BadDebtDate?.ToString(""yyyy-MM-dd"") ?? DateTime.Today.ToString(""yyyy-MM-dd""))"" />
                        </div>".Replace("\r\n", "\n");
                newContent = content.Replace("\r\n", "\n").Replace(oldHtml, newHtml);
                if (newContent != content.Replace("\r\n", "\n")) { modified = true; content = newContent; }
            }

            if (file.EndsWith("_Layout.cshtml")) {
                content = content.Replace("https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/css/select2.min.css", "https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/css/select2.min.css")
                                 .Replace("https://cdn.jsdelivr.net/npm/select2@4.1.0-rc.0/dist/js/select2.min.js", "https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.13/js/select2.min.js");
                
                string oldInit = @"$("".select2-searchable"").select2({
            theme: ""classic"",
            width: ""100%""
        });".Replace("\r\n", "\n");
                string newInit = @"$("".select2-searchable"").select2({
            theme: ""classic"",
            width: ""100%""
        }).on('select2:select', function(e) {
            var evt = document.createEvent('HTMLEvents');
            evt.initEvent('change', false, true);
            this.dispatchEvent(evt);
        });".Replace("\r\n", "\n");
                
                content = content.Replace("\r\n", "\n").Replace(oldInit, newInit);
                modified = true;
            }

            if (modified) File.WriteAllText(file, content, utf8);
        }

        string cFile = @"RoyalD.Web\Controllers\SalesBillController.cs";
        string cContent = File.ReadAllText(cFile, Encoding.UTF8).Replace("\r\n", "\n");
        cContent = Regex.Replace(cContent, @"decimal\?\s*badDebtAmount,\s*string\[\]\s*waitingProductCodes,", "decimal? badDebtAmount, DateTime? badDebtDate, string[] waitingProductCodes,");
        string oldC = @"else if (newStatus == DebtStatus.BadDebt)
            {
                debt.BadDebtAmount = badDebtAmount ?? debt.RemainingAmount;
                debt.RemainingAmount -= (debt.BadDebtAmount ?? 0);
                if (debt.RemainingAmount < 0) debt.RemainingAmount = 0;
            }".Replace("\r\n", "\n");
        string newC = @"else if (newStatus == DebtStatus.BadDebt)
            {
                debt.BadDebtAmount = badDebtAmount ?? debt.RemainingAmount;
                debt.BadDebtDate = badDebtDate ?? DateTime.Today;
                debt.RemainingAmount -= (debt.BadDebtAmount ?? 0);
                if (debt.RemainingAmount < 0) debt.RemainingAmount = 0;
            }".Replace("\r\n", "\n");
        cContent = cContent.Replace(oldC, newC);
        File.WriteAllText(cFile, cContent, utf8);

        string c2File = @"RoyalD.Web\Controllers\DebtorController.cs";
        string c2Content = File.ReadAllText(c2File, Encoding.UTF8).Replace("\r\n", "\n");
        string oldC2 = @"ViewBag.SalesRep = salesRep;
            ViewBag.SalesReps = await _cache.GetOrCreateAsync(""all_debtor_reps"", async entry =>".Replace("\r\n", "\n");
        string newC2 = @"ViewBag.SalesRep = salesRep;
            ViewBag.TotalCount = debts.Count();
            ViewBag.TotalAmount = debts.Sum(d => d.OriginalAmount);
            ViewBag.SalesReps = await _cache.GetOrCreateAsync(""all_debtor_reps"", async entry =>".Replace("\r\n", "\n");
        c2Content = c2Content.Replace(oldC2, newC2);
        File.WriteAllText(c2File, c2Content, utf8);

        Console.WriteLine("Done!");
    }
}
