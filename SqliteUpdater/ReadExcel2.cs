using System;
using System.IO;
using ExcelDataReader;
using System.Data;

class ReadD {
    static void Main() {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var path = @"C:\Users\User2\Desktop\อาร์ต\บิลขาย การ์ดลูกหนี้และการติดตามการชำระเงิน\royal-d-debtor-web\raw_data\รายงานลูกหนี้คงค้าง.XLS";
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var conf = new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } };
        var ds = reader.AsDataSet(conf);
        var tbl = ds.Tables[0];
        
        for (int r = 0; r < tbl.Rows.Count; r++) {
            var v = tbl.Rows[r][7]?.ToString();
            if (v != null && v.Contains("56711649")) {
                Console.WriteLine($"Found at row {r}:");
                for(int c=0; c < tbl.Columns.Count; c++) {
                    Console.WriteLine($"  Col {c}: {tbl.Rows[r][c]}");
                }
            }
        }
    }
}
