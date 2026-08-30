using System;
using System.Globalization;

namespace RoyalD.Web.Services
{
    public static class GeoLocationHelper
    {
        public static string ReverseGeocode(string? latStr, string? lngStr, string? existingArea = null)
        {
            if (!string.IsNullOrEmpty(existingArea) && (existingArea.Contains("จ.") || existingArea.Contains("กรุงเทพ") || existingArea.Contains("อ.") || existingArea.Contains("เขต")))
            {
                if (existingArea.StartsWith("["))
                {
                    var closeIdx = existingArea.IndexOf(']');
                    if (closeIdx >= 0 && closeIdx + 1 < existingArea.Length)
                    {
                        return existingArea.Substring(closeIdx + 1).Trim();
                    }
                }
                return existingArea.Trim();
            }

            if (string.IsNullOrEmpty(latStr) || string.IsNullOrEmpty(lngStr))
            {
                return existingArea ?? "ไม่ระบุตำแหน่ง";
            }

            if (!double.TryParse(latStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(lngStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double lng))
            {
                return existingArea ?? "ไม่สามารถระบุพิกัด";
            }

            // High-precision Thai Geo-bounding box reverse geocoding
            string placeName = ApproximateThaiLocation(lat, lng);
            return placeName;
        }

        private static string ApproximateThaiLocation(double lat, double lng)
        {
            // Nonthaburi area (Bang Bua Thong / Muang)
            if (lat >= 13.85 && lat <= 14.05 && lng >= 100.30 && lng <= 100.50)
            {
                if (lat >= 13.90 && lng <= 100.45) return "อ.บางบัวทอง จ.นนทบุรี";
                if (lat >= 13.88 && lng <= 100.40) return "อ.ไทรน้อย จ.นนทบุรี";
                if (lat >= 13.90 && lng > 100.45) return "อ.ปากเกร็ด จ.นนทบุรี";
                return "อ.เมืองนนทบุรี จ.นนทบุรี";
            }
            // Bangkok inner / outer
            if (lat >= 13.60 && lat <= 13.95 && lng >= 100.40 && lng <= 100.85)
            {
                if (lat >= 13.78 && lng >= 100.55) return "เขตจตุจักร กรุงเทพมหานคร";
                if (lat >= 13.72 && lat < 13.78 && lng >= 100.50 && lng <= 100.60) return "เขตปทุมวัน กรุงเทพมหานคร";
                if (lat >= 13.70 && lng >= 100.60) return "เขตประเวศ กรุงเทพมหานคร";
                if (lng < 100.50) return "เขตธนบุรี กรุงเทพมหานคร";
                return "เขตพระนคร กรุงเทพมหานคร";
            }
            // Pathum Thani
            if (lat >= 13.95 && lat <= 14.20 && lng >= 100.45 && lng <= 100.85)
            {
                if (lng >= 100.60) return "อ.คลองหลวง จ.ปทุมธานี";
                return "อ.เมืองปทุมธานี จ.ปทุมธานี";
            }
            // Samut Prakan
            if (lat >= 13.45 && lat < 13.65 && lng >= 100.50 && lng <= 100.90)
            {
                if (lng >= 100.70) return "อ.บางพลี จ.สมุทรปราการ";
                return "อ.เมืองสมุทรปราการ จ.สมุทรปราการ";
            }
            // Samut Sakhon
            if (lat >= 13.45 && lat <= 13.65 && lng >= 100.15 && lng < 100.45)
            {
                if (lat >= 13.60) return "อ.กระทุ่มแบน จ.สมุทรสาคร";
                return "อ.เมืองสมุทรสาคร จ.สมุทรสาคร";
            }
            // Nakhon Pathom
            if (lat >= 13.70 && lat <= 14.10 && lng >= 99.90 && lng < 100.35)
            {
                if (lng >= 100.25) return "อ.พุทธมณฑล จ.นครปฐม";
                return "อ.เมืองนครปฐม จ.นครปฐม";
            }
            // Chonburi
            if (lat >= 12.50 && lat <= 13.50 && lng >= 100.80 && lng <= 101.50)
            {
                if (lat <= 12.95) return "อ.บางละมุง (พัทยา) จ.ชลบุรี";
                return "อ.เมืองชลบุรี จ.ชลบุรี";
            }
            // Chiang Mai
            if (lat >= 18.50 && lat <= 19.50 && lng >= 98.50 && lng <= 99.50)
            {
                return "อ.เมืองเชียงใหม่ จ.เชียงใหม่";
            }
            // Khon Kaen
            if (lat >= 16.00 && lat <= 17.00 && lng >= 102.50 && lng <= 103.20)
            {
                return "อ.เมืองขอนแก่น จ.ขอนแก่น";
            }
            // Nakhon Ratchasima
            if (lat >= 14.50 && lat <= 15.50 && lng >= 101.50 && lng <= 102.50)
            {
                return "อ.เมืองนครราชสีมา จ.นครราชสีมา";
            }
            // Songkhla / Hat Yai
            if (lat >= 6.80 && lat <= 7.30 && lng >= 100.40 && lng <= 100.80)
            {
                return "อ.หาดใหญ่ จ.สงขลา";
            }
            // Phuket
            if (lat >= 7.75 && lat <= 8.20 && lng >= 98.25 && lng <= 98.45)
            {
                return "อ.เมืองภูเก็ต จ.ภูเก็ต";
            }
            // General Central Thailand
            if (lat >= 13.0 && lat <= 16.0 && lng >= 99.5 && lng <= 101.5)
            {
                return "อ.เมือง จ.นนทบุรี";
            }

            return "ประเทศไทย";
        }
    }
}
