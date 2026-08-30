using System;
using System.Collections.Generic;
using System.Linq;

namespace RoyalD.Web.Services
{
    public static class RegionHelper
    {
        public static readonly List<string> AllThailandProvinces = new()
        {
            "กรุงเทพมหานคร", "กระบี่", "กาญจนบุรี", "กาฬสินธุ์", "กำแพงเพชร", 
            "ขอนแก่น", "จันทบุรี", "ฉะเชิงเทรา", "ชลบุรี", "ชัยนาท", 
            "ชัยภูมิ", "ชุมพร", "เชียงราย", "เชียงใหม่", "ตรัง", 
            "ตราด", "ตาก", "นครนายก", "นครปฐม", "นครพนม", 
            "นครราชสีมา", "นครศรีธรรมราช", "นครสวรรค์", "นนทบุรี", "นราธิวาส", 
            "น่าน", "บึงกาฬ", "บุรีรัมย์", "ปทุมธานี", "ประจวบคีรีขันธ์", 
            "ปราจีนบุรี", "ปัตตานี", "พระนครศรีอยุธยา", "พะเยา", "พังงา", 
            "พัทลุง", "พิจิตร", "พิษณุโลก", "เพชรบุรี", "เพชรบูรณ์", 
            "แพร่", "ภูเก็ต", "มหาสารคาม", "มุกดาหาร", "แม่ฮ่องสอน", 
            "ยโสธร", "ยะลา", "ร้อยเอ็ด", "ระนอง", "ระยอง", 
            "ราชบุรี", "ลพบุรี", "ลำปาง", "ลำพูน", "เลย", 
            "ศรีสะเกษ", "สกลนคร", "สงขลา", "สตูล", "สมุทรปราการ", 
            "สมุทรสงคราม", "สมุทรสาคร", "สระแก้ว", "สระบุรี", "สิงห์บุรี", 
            "สุโขทัย", "สุพรรณบุรี", "สุราษฎร์ธานี", "สุรินทร์", "หนองคาย", 
            "หนองบัวลำภู", "อ่างทอง", "อำนาจเจริญ", "อุดรธานี", "อุตรดิตถ์", 
            "อุทัยธานี", "อุบลราชธานี"
        };

        public static readonly Dictionary<string, List<string>> Regions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ภาคกลาง"] = new List<string>
            {
                "กรุงเทพมหานคร", "กรุงเทพฯ", "กรุงเทพ", "กทม", "นนทบุรี", "ปทุมธานี", "สมุทรปราการ", "นครปฐม", 
                "สมุทรสาคร", "สมุทรสงคราม", "สุพรรณบุรี", "อยุธยา", "พระนครศรีอยุธยา", 
                "อ่างทอง", "สิงห์บุรี", "ชัยนาท", "สระบุรี", "ลพบุรี", "กาญจนบุรี", "ราชบุรี"
            },
            ["ภาคเหนือ"] = new List<string>
            {
                "เชียงใหม่", "เชียงราย", "ลำปาง", "ลำพูน", "พะเยา", "แพร่", "น่าน", 
                "แม่ฮ่องสอน", "อุตรดิตถ์", "พิษณุโลก", "สุโขทัย", "ตาก", "กำแพงเพชร", 
                "พิจิตร", "นครสวรรค์", "อุทัยธานี"
            },
            ["ภาคอีสาน"] = new List<string>
            {
                "นครราชสีมา", "ขอนแก่น", "อุดรธานี", "อุบลราชธานี", "บุรีรัมย์", "บุรีย์รัมย์", 
                "สุรินทร์", "ศรีสะเกษ", "ร้อยเอ็ด", "ชัยภูมิ", "สกลนคร", "กาฬสินธุ์", 
                "มหาสารคาม", "หนองคาย", "เลย", "เพชรบูรณ์", "หนองบัวลำภู", "บึงกาฬ", 
                "นครพนม", "มุกดาหาร", "ยโสธร", "อำนาจเจริญ"
            },
            ["ภาคตะวันออก"] = new List<string>
            {
                "ชลบุรี", "ระยอง", "ฉะเชิงเทรา", "จันทบุรี", "ตราด", "ปราจีนบุรี", 
                "สระแก้ว", "นครนายก"
            },
            ["ภาคใต้"] = new List<string>
            {
                "ภูเก็ต", "สุราษฎร์ธานี", "สงขลา", "นครศรีธรรมราช", "กระบี่", "พังงา", 
                "ระนอง", "ชุมพร", "ตรัง", "พัทลุง", "สตูล", "ปัตตานี", "ยะลา", 
                "นราธิวาส", "ประจวบคีรีขันธ์", "เพชรบุรี"
            }
        };

