using System;
using System.IO;
using ExcelDataReader;
using System.Data;

class Program {
    static void Main() {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        string file = @"c:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\รายละเอียดบิลขาย\รายละเอียดบิลขายเดือน 5.69.xlsx";
        using var stream = File.Open(file, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var ds = reader.AsDataSet();
        var tbl = ds.Tables[0];
        
        for (int r = 0; r < Math.Min(tbl.Rows.Count, 50); r++) {
            var row = tbl.Rows[r];
            for (int c = 0; c < tbl.Columns.Count; c++) {
                var val = row[c]?.ToString()?.Trim() ?? "";
                if (val.Contains("วัน")) {
                    Console.WriteLine($"Row {r:D3}, Col {c+1}: '{val}'");
                }
            }
        }
    }
}