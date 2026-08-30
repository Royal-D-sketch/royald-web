using System;
using System.Collections.Generic;
using System.Linq;
using RoyalD.Web.Models;

namespace RoyalD.Web.Services
{
    public enum VatCategory
    {
        NoVat,      // NO VAT (บิลที่ไม่ได้ขึ้นต้นด้วย R หรือยอดเงินเป็น 0)
        VatZero,    // VAT 0% (บิลขึ้นต้นด้วย R แต่ยอด VAT เป็น 0 บาท / ส่งออก / ยกเว้นภาษี เช่น KSD)
        VatOut,     // VAT นอก (บิลขึ้นต้นด้วย R ราคาแยกภาษี 7% เช่น ซีพี ออลล์, ซีพี แอ็กซ์ตร้า, โมเดิร์นเทรด)
        VatIn       // VAT ใน (บิลขึ้นต้นด้วย R ราคารวมภาษี 7% แล้ว เช่น สยามชิน, ขายสด R146629, ร้านยาทั่วไป)
    }

    public class VatCalculationResult
    {
        public VatCategory Category { get; set; }
        public decimal GrossAmount { get; set; }     // รวมเป็นเงินก่อนหักส่วนลด
        public decimal DiscountAmount { get; set; }  // ส่วนลด
        public decimal NetAmount { get; set; }       // ยอดรวมหลังหักส่วนลด
        public decimal SubTotal { get; set; }        // รวมมูลค่าสินค้าก่อนภาษี
        public decimal VatAmount { get; set; }       // ภาษีมูลค่าเพิ่ม
        public decimal GrandTotal { get; set; }      // ยอดรวมสุทธิทั้งสิ้น
        public string Label { get; set; } = "";
        public string ShortLabel { get; set; } = "";
        public string BadgeClass { get; set; } = "";
        public string Explanation { get; set; } = "";
    }

