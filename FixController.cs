using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string path = ""RoyalD.Web/Controllers/SalesBillController.cs"";
        string content = File.ReadAllText(path, Encoding.UTF8);
        string oldBlock = @""var qtyStr = Request.Form[""{code}""].ToString();
                            if (int.TryParse(qtyStr, out int qty) && qty > 0)
                            {
                                debt.PendingProducts.Add(new PendingProduct
                                {
                                    ProductCode = code,
                                    ProductName = allNames.Length > i ? allNames[i] : code,
                                    Quantity = qty
                                });
                            }"";
                            
        string newBlock = @""var qtyStr = Request.Form[""{code}""].ToString();
                            int qty = 1;
                            if (int.TryParse(qtyStr, out int parsedQty) && parsedQty > 0)
                            {
                                qty = parsedQty;
                            }
                            else
                            {
                                var matchedItem = await _db.SalesBillItems.FirstOrDefaultAsync(item => item.BillNo == billNo && item.ProductCode == code);
                                if (matchedItem != null) qty = (int)matchedItem.Qty;
                            }
                            
                            debt.PendingProducts.Add(new PendingProduct
                            {
                                ProductCode = code,
                                ProductName = allNames.Length > i ? allNames[i] : code,
                                Quantity = qty
                            });"";
                            
        content = content.Replace(oldBlock, newBlock);
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }
}
