using System;
using System.IO;
using System.Text.RegularExpressions;

class ProgramFix {
    static void Main() {
        string path = @"Controllers\SalesBillController.cs";
        string content = File.ReadAllText(path);
        
        // Fix Pay
        content = Regex.Replace(content, 
            @"public async Task<IActionResult> Pay.*?await _db\.SaveChangesAsync\(\);\s*return RedirectToAction\(""Detail"", new \{ id = billNo \}\);\s*}",
            MatchPay, RegexOptions.Singleline);
            
        // Fix UpdateStatus
        content = Regex.Replace(content, 
            @"public async Task<IActionResult> UpdateStatus.*?await _db\.SaveChangesAsync\(\);\s*return RedirectToAction\(""Detail"", new \{ id = billNo \}\);\s*}",
            MatchUpdateStatus, RegexOptions.Singleline);

        File.WriteAllText(path, content);
    }
    
    static string MatchPay(Match m) {
        string s = m.Value;
        s = s.Replace("if (bill == null) return NotFound();", "if (bill == null && debt == null) return NotFound();");
        s = s.Replace("if (bill.IsFullyPaid)", "if (bill != null && bill.IsFullyPaid)");
        s = s.Replace("if (debt == null)", "if (debt == null && bill != null)");
        s = s.Replace("bill.IsFullyPaid = true;", "if (bill != null) bill.IsFullyPaid = true;");
        s = s.Replace("bill.IsFullyPaid = false;", "if (bill != null) bill.IsFullyPaid = false;");
        s = s.Replace("return RedirectToAction(\"Detail\", new { id = billNo });", "return !string.IsNullOrEmpty(Request.Headers[\"Referer\"]) ? Redirect(Request.Headers[\"Referer\"].ToString()) : RedirectToAction(\"Detail\", new { id = billNo });");
        
        // also fix var debt = ... to be declared BEFORE if (bill == null && debt == null)
        s = s.Replace("var bill = await _db.SalesBills.FirstOrDefaultAsync(b => b.BillNo == billNo);\r\n            if (bill == null && debt == null) return NotFound();", "var bill = await _db.SalesBills.FirstOrDefaultAsync(b => b.BillNo == billNo);\r\n            var existingDebt = await _db.OutstandingDebts.Include(d => d.PaymentRecords).FirstOrDefaultAsync(d => d.BillNo == billNo);\r\n            if (bill == null && existingDebt == null) return NotFound();");
        s = s.Replace("var debt = await _db.OutstandingDebts.Include(d => d.PaymentRecords).FirstOrDefaultAsync(d => d.BillNo == billNo);", "var debt = existingDebt;");
        return s;
    }
    
    static string MatchUpdateStatus(Match m) {
        string s = m.Value;
        s = s.Replace("if (bill == null) return NotFound();", "var existingDebt = await _db.OutstandingDebts.Include(d => d.PendingProducts).Include(d => d.Attachments).FirstOrDefaultAsync(d => d.BillNo == billNo);\r\n            if (bill == null && existingDebt == null) return NotFound();");
        s = s.Replace("if (bill.IsFullyPaid", "if (bill != null && bill.IsFullyPaid");
        s = s.Replace("var debt = await _db.OutstandingDebts\r\n                .Include(d => d.PendingProducts)\r\n                .Include(d => d.Attachments)\r\n                .FirstOrDefaultAsync(d => d.BillNo == billNo);", "var debt = existingDebt;");
        s = s.Replace("bill.IsFullyPaid = true;", "if (bill != null) bill.IsFullyPaid = true;");
        s = s.Replace("bill.IsFullyPaid = false;", "if (bill != null) bill.IsFullyPaid = false;");
        s = s.Replace("return RedirectToAction(\"Detail\", new { id = billNo });", "return !string.IsNullOrEmpty(Request.Headers[\"Referer\"]) ? Redirect(Request.Headers[\"Referer\"].ToString()) : RedirectToAction(\"Detail\", new { id = billNo });");
        return s;
    }
}
