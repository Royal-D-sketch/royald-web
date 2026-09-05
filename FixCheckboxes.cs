
using System;
using System.IO;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string[] files = { @"RoyalD.Web\Views\Account\CreateUser.cshtml", @"RoyalD.Web\Views\Account\EditUser.cshtml" };
        foreach (var file in files) {
            string content = File.ReadAllText(file);
            
            // Generate checkboxes
            string isEdit = file.Contains("EditUser") ? "true" : "false";
            string getCheck(string val) {
                if (isEdit == "true") return $"checked=\"@(Model.Role == \\\"admin\\\" || userPages.Contains(\\\"{val.ToLower()}\\\"))\"";
                return ""; // For create, leave unchecked by default except the pre-checked ones, but these new ones we can leave unchecked.
            }
            
            string newBoxes = $@"
                        <div class=""col-md-3"">
                            <div class=""p-3 perm-card"">
                                <div class=""form-check"">
                                    <input class=""form-check-input page-check"" type=""checkbox"" name=""pages"" value=""CustomerProduct"" id=""p_custprod"" {getCheck("customerproduct")}>
                                    <label class=""form-check-label fw-bold"" for=""p_custprod"" style=""color:#0f766e;"">
                                        <i class=""bi bi-shop me-1""></i> ขายรายลูกค้า
                                    </label>
                                </div>
                                <small class=""text-muted d-block ms-4"" style=""font-size:0.78rem;"">สินค้าที่ลูกค้าซื้อ</small>
                            </div>
                        </div>
                        <div class=""col-md-3"">
                            <div class=""p-3 perm-card"">
                                <div class=""form-check"">
                                    <input class=""form-check-input page-check"" type=""checkbox"" name=""pages"" value=""CustomerPurchaseSummary"" id=""p_custsum"" {getCheck("customerpurchasesummary")}>
                                    <label class=""form-check-label fw-bold"" for=""p_custsum"" style=""color:#4338ca;"">
                                        <i class=""bi bi-person-lines-fill me-1""></i> สรุปซื้อรายลูกค้า
                                    </label>
                                </div>
                                <small class=""text-muted d-block ms-4"" style=""font-size:0.78rem;"">สรุปยอดแต่ละเดือน</small>
                            </div>
                        </div>
                        <div class=""col-md-3"">
                            <div class=""p-3 perm-card"">
                                <div class=""form-check"">
                                    <input class=""form-check-input page-check"" type=""checkbox"" name=""pages"" value=""Installment"" id=""p_install"" {getCheck("installment")}>
                                    <label class=""form-check-label fw-bold"" for=""p_install"" style=""color:#d97706;"">
                                        <i class=""bi bi-credit-card-fill me-1""></i> ลูกค้าผ่อนชำระ
                                    </label>
                                </div>
                                <small class=""text-muted d-block ms-4"" style=""font-size:0.78rem;"">บิลผ่อนชำระ</small>
                            </div>
                        </div>
                        <div class=""col-md-3"">
                            <div class=""p-3 perm-card"">
                                <div class=""form-check"">
                                    <input class=""form-check-input page-check"" type=""checkbox"" name=""pages"" value=""BadDebt"" id=""p_baddebt"" {getCheck("baddebt")}>
                                    <label class=""form-check-label fw-bold"" for=""p_baddebt"" style=""color:#dc2626;"">
                                        <i class=""bi bi-exclamation-octagon me-1""></i> หนี้สูญ
                                    </label>
                                </div>
                                <small class=""text-muted d-block ms-4"" style=""font-size:0.78rem;"">รายการหนี้สูญ</small>
                            </div>
                        </div>
                        <div class=""col-md-3"">
                            <div class=""p-3 perm-card"">
                                <div class=""form-check"">
                                    <input class=""form-check-input page-check"" type=""checkbox"" name=""pages"" value=""ReturnNotes"" id=""p_return"" {getCheck("returnnotes")}>
                                    <label class=""form-check-label fw-bold"" for=""p_return"" style=""color:#6b7280;"">
                                        <i class=""bi bi-arrow-return-left me-1""></i> รับคืนสินค้า
                                    </label>
                                </div>
                                <small class=""text-muted d-block ms-4"" style=""font-size:0.78rem;"">รายการรับคืน</small>
                            </div>
                        </div>
";
            
            // Insert before the end of the pagePermissionsBlock
            // Find the last occurrence of col-md-3 before the end of row g-2 mb-4
            int idx = content.IndexOf("id=\"pagePermissionsBlock\"");
            if (idx == -1) continue;
            
            int endIdx = content.IndexOf("</form>", idx);
            if (endIdx == -1) continue;
            
            // We want to insert the newBoxes right after the last col-md-3 block in that row.
            // Actually, we can just replace the closing tag of the last col-md-3 block before the next row or something.
            // Let s just find "value=\"Upload\"" block and insert it after that.
            int uploadIdx = content.IndexOf("value=\"Upload\"", idx);
            if (uploadIdx != -1) {
                int uploadEnd = content.IndexOf("</div>\n                          </div>", uploadIdx);
                if (uploadEnd != -1) {
                    content = content.Insert(uploadEnd + 38, "\n" + newBoxes);
                }
            }
            
            File.WriteAllText(file, content);
        }
    }
}

