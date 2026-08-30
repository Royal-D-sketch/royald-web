"use client";

import { useState } from "react";
import toast from "react-hot-toast";

interface PaymentModalProps {
  billNo: string;
  onClose: () => void;
  onSuccess?: () => void;
}

export default function PaymentModal({ billNo, onClose, onSuccess }: PaymentModalProps) {
  const [method, setMethod] = useState<"Transfer" | "Check">("Transfer");
  const [amount, setAmount] = useState("");
  const [payDate, setPayDate] = useState("");
  const [note, setNote] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [checkDate, setCheckDate] = useState("");
  const [bank, setBank] = useState("");
  const [checkNo, setCheckNo] = useState("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const form = new FormData();
    form.append("billNo", billNo);
    form.append("method", method);
    form.append("amount", amount);
    form.append("payDate", payDate);
    if (note) form.append("note", note);
    if (method === "Transfer" && file) {
      form.append("file", file);
    }
    if (method === "Check") {
      form.append("checkDate", checkDate);
      form.append("bank", bank);
      form.append("checkNo", checkNo);
    }
    try {
      const res = await fetch("/api/payment", {
        method: "POST",
        body: form,
      });
      if (!res.ok) throw new Error("Network error");
      toast.success("บันทึกการรับชำระเงินสำเร็จ");
      onClose();
      onSuccess?.();
    } catch (err) {
      toast.error("เกิดข้อผิดพลาดในการบันทึก");
    }
  };

  return (
    <div className="modal fade show" style={{ display: "block" }} tabIndex={-1}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">บันทึกการชำระเงิน (Bill {billNo})</h5>
            <button type="button" className="btn-close" onClick={onClose} />
          </div>
          <form onSubmit={handleSubmit}>
            <div className="modal-body">
              <div className="mb-3">
                <label className="form-label fw-bold">วิธีการชำระเงิน</label>
                <select
                  className="form-select"
                  value={method}
                  onChange={(e) => setMethod(e.target.value as any)}
                >
                  <option value="Transfer">โอนเงิน</option>
                  <option value="Check">เช็ค</option>
                </select>
              </div>
              <div className="mb-3">
                <label className="form-label fw-bold">จำนวนเงิน</label>
                <input
                  type="number"
                  className="form-control"
                  required
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                />
              </div>
              <div className="mb-3">
                <label className="form-label fw-bold">วันที่รับเงิน</label>
                <input
                  type="date"
                  className="form-control"
                  required
                  value={payDate}
                  onChange={(e) => setPayDate(e.target.value)}
                />
              </div>
              {method === "Transfer" && (
                <div className="mb-3">
                  <label className="form-label fw-bold">สลิปโอนเงิน (ไฟล์)</label>
                  <input
                    type="file"
                    className="form-control"
                    accept="image/*,.pdf"
                    onChange={(e) => setFile(e.target.files?.[0] || null)}
                  />
                </div>
              )}
              {method === "Check" && (
                <>
                  <div className="mb-3">
                    <label className="form-label fw-bold">วันที่ในหน้าเช็ค</label>
                    <input
                      type="date"
                      className="form-control"
                      required
                      value={checkDate}
                      onChange={(e) => setCheckDate(e.target.value)}
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-bold">ธนาคาร</label>
                    <input
                      type="text"
                      className="form-control"
                      required
                      value={bank}
                      onChange={(e) => setBank(e.target.value)}
                    />
                  </div>
                  <div className="mb-3">
                    <label className="form-label fw-bold">เลขที่เช็ค</label>
                    <input
                      type="text"
                      className="form-control"
                      required
                      value={checkNo}
                      onChange={(e) => setCheckNo(e.target.value)}
                    />
                  </div>
                </>
              )}
              <div className="mb-3">
                <label className="form-label fw-bold">หมายเหตุ (ถ้ามี)</label>
                <textarea
                  className="form-control"
                  rows={2}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-secondary" onClick={onClose}>
                ยกเลิก
              </button>
              <button type="submit" className="btn btn-primary">
                บันทึก
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
