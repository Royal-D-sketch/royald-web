using System;
using System.Text.RegularExpressions;

class Program {
    static void Main() {
        string[] row = new string[20];
        row[0] = "Product A";
        row[10] = "50";
        row[11] = "50";
        row[12] = "15.00";
        row[13] = "";
        row[14] = "750.00";
        
        int lastIdx = -1;
        for (int i = row.Length - 1; i >= 1; i--) {
            if (!string.IsNullOrWhiteSpace(row[i])) {
                lastIdx = i;
                break;
            }
        }
        
        Console.WriteLine($"lastIdx: {lastIdx}");
        
        if (lastIdx >= 4) {
            decimal amt = ParseDecimal(row[lastIdx]);
            string rawDiscount = row[lastIdx - 1]?.Trim() ?? "";
            decimal discount = 0;
            if (rawDiscount.EndsWith("%")) {
                discount = ParseDecimal(rawDiscount.Replace("%", ""));
            } else {
                discount = ParseDecimal(rawDiscount);
            }
            decimal price = ParseDecimal(row[lastIdx - 2]);
            string unit = row[lastIdx - 3]?.Trim() ?? "";
            decimal qty = ParseDecimal(row[lastIdx - 4]);
            
            Console.WriteLine($"Qty: {qty}, Unit: {unit}, Price: {price}, Disc: {discount}, Amt: {amt}");
        }
    }
    
    static decimal ParseDecimal(string s) {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Replace(",", "").Trim();
        return decimal.TryParse(s, out decimal result) ? result : 0;
    }
}