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
            ["ออนไลน์"] = AllThailandProvinces,
            ["กรุงเทพ, นนทบุรี, สมุทรปราการ"] = new List<string>
            {
                "กรุงเทพมหานคร", "กรุงเทพฯ", "กรุงเทพ", "กทม", "นนทบุรี", "สมุทรปราการ"
            },
            ["ต่างจังหวัด"] = new List<string>
            {
                "กระบี่", "กาญจนบุรี", "กาฬสินธุ์", "กำแพงเพชร", "ขอนแก่น", "จันทบุรี", "ฉะเชิงเทรา", "ชลบุรี", "ชัยนาท",
                "ชัยภูมิ", "ชุมพร", "เชียงราย", "เชียงใหม่", "ตรัง", "ตราด", "ตาก", "นครนายก", "นครปฐม", "นครพนม", "นครราชสีมา",
                "นครศรีธรรมราช", "นครสวรรค์", "นราธิวาส", "น่าน", "บึงกาฬ", "บุรีรัมย์", "บุรีย์รัมย์", "ปทุมธานี", "ประจวบคีรีขันธ์",
                "ปราจีนบุรี", "ปัตตานี", "พระนครศรีอยุธยา", "อยุธยา", "พะเยา", "พังงา", "พัทลุง", "พิจิตร", "พิษณุโลก",
                "เพชรบุรี", "เพชรบูรณ์", "แพร่", "ภูเก็ต", "มหาสารคาม", "มุกดาหาร", "แม่ฮ่องสอน", "ยโสธร", "ยะลา",
                "ร้อยเอ็ด", "ระนอง", "ระยอง", "ราชบุรี", "ลพบุรี", "ลำปาง", "ลำพูน", "เลย", "ศรีสะเกษ", "สกลนคร",
                "สงขลา", "สตูล", "สมุทรสงคราม", "สมุทรสาคร", "สระแก้ว", "สระบุรี", "สิงห์บุรี", "สุโขทัย", "สุพรรณบุรี", "สุราษฎร์ธานี",
                "สุรินทร์", "หนองคาย", "หนองบัวลำภู", "อ่างทอง", "อำนาจเจริญ", "อุดรธานี", "อุตรดิตถ์", "อุทัยธานี", "อุบลราชธานี"
            },
            ["โมเดิร์นเทรด"] = AllThailandProvinces,
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
            ["ออนไลน์"] = AllThailandProvinces,
            ["กรุงเทพ, นนทบุรี, สมุทรปราการ"] = new List<string>
            {
                "กรุงเทพมหานคร", "นนทบุรี", "สมุทรปราการ"
            },
            ["ต่างจังหวัด"] = new List<string>
            {
                "กระบี่", "กาญจนบุรี", "กาฬสินธุ์", "กำแพงเพชร", "ขอนแก่น", "จันทบุรี", "ฉะเชิงเทรา", "ชลบุรี", "ชัยนาท",
                "ชัยภูมิ", "ชุมพร", "เชียงราย", "เชียงใหม่", "ตรัง", "ตราด", "ตาก", "นครนายก", "นครปฐม", "นครพนม", "นครราชสีมา",
                "นครศรีธรรมราช", "นครสวรรค์", "นราธิวาส", "น่าน", "บึงกาฬ", "บุรีรัมย์", "ปทุมธานี", "ประจวบคีรีขันธ์",
                "ปราจีนบุรี", "ปัตตานี", "พระนครศรีอยุธยา", "พะเยา", "พังงา", "พัทลุง", "พิจิตร", "พิษณุโลก",
                "เพชรบุรี", "เพชรบูรณ์", "แพร่", "ภูเก็ต", "มหาสารคาม", "มุกดาหาร", "แม่ฮ่องสอน", "ยโสธร", "ยะลา",
                "ร้อยเอ็ด", "ระนอง", "ระยอง", "ราชบุรี", "ลพบุรี", "ลำปาง", "ลำพูน", "เลย", "ศรีสะเกษ", "สกลนคร",
                "สงขลา", "สตูล", "สมุทรสงคราม", "สมุทรสาคร", "สระแก้ว", "สระบุรี", "สิงห์บุรี", "สุโขทัย", "สุพรรณบุรี", "สุราษฎร์ธานี",
                "สุรินทร์", "หนองคาย", "หนองบัวลำภู", "อ่างทอง", "อำนาจเจริญ", "อุดรธานี", "อุตรดิตถ์", "อุทัยธานี", "อุบลราชธานี"
            },
            ["โมเดิร์นเทรด"] = AllThailandProvinces,
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
                    else if (r == "ออนไลน์" || r == "บิลขายออนไลน์" || r.Equals("online", StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddRange(AllThailandProvinces);
                    }
                    else if (r == "กรุงเทพ, นนทบุรี, สมุทรปราการ" || r == "กรุงเทพ,นนทบุรี,สมุทรปราการ" || r == "กรุงเทพฯ และปริมณฑล" || r == "กรุงเทพและปริมณฑล" || r == "กรุงเทพมหานครและปริมณฑล" || r.Equals("bkk", StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddRange(new[] { "กรุงเทพมหานคร", "กรุงเทพฯ", "กรุงเทพ", "กทม", "นนทบุรี", "สมุทรปราการ" });
                    }
                    else if (r == "ต่างจังหวัด" || r == "ต่างจังหวัด (ทุกภาค)" || r.Equals("upcountry", StringComparison.OrdinalIgnoreCase))
                    {
                        var bkkVicinity = new HashSet<string>(new[] { "กรุงเทพมหานคร", "กรุงเทพฯ", "กรุงเทพ", "กทม", "นนทบุรี", "สมุทรปราการ" }, StringComparer.OrdinalIgnoreCase);
                        var upcProvinces = AllThailandProvinces.Where(p => !bkkVicinity.Contains(p)).ToList();
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

                var withoutProv = raw;
                int parenIdx = withoutProv.IndexOf('(');
                if (parenIdx >= 0)
                {
                    withoutProv = withoutProv.Substring(0, parenIdx).Trim();
                    if (!string.IsNullOrEmpty(withoutProv)) res.Add(withoutProv);
                }

                var clean = withoutProv;
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

                if (clean.StartsWith("เมือง", StringComparison.OrdinalIgnoreCase))
                {
                    res.Add("เมือง");
                    res.Add("อ.เมือง");
                    res.Add("อำเภอเมือง");
                }
            }
            return res.ToList();
        }

        public static bool IsBkkAndVicinity(string? province, string? district)
        {
            string p = (province ?? "").Trim();
            string d = (district ?? "").Trim();
            string combined = (p + " " + d).ToLower();

            var bkkKeywords = new[] { "กรุงเทพ", "กทม", "bangkok", "นนทบุรี", "ปทุมธานี", "สมุทรปราการ", "สมุทรสาคร", "นครปฐม" };
            return bkkKeywords.Any(k => combined.Contains(k));
        }

        public static string CleanProvinceName(string p)
        {
            if (string.IsNullOrEmpty(p)) return "";
            var s = p.Trim();
            if (s.StartsWith("จ.", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2).Trim();
            else if (s.StartsWith("จังหวัด", StringComparison.OrdinalIgnoreCase)) s = s.Substring(6).Trim();
            return s;
        }

        public static AreaFilterResult ResolveAreaFilter(
            bool isRestricted,
            string? userAllowedRegion,
            string? userAllowedProvinces,
            string? requestedRegion,
            string? requestedProvince)
        {
            var res = new AreaFilterResult();

            var assignedRegions = new List<string>();
            if (isRestricted && !string.IsNullOrWhiteSpace(userAllowedRegion))
            {
                assignedRegions = userAllowedRegion
                    .Split(new[] { ',', ';', '/', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var assignedProvinces = new List<string>();
            if (isRestricted && !string.IsNullOrWhiteSpace(userAllowedProvinces))
            {
                assignedProvinces = userAllowedProvinces
                    .Split(new[] { ',', ';', '/', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            res.HasRegionRestriction = assignedRegions.Count > 0;
            res.HasProvinceRestriction = assignedProvinces.Count > 0;

            // 1. Available Regions for Dropdown
            if (res.HasRegionRestriction)
            {
                res.AvailableRegions = assignedRegions;
            }
            else
            {
                res.AvailableRegions = GetRegions();
            }

            // 2. Resolve Selected Region
            string? effectiveRegion = null;
            if (!string.IsNullOrWhiteSpace(requestedRegion))
            {
                var reqTrim = requestedRegion.Trim();
                if (res.HasRegionRestriction)
                {
                    var matched = assignedRegions.FirstOrDefault(r => 
                        string.Equals(r, reqTrim, StringComparison.OrdinalIgnoreCase) ||
                        r.IndexOf(reqTrim, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        reqTrim.IndexOf(r, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (matched != null)
                    {
                        effectiveRegion = matched;
                    }
                }
                else
                {
                    effectiveRegion = reqTrim;
                }
            }
            res.SelectedRegion = effectiveRegion ?? "";

            // 3. Resolve Available Provinces for Dropdown
            List<string> baseProvinces;
            if (res.HasProvinceRestriction)
            {
                if (!string.IsNullOrEmpty(res.SelectedRegion))
                {
                    var regionProvs = GetMatchingProvinces(res.SelectedRegion);
                    var matchedProvs = assignedProvinces.Where(p => regionProvs.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();
                    baseProvinces = matchedProvs.Count > 0 ? matchedProvs : assignedProvinces;
                }
                else
                {
                    baseProvinces = assignedProvinces;
                }
            }
            else if (!string.IsNullOrEmpty(res.SelectedRegion))
            {
                baseProvinces = GetDisplayProvinces(res.SelectedRegion);
            }
            else if (res.HasRegionRestriction)
            {
                baseProvinces = GetDisplayProvinces(string.Join(",", assignedRegions));
            }
            else
            {
                baseProvinces = AllThailandProvinces;
            }
            res.AvailableProvinces = baseProvinces.Distinct().OrderBy(p => p).ToList();

            // 4. Resolve Selected Province
            string? effectiveProvince = null;
            if (!string.IsNullOrWhiteSpace(requestedProvince))
            {
                var provTrim = requestedProvince.Trim();
                var matchedProv = res.AvailableProvinces.FirstOrDefault(p => 
                    string.Equals(p, provTrim, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(CleanProvinceName(p), CleanProvinceName(provTrim), StringComparison.OrdinalIgnoreCase) ||
                    p.Contains(provTrim, StringComparison.OrdinalIgnoreCase) ||
                    provTrim.Contains(p, StringComparison.OrdinalIgnoreCase));
                
                if (matchedProv != null)
                {
                    effectiveProvince = matchedProv;
                }
                else if (!res.HasProvinceRestriction && !res.HasRegionRestriction)
                {
                    effectiveProvince = provTrim;
                }
            }
            res.SelectedProvince = effectiveProvince ?? "";

            // 5. Effective Province Variants to filter in DB Query
            if (!string.IsNullOrEmpty(effectiveProvince))
            {
                res.EffectiveProvinceVariants = ExpandProvinceVariants(new[] { effectiveProvince });
            }
            else if (!string.IsNullOrEmpty(effectiveRegion))
            {
                var regProvs = GetMatchingProvinces(effectiveRegion);
                if (res.HasProvinceRestriction)
                {
                    var expAssigned = ExpandProvinceVariants(assignedProvinces);
                    res.EffectiveProvinceVariants = regProvs.Where(p => expAssigned.Contains(p)).ToList();
                }
                else
                {
                    res.EffectiveProvinceVariants = regProvs;
                }
            }
            else if (res.HasProvinceRestriction)
            {
                res.EffectiveProvinceVariants = ExpandProvinceVariants(assignedProvinces);
            }
            else if (res.HasRegionRestriction)
            {
                res.EffectiveProvinceVariants = GetMatchingProvinces(string.Join(",", assignedRegions));
            }
            else
            {
                res.EffectiveProvinceVariants = null;
            }

            // 6. Populate RegionProvincesMap for cascaded dropdowns in UI
            foreach (var r in res.AvailableRegions)
            {
                if (res.HasProvinceRestriction)
                {
                    var regProvs = GetMatchingProvinces(r);
                    var matched = assignedProvinces.Where(p => regProvs.Contains(p, StringComparer.OrdinalIgnoreCase)).Distinct().OrderBy(p => p).ToList();
                    res.RegionProvincesMap[r] = matched.Count > 0 ? matched : new List<string>();
                }
                else if (DisplayProvinces.TryGetValue(r, out var provList))
                {
                    res.RegionProvincesMap[r] = provList.OrderBy(p => p).ToList();
                }
                else
                {
                    res.RegionProvincesMap[r] = GetDisplayProvinces(r);
                }
            }

            return res;
        }

        public static IQueryable<RoyalD.Web.Models.SalesBill> FilterModernTradeSalesBills(
            IQueryable<RoyalD.Web.Models.SalesBill> q,
            bool onlyModernTrade)
        {
            var mtCodes = DashboardService.ModernTradeCodes.ToList();
            if (onlyModernTrade)
            {
                return q.Where(b => 
                    b.CustomerCode != "103166" &&
                    (b.CustomerName == null || (
                        !b.CustomerName.Contains("สาลี่สุพรรณ") &&
                        !b.CustomerName.Contains("คุณเอกชัย") &&
                        !b.CustomerName.Contains("ยากาแร็ต") &&
                        !b.CustomerName.Contains("คลองสี่วา") &&
                        !b.CustomerName.Contains("เอกชัยกอล์ฟ") &&
                        !b.CustomerName.Contains("เพียวเคมม์") &&
                        !b.CustomerName.Contains("ปรีชา  วิลล่า") &&
                        !b.CustomerName.Contains("มหาชัยวิลล่า") &&
                        !b.CustomerName.Contains("ท็อป ไฮเทค") &&
                        !b.CustomerName.Contains("ท็อป พาร์ทเนอร์") &&
                        !b.CustomerName.Contains("วินนิ่ง")
                    )) &&
                    (
                        (b.SalesRep != null && (b.SalesRep.Contains("วันวิสา") || b.SalesRep.Contains("ตันหยง") || b.SalesRep.Contains("ณัฐกุล") || b.SalesRep.Contains("อิทธิพัท") || b.SalesRep.Contains("ธนดล")))
                        || mtCodes.Contains(b.CustomerCode) || (
                        b.CustomerName != null &&
                        (
                            b.CustomerName.Contains("แมสมาร์เกตติ้ง") ||
                        b.CustomerName.Contains("แมสมาร์เก็ตติ้ง") ||
                        b.CustomerName.Contains("แมสมาร์ติ้ง") ||
                        b.CustomerName.ToLower().Contains("mass marketing") ||
                        b.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                        b.CustomerName.Contains("ซีพี แอ๊กซ์ตร้า") ||
                        b.CustomerName.Contains("สยามแม็คโคร") ||
                        b.CustomerName.Contains("สยาม แม็คโคร") ||
                        b.CustomerName.Contains("แม็คโคร") ||
                        b.CustomerName.Contains("ซีพี ออลล์") ||
                        b.CustomerName.Contains("ซีพีออลล์") ||
                        b.CustomerName.Contains("เซ็นทรัล ฟู้ด") ||
                        b.CustomerName.Contains("บิ๊กซี") ||
                        b.CustomerName.ToLower().Contains("big c") ||
                        b.CustomerName.Contains("สุวรรณชาด") ||
                        b.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") ||
                        b.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                        b.CustomerName.Contains("พี แอนด์ เอฟ อินทิเกรท") ||
                        b.CustomerName.Contains("อิออน (ไทยแลนด์)") ||
                        b.CustomerName.Contains("ซี.เจ.เอ็กซ์เพรส") ||
                        b.CustomerName.Contains("ซี.เจ. เอ็กซ์เพรส") ||
                        b.CustomerName.Contains("ฟู้ดแลนด์") ||
                        b.CustomerName.Contains("ฮาร์เบอร์ แลนด์") ||
                        b.CustomerName.Contains("ฮาร์เบอร์แลนด์") ||
                        b.CustomerName.Contains("บู๊ทส์ รีเทล") ||
                        b.CustomerName.Contains("เดอะมอลล์ กรุ๊ป") ||
                        b.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                        b.CustomerName.Contains("สห ลอว์สัน") ||
                        b.CustomerName.Contains("เซ็นทรัล วัตสัน") ||
                        b.CustomerName.Contains("ซูรูฮะ (ประเทศไทย)") ||
                        b.CustomerName.Contains("ฟาร์มาฮอฟ") ||
                        b.CustomerName.Contains("ฟาร์มมาฮอฟ") ||
                        b.CustomerName.ToLower().Contains("pharmahof") ||
                        b.CustomerName.Contains("ไทยฟู้ดส์") || b.CustomerName.Contains("ไทย ฟู้ดส์") ||
                        b.CustomerName.Contains("ฟาสซิโน") ||
                        b.CustomerName.Contains("ใบเมี่ยง")
                        )
                    ))
                );
            }
            else
            {
                return q.Where(b => 
                    b.CustomerCode == "103166" ||
                    (b.CustomerName != null && (
                        b.CustomerName.Contains("สาลี่สุพรรณ") ||
                        b.CustomerName.Contains("คุณเอกชัย") ||
                        b.CustomerName.Contains("ยากาแร็ต") ||
                        b.CustomerName.Contains("คลองสี่วา") ||
                        b.CustomerName.Contains("เอกชัยกอล์ฟ") ||
                        b.CustomerName.Contains("เพียวเคมม์") ||
                        b.CustomerName.Contains("ปรีชา  วิลล่า") ||
                        b.CustomerName.Contains("มหาชัยวิลล่า") ||
                        b.CustomerName.Contains("ท็อป ไฮเทค") ||
                        b.CustomerName.Contains("ท็อป พาร์ทเนอร์") ||
                        b.CustomerName.Contains("วินนิ่ง")
                    )) ||
                    (
                        !(b.SalesRep != null && (b.SalesRep.Contains("วันวิสา") || b.SalesRep.Contains("ตันหยง") || b.SalesRep.Contains("ณัฐกุล") || b.SalesRep.Contains("อิทธิพัท") || b.SalesRep.Contains("ธนดล")))
                        && !mtCodes.Contains(b.CustomerCode) && !(
                        b.CustomerName != null &&
                        (
                            b.CustomerName.Contains("แมสมาร์เกตติ้ง") ||
                        b.CustomerName.Contains("แมสมาร์เก็ตติ้ง") ||
                        b.CustomerName.Contains("แมสมาร์ติ้ง") ||
                        b.CustomerName.ToLower().Contains("mass marketing") ||
                        b.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                        b.CustomerName.Contains("ซีพี แอ๊กซ์ตร้า") ||
                        b.CustomerName.Contains("สยามแม็คโคร") ||
                        b.CustomerName.Contains("สยาม แม็คโคร") ||
                        b.CustomerName.Contains("แม็คโคร") ||
                        b.CustomerName.Contains("ซีพี ออลล์") ||
                        b.CustomerName.Contains("ซีพีออลล์") ||
                        b.CustomerName.Contains("เซ็นทรัล ฟู้ด") ||
                        b.CustomerName.Contains("บิ๊กซี") ||
                        b.CustomerName.ToLower().Contains("big c") ||
                        b.CustomerName.Contains("สุวรรณชาด") ||
                        b.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") ||
                        b.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                        b.CustomerName.Contains("พี แอนด์ เอฟ อินทิเกรท") ||
                        b.CustomerName.Contains("อิออน (ไทยแลนด์)") ||
                        b.CustomerName.Contains("ซี.เจ.เอ็กซ์เพรส") ||
                        b.CustomerName.Contains("ซี.เจ. เอ็กซ์เพรส") ||
                        b.CustomerName.Contains("ฟู้ดแลนด์") ||
                        b.CustomerName.Contains("ฮาร์เบอร์ แลนด์") ||
                        b.CustomerName.Contains("ฮาร์เบอร์แลนด์") ||
                        b.CustomerName.Contains("บู๊ทส์ รีเทล") ||
                        b.CustomerName.Contains("เดอะมอลล์ กรุ๊ป") ||
                        b.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                        b.CustomerName.Contains("สห ลอว์สัน") ||
                        b.CustomerName.Contains("เซ็นทรัล วัตสัน") ||
                        b.CustomerName.Contains("ซูรูฮะ (ประเทศไทย)") ||
                        b.CustomerName.Contains("ฟาร์มาฮอฟ") ||
                        b.CustomerName.Contains("ฟาร์มมาฮอฟ") ||
                        b.CustomerName.ToLower().Contains("pharmahof") ||
                        b.CustomerName.Contains("ไทยฟู้ดส์") || b.CustomerName.Contains("ไทย ฟู้ดส์") ||
                        b.CustomerName.Contains("ฟาสซิโน") ||
                        b.CustomerName.Contains("ใบเมี่ยง")
                        )
                    ))
                );
            }
        }

        

        public static IQueryable<RoyalD.Web.Models.SalesBillItem> FilterModernTradeSalesBillItems(
            IQueryable<RoyalD.Web.Models.SalesBillItem> q,
            bool onlyModernTrade)
        {
            var mtCodes = DashboardService.ModernTradeCodes.ToList();
            if (onlyModernTrade)
            {
                return q.Where(i => 
                    i.SalesBill.CustomerCode != "103166" &&
                    (i.SalesBill.CustomerName == null || (
                        !i.SalesBill.CustomerName.Contains("สาลี่สุพรรณ") &&
                        !i.SalesBill.CustomerName.Contains("คุณเอกชัย") &&
                        !i.SalesBill.CustomerName.Contains("ยากาแร็ต") &&
                        !i.SalesBill.CustomerName.Contains("คลองสี่วา") &&
                        !i.SalesBill.CustomerName.Contains("เอกชัยกอล์ฟ") &&
                        !i.SalesBill.CustomerName.Contains("เพียวเคมม์") &&
                        !i.SalesBill.CustomerName.Contains("ปรีชา  วิลล่า") &&
                        !i.SalesBill.CustomerName.Contains("มหาชัยวิลล่า") &&
                        !i.SalesBill.CustomerName.Contains("ท็อป ไฮเทค") &&
                        !i.SalesBill.CustomerName.Contains("ท็อป พาร์ทเนอร์") &&
                        !i.SalesBill.CustomerName.Contains("วินนิ่ง")
                    )) &&
                    (
                        (i.SalesBill.SalesRep != null && (i.SalesBill.SalesRep.Contains("วันวิสา") || i.SalesBill.SalesRep.Contains("ตันหยง") || i.SalesBill.SalesRep.Contains("ณัฐกุล") || i.SalesBill.SalesRep.Contains("อิทธิพัท") || i.SalesBill.SalesRep.Contains("ธนดล")))
                        || mtCodes.Contains(i.SalesBill.CustomerCode) || (
                        i.SalesBill.CustomerName != null &&
                        (
                            i.SalesBill.CustomerName.Contains("แมสมาร์เกตติ้ง") ||
                        i.SalesBill.CustomerName.Contains("แมสมาร์เก็ตติ้ง") ||
                        i.SalesBill.CustomerName.Contains("แมสมาร์ติ้ง") ||
                        i.SalesBill.CustomerName.ToLower().Contains("mass marketing") ||
                        i.SalesBill.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                        i.SalesBill.CustomerName.Contains("ซีพี แอ๊กซ์ตร้า") ||
                        i.SalesBill.CustomerName.Contains("สยามแม็คโคร") ||
                        i.SalesBill.CustomerName.Contains("สยาม แม็คโคร") ||
                        i.SalesBill.CustomerName.Contains("แม็คโคร") ||
                        i.SalesBill.CustomerName.Contains("ซีพี ออลล์") ||
                        i.SalesBill.CustomerName.Contains("ซีพีออลล์") ||
                        i.SalesBill.CustomerName.Contains("เซ็นทรัล ฟู้ด") ||
                        i.SalesBill.CustomerName.Contains("บิ๊กซี") ||
                        i.SalesBill.CustomerName.ToLower().Contains("big c") ||
                        i.SalesBill.CustomerName.Contains("สุวรรณชาด") ||
                        i.SalesBill.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") ||
                        i.SalesBill.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                        i.SalesBill.CustomerName.Contains("พี แอนด์ เอฟ อินทิเกรท") ||
                        i.SalesBill.CustomerName.Contains("อิออน (ไทยแลนด์)") ||
                        i.SalesBill.CustomerName.Contains("ซี.เจ.เอ็กซ์เพรส") ||
                        i.SalesBill.CustomerName.Contains("ซี.เจ. เอ็กซ์เพรส") ||
                        i.SalesBill.CustomerName.Contains("ฟู้ดแลนด์") ||
                        i.SalesBill.CustomerName.Contains("ฮาร์เบอร์ แลนด์") ||
                        i.SalesBill.CustomerName.Contains("ฮาร์เบอร์แลนด์") ||
                        i.SalesBill.CustomerName.Contains("บู๊ทส์ รีเทล") ||
                        i.SalesBill.CustomerName.Contains("เดอะมอลล์ กรุ๊ป") ||
                        i.SalesBill.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                        i.SalesBill.CustomerName.Contains("สห ลอว์สัน") ||
                        i.SalesBill.CustomerName.Contains("เซ็นทรัล วัตสัน") ||
                        i.SalesBill.CustomerName.Contains("ซูรูฮะ (ประเทศไทย)") ||
                        i.SalesBill.CustomerName.Contains("ฟาร์มาฮอฟ") ||
                        i.SalesBill.CustomerName.Contains("ฟาร์มมาฮอฟ") ||
                        i.SalesBill.CustomerName.ToLower().Contains("pharmahof") ||
                        i.SalesBill.CustomerName.Contains("ไทยฟู้ดส์") || i.SalesBill.CustomerName.Contains("ไทย ฟู้ดส์") ||
                        i.SalesBill.CustomerName.Contains("ฟาสซิโน") ||
                        i.SalesBill.CustomerName.Contains("ใบเมี่ยง")
                        )
                    ))
                );
            }
            else
            {
                return q.Where(i => 
                    i.SalesBill.CustomerCode == "103166" ||
                    (i.SalesBill.CustomerName != null && (
                        i.SalesBill.CustomerName.Contains("สาลี่สุพรรณ") ||
                        i.SalesBill.CustomerName.Contains("คุณเอกชัย") ||
                        i.SalesBill.CustomerName.Contains("ยากาแร็ต") ||
                        i.SalesBill.CustomerName.Contains("คลองสี่วา") ||
                        i.SalesBill.CustomerName.Contains("เอกชัยกอล์ฟ") ||
                        i.SalesBill.CustomerName.Contains("เพียวเคมม์") ||
                        i.SalesBill.CustomerName.Contains("ปรีชา  วิลล่า") ||
                        i.SalesBill.CustomerName.Contains("มหาชัยวิลล่า") ||
                        i.SalesBill.CustomerName.Contains("ท็อป ไฮเทค") ||
                        i.SalesBill.CustomerName.Contains("ท็อป พาร์ทเนอร์") ||
                        i.SalesBill.CustomerName.Contains("วินนิ่ง")
                    )) ||
                    (
                        !(i.SalesBill.SalesRep != null && (i.SalesBill.SalesRep.Contains("วันวิสา") || i.SalesBill.SalesRep.Contains("ตันหยง") || i.SalesBill.SalesRep.Contains("ณัฐกุล") || i.SalesBill.SalesRep.Contains("อิทธิพัท") || i.SalesBill.SalesRep.Contains("ธนดล")))
                        && !mtCodes.Contains(i.SalesBill.CustomerCode) && !(
                        i.SalesBill.CustomerName != null &&
                        (
                            i.SalesBill.CustomerName.Contains("แมสมาร์เกตติ้ง") ||
                        i.SalesBill.CustomerName.Contains("แมสมาร์เก็ตติ้ง") ||
                        i.SalesBill.CustomerName.Contains("แมสมาร์ติ้ง") ||
                        i.SalesBill.CustomerName.ToLower().Contains("mass marketing") ||
                        i.SalesBill.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                        i.SalesBill.CustomerName.Contains("ซีพี แอ๊กซ์ตร้า") ||
                        i.SalesBill.CustomerName.Contains("สยามแม็คโคร") ||
                        i.SalesBill.CustomerName.Contains("สยาม แม็คโคร") ||
                        i.SalesBill.CustomerName.Contains("แม็คโคร") ||
                        i.SalesBill.CustomerName.Contains("ซีพี ออลล์") ||
                        i.SalesBill.CustomerName.Contains("ซีพีออลล์") ||
                        i.SalesBill.CustomerName.Contains("เซ็นทรัล ฟู้ด") ||
                        i.SalesBill.CustomerName.Contains("บิ๊กซี") ||
                        i.SalesBill.CustomerName.ToLower().Contains("big c") ||
                        i.SalesBill.CustomerName.Contains("สุวรรณชาด") ||
                        i.SalesBill.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") ||
                        i.SalesBill.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                        i.SalesBill.CustomerName.Contains("พี แอนด์ เอฟ อินทิเกรท") ||
                        i.SalesBill.CustomerName.Contains("อิออน (ไทยแลนด์)") ||
                        i.SalesBill.CustomerName.Contains("ซี.เจ.เอ็กซ์เพรส") ||
                        i.SalesBill.CustomerName.Contains("ซี.เจ. เอ็กซ์เพรส") ||
                        i.SalesBill.CustomerName.Contains("ฟู้ดแลนด์") ||
                        i.SalesBill.CustomerName.Contains("ฮาร์เบอร์ แลนด์") ||
                        i.SalesBill.CustomerName.Contains("ฮาร์เบอร์แลนด์") ||
                        i.SalesBill.CustomerName.Contains("บู๊ทส์ รีเทล") ||
                        i.SalesBill.CustomerName.Contains("เดอะมอลล์ กรุ๊ป") ||
                        i.SalesBill.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                        i.SalesBill.CustomerName.Contains("สห ลอว์สัน") ||
                        i.SalesBill.CustomerName.Contains("เซ็นทรัล วัตสัน") ||
                        i.SalesBill.CustomerName.Contains("ซูรูฮะ (ประเทศไทย)") ||
                        i.SalesBill.CustomerName.Contains("ฟาร์มาฮอฟ") ||
                        i.SalesBill.CustomerName.Contains("ฟาร์มมาฮอฟ") ||
                        i.SalesBill.CustomerName.ToLower().Contains("pharmahof") ||
                        i.SalesBill.CustomerName.Contains("ไทยฟู้ดส์") || i.SalesBill.CustomerName.Contains("ไทย ฟู้ดส์") ||
                        i.SalesBill.CustomerName.Contains("ฟาสซิโน") ||
                        i.SalesBill.CustomerName.Contains("ใบเมี่ยง")
                        )
                    ))
                );
            }
        }

        

        public static bool IsOnlineSalesRep(string? rep)
        {
            if (string.IsNullOrWhiteSpace(rep)) return false;
            var r = rep.Trim();
            return r.StartsWith("บริษัท*") || r.StartsWith("บริษัทT");
        }

        public static IQueryable<RoyalD.Web.Models.SalesBill> FilterOnlineSalesBills(
            IQueryable<RoyalD.Web.Models.SalesBill> q,
            bool isOnline)
        {
            if (isOnline)
            {
                return q.Where(b => b.SalesRep != null && (b.SalesRep.StartsWith("บริษัท*") || b.SalesRep.StartsWith("บริษัทT")));
            }
            else
            {
                return q.Where(b => b.SalesRep == null || (!b.SalesRep.StartsWith("บริษัท*") && !b.SalesRep.StartsWith("บริษัทT")));
            }
        }

        public static IQueryable<RoyalD.Web.Models.SalesBillItem> FilterOnlineSalesBillItems(
            IQueryable<RoyalD.Web.Models.SalesBillItem> q,
            bool isOnline)
        {
            if (isOnline)
            {
                return q.Where(i => i.SalesBill.SalesRep != null && (i.SalesBill.SalesRep.StartsWith("บริษัท*") || i.SalesBill.SalesRep.StartsWith("บริษัทT")));
            }
            else
            {
                return q.Where(i => i.SalesBill.SalesRep == null || (!i.SalesBill.SalesRep.StartsWith("บริษัท*") && !i.SalesBill.SalesRep.StartsWith("บริษัทT")));
            }
        }

        public static IQueryable<RoyalD.Web.Models.SalesBill> ApplySalesBillAreaFilter(
            IQueryable<RoyalD.Web.Models.SalesBill> q,
            AreaFilterResult areaScope)
        {
            var parts = (areaScope.SelectedRegion ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .ToList();

            bool isOnline = parts.Any(r => r == "ออนไลน์" || r == "บิลขายออนไลน์" || string.Equals(r, "online", StringComparison.OrdinalIgnoreCase));
            bool isMt = parts.Any(r => r == "โมเดิร์นเทรด" || string.Equals(r, "moderntrade", StringComparison.OrdinalIgnoreCase));
            bool isUpc = parts.Any(r => r == "ต่างจังหวัด" || r == "ต่างจังหวัด (ทุกภาค)" || string.Equals(r, "upcountry", StringComparison.OrdinalIgnoreCase));
            bool isBkk = parts.Any(r => r == "กรุงเทพ, นนทบุรี, สมุทรปราการ" || r == "กรุงเทพ,นนทบุรี,สมุทรปราการ" || r == "กรุงเทพฯ และปริมณฑล" || r == "กรุงเทพและปริมณฑล" || r == "กรุงเทพมหานครและปริมณฑล" || string.Equals(r, "bkk", StringComparison.OrdinalIgnoreCase));

            int count = (isOnline ? 1 : 0) + (isMt ? 1 : 0) + (isUpc ? 1 : 0) + (isBkk ? 1 : 0);

            var bkkVariants = GetMatchingProvinces("กรุงเทพ, นนทบุรี, สมุทรปราการ");
            var mtCodes = DashboardService.ModernTradeCodes.ToList();

            if (count > 0 && count < 4)
            {
                if (!isOnline)
                {
                    q = FilterOnlineSalesBills(q, false);

                    if (isMt && !isBkk && !isUpc)
                    {
                        // MT only
                        q = FilterModernTradeSalesBills(q, true);
                    }
                    else if (!isMt && isBkk && !isUpc)
                    {
                        // BKK only
                        q = FilterModernTradeSalesBills(q, false);
                        q = q.Where(b => bkkVariants.Contains(b.Province)
                                      || b.Province.Contains("กรุงเทพ") || b.Province.Contains("กทม")
                                      || b.Province.Contains("นนทบุรี")
                                      || b.Province.Contains("สมุทรปราการ"));
                    }
                    else if (!isMt && !isBkk && isUpc)
                    {
                        // UPC only
                        q = FilterModernTradeSalesBills(q, false);
                        q = q.Where(b => !bkkVariants.Contains(b.Province)
                                      && !b.Province.Contains("กรุงเทพ") && !b.Province.Contains("กทม")
                                      && !b.Province.Contains("นนทบุรี")
                                      && !b.Province.Contains("สมุทรปราการ"));
                    }
                    else if (isMt && isUpc && !isBkk)
                    {
                        // MT + Upcountry (Exclude BKK regular shops)
                        q = q.Where(b => (!bkkVariants.Contains(b.Province)
                                      && !b.Province.Contains("กรุงเทพ") && !b.Province.Contains("กทม")
                                      && !b.Province.Contains("นนทบุรี")
                                      && !b.Province.Contains("สมุทรปราการ"))
                                      || mtCodes.Contains(b.CustomerCode)
                                      || (b.CustomerName != null && (
                                          b.CustomerName.Contains("บิ๊กซี") || b.CustomerName.ToLower().Contains("big c") ||
                                          b.CustomerName.Contains("แม็คโคร") || b.CustomerName.Contains("ซีพี ออลล์") ||
                                          b.CustomerName.Contains("ซีพีออลล์") || b.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          b.CustomerName.Contains("เซ็นทรัล") || b.CustomerName.Contains("เดอะมอลล์") ||
                                          b.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || b.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          b.CustomerName.Contains("โลตัส") || b.CustomerName.Contains("ซี.เจ") ||
                                          b.CustomerName.Contains("ฟู้ดแลนด์") || b.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          b.CustomerName.Contains("วัตสัน") || b.CustomerName.Contains("บู๊ทส์") ||
                                          b.CustomerName.Contains("ซูรูฮะ") || b.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                    else if (isMt && isBkk && !isUpc)
                    {
                        // MT + BKK (Exclude Upcountry regular shops)
                        q = q.Where(b => bkkVariants.Contains(b.Province)
                                      || b.Province.Contains("กรุงเทพ") || b.Province.Contains("กทม")
                                      || b.Province.Contains("นนทบุรี")
                                      || b.Province.Contains("สมุทรปราการ")
                                      || mtCodes.Contains(b.CustomerCode)
                                      || (b.CustomerName != null && (
                                          b.CustomerName.Contains("บิ๊กซี") || b.CustomerName.ToLower().Contains("big c") ||
                                          b.CustomerName.Contains("แม็คโคร") || b.CustomerName.Contains("ซีพี ออลล์") ||
                                          b.CustomerName.Contains("ซีพีออลล์") || b.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          b.CustomerName.Contains("เซ็นทรัล") || b.CustomerName.Contains("เดอะมอลล์") ||
                                          b.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || b.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          b.CustomerName.Contains("โลตัส") || b.CustomerName.Contains("ซี.เจ") ||
                                          b.CustomerName.Contains("ฟู้ดแลนด์") || b.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          b.CustomerName.Contains("วัตสัน") || b.CustomerName.Contains("บู๊ทส์") ||
                                          b.CustomerName.Contains("ซูรูฮะ") || b.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                    else if (!isMt && isBkk && isUpc)
                    {
                        // BKK + UPC (All regular offline, exclude MT)
                        q = FilterModernTradeSalesBills(q, false);
                    }
                }
                else
                {
                    // isOnline is true
                    if (!isMt && !isBkk && !isUpc)
                    {
                        // Online only
                        q = FilterOnlineSalesBills(q, true);
                    }
                    else if (!isMt && isBkk && isUpc)
                    {
                        // Online + BKK + UPC (Exclude MT)
                        q = FilterModernTradeSalesBills(q, false);
                    }
                    else if (isMt && !isBkk && !isUpc)
                    {
                        // Online + MT
                        q = q.Where(b => (b.SalesRep != null && (b.SalesRep.StartsWith("บริษัท*") || b.SalesRep.StartsWith("บริษัทT")))
                                      || mtCodes.Contains(b.CustomerCode)
                                      || (b.CustomerName != null && (
                                          b.CustomerName.Contains("บิ๊กซี") || b.CustomerName.ToLower().Contains("big c") ||
                                          b.CustomerName.Contains("แม็คโคร") || b.CustomerName.Contains("ซีพี ออลล์") ||
                                          b.CustomerName.Contains("ซีพีออลล์") || b.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          b.CustomerName.Contains("เซ็นทรัล") || b.CustomerName.Contains("เดอะมอลล์") ||
                                          b.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || b.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          b.CustomerName.Contains("โลตัส") || b.CustomerName.Contains("ซี.เจ") ||
                                          b.CustomerName.Contains("ฟู้ดแลนด์") || b.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          b.CustomerName.Contains("วัตสัน") || b.CustomerName.Contains("บู๊ทส์") ||
                                          b.CustomerName.Contains("ซูรูฮะ") || b.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                    else if (isMt && isUpc && !isBkk)
                    {
                        // Online + MT + UPC (Exclude BKK regular shops)
                        q = q.Where(b => (b.SalesRep != null && (b.SalesRep.StartsWith("บริษัท*") || b.SalesRep.StartsWith("บริษัทT")))
                                      || (!bkkVariants.Contains(b.Province)
                                          && !b.Province.Contains("กรุงเทพ") && !b.Province.Contains("กทม")
                                          && !b.Province.Contains("นนทบุรี")
                                          && !b.Province.Contains("สมุทรปราการ"))
                                      || mtCodes.Contains(b.CustomerCode)
                                      || (b.CustomerName != null && (
                                          b.CustomerName.Contains("บิ๊กซี") || b.CustomerName.ToLower().Contains("big c") ||
                                          b.CustomerName.Contains("แม็คโคร") || b.CustomerName.Contains("ซีพี ออลล์") ||
                                          b.CustomerName.Contains("ซีพีออลล์") || b.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          b.CustomerName.Contains("เซ็นทรัล") || b.CustomerName.Contains("เดอะมอลล์") ||
                                          b.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || b.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          b.CustomerName.Contains("โลตัส") || b.CustomerName.Contains("ซี.เจ") ||
                                          b.CustomerName.Contains("ฟู้ดแลนด์") || b.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          b.CustomerName.Contains("วัตสัน") || b.CustomerName.Contains("บู๊ทส์") ||
                                          b.CustomerName.Contains("ซูรูฮะ") || b.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                    else if (isMt && isBkk && !isUpc)
                    {
                        // Online + MT + BKK (Exclude UPC regular shops)
                        q = q.Where(b => (b.SalesRep != null && (b.SalesRep.StartsWith("บริษัท*") || b.SalesRep.StartsWith("บริษัทT")))
                                      || bkkVariants.Contains(b.Province)
                                      || b.Province.Contains("กรุงเทพ") || b.Province.Contains("กทม")
                                      || b.Province.Contains("นนทบุรี")
                                      || b.Province.Contains("สมุทรปราการ")
                                      || mtCodes.Contains(b.CustomerCode)
                                      || (b.CustomerName != null && (
                                          b.CustomerName.Contains("บิ๊กซี") || b.CustomerName.ToLower().Contains("big c") ||
                                          b.CustomerName.Contains("แม็คโคร") || b.CustomerName.Contains("ซีพี ออลล์") ||
                                          b.CustomerName.Contains("ซีพีออลล์") || b.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          b.CustomerName.Contains("เซ็นทรัล") || b.CustomerName.Contains("เดอะมอลล์") ||
                                          b.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || b.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          b.CustomerName.Contains("โลตัส") || b.CustomerName.Contains("ซี.เจ") ||
                                          b.CustomerName.Contains("ฟู้ดแลนด์") || b.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          b.CustomerName.Contains("วัตสัน") || b.CustomerName.Contains("บู๊ทส์") ||
                                          b.CustomerName.Contains("ซูรูฮะ") || b.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                }
            }

            if (!string.IsNullOrEmpty(areaScope.SelectedProvince) && areaScope.EffectiveProvinceVariants != null && areaScope.EffectiveProvinceVariants.Count > 0)
            {
                q = q.Where(b => areaScope.EffectiveProvinceVariants.Contains(b.Province));
            }

            return q;
        }

        public static IQueryable<RoyalD.Web.Models.SalesBillItem> ApplySalesBillItemAreaFilter(
            IQueryable<RoyalD.Web.Models.SalesBillItem> q,
            AreaFilterResult areaScope)
        {
            var parts = (areaScope.SelectedRegion ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .ToList();

            bool isOnline = parts.Any(r => r == "ออนไลน์" || r == "บิลขายออนไลน์" || string.Equals(r, "online", StringComparison.OrdinalIgnoreCase));
            bool isMt = parts.Any(r => r == "โมเดิร์นเทรด" || string.Equals(r, "moderntrade", StringComparison.OrdinalIgnoreCase));
            bool isUpc = parts.Any(r => r == "ต่างจังหวัด" || r == "ต่างจังหวัด (ทุกภาค)" || string.Equals(r, "upcountry", StringComparison.OrdinalIgnoreCase));
            bool isBkk = parts.Any(r => r == "กรุงเทพ, นนทบุรี, สมุทรปราการ" || r == "กรุงเทพ,นนทบุรี,สมุทรปราการ" || r == "กรุงเทพฯ และปริมณฑล" || r == "กรุงเทพและปริมณฑล" || r == "กรุงเทพมหานครและปริมณฑล" || string.Equals(r, "bkk", StringComparison.OrdinalIgnoreCase));

            int count = (isOnline ? 1 : 0) + (isMt ? 1 : 0) + (isUpc ? 1 : 0) + (isBkk ? 1 : 0);

            var bkkVariants = GetMatchingProvinces("กรุงเทพ, นนทบุรี, สมุทรปราการ");
            var mtCodes = DashboardService.ModernTradeCodes.ToList();

            if (count > 0 && count < 4)
            {
                if (!isOnline)
                {
                    q = FilterOnlineSalesBillItems(q, false);

                    if (isMt && !isBkk && !isUpc)
                    {
                        // MT only
                        q = FilterModernTradeSalesBillItems(q, true);
                    }
                    else if (!isMt && isBkk && !isUpc)
                    {
                        // BKK only
                        q = FilterModernTradeSalesBillItems(q, false);
                        q = q.Where(i => bkkVariants.Contains(i.SalesBill.Province)
                                      || i.SalesBill.Province.Contains("กรุงเทพ") || i.SalesBill.Province.Contains("กทม")
                                      || i.SalesBill.Province.Contains("นนทบุรี")
                                      || i.SalesBill.Province.Contains("สมุทรปราการ"));
                    }
                    else if (!isMt && !isBkk && isUpc)
                    {
                        // UPC only
                        q = FilterModernTradeSalesBillItems(q, false);
                        q = q.Where(i => !bkkVariants.Contains(i.SalesBill.Province)
                                      && !i.SalesBill.Province.Contains("กรุงเทพ") && !i.SalesBill.Province.Contains("กทม")
                                      && !i.SalesBill.Province.Contains("นนทบุรี")
                                      && !i.SalesBill.Province.Contains("สมุทรปราการ"));
                    }
                    else if (isMt && isUpc && !isBkk)
                    {
                        // MT + Upcountry (Exclude BKK regular shops)
                        q = q.Where(i => (!bkkVariants.Contains(i.SalesBill.Province)
                                      && !i.SalesBill.Province.Contains("กรุงเทพ") && !i.SalesBill.Province.Contains("กทม")
                                      && !i.SalesBill.Province.Contains("นนทบุรี")
                                      && !i.SalesBill.Province.Contains("สมุทรปราการ"))
                                      || mtCodes.Contains(i.SalesBill.CustomerCode)
                                      || (i.SalesBill.CustomerName != null && (
                                          i.SalesBill.CustomerName.Contains("บิ๊กซี") || i.SalesBill.CustomerName.ToLower().Contains("big c") ||
                                          i.SalesBill.CustomerName.Contains("แม็คโคร") || i.SalesBill.CustomerName.Contains("ซีพี ออลล์") ||
                                          i.SalesBill.CustomerName.Contains("ซีพีออลล์") || i.SalesBill.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          i.SalesBill.CustomerName.Contains("เซ็นทรัล") || i.SalesBill.CustomerName.Contains("เดอะมอลล์") ||
                                          i.SalesBill.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || i.SalesBill.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          i.SalesBill.CustomerName.Contains("โลตัส") || i.SalesBill.CustomerName.Contains("ซี.เจ") ||
                                          i.SalesBill.CustomerName.Contains("ฟู้ดแลนด์") || i.SalesBill.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          i.SalesBill.CustomerName.Contains("วัตสัน") || i.SalesBill.CustomerName.Contains("บู๊ทส์") ||
                                          i.SalesBill.CustomerName.Contains("ซูรูฮะ") || i.SalesBill.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                    else if (isMt && isBkk && !isUpc)
                    {
                        // MT + BKK (Exclude Upcountry regular shops)
                        q = q.Where(i => bkkVariants.Contains(i.SalesBill.Province)
                                      || i.SalesBill.Province.Contains("กรุงเทพ") || i.SalesBill.Province.Contains("กทม")
                                      || i.SalesBill.Province.Contains("นนทบุรี")
                                      || i.SalesBill.Province.Contains("สมุทรปราการ")
                                      || mtCodes.Contains(i.SalesBill.CustomerCode)
                                      || (i.SalesBill.CustomerName != null && (
                                          i.SalesBill.CustomerName.Contains("บิ๊กซี") || i.SalesBill.CustomerName.ToLower().Contains("big c") ||
                                          i.SalesBill.CustomerName.Contains("แม็คโคร") || i.SalesBill.CustomerName.Contains("ซีพี ออลล์") ||
                                          i.SalesBill.CustomerName.Contains("ซีพีออลล์") || i.SalesBill.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          i.SalesBill.CustomerName.Contains("เซ็นทรัล") || i.SalesBill.CustomerName.Contains("เดอะมอลล์") ||
                                          i.SalesBill.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || i.SalesBill.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          i.SalesBill.CustomerName.Contains("โลตัส") || i.SalesBill.CustomerName.Contains("ซี.เจ") ||
                                          i.SalesBill.CustomerName.Contains("ฟู้ดแลนด์") || i.SalesBill.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          i.SalesBill.CustomerName.Contains("วัตสัน") || i.SalesBill.CustomerName.Contains("บู๊ทส์") ||
                                          i.SalesBill.CustomerName.Contains("ซูรูฮะ") || i.SalesBill.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                    else if (!isMt && isBkk && isUpc)
                    {
                        // BKK + UPC (All regular offline, exclude MT)
                        q = FilterModernTradeSalesBillItems(q, false);
                    }
                }
                else
                {
                    // isOnline is true
                    if (!isMt && !isBkk && !isUpc)
                    {
                        // Online only
                        q = FilterOnlineSalesBillItems(q, true);
                    }
                    else if (!isMt && isBkk && isUpc)
                    {
                        // Online + BKK + UPC (Exclude MT)
                        q = FilterModernTradeSalesBillItems(q, false);
                    }
                    else if (isMt && !isBkk && !isUpc)
                    {
                        // Online + MT
                        q = q.Where(i => (i.SalesBill.SalesRep != null && (i.SalesBill.SalesRep.StartsWith("บริษัท*") || i.SalesBill.SalesRep.StartsWith("บริษัทT")))
                                      || mtCodes.Contains(i.SalesBill.CustomerCode)
                                      || (i.SalesBill.CustomerName != null && (
                                          i.SalesBill.CustomerName.Contains("บิ๊กซี") || i.SalesBill.CustomerName.ToLower().Contains("big c") ||
                                          i.SalesBill.CustomerName.Contains("แม็คโคร") || i.SalesBill.CustomerName.Contains("ซีพี ออลล์") ||
                                          i.SalesBill.CustomerName.Contains("ซีพีออลล์") || i.SalesBill.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          i.SalesBill.CustomerName.Contains("เซ็นทรัล") || i.SalesBill.CustomerName.Contains("เดอะมอลล์") ||
                                          i.SalesBill.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || i.SalesBill.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          i.SalesBill.CustomerName.Contains("โลตัส") || i.SalesBill.CustomerName.Contains("ซี.เจ") ||
                                          i.SalesBill.CustomerName.Contains("ฟู้ดแลนด์") || i.SalesBill.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          i.SalesBill.CustomerName.Contains("วัตสัน") || i.SalesBill.CustomerName.Contains("บู๊ทส์") ||
                                          i.SalesBill.CustomerName.Contains("ซูรูฮะ") || i.SalesBill.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                    else if (isMt && isUpc && !isBkk)
                    {
                        // Online + MT + UPC (Exclude BKK regular shops)
                        q = q.Where(i => (i.SalesBill.SalesRep != null && (i.SalesBill.SalesRep.StartsWith("บริษัท*") || i.SalesBill.SalesRep.StartsWith("บริษัทT")))
                                      || (!bkkVariants.Contains(i.SalesBill.Province)
                                          && !i.SalesBill.Province.Contains("กรุงเทพ") && !i.SalesBill.Province.Contains("กทม")
                                          && !i.SalesBill.Province.Contains("นนทบุรี")
                                          && !i.SalesBill.Province.Contains("สมุทรปราการ"))
                                      || mtCodes.Contains(i.SalesBill.CustomerCode)
                                      || (i.SalesBill.CustomerName != null && (
                                          i.SalesBill.CustomerName.Contains("บิ๊กซี") || i.SalesBill.CustomerName.ToLower().Contains("big c") ||
                                          i.SalesBill.CustomerName.Contains("แม็คโคร") || i.SalesBill.CustomerName.Contains("ซีพี ออลล์") ||
                                          i.SalesBill.CustomerName.Contains("ซีพีออลล์") || i.SalesBill.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          i.SalesBill.CustomerName.Contains("เซ็นทรัล") || i.SalesBill.CustomerName.Contains("เดอะมอลล์") ||
                                          i.SalesBill.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || i.SalesBill.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          i.SalesBill.CustomerName.Contains("โลตัส") || i.SalesBill.CustomerName.Contains("ซี.เจ") ||
                                          i.SalesBill.CustomerName.Contains("ฟู้ดแลนด์") || i.SalesBill.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          i.SalesBill.CustomerName.Contains("วัตสัน") || i.SalesBill.CustomerName.Contains("บู๊ทส์") ||
                                          i.SalesBill.CustomerName.Contains("ซูรูฮะ") || i.SalesBill.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                    else if (isMt && isBkk && !isUpc)
                    {
                        // Online + MT + BKK (Exclude UPC regular shops)
                        q = q.Where(i => (i.SalesBill.SalesRep != null && (i.SalesBill.SalesRep.StartsWith("บริษัท*") || i.SalesBill.SalesRep.StartsWith("บริษัทT")))
                                      || bkkVariants.Contains(i.SalesBill.Province)
                                      || i.SalesBill.Province.Contains("กรุงเทพ") || i.SalesBill.Province.Contains("กทม")
                                      || i.SalesBill.Province.Contains("นนทบุรี")
                                      || i.SalesBill.Province.Contains("สมุทรปราการ")
                                      || mtCodes.Contains(i.SalesBill.CustomerCode)
                                      || (i.SalesBill.CustomerName != null && (
                                          i.SalesBill.CustomerName.Contains("บิ๊กซี") || i.SalesBill.CustomerName.ToLower().Contains("big c") ||
                                          i.SalesBill.CustomerName.Contains("แม็คโคร") || i.SalesBill.CustomerName.Contains("ซีพี ออลล์") ||
                                          i.SalesBill.CustomerName.Contains("ซีพีออลล์") || i.SalesBill.CustomerName.Contains("ซีพี แอ็กซ์ตร้า") ||
                                          i.SalesBill.CustomerName.Contains("เซ็นทรัล") || i.SalesBill.CustomerName.Contains("เดอะมอลล์") ||
                                          i.SalesBill.CustomerName.Contains("ปตท. บริหารธุรกิจค้าปลีก") || i.SalesBill.CustomerName.Contains("ปิโตรเลียมไทยคอร์ปอเรชั่น") ||
                                          i.SalesBill.CustomerName.Contains("โลตัส") || i.SalesBill.CustomerName.Contains("ซี.เจ") ||
                                          i.SalesBill.CustomerName.Contains("ฟู้ดแลนด์") || i.SalesBill.CustomerName.Contains("วิลล่า มาร์เก็ท") ||
                                          i.SalesBill.CustomerName.Contains("วัตสัน") || i.SalesBill.CustomerName.Contains("บู๊ทส์") ||
                                          i.SalesBill.CustomerName.Contains("ซูรูฮะ") || i.SalesBill.CustomerName.Contains("ฟาร์มาฮอฟ")
                                      )));
                    }
                }
            }

            if (!string.IsNullOrEmpty(areaScope.SelectedProvince) && areaScope.EffectiveProvinceVariants != null && areaScope.EffectiveProvinceVariants.Count > 0)
            {
                q = q.Where(i => areaScope.EffectiveProvinceVariants.Contains(i.SalesBill.Province));
            }

            return q;
        }

    }

    public class AreaFilterResult
    {
        public bool HasRegionRestriction { get; set; }
        public bool HasProvinceRestriction { get; set; }
        public List<string> AvailableRegions { get; set; } = new();
        public string SelectedRegion { get; set; } = "";
        public List<string> AvailableProvinces { get; set; } = new();
        public string SelectedProvince { get; set; } = "";
        public List<string>? EffectiveProvinceVariants { get; set; }
        public Dictionary<string, List<string>> RegionProvincesMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}