        public static readonly Dictionary<string, List<string>> DisplayProvinces = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ภาคกลาง"] = new List<string>
            {
                "กรุงเทพมหานคร", "นนทบุรี", "ปทุมธานี", "สมุทรปราการ", "นครปฐม", 
                "สมุทรสาคร", "สมุทรสงคราม", "สุพรรณบุรี", "พระนครศรีอยุธยา", 
                "อ่างทอง", "สิงห์บุรี", "ชัยนาท", "สระบุรี", "ลพบุรี", "กาญจนบุรี", "ราชบุรี"
            },
            ["ภาคเหนือ"] = new List<string>
            {
                "เชียงใหม่", "เชียงราย", "ลำปาง", "ลำพูน", "พะเยา", "แพร่", "น่าน", 
                "แม่ฮ่องสอน", "อุตรดิตถ์", "พิษณุโลก", "สุโขทัย", "ตาก", "กำแพงเพชร", 
                "พิจิตร", "นครสวรรค์", "อุทัยธานี"
            },
            ["ภาคอีสาน"] = new List<string>
            {
                "นครราชสีมา", "ขอนแก่น", "อุดรธานี", "อุบลราชธานี", "บุรีรัมย์", 
                "สุรินทร์", "ศรีสะเกษ", "ร้อยเอ็ด", "ชัยภูมิ", "สกลนคร", "กาฬสินธุ์", 
                "มหาสารคาม", "หนองคาย", "เลย", "เพชรบูรณ์", "หนองบัวลำภู", "บึงกาฬ", 
                "นครพนม", "มุกดาหาร", "ยโสธร", "อำนาจเจริญ"
            },
            ["ภาคตะวันออก"] = new List<string>
            {
                "ชลบุรี", "ระยอง", "ฉะเชิงเทรา", "จันทบุรี", "ตราด", "ปราจีนบุรี", 
                "สระแก้ว", "นครนายก"
            },
            ["ภาคใต้"] = new List<string>
            {
                "ภูเก็ต", "สุราษฎร์ธานี", "สงขลา", "นครศรีธรรมราช", "กระบี่", "พังงา", 
                "ระนอง", "ชุมพร", "ตรัง", "พัทลุง", "สตูล", "ปัตตานี", "ยะลา", 
                "นราธิวาส", "ประจวบคีรีขันธ์", "เพชรบุรี"
            }
        };

        public static List<string> GetRegions() => Regions.Keys.ToList();

        public static List<string> GetAllProvinces() => AllThailandProvinces.OrderBy(p => p).ToList();

        public static List<string> GetDisplayProvinces(string? region = null)
        {
            if (!string.IsNullOrEmpty(region))
            {
                var regionList = region.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
                var result = new List<string>();
                foreach (var r in regionList)
                {
                    if (DisplayProvinces.ContainsKey(r))
                        result.AddRange(DisplayProvinces[r]);
                }
                if (result.Count > 0)
                    return result.Distinct().OrderBy(p => p).ToList();
            }
            return AllThailandProvinces.OrderBy(p => p).ToList();
        }

        public static List<string> GetMatchingProvinces(string? region)
        {
            if (!string.IsNullOrEmpty(region))
            {
                var regionList = region.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(r => r.Trim()).ToList();
                var result = new List<string>();
                foreach (var r in regionList)
                {
                    if (r == "กรุงเทพฯ" || r == "กรุงเทพ" || r == "กทม" || r.Equals("bkk", StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddRange(new[] { "กรุงเทพมหานคร", "กรุงเทพฯ", "กรุงเทพ", "กทม" });
                    }
                    else if (r == "ต่างจังหวัด" || r.Equals("upcountry", StringComparison.OrdinalIgnoreCase))
                    {
                        var upcProvinces = AllThailandProvinces.Where(p => !p.Contains("กรุงเทพ") && !p.Contains("กทม")).ToList();
                        result.AddRange(upcProvinces);
                    }
                    else if (Regions.ContainsKey(r))
                    {
                        result.AddRange(Regions[r]);
                    }
                    else
                    {
                        result.Add(r);
                    }
                }
                if (result.Count > 0)
                    return ExpandProvinceVariants(result.Distinct());
            }
            return ExpandProvinceVariants(AllThailandProvinces);
        }

        public static List<string> ExpandProvinceVariants(IEnumerable<string>? provs)
        {
            if (provs == null) return new List<string>();
            var res = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in provs)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                var raw = p.Trim();
                res.Add(raw);

                var clean = raw;
                if (clean.StartsWith("จ.", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(2).Trim();
                else if (clean.StartsWith("จังหวัด", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(6).Trim();

                res.Add(clean);
                res.Add("จ." + clean);
                res.Add("จ. " + clean);
                res.Add("จังหวัด" + clean);
                res.Add("จังหวัด " + clean);

                if (clean.Equals("กรุงเทพมหานคร", StringComparison.OrdinalIgnoreCase) || clean.Equals("กทม", StringComparison.OrdinalIgnoreCase) || clean.Equals("กรุงเทพ", StringComparison.OrdinalIgnoreCase) || clean.Equals("กรุงเทพฯ", StringComparison.OrdinalIgnoreCase))
                {
                    res.Add("กรุงเทพมหานคร");
                    res.Add("จ.กรุงเทพมหานคร");
                    res.Add("จังหวัดกรุงเทพมหานคร");
                    res.Add("กรุงเทพฯ");
                    res.Add("กรุงเทพ");
                    res.Add("กทม.");
                    res.Add("กทม");
                    res.Add("จ.กทม");
                }
                else if (clean.Equals("พระนครศรีอยุธยา", StringComparison.OrdinalIgnoreCase) || clean.Equals("อยุธยา", StringComparison.OrdinalIgnoreCase))
                {
                    res.Add("พระนครศรีอยุธยา");
                    res.Add("อยุธยา");
                    res.Add("จ.พระนครศรีอยุธยา");
                    res.Add("จ.อยุธยา");
                }
            }
            return res.ToList();
        }

        public static List<string> ExpandDistrictVariants(IEnumerable<string>? dists)
        {
            if (dists == null) return new List<string>();
            var res = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in dists)
            {
                if (string.IsNullOrWhiteSpace(d)) continue;
                var raw = d.Trim();
                res.Add(raw);

                var clean = raw;
                if (clean.StartsWith("อ.", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(2).Trim();
                else if (clean.StartsWith("อำเภอ", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(5).Trim();
                else if (clean.StartsWith("เขต", StringComparison.OrdinalIgnoreCase)) clean = clean.Substring(3).Trim();

                res.Add(clean);
                res.Add("อ." + clean);
                res.Add("อ. " + clean);
                res.Add("อำเภอ" + clean);
                res.Add("อำเภอ " + clean);
                res.Add("เขต" + clean);
                res.Add("เขต " + clean);
            }
            return res.ToList();
        }
    }
}