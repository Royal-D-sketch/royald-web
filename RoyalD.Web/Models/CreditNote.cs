using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RoyalD.Web.Models
{
    public class CreditNote
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// เลขที่ใบลดหนี้ เช่น CN26-10-0001
        /// </summary>
        [Required, MaxLength(50)]
        public string CreditNoteNo { get; set; } = string.Empty;

        public DateTime CreditNoteDate { get; set; } = DateTime.Today;

        [MaxLength(50)]
        public string CustomerCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string CustomerAddress { get; set; } = string.Empty;

        [MaxLength(50)]
        public string CustomerTaxId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string CustomerBranch { get; set; } = "สำนักงานใหญ่";

        /// <summary>
        /// อ้างอิงเลขที่ใบกำกับภาษี/บิลเดิม
        /// </summary>
        [MaxLength(100)]
        public string OriginalBillNo { get; set; } = string.Empty;

        public DateTime? OriginalBillDate { get; set; }

        /// <summary>
        /// มูลค่าตามใบกำกับภาษีเดิม
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalBillAmount { get; set; }

        /// <summary>
        /// มูลค่าที่ถูกต้อง (หลังหักยอดคืน)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CorrectAmount { get; set; }

        /// <summary>
        /// มูลค่าผลต่างที่ลดลง (ยอดก่อนภาษีของใบลดหนี้)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DifferenceAmount { get; set; }

        /// <summary>
        /// ผู้แทนขายเดิมตามบิล
        /// </summary>
        [MaxLength(200)]
        public string OriginalSalesRep { get; set; } = string.Empty;

        /// <summary>
        /// ผู้แทนขายปัจจุบันที่บัญชีเลือก (แสดงบนหัวเอกสาร)
        /// </summary>
        [MaxLength(200)]
        public string SalesRep { get; set; } = string.Empty;

        /// <summary>
        /// เป้าหมายการหักค่าคอมมิชชัน:
        /// "OriginalRep" = หักเซลส์เดิมตามบิล (ไม่หักเซลส์คนใหม่)
        /// "CompanyCentral" = เข้าส่วนกลางบริษัท (กรณีเซลส์เดิมลาออก)
        /// </summary>
        [MaxLength(50)]
        public string CommissionDeductTarget { get; set; } = "OriginalRep";

        /// <summary>
        /// สาเหตุการออกใบลดหนี้ (ตามหลักสรรพากร ม.86/10)
        /// </summary>
        [MaxLength(500)]
        public string Reason { get; set; } = "รับคืนสินค้าชำรุด/เสียหาย/หมดอายุ";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountBeforeVat { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        public bool IsPriceOverridden { get; set; }

        [MaxLength(1000)]
        public string PriceOverrideReason { get; set; } = string.Empty;

        /// <summary>
        /// ตัดหักยอดหนี้จากบิลเดิมทันทีหรือไม่
        /// </summary>
        public bool IsArOffset { get; set; } = true;

        [MaxLength(50)]
        public string Status { get; set; } = "Approved"; // Draft, Approved, Cancelled

        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string PaperSizePreference { get; set; } = "A4"; // A4, Continuous9x11

        [MaxLength(500)]
        public string Remark { get; set; } = string.Empty;

        [MaxLength(500)]
        public string AttachmentPath { get; set; } = string.Empty;

        // Navigation
        public ICollection<CreditNoteItem> Items { get; set; } = new List<CreditNoteItem>();
        public ICollection<CreditNoteWarehouseEntry> WarehouseEntries { get; set; } = new List<CreditNoteWarehouseEntry>();
    }

    public class CreditNoteItem
    {
        [Key]
        public int Id { get; set; }

        public int CreditNoteId { get; set; }

        [MaxLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [MaxLength(300)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Unit { get; set; } = "ชิ้น";

        /// <summary>
        /// จำนวนคืนรวมทุกคลัง
        /// </summary>
        [Column(TypeName = "decimal(18,3)")]
        public decimal TotalReturnQty { get; set; }

        /// <summary>
        /// ราคาเฉลี่ยสุทธิต่อหน่วยที่คำนวณจากบิลเดิม
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal OriginalNetUnitPrice { get; set; }

        /// <summary>
        /// ราคาต่อหน่วยที่ใช้คิดเงินจริง (อาจเป็นราคาเฉลี่ย หรือราคาที่คีย์ Override)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitPrice { get; set; }

        public bool IsPriceOverridden { get; set; }

        [MaxLength(500)]
        public string PriceOverrideReason { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountBeforeVat { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(50)]
        public string ReferenceBillNo { get; set; } = string.Empty;

        public bool BillNotFound { get; set; }

        /// <summary>
        /// สรุปการแยกคลัง เช่น คลัง 1: 5, คลัง 3: 2, คลัง 7: 1
        /// </summary>
        [MaxLength(500)]
        public string WarehouseBreakdown { get; set; } = string.Empty;

        // Navigation
        public CreditNote? CreditNote { get; set; }
    }

    public class CreditNoteWarehouseEntry
    {
        [Key]
        public int Id { get; set; }

        public int CreditNoteId { get; set; }

        [MaxLength(50)]
        public string ProductCode { get; set; } = string.Empty;

        [MaxLength(300)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string WarehouseNo { get; set; } = "1"; // 1, 3, 7

        public DateTime? ExpiryDate { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Qty { get; set; }

        [MaxLength(50)]
        public string LotNo { get; set; } = string.Empty;

        // Navigation
        public CreditNote? CreditNote { get; set; }
    }
}
