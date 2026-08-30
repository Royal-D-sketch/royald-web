using System;
using System.IO;
using System.Data;
using ExcelDataReader;

class Program {
    static void Main() {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var files = Directory.GetFiles(@"..\raw_data", "*.XLS");
        if (files.Length == 0) files = Directory.GetFiles(@"raw_data", "*.XLS");
        
        foreach (var file in files) {
            using var stream = File.OpenRead(file);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration { ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false } });
            var tbl = ds.Tables[0];
            Console.WriteLine("=== File: " + Path.GetFileName(file) + " ===");
            int sampleCount = 0;
            for (int r = 4; r < tbl.Rows.Count; r++) {
                var row = tbl.Rows[r];
                var c0 = row[0]?.ToString()?.Trim() ?? "";
                if (c0.StartsWith("R") || c0.StartsWith("6") || c0.StartsWith("B")) {
                    string dynPhone = "";
                    int dynCredit = 0;
                    for (int i = 4; i < tbl.Columns.Count; i++) {
                        var cell = row[i]?.ToString()?.Trim() ?? "";
                        if (string.IsNullOrEmpty(cell)) continue;
                        if (cell.StartsWith("โทร", StringComparison.OrdinalIgnoreCase) || (cell.StartsWith("0") && cell.Contains("-"))) {
                            dynPhone = cell;
                        } else if (int.TryParse(cell, out int credVal) && credVal >= 0 && credVal <= 180) {
                            dynCredit = credVal;
                        }
                    }
                    if (sampleCount++ < 5 || c0 == "R148477") {
                        Console.WriteLine($"Bill: {c0} | Phone: '{dynPhone}' | Credit: {dynCredit} | Row: " + string.Join(" | ", row.ItemArray));
                    }
                }
            }
        }
    }
}
