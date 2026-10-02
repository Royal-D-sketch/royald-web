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
            ["กรุงเทพฯ และปริมณฑล"] = new List<string>
            {
                "กรุงเทพมหานคร", "กรุงเทพฯ", "กรุงเทพ", "กทม", "นนทบุรี", "ปทุมธานี", "สมุทรปราการ", "สมุทรสาคร", "นครปฐม"
            },
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
            ["กรุงเทพฯ และปริมณฑล"] = new List<string>
            {
                "กรุงเทพมหานคร", "นนทบุรี", "ปทุมธานี", "สมุทรปราการ", "สมุทรสาคร", "นครปฐม"
            },
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
                    else if (r == "กรุงเทพฯ และปริมณฑล" || r == "กรุงเทพและปริมณฑล" || r == "กรุงเทพมหานครและปริมณฑล")
                    {
                        result.AddRange(new[] { "กรุงเทพมหานคร", "กรุงเทพฯ", "กรุงเทพ", "กทม", "นนทบุรี", "ปทุมธานี", "สมุทรปราการ", "สมุทรสาคร", "นครปฐม" });
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