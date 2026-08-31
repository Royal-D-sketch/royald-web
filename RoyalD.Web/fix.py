import re

with open('Services/ExcelImportService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace the ParseDate method
old_func = re.search(r'private static DateTime ParseDate\((.*?)\)\s*\{.*?return DateTime\.Today;\s*\}', content, re.DOTALL)
if old_func:
    new_func = '''private static DateTime ParseDate(object? obj)
        {
            if (obj == null) return DateTime.Today;
            if (obj is DateTime dt) return dt;
            if (obj is double dDate) return DateTime.FromOADate(dDate);

            string s = obj.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(s)) return DateTime.Today;
            
            if (DateTime.TryParse(s, new System.Globalization.CultureInfo("th-TH"), System.Globalization.DateTimeStyles.None, out var resTh))
            {
                if (resTh.Year > 2500) resTh = resTh.AddYears(-543);
                return resTh;
            }
            
            if (DateTime.TryParse(s, new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out var resUs))
            {
                if (resUs.Year > 2500) resUs = resUs.AddYears(-543);
                return resUs;
            }

            if (s.Contains("/"))
            {
                var parts = s.Split('/');
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[0], out int d) && int.TryParse(parts[1], out int m) && int.TryParse(parts[2].Split(' ')[0], out int y))
                    {
                        if (y > 2500) y -= 543;
                        else if (y < 100) y += 2000;
                        if (y > 2050) y -= 43;
                        try { return new DateTime(y, m, d); } catch { }
                        try { return new DateTime(y, d, m); } catch { }
                    }
                }
            }
            if (DateTime.TryParse(s, out var res)) return res;
            return DateTime.Today;
        }'''
    content = content[:old_func.start()] + new_func + content[old_func.end():]
    
with open('Services/ExcelImportService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
