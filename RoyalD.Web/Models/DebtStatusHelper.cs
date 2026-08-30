using RoyalD.Web.Models;

namespace RoyalD.Web.Models
{
    public static class DebtStatusHelper
    {
        public static string ToThaiString(this DebtStatus status)
        {
            return status switch
            {
                DebtStatus.Outstanding => "ค้างชำระ (บิลปกติ)",
                DebtStatus.Installment => "ผ่อนชำระ",
                DebtStatus.Postponed => "เลื่อนนัดชำระ",
                DebtStatus.BadDebt => "หนี้สูญ",
                DebtStatus.Consignment => "สินค้าฝากขาย",
                DebtStatus.ReturnIssued => "รับคืนสินค้า (ออกใบลดหนี้แล้ว)",
                DebtStatus.ReturnPending => "รับคืนสินค้า (รอออกใบลดหนี้)",
                DebtStatus.ChangeProduct => "เปลี่ยนสินค้า",
                DebtStatus.Delivering => "บิลอยู่จัดส่ง",
                DebtStatus.WaitingGoods => "รอสินค้า",
                DebtStatus.Cancelled => "บิลยกเลิก",
                DebtStatus.PaidCash => "ชำระเงินสด",
                DebtStatus.PaidTransfer => "ชำระเงินโอน",
                DebtStatus.PaidCheck => "ชำระเช็ค",
                DebtStatus.CheckReturned => "เช็คคืน",
                _ => status.ToString()
            };
        }
    }
}
