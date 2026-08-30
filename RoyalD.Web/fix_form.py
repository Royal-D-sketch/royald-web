import re
import sys

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    # We will replace the <form ... Pay ...> entirely with our new one.
    # Because there are two identical forms in the file, we can just replace them both.
    
    new_form_html = '''<form asp-controller="SalesBill" asp-action="Pay" method="post" enctype="multipart/form-data" onsubmit="return syncPayment()">
                        @Html.AntiForgeryToken()
                        <input type="hidden" name="billNo" value="@Model.BillNo" />
                        <input type="hidden" name="method" value="Cash" id="methodField" />
                        
                        <input type="hidden" name="amount" id="realAmount" value="@(debt?.RemainingAmount ?? Model.TotalAmount)" />
                        <input type="hidden" name="payDate" id="realPayDate" value="@DateTime.Today.ToString("yyyy-MM-dd")" />

                        <!-- Nav Tabs: เงินสด / โอนเงิน / เช็ค -->
                        <ul class="nav nav-pills nav-fill mb-3" id="payTabs" role="tablist">
                            <li class="nav-item">
                                <button class="nav-link active fw-bold" id="cash-tab" data-bs-toggle="tab" data-bs-target="#cash-pane" type="button" onclick="setPayMethod('Cash')">
                                    💵 เงินสด (Cash)
                                </button>
                            </li>
                            <li class="nav-item">
                                <button class="nav-link fw-bold" id="transfer-tab" data-bs-toggle="tab" data-bs-target="#transfer-pane" type="button" onclick="setPayMethod('Transfer')">
                                    📱 โอนเงิน (Transfer)
                                </button>
                            </li>
                            <li class="nav-item">
                                <button class="nav-link fw-bold" id="check-tab" data-bs-toggle="tab" data-bs-target="#check-pane" type="button" onclick="setPayMethod('Check')">
                                    🏦 เช็ค (Check)
                                </button>
                            </li>
                        </ul>

                        <div class="tab-content" id="payTabsContent">
                            <!-- เงินสด -->
                            <div class="tab-pane fade show active" id="cash-pane">
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">จำนวนเงินรับชำระ (Amount) <span class="text-danger">*</span></label>
                                    <input type="number" step="0.01" id="amount_Cash" class="form-control form-control-lg fw-bold text-success" required max="@(debt?.RemainingAmount ?? Model.TotalAmount)" value="@(debt?.RemainingAmount ?? Model.TotalAmount)" />
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">วันที่รับเงินสด (Date)</label>
                                    <input type="date" id="payDate_Cash" class="form-control" value="@DateTime.Today.ToString("yyyy-MM-dd")" />
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">หมายเหตุ (Note)</label>
                                    <input type="text" name="note" id="note_Cash" class="form-control" placeholder="เช่น รับเงินสดหน้าร้าน" />
                                </div>
                            </div>

                            <!-- โอนเงิน -->
                            <div class="tab-pane fade" id="transfer-pane">
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">จำนวนเงินโอน (Amount) <span class="text-danger">*</span></label>
                                    <input type="number" step="0.01" id="amount_Transfer" class="form-control form-control-lg fw-bold text-success" max="@(debt?.RemainingAmount ?? Model.TotalAmount)" value="@(debt?.RemainingAmount ?? Model.TotalAmount)" />
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">วันที่โอนเงิน (Transfer Date)</label>
                                    <input type="date" id="payDate_Transfer" class="form-control" value="@DateTime.Today.ToString("yyyy-MM-dd")" />
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">แนบรูปสลิปโอนเงิน (Slip Image)</label>
                                    <input type="file" name="file" class="form-control" accept="image/*,.pdf" />
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">หมายเหตุ (Note)</label>
                                    <input type="text" id="note_Transfer" class="form-control" placeholder="เช่น โอนเข้าบัญชีกสิกร" />
                                </div>
                            </div>

                            <!-- เช็ค -->
                            <div class="tab-pane fade" id="check-pane">
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">จำนวนเงินตามเช็ค (Amount) <span class="text-danger">*</span></label>
                                    <input type="number" step="0.01" id="amount_Check" class="form-control form-control-lg fw-bold text-success" max="@(debt?.RemainingAmount ?? Model.TotalAmount)" value="@(debt?.RemainingAmount ?? Model.TotalAmount)" />
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">วันที่รับเช็ค (Receive Date)</label>
                                    <input type="date" id="payDate_Check" class="form-control" value="@DateTime.Today.ToString("yyyy-MM-dd")" />
                                </div>
                                <div class="row g-2 mb-3">
                                    <div class="col-md-6">
                                        <label class="form-label fw-bold small text-muted">วันที่ในหน้าเช็ค (Cheque Date)</label>
                                        <input type="date" name="checkDate" class="form-control" />
                                    </div>
                                    <div class="col-md-6">
                                        <label class="form-label fw-bold small text-muted">ธนาคาร (Bank)</label>
                                        <input type="text" name="bank" class="form-control" placeholder="เช่น กสิกร, กรุงเทพ" />
                                    </div>
                                    <div class="col-md-12">
                                        <label class="form-label fw-bold small text-muted">เลขที่เช็ค (Check No.)</label>
                                        <input type="text" name="checkNo" class="form-control" placeholder="เช่น 1234567" />
                                    </div>
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">แนบรูปหน้าเช็ค (Cheque Image)</label>
                                    <input type="file" name="file" class="form-control" accept="image/*,.pdf" />
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold small text-muted">หมายเหตุ (Note)</label>
                                    <input type="text" id="note_Check" class="form-control" placeholder="เช่น รอเช็คเคลียร์" />
                                </div>
                            </div>
                        </div>

                        <button type="submit" class="btn btn-success w-100 fw-bold py-2 mt-2">
                            <i class="bi bi-save"></i> ยืนยันการรับชำระเงิน
                        </button>
                    </form>'''

    # Use regex to find and replace both <form asp-action="Pay"> blocks
    # Note: Regex requires dotall
    pattern = re.compile(r'<form asp-controller="SalesBill" asp-action="Pay" method="post" enctype="multipart/form-data">.*?</form>', re.DOTALL)
    new_content = pattern.sub(new_form_html, content)

    # We also need to add the JavaScript functions!
    js_code = '''
<script>
    function setPayMethod(method) {
        document.getElementById('methodField').value = method;
    }
    
    function syncPayment() {
        var method = document.getElementById('methodField').value;
        document.getElementById('realAmount').value = document.getElementById('amount_' + method).value;
        document.getElementById('realPayDate').value = document.getElementById('payDate_' + method).value;
        
        // Also sync notes! Wait, we can just name all notes "note" and let backend bind the first one?
        // No, backend binds them all as comma separated. Let's rename notes.
        // We will just disable the inactive inputs before submit.
        
        ['Cash', 'Transfer', 'Check'].forEach(m => {
            if (m !== method) {
                let noteInput = document.getElementById('note_' + m);
                if (noteInput) noteInput.disabled = true;
                
                // disable files in other tabs so they don't upload
                if (m === 'Transfer') document.querySelector('#transfer-pane input[type="file"]').disabled = true;
                if (m === 'Check') {
                    let cp = document.getElementById('check-pane');
                    let inputs = cp.querySelectorAll('input:not([id^="amount_"]):not([id^="payDate_"]):not([id^="note_"])');
                    inputs.forEach(inp => inp.disabled = true);
                }
            }
        });
        
        var noteVal = document.getElementById('note_' + method);
        if(noteVal) {
            noteVal.name = "note";
        }
        
        return true;
    }
</script>
'''
    # Insert JS before closing body or script section
    if "syncPayment()" not in new_content:
        new_content = new_content.replace('function setPayType(type) {', js_code + '\nfunction setPayType(type) {')

    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(new_content)
        print("Success for " + filepath)

process_file("Views/SalesBill/Detail.cshtml")
