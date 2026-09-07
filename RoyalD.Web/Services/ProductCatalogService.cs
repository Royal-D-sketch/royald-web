using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RoyalD.Web.Services
{
    public static class ProductCatalogService
    {
        private static readonly Dictionary<string, string> _unitsByCode = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _unitsByName = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<(string Code, string Name, string Unit)> _allProducts = new();
        private static readonly object _lock = new();
        private static bool _initialized = false;

        static ProductCatalogService()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;
                LoadMasterData();
                _initialized = true;
            }
        }

        private static void LoadMasterData()
        {
            try
            {
                string[] searchPaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "products_master.csv"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Data", "products_master.csv"),
                    Path.Combine(Directory.GetCurrentDirectory(), "RoyalD.Web", "Data", "products_master.csv"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "products_master.csv"),
                    Path.Combine(Directory.GetCurrentDirectory(), "products_master.csv")
                };

                string? foundPath = searchPaths.FirstOrDefault(File.Exists);
                if (foundPath != null)
                {
                    var lines = File.ReadAllLines(foundPath);
                    foreach (var line in lines.Skip(1))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var parts = line.Split(',');
                        if (parts.Length >= 3)
                        {
                            string code = parts[0].Trim();
                            string name = parts[1].Trim();
                            string unit = parts[2].Trim();
                            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(unit))
                            {
                                RegisterProduct(code, name, unit);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback will supply defaults
            }

            EnsureEmbeddedCatalog();
        }

        private static void RegisterProduct(string code, string name, string unit)
        {
            if (string.IsNullOrWhiteSpace(unit)) return;
            unit = unit.Trim();

            if (!string.IsNullOrWhiteSpace(code))
            {
                _unitsByCode[code.Trim()] = unit;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                _unitsByName[name.Trim()] = unit;
                string cleanName = NormalizeName(name);
                if (!string.IsNullOrEmpty(cleanName))
                {
                    _unitsByName[cleanName] = unit;
                }
            }

            _allProducts.Add((code.Trim(), name.Trim(), unit));
        }

        private static string NormalizeName(string name)
        {
            return name.Replace(" ", "").Replace("-", "").Replace("_", "").ToLowerInvariant();
        }

        public static string GetUnit(string? productCode, string? productName, string? fallbackUnit = null)
        {
            EnsureInitialized();

            // 1. Check by Product Code
            if (!string.IsNullOrWhiteSpace(productCode))
            {
                string codeTrim = productCode.Trim();
                if (_unitsByCode.TryGetValue(codeTrim, out var u1) && !string.IsNullOrWhiteSpace(u1))
                    return u1;

                // Try base code if code has dash e.g. "100301-1" -> "100301"
                int dashIdx = codeTrim.IndexOf('-');
                if (dashIdx > 0)
                {
                    string rootCode = codeTrim.Substring(0, dashIdx).Trim();
                    if (_unitsByCode.TryGetValue(rootCode, out var uRoot) && !string.IsNullOrWhiteSpace(uRoot))
                        return uRoot;
                }
            }

            // 2. Check by Product Name
            if (!string.IsNullOrWhiteSpace(productName))
            {
                string nameTrim = productName.Trim();
                if (_unitsByName.TryGetValue(nameTrim, out var u2) && !string.IsNullOrWhiteSpace(u2))
                    return u2;

                string norm = NormalizeName(nameTrim);
                if (_unitsByName.TryGetValue(norm, out var uNorm) && !string.IsNullOrWhiteSpace(uNorm))
                    return uNorm;

                // Substring matching against master list
                foreach (var p in _allProducts)
                {
                    if (!string.IsNullOrWhiteSpace(p.Name))
                    {
                        if (nameTrim.Contains(p.Name, StringComparison.OrdinalIgnoreCase) ||
                            p.Name.Contains(nameTrim, StringComparison.OrdinalIgnoreCase))
                        {
                            return p.Unit;
                        }
                    }
                }
            }

            // 3. Check fallbackUnit
            if (!string.IsNullOrWhiteSpace(fallbackUnit))
            {
                return fallbackUnit.Trim();
            }

            // 4. Heuristic check based on product name keywords
            if (!string.IsNullOrWhiteSpace(productName))
            {
                string n = productName;
                if (n.Contains("ซอง")) return "ซอง";
                if (n.Contains("ลัง")) return "ลัง";
                if (n.Contains("กล่อง") || n.Contains("Box", StringComparison.OrdinalIgnoreCase)) return "กล่อง";
                if (n.Contains("แพ็ค") || n.Contains("แพค") || n.Contains("Pack", StringComparison.OrdinalIgnoreCase)) return "แพ็ค";
                if (n.Contains("ขวด") || n.Contains("Bottle", StringComparison.OrdinalIgnoreCase)) return "ขวด";
                if (n.Contains("หลอด")) return "หลอด";
                if (n.Contains("หีบ")) return "หีบ";
                if (n.Contains("ถุง") || n.Contains("Bag", StringComparison.OrdinalIgnoreCase)) return "ถุง";
                if (n.Contains("ชุด") || n.Contains("Set", StringComparison.OrdinalIgnoreCase)) return "ชุด";
                if (n.Contains("กระป๋อง") || n.Contains("Can", StringComparison.OrdinalIgnoreCase)) return "กระป๋อง";
                if (n.Contains("ตัว")) return "ตัว";
                if (n.Contains("ใบ")) return "ใบ";
                if (n.Contains("เครื่อง")) return "เครื่อง";
                if (n.Contains("เล่ม")) return "เล่ม";
                if (n.Contains("ตลับ")) return "ตลับ";
                if (n.Contains("แกลลอน")) return "แกลลอน";
                if (n.Contains("คัน")) return "คัน";
                if (n.Contains("งาน")) return "งาน";
                if (n.Contains("ดวง")) return "ดวง";
                if (n.Contains("แผ่น")) return "แผ่น";
                if (n.Contains("ผืน")) return "ผืน";
                if (n.Contains("อัน")) return "อัน";
                if (n.Contains("ชิ้น")) return "ชิ้น";
            }

            return "-";
        }

        private static void EnsureEmbeddedCatalog()
        {
            var defaults = new (string, string, string)[]
            {
                ("100101", "รอแยล-ดี 15 กรัม", "ซอง"),
                ("100102", "เกลือแร่ชนิดผง รอแยล-ดี 500 กรัม", "แพ็ค"),
                ("100201", "โรซ่าร์ - ดี 20 กรัม", "ซอง"),
                ("100300", "รอแยล-ดี 25 กรัม (RDTC 50)", "ซอง"),
                ("100301", "รอแยล-ดี 25 กรัม", "ซอง"),
                ("100301-1", "รอแยล-ดี 25 กรัม ชนิด 50 ซอง (ลัง)", "ลัง"),
                ("100302", "รอแยล-ดี 25 กรัม (RDB)", "ซอง"),
                ("100303", "รอแยล-ดี 25 กรัม (RDC)", "ซอง"),
                ("100304", "รอแยล-ดี 25 กรัม (ชนิด 5 ซอง)", "กล่อง"),
                ("100305", "ผงชงชิมรอแยล-ดี 500 กรัม (RDC )", "แพ็ค"),
                ("100306", "ผงชงชิม รอแยล-ดี 500 กรัม (RDB)", "แพ็ค"),
                ("100307", "รอแยล-ดี กลิ่นผลไม้รวม 25 กรัม (RDT10)", "กล่อง"),
                ("100308", "รอแยล-ดี 25 กรัม (ลัง)(RDT 10)", "ลัง"),
                ("100309", "รอแยล-ดี กลิ่นส้ม 25 กรัม (RDO 10)", "กล่อง"),
                ("100310", "รอแยล-ดี กลิ่นสตอรเบอร์รี่ 25 กรัม (RDS 50)", "ซอง"),
                ("100311", "รอแยล-ดี กลิ่นองุ่น  25 กรัม (RDG 50)", "ซอง"),
                ("100312", "รอแยล-ดี 25 กรัม (กล่อง)RDT50", "กล่อง"),
                ("100317", "รอแยล-ดี สปอร์ต 35 กรัม", "ถุง"),
                ("100318", "รอแยล-ดี กลิ่นสตอรเบอร์รี่ 25 กรัม (RDS 10)", "กล่อง"),
                ("100319", "รอแยล-ดี กลิ่นองุ่น 25 กรัม (RDG10)", "กล่อง"),
                ("100320", "เครื่องดื่มเกลือแร่ กลิ่นส้ม ชนิดผง (ตรา รอแยล-ดี)", "กล่อง"),
                ("100401", "นีโอไลต์ 25 กรัม", "ซอง"),
                ("100402", "นีโอไลต์ 25 กรัม (ชนิด 25 ซอง)", "ซอง"),
                ("100403", "นีโอไลต์ 25 กรัม (กล่อง)", "กล่อง"),
                ("100404", "นีโอไลต์ 25 กรัม ชนิด 25 ซอง (ลัง)(NL25)", "ลัง"),
                ("100405", "นีโอไลต์ 500 กรัม", "แพ็ค"),
                ("100601", "กัมลัง-ดี (รสส้มแคลิฟอร์เนีย) 20 กรัม", "ซอง"),
                ("100602", "กัมลัง-ดี (รสมะนาว) 20 กรัม", "ซอง"),
                ("200101", "แคลซี่ - ดี 20 กรัม", "กล่อง"),
                ("200102", "แคลซี่-ดี 20 กรัม (ตปท.)", "กล่อง"),
                ("200104", "ผงชงชิม แคลซี่-ดี 500 กรัม", "แพ็ค"),
                ("200105", "แคลซี่-ดี โกลด์  500 กรัม", "แพ็ค"),
                ("200107", "แคลซี่ - ดี 20 กรัม(หีบ)", "หีบ"),
                ("200108", "แคลซี่ - ดี 20 กรัม  (ชนิด 5 ซอง)", "ถุง"),
                ("200110", "แคลซี่-ดี โกลด์", "กล่อง"),
                ("200114", "แคลซี่ - ดี 20 กรัม 10 ซอง (แพ็ค3)", "แพ็ค"),
                ("200116", "แคลซี่-ดี 20 กรัม(รุ่น 36 ถุง )", "ลัง"),
                ("200118", "เสริมอาหารแคลซี่ - ดี (ชนิด 10 ซอง)", "กล่อง"),
                ("200201", "แคลซี่-ดี ช็อกโก 320 กรัม (ต่างประเทศ)", "กระป๋อง"),
                ("200204", "แคลซี่-ดี ช็อกโก ขนาด 30 กรัม", "กล่อง"),
                ("200206", "แคลซี่-ดี ช็อกโก ขนาด 30 กรัม (ชนิด 5 ซอง)", "ถุง"),
                ("200211", "เสริมอาหารแคลซี่-ดี ช็อกโก  (ชนิด 10 ซอง)", "กล่อง"),
                ("200300", "รอแยล-ดี เวย์โปรตีน รสช๊อกโกแลต 30 กรัม", "กล่อง"),
                ("200302", "รอแยล-ดี เวย์โปรตีน รสช๊อกโกแลต (ชนิด 5 ซอง)", "กล่อง"),
                ("200303", "เสริมอาหารเวย์โปรตีน รสช็อกโกแลต (ชนิด 5 ซอง)", "กล่อง"),
                ("200305", "เสริมอาหารเวย์โปรตีน รสชาเขียว (ชนิด 10 ซอง)", "กล่อง"),
                ("200306", "เสริมอาหารเวย์โปรตีน รสวนิลลา (ชนิด 10 ซอง)", "กล่อง"),
                ("200312", "ผลิตภัณฑ์เสริมอาหาร กลิ่นองุ่น ตรา รอแยล-ดี เจล", "กล่อง"),
                ("200313", "ผลิตภัณฑ์เสริมอาหาร รสโกโก้ ตรา รอแยล-ดี เจล", "กล่อง"),
                ("200314", "ผลิตภัณฑ์เสริมอาหาร กลิ่นส้ม ตรา รอแยล-ดี เจล", "กล่อง"),
                ("200320", "ผลิตภัณฑ์เสริมอาหาร กลิ่นกาแฟ ตรา รอแยล-ดี เจล(5 ซอง)", "กล่อง"),
                ("200321", "ผลิตภัณฑ์เสริมอาหาร กลิ่นส้ม ตรา รอแยล-ดี เจล(5 ซอง)", "กล่อง"),
                ("200322", "ผลิตภัณฑ์เสริมอาหาร กลิ่นกาแฟ ตรา รอแยล-ดี เจล", "กล่อง"),
                ("200324", "แคล์ร โพรไบโอติก พลัสวิตามินซี (ผลิตภัณฑ์เสริมอาหาร)", "หลอด"),
                ("200325", "ผลิตภัณฑ์เสริมอาหาร กลิ่นเรดฟรุ๊ท ตรา รอแยล-ดี เจล 10 ซอง", "กล่อง"),
                ("200400", "I-Fresh รสส้ม 250 กรัม", "แพ็ค"),
                ("200401", "I-Fresh  รสส้ม  500 กรัม", "แพ็ค"),
                ("200403", "I-Fresh รสชามะนาว 500 กรัม", "แพ็ค"),
                ("200404", "I-Fresh กลิ่นพั้นซ์ 500 กรัม", "แพ็ค"),
                ("200405", "I-Fresh กลิ่นแอปเปิ้ล 500 กรัม", "แพ็ค"),
                ("200406", "I-Fresh กลิ่นองุ่น 500 กรัม", "แพ็ค"),
                ("300102", "รอแยลคอฟฟี่ ผสมเห็ดหลินจือ", "กล่อง"),
                ("300103", "รอแยลคอฟฟี่ ผสมโสมสกัด", "กล่อง"),
                ("300106", "กาแฟดำชนิดผงผสมเห็ดหลินจือ (ชนิด 10 ซอง)", "กล่อง"),
                ("300108", "รอแยลคอฟฟี่ ผสมเห็ดหลินจือ ขนาด 12 ซอง", "แพ็ค"),
                ("300109", "กาแฟดริปดอยช้าง คั่วเข้ม ตรา รอแยล-คอฟฟี่ ( 5 ซอง)", "กล่อง"),
                ("300110", "กาแฟดริปดอยช้าง คั่วกลาง ตรา รอแยล-คอฟฟี่ ( 5 ซอง)", "กล่อง"),
                ("300113", "เมล็ดกาแฟคั่ว บราซิล ซานโดส 200 กรัม", "ถุง"),
                ("300114", "เมล็ดกาแฟคั่ว โคลัมเบีย 200 กรัม", "ถุง"),
                ("300115", "เมล็ดกาแฟคั่ว ดอยช้าง อาราบิก้า 100% 250 กรัม", "ถุง"),
                ("300116", "เมล็ดกาแฟคั่ว อาราบิก้า 100% 250 กรัม", "ถุง"),
                ("300301", "รอแยล-คอฟฟี่  20 กรัม (3 in 1)", "แพ็ค"),
                ("300501", "แคลร์ส คอฟฟี่ บูสเตอร์", "กล่อง"),
                ("300502", "แคลร์ส คอฟฟี่ บิวตี้", "กล่อง"),
                ("300517", "แคลร์ คอฟฟี่ พรีไบโอ 14 กรัม", "กล่อง"),
                ("300518", "แคลร์ส คอฟฟี่ คาปูชิโน 14 กรัม", "กล่อง"),
                ("300519", "แคล์ร คอลลาเจน (ชนิด15 ซอง )", "กล่อง"),
                ("300526", "แคล์ร คอลลาเจน (ชนิด 10 ซอง)", "กล่อง"),
                ("300530", "รอแยล-คอฟฟี่ อาราบิก้าโรบัสต้าเบลนด์ 10", "กล่อง"),
                ("300531", "รอแยล-คอฟฟี่ อาราบิก้าโรบัสต้าเบลนด์ 05", "ถุง"),
                ("300534", "รอแยล-คอฟฟี่ อาราบิก้าโรบัสต้าเบลนด์(รุ่น27ซอง)", "แพ็ค"),
                ("300551", "เครื่องดื่มชนิดผง ตรา แคล์ร ไฟเบอร์", "กล่อง"),
                ("300558", "กาแฟสำเร็จรูปผสมอาราบิก้า ตรารอแยล คอฟฟี่โกลด์ 100กรัม", "ถุง"),
                ("300563", "กาแฟสำเร็จรูป อาราบิก้า เอสเปรสโซ่ ตรารอแยล-คอฟฟี่โกลด์(รุ่น22ซอง)", "แพ็ค"),
                ("300568", "อาราบิก้า ลาเต้ กาแฟปรุงสำเร็จชนิดผง ตรารอแยล-คอฟฟี่(รุ่น22ซอง)", "แพ็ค"),
                ("500101", "โคโซแตน 60 ml.", "ขวด"),
                ("500102", "เนเจอร์ทุเรียน ขนาด 100 กรัม", "กล่อง"),
                ("500103", "เนเจอร์ทุเรียน ขนาด 40 กรัม", "ถุง"),
                ("500110", "เนเจอร์ไวต้า รสส้มแคลิฟอร์เนีย  500 กรัม", "แพ็ค"),
                ("500111", "เนเจอร์ไวต้า รสองุ่น 500 กรัม", "แพ็ค"),
                ("500119", "เนเจอร์ทุเรียน ขนาด 200 กรัม", "แพ็ค"),
                ("500167", "ถั่งเช่า 30 แคปซูล ตรา รอแยล คอร์ดี้-พลัส", "ขวด"),
                ("500168", "ถั่งเช่า 24 กล่อง ตรา รอแยล คอร์ดี้-พลัส", "กล่อง"),
                ("500171", "โยเกิร์ตอบกรอบ รสออริจินอล รูปหัวใจ 25กรัม", "กล่อง"),
                ("500172", "โยเกิร์ตอบกรอบ รสสตอเบอร์รี่ รูปหัวใจ 25กรัม", "กล่อง"),
                ("500184", "โยเกิร์ตอบกรอบ รสออริจินอล รูปหัวใจ 20 กรัม (ซอง)", "ซอง"),
                ("500185", "โยเกิร์ตอบกรอบ รสสตอเบอร์รี่ รูปหัวใจ 20 กรัม (ซอง)", "ซอง"),
                ("500186", "ถั่งเช่า ผสมมัลติวิตามิน ตรารอแยล คอร์ดี้-พลัส", "กล่อง"),
                ("500195", "นมปรุงแต่งอัดเม็ด รสหวาน 12 (กล่อง)", "กล่อง"),
                ("500196", "นมปรุงแต่งอัดเม็ด รสช็อกโกแลต 12 (กล่อง)", "กล่อง"),
                ("500200", "วิตามินซี 60มก. กลิ่นส้ม ตราเนเจอร์ ไบท์", "ซอง"),
                ("500201", "วิตามินซี 60มก. กลิ่นองุ่น ตราเนเจอร์ ไบท์", "ซอง"),
                ("500204", "Natural lemon flavoured beverage (Royal-d c1000 brand)", "กล่อง"),
                ("500210", "นมปรุงแต่งอัดเม็ด รสหวาน 10 (กล่อง)", "กล่อง"),
                ("500211", "นมปรุงแต่งอัดเม็ด รสช็อกโกแลต 10 (กล่อง)", "กล่อง"),
                ("500212", "นมปรุงแต่งอัดเม็ด รสสตอเบอร์รี่ 10 (กล่อง)", "กล่อง"),
                ("500301", "รอแยล-ดี น้ำ รสสตอเบอร์รี่ 390 ml. (RWS 390)", "ขวด"),
                ("500302", "รอแยล-ดี น้ำ รสองุ่น 390 ml.(RWG 390)", "ขวด"),
                ("500303", "รอแยล-ดี น้ำ 390 ml. (RDW 390)", "ขวด"),
                ("500304", "รอแยล-ดี น้ำ รสสตอเบอร์รี่ 330 ml. (RWS 330)", "ขวด"),
                ("500305", "รอแยล-ดี น้ำ รสองุ่น 330 ml.(RWG 330)", "ขวด"),
                ("500306", "รอแยล-ดี น้ำ 330 ml. (RDW 330) ( 24 ขวด)", "ขวด"),
                ("500307", "เครื่องดื่มน้ำรสองุ่น12% ผสมเกลือแร่ 330 ml.(RWM330)", "ขวด"),
                ("500308", "รอแยล-ดี วิตามิน วอเตอร์ วิตามิน B", "ขวด"),
                ("500309", "รอแยล-ดี วิตามิน วอเตอร์ วิตามิน C", "ขวด"),
                ("500310", "เครื่องดื่มวิตามิน ซี 1000มก(ตปท)", "ขวด"),
                ("500311", "ผลิตภัณฑ์เสริมอาหาร วิตามินซี 1000 มก. (ตรา รอแยล-ดี)", "ขวด"),
                ("500312", "เครื่องดื่มกลิ่นส้มผสมวิตามินและเกลือแร่ ขนาด 330 ml (RBW330)", "ขวด"),
                ("500313", "เครื่องดื่มรสขิงผสมเลมอน รอแยล-ดี", "ขวด"),
                ("500314", "ผลิตภัณฑ์เสริมอาหาร ตรารอแยล คอร์ดี้ พลัส ถั่งเช่าสกัด 2000มก.", "ขวด"),
                ("505000", "เครื่องดื่ม Synergy (ไทย)", "ขวด"),
                ("505003", "เครื่องดื่มน้ำทับทิมตรา ซินเนอร์จี้ ไฮ-ซี (รุ่น 60 ขวด)", "ขวด")
            };

            foreach (var item in defaults)
            {
                if (!_unitsByCode.ContainsKey(item.Item1))
                {
                    RegisterProduct(item.Item1, item.Item2, item.Item3);
                }
            }
        }
    }
}
