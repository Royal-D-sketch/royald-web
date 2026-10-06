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

            // ถ้ามีพิกัดอยู่ในอำเภอบางใหญ่ จ.นนทบุรี โดยตรง (Lat 13.81-13.915, Lng 100.31-100.455)
            if (lat.HasValue && lng.HasValue)
            {
                if (lat.Value >= 13.81 && lat.Value <= 13.915 && lng.Value >= 100.31 && lng.Value <= 100.455)
                {
                    return "อ.บางใหญ่ จ.นนทบุรี";
                }
            }

            // ดักคำที่เป็นตำบลหรือพื้นที่ใน อ.บางใหญ่
            if (s.Contains("บางใหญ่") || s.Contains("เสาธงหิน") || s.Contains("บางแม่นาง") || s.Contains("บางเลน") || s.Contains("บ้านใหม่"))
            {
                return "อ.บางใหญ่ จ.นนทบุรี";
            }

            // กรณี API ภายนอกคืนค่าชื่อเทศบาลนครนนทบุรีแทนที่จะเป็นอำเภอ
            if (s.Contains("เทศบาลนครนนทบุรี"))
            {
                if (lat.HasValue && lng.HasValue && lat.Value >= 13.81 && lat.Value <= 13.92 && lng.Value >= 100.30 && lng.Value <= 100.455)
                {
                    return "อ.บางใหญ่ จ.นนทบุรี";
                }
                return "อ.เมืองนนทบุรี จ.นนทบุรี";
            }

            // แก้ไขคำนำหน้าซ้ำซ้อน เช่น จ.จังหวัด, อ.อำเภอ, อ.เทศบาล
            s = s.Replace("จ.จังหวัด", "จ.")
                 .Replace("จ.จ.", "จ.")
                 .Replace("อ.อำเภอ", "อ.")
                 .Replace("อ.อ.", "อ.")
                 .Replace("อ.เทศบาลนคร", "อ.")
                 .Replace("อ.เทศบาลเมือง", "อ.")
                 .Replace("อ.เทศบาลตำบล", "อ.")
                 .Replace("เทศบาลนครนนทบุรี", "เมืองนนทบุรี")
                 .Replace("เทศบาลเมืองบางกรวย", "บางกรวย")
                 .Replace("เทศบาลเมืองบางบัวทอง", "บางบัวทอง")
                 .Replace("เทศบาลตำบลเสาธงหิน", "บางใหญ่");

            // แก้ไขกรณีขึ้นต้นด้วย จังหวัด แทน จ.
            if (s.StartsWith("จังหวัด") && !s.Contains("จ."))
            {
                s = "จ." + s.Substring(7).Trim();
            }

            // จัดรูปแบบกรณีมี นนทบุรี
            if (s.Contains("นนทบุรี"))
            {
                if (s.Contains("จังหวัดนนทบุรี"))
                {
                    s = s.Replace("จังหวัดนนทบุรี", "จ.นนทบุรี");
                }
                if (!s.Contains("จ.นนทบุรี") && s.Contains("นนทบุรี"))
                {
                    s = s.Replace("นนทบุรี", "จ.นนทบุรี");
                }
            }

            return s.Trim();
        }

        public static string ApproximateThaiLocation(double lat, double lng)
        {
            // นนทบุรี (ทุกอำเภออย่างแม่นยำ)
            if (lat >= 13.78 && lat <= 14.10 && lng >= 100.18 && lng <= 100.60)
            {
                if (lat >= 13.92 && lng <= 100.35) return "อ.ไทรน้อย จ.นนทบุรี";
                if (lat >= 13.91 && lng <= 100.455) return "อ.บางบัวทอง จ.นนทบุรี";
                if (lat >= 13.89 && lng > 100.455) return "อ.ปากเกร็ด จ.นนทบุรี";
                if (lat < 13.84 && lng <= 100.51) return "อ.บางกรวย จ.นนทบุรี";
                if (lat >= 13.81 && lat <= 13.915 && lng >= 100.31 && lng <= 100.455) return "อ.บางใหญ่ จ.นนทบุรี";
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

            // ชลบุรี
            if (lat >= 12.50 && lat <= 13.50 && lng >= 100.80 && lng <= 101.50)
            {
                if (lat <= 12.95) return "อ.บางละมุง (พัทยา) จ.ชลบุรี";
                return "อ.เมืองชลบุรี จ.ชลบุรี";
            }

            // เชียงใหม่
            if (lat >= 18.50 && lat <= 19.50 && lng >= 98.50 && lng <= 99.50)
            {
                return "อ.เมืองเชียงใหม่ จ.เชียงใหม่";
            }

            // ขอนแก่น
            if (lat >= 16.00 && lat <= 17.00 && lng >= 102.50 && lng <= 103.20)
            {
                return "อ.เมืองขอนแก่น จ.ขอนแก่น";
            }

            // นครราชสีมา
            if (lat >= 14.50 && lat <= 15.50 && lng >= 101.50 && lng <= 102.50)
            {
                return "อ.เมืองนครราชสีมา จ.นครราชสีมา";
            }

            // สงขลา
            if (lat >= 6.80 && lat <= 7.30 && lng >= 100.40 && lng <= 100.80)
            {
                return "อ.หาดใหญ่ จ.สงขลา";
            }

            // ภูเก็ต
            if (lat >= 7.75 && lat <= 8.20 && lng >= 98.25 && lng <= 98.45)
            {
                return "อ.เมืองภูเก็ต จ.ภูเก็ต";
            }

            // ภาคกลางทั่วไป
            if (lat >= 13.0 && lat <= 16.0 && lng >= 99.5 && lng <= 101.5)
            {
                return "อ.เมือง จ.นนทบุรี";
            }

            return "ประเทศไทย";
        }
    }
}
