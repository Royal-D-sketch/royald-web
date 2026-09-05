import sys

def process(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    target1 = '''                decimal mCollected = 0m;
                var repDebts = isOverall ? debts : debts.Where(x => x.SalesRep != null && x.SalesRep.Contains(repName.Trim())).ToList();
                foreach (var d in repDebts)
                {
                    var dPayments = payments.Where(p => p.OutstandingDebtId == d.Id && 
                        (p.PaidDate.Year > 2500 ? p.PaidDate.Year - 543 : p.PaidDate.Year) == mkYear && p.PaidDate.Month == mkMonth).ToList();
                    
                    if (d.ReceiptDate.HasValue && 
                        (d.ReceiptDate.Value.Year > 2500 ? d.ReceiptDate.Value.Year - 543 : d.ReceiptDate.Value.Year) == mkYear && 
                        d.ReceiptDate.Value.Month == mkMonth)
                    {
                        mCollected += (d.OriginalAmount - d.RemainingAmount);
                    }
                }'''

    target2 = '''                    decimal mCollected = 0m;
                    var repDebts = isOverall ? debts : debts.Where(x => x.SalesRep != null && x.SalesRep.Contains(repName.Trim())).ToList();
                    foreach (var d in repDebts)
                    {
                        var dPayments = payments.Where(p => p.OutstandingDebtId == d.Id && 
                            (p.PaidDate.Year > 2500 ? p.PaidDate.Year - 543 : p.PaidDate.Year) == mkYear && p.PaidDate.Month == mkMonth).ToList();
                        
                        if (d.ReceiptDate.HasValue && 
                            (d.ReceiptDate.Value.Year > 2500 ? d.ReceiptDate.Value.Year - 543 : d.ReceiptDate.Value.Year) == mkYear && 
                            d.ReceiptDate.Value.Month == mkMonth)
                        {
                            mCollected += (d.OriginalAmount - d.RemainingAmount);
                        }
                    }'''

    replace1 = '''                decimal mCollected = 0m;
                var repDebts = isOverall ? debts : debts.Where(x => x.SalesRep != null && x.SalesRep.Contains(repName.Trim())).ToList();
                var processedBillNos = new System.Collections.Generic.HashSet<string>();
                foreach (var d in repDebts)
                {
                    processedBillNos.Add(d.BillNo);
                    var dPayments = payments.Where(p => p.OutstandingDebtId == d.Id && 
                        (p.PaidDate.Year > 2500 ? p.PaidDate.Year - 543 : p.PaidDate.Year) == mkYear && p.PaidDate.Month == mkMonth).ToList();
                    
                    if (dPayments.Any())
                    {
                        mCollected += dPayments.Sum(p => p.PaidAmount);
                    }
                    else if (d.ReceiptDate.HasValue && 
                        (d.ReceiptDate.Value.Year > 2500 ? d.ReceiptDate.Value.Year - 543 : d.ReceiptDate.Value.Year) == mkYear && 
                        d.ReceiptDate.Value.Month == mkMonth)
                    {
                        mCollected += (d.OriginalAmount - d.RemainingAmount);
                    }
                }
                
                var repBills = isOverall ? bills : bills.Where(x => x.SalesRep != null && x.SalesRep.Contains(repName.Trim())).ToList();
                foreach (var b in repBills)
                {
                    if (!processedBillNos.Contains(b.BillNo) && b.IsFullyPaid && b.ReceiptDate.HasValue)
                    {
                        int rYear = b.ReceiptDate.Value.Year > 2500 ? b.ReceiptDate.Value.Year - 543 : b.ReceiptDate.Value.Year;
                        if (rYear == mkYear && b.ReceiptDate.Value.Month == mkMonth)
                        {
                            mCollected += (decimal)b.TotalAmount;
                        }
                    }
                }'''

    replace2 = '''                    decimal mCollected = 0m;
                    var repDebts = isOverall ? debts : debts.Where(x => x.SalesRep != null && x.SalesRep.Contains(repName.Trim())).ToList();
                    var processedBillNos = new System.Collections.Generic.HashSet<string>();
                    foreach (var d in repDebts)
                    {
                        processedBillNos.Add(d.BillNo);
                        var dPayments = payments.Where(p => p.OutstandingDebtId == d.Id && 
                            (p.PaidDate.Year > 2500 ? p.PaidDate.Year - 543 : p.PaidDate.Year) == mkYear && p.PaidDate.Month == mkMonth).ToList();
                        
                        if (dPayments.Any())
                        {
                            mCollected += dPayments.Sum(p => p.PaidAmount);
                        }
                        else if (d.ReceiptDate.HasValue && 
                            (d.ReceiptDate.Value.Year > 2500 ? d.ReceiptDate.Value.Year - 543 : d.ReceiptDate.Value.Year) == mkYear && 
                            d.ReceiptDate.Value.Month == mkMonth)
                        {
                            mCollected += (d.OriginalAmount - d.RemainingAmount);
                        }
                    }
                    
                    var repBills = isOverall ? bills : bills.Where(x => x.SalesRep != null && x.SalesRep.Contains(repName.Trim())).ToList();
                    foreach (var b in repBills)
                    {
                        if (!processedBillNos.Contains(b.BillNo) && b.IsFullyPaid && b.ReceiptDate.HasValue)
                        {
                            int rYear = b.ReceiptDate.Value.Year > 2500 ? b.ReceiptDate.Value.Year - 543 : b.ReceiptDate.Value.Year;
                            if (rYear == mkYear && b.ReceiptDate.Value.Month == mkMonth)
                            {
                                mCollected += (decimal)b.TotalAmount;
                            }
                        }
                    }'''

    content = content.replace(target1, replace1)
    content = content.replace(target2, replace2)

    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)

process('RoyalD.Web/Services/ReportService.cs')
