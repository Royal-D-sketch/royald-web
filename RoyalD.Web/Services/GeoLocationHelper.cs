using System;
using System.Globalization;

namespace RoyalD.Web.Services
{
    public static class GeoLocationHelper
    {
        public static string ReverseGeocode(string? latStr, string? lngStr, string? existingArea = null)
        {
            double lat = 0, lng = 0;
            bool hasCoords = !string.IsNullOrEmpty(latStr) && !string.IsNullOrEmpty(lngStr) &&
                             double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out lat) &&
                             double.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out lng);

            // 1. ตรวจสอบและทำความสะอาดชื่อพื้นที่เดิม (existingArea)
            if (!string.IsNullOrEmpty(existingArea) && existingArea != "-" && existingArea != "ไม่ระบุตำแหน่ง" && existingArea != "ไม่สามารถระบุพิกัด")
            {
                string cleaned = CleanLocationString(existingArea, hasCoords ? lat : null, hasCoords ? lng : null);
                if (!string.IsNullOrEmpty(cleaned))
                {
                    return cleaned;
                }
            }

            // 2. หากมีพิกัด GPS ใช้ระบบระบุตำแหน่งตามพิกัดที่มีความแม่นยำสูง
            if (hasCoords)
            {
                return ApproximateThaiLocation(lat, lng);
            }

            if (!string.IsNullOrEmpty(existingArea))
            {
                return CleanLocationString(existingArea, null, null);
            }