    public static class VatHelper
    {
        public static VatCategory GetVatCategory(string billNo, string custCode, string custName, string province, decimal totalAmount, decimal itemSum, string salesRep = "")
        {
            custName ??= "";
            province ??= "";
            billNo ??= "";
            custCode ??= "";
            salesRep ??= "";

            bool startsWithR = billNo.Trim().StartsWith("R", StringComparison.OrdinalIgnoreCase);

            // 1. บิลที่ไม่ได้ขึ้นต้นด้วย R หรือ ไม่มียอดเงิน -> NO VAT
            if (!startsWithR || (totalAmount <= 0 && itemSum <= 0))
            {
                return VatCategory.NoVat;
            }

            // 2. บิลที่ขึ้นต้นด้วย R แต่ยอด VAT เป็น 0 (เช่น ส่งออก KSD, ต่างประเทศ, ยกเว้นภาษี) -> VAT 0%
            bool isZeroVat = custName.IndexOf("KSD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             custName.IndexOf("EXPORT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             custName.IndexOf("IMPORT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             custName.IndexOf("COSMETICS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             custName.IndexOf("SOLE CO", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             province.IndexOf("LAO", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             province.IndexOf("PDR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             province.IndexOf("P.D.R", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             province.IndexOf("CAMBODIA", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             province.IndexOf("ต่างประเทศ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             custCode == "001" || custCode == "006" || custCode == "022" ||
                             custCode.StartsWith("00") || (custCode.StartsWith("0") && custCode.Length <= 3);

            if (isZeroVat)
            {
                return VatCategory.VatZero;
            }

            // ลูกค้าสยามชิน, ขายสด, ออนไลน์ -> บิล VAT ใน เสมอ
            if (custCode == "850051" || custName.IndexOf("สยามชิน", StringComparison.OrdinalIgnoreCase) >= 0 ||
                custCode == "102448" || custName.IndexOf("ขายสด", StringComparison.OrdinalIgnoreCase) >= 0 ||
                custCode == "102863" || custName.IndexOf("Shope", StringComparison.OrdinalIgnoreCase) >= 0 ||
                custCode == "103079" || custName.IndexOf("TikTo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                custCode == "102862" || custName.IndexOf("LAZAD", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return VatCategory.VatIn;
            }

            // 3. บิลขึ้นต้นด้วย R ที่เป็น Modern Trade / Key Account (คิด VAT นอก 7% เพิ่มท้ายบิล)
            // 3.1 ตรวจสอบจากผู้แทนโมเดิร์นเทรด: ณัฐกุล, วันวิสา, ตันหยง, อิทธิพัทธ์, ธนดล
            var mtReps = new[] { "ณัฐกุล", "วันวิสา", "ตันหยง", "อิทธิพัทธ์", "ธนดล" };
            if (mtReps.Any(r => salesRep.IndexOf(r, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return VatCategory.VatOut;
            }

            // 3.2 ตรวจสอบจากรหัสลูกค้า Modern Trade / ห้าง / ซุปเปอร์มาร์เก็ต
            if (custCode.StartsWith("10501") || custCode.StartsWith("1050") || custCode.StartsWith("101901") ||
                custCode.StartsWith("102445") || custCode.StartsWith("103468") ||
                custCode.StartsWith("102003") || custCode.StartsWith("700084") ||
                custCode.StartsWith("110239") || custCode.StartsWith("1020453") || custCode.StartsWith("1020379") ||
                custCode.StartsWith("1020327") || custCode.StartsWith("102677") || custCode.StartsWith("101298") ||
                custCode.StartsWith("102719") || custCode.StartsWith("102265") || custCode.StartsWith("102410") ||
                custCode.StartsWith("103329") || custCode.StartsWith("102751") || custCode.StartsWith("102293") ||
                custCode.StartsWith("103085") || custCode.StartsWith("102276") || custCode.StartsWith("102275") ||
                custCode.StartsWith("102307") || custCode.StartsWith("102673") || custCode.StartsWith("200263") ||
                custCode.StartsWith("102662") || custCode.StartsWith("102674") || custCode.StartsWith("730168") ||
                custCode.StartsWith("1020428") || custCode.StartsWith("103456"))
            {
                return VatCategory.VatOut;
            }

            // 3.3 ตรวจสอบจากชื่อลูกค้า Modern Trade
            var mtKeywords = new[] {
                "ซีพี", "CP ALL", "CP AXTRA", "CP EXTRA", "แม็คโคร", "MAKRO", "โลตัส", "LOTUS", "บิ๊กซี", "BIG C",
                "ซี.เจ.", "CJ EXPRESS", "เซ็นทรัล", "CENTRAL", "TOPS", "ท็อปส์", "เดอะมอลล์", "THE MALL",
                "วัตสัน", "WATSON", "บู๊ทส์", "BOOTS", "ลอว์สัน", "LAWSON", "ฟู้ดแลนด์", "FOODLAND",
                "ปตท.", "PTT", "JIFFY", "จิฟฟี่", "ปิโตรเลียมไทย", "MAX MART", "อิออน", "MAXVALU",
                "สุวรรณชาด", "GOLDEN PLACE", "ซูรูฮะ", "TSURUHA", "ดีแคทลอน", "DECATHLON", "วิลล่า", "VILLA MARKET",
                "ฮาร์เบอร์แลนด์", "HARBORLAND"
            };

            foreach (var kw in mtKeywords)
            {
                if (custName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return VatCategory.VatOut;
                }
            }

            // 4. ลูกค้าทั่วไป / ร้านยา / คลินิก -> VAT ใน
            return VatCategory.VatIn;
        }

        public static VatCalculationResult Calculate(string billNo, string custCode, string custName, string province, decimal totalAmount, decimal itemSum, string salesRep = "", IEnumerable<SalesBillItem>? items = null)
        {
            var cat = GetVatCategory(billNo, custCode, custName, province, totalAmount, itemSum, salesRep);
            
            decimal grossAmount = 0m;
            decimal discountAmount = 0m;
            decimal netAmount = 0m;

            if (items != null && items.Any())
            {
                foreach (var it in items)
                {
                    decimal lineGross = it.Qty * it.Price;
                    decimal lineDiscount = 0m;
                    if (it.Discount > 0)
                    {
                        if (it.Discount <= 100m)
                        {
                            lineDiscount = Math.Round(lineGross * (it.Discount / 100m), 2);
                        }
                        else
                        {
                            lineDiscount = Math.Min(lineGross, it.Discount);
                        }
                    }
                    grossAmount += lineGross;
                    discountAmount += lineDiscount;
                }
                netAmount = grossAmount - discountAmount;
                if (netAmount <= 0) netAmount = totalAmount > 0 ? totalAmount : itemSum;
            }
            else
            {
                grossAmount = totalAmount > 0 ? totalAmount : itemSum;
                discountAmount = 0m;
                netAmount = grossAmount;
            }

            decimal subTotal = 0m;
            decimal vatAmount = 0m;
            decimal grandTotal = 0m;

            switch (cat)
            {
                case VatCategory.VatOut:
                    // VAT นอก: netAmount คือราคาก่อนภาษี (Sub Total) แล้วบวก VAT 7% เพิ่มท้ายบิล
                    subTotal = netAmount;
                    vatAmount = Math.Round(netAmount * 0.07m, 2);
                    grandTotal = subTotal + vatAmount;
                    break;

                case VatCategory.VatIn:
                    // VAT ใน: netAmount คือยอดรวมสุทธิทั้งสิ้น (Grand Total) ถอด VAT 7/107
                    grandTotal = netAmount;
                    subTotal = Math.Round(netAmount * 100m / 107m, 2);
                    vatAmount = grandTotal - subTotal;
                    break;

                case VatCategory.VatZero:
                    // VAT 0%: ส่งออก / อัตราศูนย์
                    grandTotal = netAmount;
                    subTotal = netAmount;
                    vatAmount = 0m;
                    break;

                case VatCategory.NoVat:
                default:
                    // NO VAT: ไม่มีภาษี
                    grandTotal = netAmount;
                    subTotal = netAmount;
                    vatAmount = 0m;
                    break;
            }

            return new VatCalculationResult
            {
                Category = cat,
                GrossAmount = grossAmount,
                DiscountAmount = discountAmount,
                NetAmount = netAmount,
                SubTotal = subTotal,
                VatAmount = vatAmount,
                GrandTotal = grandTotal,
                Label = GetLabel(cat),
                ShortLabel = GetShortLabel(cat),
                BadgeClass = GetBadgeClass(cat),
                Explanation = GetExplanation(cat, custName, totalAmount, billNo)
            };
        }

        public static string GetLabel(VatCategory cat) => cat switch
        {
            VatCategory.NoVat => "NO VAT (ไม่มีภาษีมูลค่าเพิ่ม)",
            VatCategory.VatZero => "VAT 0% (ภาษีมูลค่าเพิ่ม 0%)",
            VatCategory.VatOut => "VAT นอก (ราคาแยกภาษี 7%)",
            _ => "VAT ใน (ราคารวมภาษี 7% แล้ว)"
        };

        public static string GetShortLabel(VatCategory cat) => cat switch
        {
            VatCategory.NoVat => "NO VAT",
            VatCategory.VatZero => "VAT 0%",
            VatCategory.VatOut => "VAT นอก",
            _ => "VAT ใน"
        };

        public static string GetBadgeClass(VatCategory cat) => cat switch
        {
            VatCategory.NoVat => "bg-secondary text-white",
            VatCategory.VatZero => "bg-info text-dark border border-info",
            VatCategory.VatOut => "bg-warning text-dark border border-dark",
            _ => "bg-success text-white"
        };

        public static string GetExplanation(VatCategory cat, string custName, decimal totalAmount, string billNo) => cat switch
        {
            VatCategory.NoVat => (totalAmount <= 0 ? "บิลนี้ไม่มียอดเงิน จึงไม่มีการคิดภาษีมูลค่าเพิ่ม (NO VAT)" : "บิลนี้ไม่ได้ขึ้นต้นด้วย R จึงเป็นบิลไม่มีภาษีมูลค่าเพิ่ม (NO VAT)"),
            VatCategory.VatZero => $"บิลขึ้นต้นด้วย R แต่ยอดภาษีมูลค่าเพิ่มเป็น 0 บาท ({custName}) จึงเป็นอัตราภาษี 0% (VAT 0%)",
            VatCategory.VatOut => "บิลนี้เป็นราคาแยกภาษี (VAT นอก) ราคาสินค้าต่อหน่วยยังไม่รวมภาษี มีการบวก VAT 7% เพิ่มท้ายบิล",
            _ => "บิลนี้เป็นราคารวมภาษีแล้ว (VAT ใน) ราคาสินค้าต่อหน่วยและยอดรวม ได้รวมภาษีมูลค่าเพิ่ม 7% เรียบร้อยแล้ว"
        };
    }
}