            return "ไม่ระบุตำแหน่ง";
        }

        public static string CleanLocationString(string input, double? lat = null, double? lng = null)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string s = input.Trim();

            // ตัดส่วน [พิกัด] ออกหากมีติดมาข้างหน้า เช่น [13.9230, 100.3480]
            if (s.StartsWith("["))
            {
                var closeIdx = s.IndexOf(']');
                if (closeIdx >= 0 && closeIdx + 1 < s.Length)
                {
                    s = s.Substring(closeIdx + 1).Trim();
                }
            }

            // หากเป็นข้อความสถานที่ระดับละเอียด (มี ถนน, ซอย, ตำบล, ต., แขวง, หรือ อำเภอ) ให้คงรายละเอียดสถานที่และถนน/ตำบลไว้ครบถ้วน
            bool hasDetail = s.Contains("ถนน") || s.Contains("ถ.") || s.Contains("ซอย") || s.Contains("ซ.") ||
                             s.Contains("ตำบล") || s.Contains("ต.") || s.Contains("แขวง") || s.Contains("อำเภอ") || s.Contains("อ.") || s.Contains("เขต");

            if (!hasDetail)
            {
                // 1. กรุงเทพมหานคร โดยเฉพาะเขตบางพลัด (ที่ตั้งสำนักงานใหญ่) และเขตใกล้เคียง
                if (s.Contains("บางพลัด") || s.Contains("บางอ้อ"))
                {
                    return "เขตบางพลัด กรุงเทพมหานคร";
                }
                if (s.Contains("บางซื่อ"))
                {
                    return "เขตบางซื่อ กรุงเทพมหานคร";
                }
                if (s.Contains("ดุสิต"))
                {
                    return "เขตดุสิต กรุงเทพมหานคร";
                }
                if (s.Contains("จตุจักร"))
                {
                    return "เขตจตุจักร กรุงเทพมหานคร";
                }

                // 2. นนทบุรี ไม่บังคับทับซ้อน ให้ตรงตามความเป็นจริง
                if (s.Contains("บางบัวทอง") || s.Contains("พิมลราช") || s.Contains("โสนลอย") || s.Contains("บางคูรัด") || s.Contains("ละหาร") || s.Contains("ลำโพ"))
                {
                    return "อ.บางบัวทอง จ.นนทบุรี";
                }

                if (s.Contains("บางใหญ่") || s.Contains("เสาธงหิน") || s.Contains("บางแม่นาง") || s.Contains("บางเลน") || s.Contains("บ้านใหม่") || s.Contains("บางม่วง"))
                {
                    return "อ.บางใหญ่ จ.นนทบุรี";
                }

                if (s.Contains("เทศบาลนครนนทบุรี"))
                {
                    if (lat.HasValue && lng.HasValue && lat.Value >= 13.81 && lat.Value <= 13.92 && lng.Value >= 100.30 && lng.Value <= 100.455)
                    {
                        return "อ.บางใหญ่ จ.นนทบุรี";
                    }
                    return "อ.เมืองนนทบุรี จ.นนทบุรี";
                }
            }

            // แก้ไขคำนำหน้าซ้ำซ้อน เช่น จ.จังหวัด, อ.อำเภอ, ต.ตำบล
            s = s.Replace("จ.จังหวัด", "จ.")
                 .Replace("จ.จ.", "จ.")
                 .Replace("อ.อำเภอ", "อ.")
                 .Replace("อ.อ.", "อ.")
                 .Replace("ต.ตำบล", "ต.")
                 .Replace("ต.ต.", "ต.")
                 .Replace("แขวงแขวง", "แขวง")
                 .Replace("เขตเขต", "เขต")
                 .Replace("อ.เทศบาลนคร", "อ.")
                 .Replace("อ.เทศบาลเมือง", "อ.")
                 .Replace("อ.เทศบาลตำบล", "อ.")
                 .Replace("เทศบาลตำบลเสาธงหิน", "บางใหญ่");

            // แก้ไขกรณีขึ้นต้นด้วย จังหวัด แทน จ.
            if (s.StartsWith("จังหวัด") && !s.Contains("จ."))
            {
                s = "จ." + s.Substring(7).Trim();
            }

            return s.Trim();
        }

        private static readonly (string Name, double Lat, double Lng)[] ProvinceCoordinates = new[]
        {
            ("กรุงเทพมหานคร", 13.7563, 100.5018),
            ("นนทบุรี", 13.8621, 100.5144),
            ("ปทุมธานี", 14.0208, 100.5250),
            ("สมุทรปราการ", 13.5991, 100.5998),
            ("สมุทรสาคร", 13.5475, 100.2744),
            ("นครปฐม", 13.8196, 100.0443),
            ("สมุทรสงคราม", 13.4098, 99.9989),
            ("พระนครศรีอยุธยา", 14.3532, 100.5684),
            ("สระบุรี", 14.5289, 100.9108),
            ("ลพบุรี", 14.7995, 100.6534),
            ("สิงห์บุรี", 14.8911, 100.4048),
            ("ชัยนาท", 15.1852, 100.1251),
            ("อ่างทอง", 14.5896, 100.4550),
            ("สุพรรณบุรี", 14.4745, 100.1177),
            ("นครนายก", 14.2069, 101.2131),
            ("ปราจีนบุรี", 14.0509, 101.3732),
            ("สระแก้ว", 13.8140, 102.0728),
            ("ฉะเชิงเทรา", 13.6904, 101.0779),
            ("ชลบุรี", 13.3611, 100.9847),
            ("ระยอง", 12.6814, 101.2816),
            ("จันทบุรี", 12.6114, 102.1039),
            ("ตราด", 12.2428, 102.5175),
            ("กาญจนบุรี", 14.0228, 99.5328),
            ("ราชบุรี", 13.5359, 99.8164),
            ("เพชรบุรี", 13.1119, 99.9399),
            ("ประจวบคีรีขันธ์", 11.8124, 99.7972),
            ("เชียงใหม่", 18.7883, 98.9853),
            ("ลำพูน", 18.5745, 99.0087),
            ("ลำปาง", 18.2888, 99.4928),
            ("อุตรดิตถ์", 17.6201, 100.0993),
            ("แพร่", 18.1446, 100.1410),
            ("น่าน", 18.7831, 100.7782),
            ("พะเยา", 19.1664, 99.9019),
            ("เชียงราย", 19.9072, 99.8325),
            ("แม่ฮ่องสอน", 19.3021, 97.9654),
            ("นครสวรรค์", 15.6930, 100.1226),
            ("อุทัยธานี", 15.3835, 100.0245),
            ("กำแพงเพชร", 16.4828, 99.5227),
            ("ตาก", 16.8839, 99.1258),
            ("สุโขทัย", 17.0056, 99.8264),
            ("พิษณุโลก", 16.8211, 100.2659),
            ("พิจิตร", 16.4419, 100.3488),
            ("เพชรบูรณ์", 16.4190, 101.1567),
            ("นครราชสีมา", 14.9799, 102.0978),
            ("บุรีรัมย์", 14.9930, 103.1029),
            ("สุรินทร์", 14.8818, 103.4936),
            ("ศรีสะเกษ", 15.1186, 104.3220),
            ("อุบลราชธานี", 15.2448, 104.8473),
            ("ยโสธร", 15.7926, 104.1453),
            ("ชัยภูมิ", 15.8105, 102.0315),
            ("อำนาจเจริญ", 15.8657, 104.6258),
            ("บึงกาฬ", 18.3614, 103.6534),
            ("หนองบัวลำภู", 17.2044, 102.4410),
            ("ขอนแก่น", 16.4322, 102.8236),
            ("อุดรธานี", 17.4138, 102.7872),
            ("เลย", 17.4860, 101.7223),
            ("หนองคาย", 17.8783, 102.7420),
            ("มหาสารคาม", 16.1848, 103.3007),
            ("ร้อยเอ็ด", 16.0538, 103.6520),
            ("กาฬสินธุ์", 16.4328, 103.5068),
            ("สกลนคร", 17.1546, 104.1486),
            ("นครพนม", 17.3998, 104.7814),
            ("มุกดาหาร", 16.5434, 104.7235),
            ("นครศรีธรรมราช", 8.4304, 99.9631),
            ("กระบี่", 8.0863, 98.9063),
            ("พังงา", 8.4501, 98.5255),
            ("ภูเก็ต", 7.8804, 98.3923),
            ("สุราษฎร์ธานี", 9.1382, 99.3217),
            ("ระนอง", 9.9529, 98.6348),
            ("ชุมพร", 10.4930, 99.1800),
            ("สงขลา", 7.1898, 100.5954),
            ("สตูล", 6.6238, 100.0674),
            ("ตรัง", 7.5563, 99.6114),
            ("พัทลุง", 7.6166, 100.0740),
            ("ปัตตานี", 6.8675, 101.2501),
            ("ยะลา", 6.5411, 101.2804),
            ("นราธิวาส", 6.4255, 101.8253)
        };

        public static string ApproximateThaiLocation(double lat, double lng)
        {
            // เขตบางพลัด / บางซื่อ กรุงเทพมหานคร (Lat 13.78 - 13.825, Lng 100.48 - 100.535) สำนักงานใหญ่
            if (lat >= 13.78 && lat <= 13.825 && lng >= 100.48 && lng <= 100.535)
            {
                if (lng <= 100.512) return "เขตบางพลัด กรุงเทพมหานคร";
                return "เขตบางซื่อ กรุงเทพมหานคร";
            }

            // นนทบุรี (แยกบางใหญ่ และ บางบัวทอง ตามแนวพิกัดจริง)
            if (lat >= 13.825 && lat <= 14.10 && lng >= 100.18 && lng <= 100.60)
            {
                if (lat >= 13.95 && lng <= 100.35) return "อ.ไทรน้อย จ.นนทบุรี";
                if (lat >= 13.905 && lng <= 100.455) return "อ.บางบัวทอง จ.นนทบุรี";
                if (lat >= 13.89 && lng > 100.455) return "อ.ปากเกร็ด จ.นนทบุรี";
                if (lat < 13.84 && lng <= 100.51) return "อ.บางกรวย จ.นนทบุรี";
                if (lat >= 13.825 && lat < 13.905 && lng >= 100.30 && lng <= 100.455) return "อ.บางใหญ่ จ.นนทบุรี";
                return "อ.เมืองนนทบุรี จ.นนทบุรี";
            }

            // กรุงเทพมหานคร
            if (lat >= 13.60 && lat <= 13.95 && lng >= 100.40 && lng <= 100.85)
            {
                if (lat >= 13.78 && lng >= 100.55) return "เขตจตุจักร กรุงเทพมหานคร";
                if (lat >= 13.72 && lat < 13.78 && lng >= 100.50 && lng <= 100.60) return "เขตปทุมวัน กรุงเทพมหานคร";
                if (lat >= 13.70 && lng >= 100.60) return "เขตประเวศ กรุงเทพมหานคร";
                if (lng < 100.50) return "เขตธนบุรี กรุงเทพมหานคร";
                return "เขตพระนคร กรุงเทพมหานคร";
            }

            // ปทุมธานี
            if (lat >= 13.95 && lat <= 14.20 && lng >= 100.45 && lng <= 100.85)
            {
                if (lng >= 100.60) return "อ.คลองหลวง จ.ปทุมธานี";
                return "อ.เมืองปทุมธานี จ.ปทุมธานี";
            }

            // สมุทรปราการ
            if (lat >= 13.45 && lat < 13.65 && lng >= 100.50 && lng <= 100.90)
            {
                if (lng >= 100.70) return "อ.บางพลี จ.สมุทรปราการ";
                return "อ.เมืองสมุทรปราการ จ.สมุทรปราการ";
            }

            // สมุทรสาคร
            if (lat >= 13.45 && lat <= 13.65 && lng >= 100.15 && lng < 100.45)
            {
                if (lat >= 13.60) return "อ.กระทุ่มแบน จ.สมุทรสาคร";
                return "อ.เมืองสมุทรสาคร จ.สมุทรสาคร";
            }

            // นครปฐม
            if (lat >= 13.70 && lat <= 14.10 && lng >= 99.90 && lng < 100.35)
            {
                if (lng >= 100.25) return "อ.พุทธมณฑล จ.นครปฐม";
                return "อ.เมืองนครปฐม จ.นครปฐม";
            }

            // คำนวณหาระยะทางที่ใกล้ที่สุดจากพิกัดศูนย์กลางจังหวัดในประเทศไทย (Haversine formula)
            double minDistance = double.MaxValue;
            string closestProvince = "";

            foreach (var prov in ProvinceCoordinates)
            {
                double dLat = (prov.Lat - lat) * Math.PI / 180.0;
                double dLng = (prov.Lng - lng) * Math.PI / 180.0;
                double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                           Math.Cos(lat * Math.PI / 180.0) * Math.Cos(prov.Lat * Math.PI / 180.0) *
                           Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
                double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                double distKm = 6371.0 * c;

                if (distKm < minDistance)
                {
                    minDistance = distKm;
                    closestProvince = prov.Name;
                }
            }

            if (!string.IsNullOrEmpty(closestProvince) && minDistance <= 80.0)
            {
                if (closestProvince == "กรุงเทพมหานคร") return "กรุงเทพมหานคร";
                return "จ." + closestProvince;
            }

            return "ประเทศไทย";
        }
    }
}
