"use client";

import { useEffect, useState } from "react";
import { useSession, signOut } from "next-auth/react";
import dayjs from "dayjs";
import PaymentModal from "../../components/PaymentModal";
import toast, { Toaster } from "react-hot-toast";

type Bill = {
  BillNo: string;
  Date: string;
  CustomerName: string;
  TotalAmount: number;
  RemainingAmount: number;
  Status: string;
  SalesRep: string;
  Province: string;
};

export default function SalesDashboard() {
  const { data: session } = useSession();
  const [bills, setBills] = useState<Bill[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedBill, setSelectedBill] = useState<string | null>(null);
  const [showModal, setShowModal] = useState(false);

  // Upload modal state
  const [showUploadModal, setShowUploadModal] = useState(false);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);

  // Filters
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("all");

  const fetchBills = async () => {
    try {
      const res = await fetch("/api/bills");
      if (!res.ok) throw new Error("Failed to fetch bills");
      const data = await res.json();
      setBills(data);
    } catch (err) {
      toast.error("ไม่สามารถดึงข้อมูลบิลได้");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchBills();
  }, []);

  const openModal = (billNo: string) => {
    setSelectedBill(billNo);
    setShowModal(true);
  };

  const closeModal = () => {
    setShowModal(false);
    setSelectedBill(null);
  };

  const handleSuccess = () => {
    fetchBills();
    closeModal();
  };

  const handleUploadSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!uploadFile) {
      toast.error("กรุณาเลือกไฟล์เพื่ออัปโหลด");
      return;
    }

    setUploading(true);
    const form = new FormData();
    form.append("file", uploadFile);

    try {
      const res = await fetch("/api/upload-cd", {
        method: "POST",
        body: form,
      });
      const data = await res.json();
      if (res.ok) {
        toast.success(data.message || "อัปโหลดและนำเข้าบิลสำเร็จ");
        setShowUploadModal(false);
        setUploadFile(null);
        fetchBills();
      } else {
        toast.error(data.error || "เกิดข้อผิดพลาดในการอัปโหลด");
      }
    } catch (err) {
      toast.error("ไม่สามารถเชื่อมต่อเซิร์ฟเวอร์อัปโหลดได้");
    } finally {
      setUploading(false);
    }
  };

  const userObj = session?.user as any;

  // Filter bills
  const filteredBills = bills.filter((b) => {
    const matchSearch =
      b.BillNo.toLowerCase().includes(search.toLowerCase()) ||
      b.CustomerName.toLowerCase().includes(search.toLowerCase()) ||
      (b.SalesRep && b.SalesRep.toLowerCase().includes(search.toLowerCase()));

    const matchStatus =
      statusFilter === "all" ||
      (statusFilter === "outstanding" && b.Status !== "Paid") ||
      (statusFilter === "paid" && b.Status === "Paid");

    return matchSearch && matchStatus;
  });

  const totalAmountSum = filteredBills.reduce((acc, cur) => acc + Number(cur.TotalAmount || 0), 0);
  const remainingSum = filteredBills.reduce((acc, cur) => acc + Number(cur.RemainingAmount || 0), 0);

  return (
    <div className="container py-4">
      <Toaster position="top-right" />

      {/* Header Bar */}
      <div className="d-flex flex-wrap justify-content-between align-items-center mb-4 pb-3 border-bottom gap-2">
        <div>
          <h2 className="fw-bold mb-1" style={{ color: "#1e3a8a" }}>
            <i className="bi bi-receipt-cutoff me-2"></i>ระบบติดตามบิลขายและการ์ดลูกหนี้
          </h2>
          <div className="text-muted small">
            ผู้ใช้งาน: <strong className="text-dark">{userObj?.name || userObj?.username || "Admin"}</strong> | 
            ตำแหน่ง: <span className="badge bg-primary ms-1">{userObj?.job_position || "Admin"}</span>
          </div>
        </div>
        <div className="d-flex gap-2">
          <button
            onClick={() => setShowUploadModal(true)}
            className="btn btn-primary btn-sm fw-semibold shadow-sm px-3"
          >
            <i className="bi bi-cloud-arrow-up-fill me-1"></i> อัปโหลดไฟล์บิล (CD / Excel)
          </button>
          <button
            onClick={() => signOut({ callbackUrl: "/login" })}
            className="btn btn-outline-danger btn-sm fw-semibold shadow-sm"
          >
            <i className="bi bi-box-arrow-right me-1"></i> ออกจากระบบ
          </button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="row g-3 mb-4">
        <div className="col-md-4">
          <div className="card shadow-sm border-0 bg-primary text-white p-3">
            <div className="small text-white-50">จำนวนบิลทั้งหมด</div>
            <h3 className="fw-bold mb-0">{filteredBills.length.toLocaleString()} บิล</h3>
          </div>
        </div>
        <div className="col-md-4">
          <div className="card shadow-sm border-0 bg-info text-dark p-3">
            <div className="small text-muted">ยอดขายรวม</div>
            <h3 className="fw-bold mb-0">฿{totalAmountSum.toLocaleString("th-TH", { minimumFractionDigits: 2 })}</h3>
          </div>
        </div>
        <div className="col-md-4">
          <div className="card shadow-sm border-0 bg-warning text-dark p-3">
            <div className="small text-muted">ยอดคงเหลือค้างชำระ</div>
            <h3 className="fw-bold mb-0">฿{remainingSum.toLocaleString("th-TH", { minimumFractionDigits: 2 })}</h3>
          </div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="card shadow-sm border-0 mb-4">
        <div className="card-body p-3">
          <div className="row g-2 align-items-center">
            <div className="col-md-6">
              <div className="input-group">
                <span className="input-group-text bg-light border-end-0">
                  <i className="bi bi-search text-muted"></i>
                </span>
                <input
                  type="text"
                  className="form-control border-start-0"
                  placeholder="ค้นหาเลขที่บิล, ชื่อลูกค้า, หรือผู้แทนขาย..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
            </div>
            <div className="col-md-6 text-md-end">
              <div className="btn-group">
                <button
                  type="button"
                  className={`btn btn-sm ${statusFilter === "all" ? "btn-dark" : "btn-outline-secondary"}`}
                  onClick={() => setStatusFilter("all")}
                >
                  ทั้งหมด
                </button>
                <button
                  type="button"
                  className={`btn btn-sm ${statusFilter === "outstanding" ? "btn-danger" : "btn-outline-danger"}`}
                  onClick={() => setStatusFilter("outstanding")}
                >
                  ค้างชำระ
                </button>
                <button
                  type="button"
                  className={`btn btn-sm ${statusFilter === "paid" ? "btn-success" : "btn-outline-success"}`}
                  onClick={() => setStatusFilter("paid")}
                >
                  ชำระครบแล้ว
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Main Table */}
      {loading ? (
        <div className="text-center py-5">
          <div className="spinner-border text-primary" role="status"></div>
          <p className="mt-2 text-muted">กำลังโหลดข้อมูลบิล...</p>
        </div>
      ) : (
        <div className="card shadow-sm border-0">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light">
                <tr>
                  <th className="py-3">เลขที่บิล</th>
                  <th className="py-3">วันที่ (DD/MM/YYYY)</th>
                  <th className="py-3">ชื่อลูกค้า</th>
                  <th className="py-3">ผู้แทนขาย</th>
                  <th className="py-3 text-end">ยอดรวม (บาท)</th>
                  <th className="py-3 text-end">คงเหลือ (บาท)</th>
                  <th className="py-3 text-center">สถานะ</th>
                  <th className="py-3 text-center">จัดการ</th>
                </tr>
              </thead>
              <tbody>
                {filteredBills.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="text-center py-5 text-muted">
                      <i className="bi bi-inbox fs-1 d-block mb-2 text-secondary"></i>
                      ไม่พบข้อมูลบิลขายในระบบ<br />
                      <button
                        onClick={() => setShowUploadModal(true)}
                        className="btn btn-outline-primary btn-sm mt-3"
                      >
                        <i className="bi bi-upload me-1"></i> คลิกที่นี่เพื่ออัปโหลดไฟล์บิลขาย
                      </button>
                    </td>
                  </tr>
                ) : (
                  filteredBills.map((b) => (
                    <tr key={b.BillNo}>
                      <td className="fw-semibold text-primary">{b.BillNo}</td>
                      <td>{b.Date ? dayjs(b.Date).format("DD/MM/YYYY") : "-"}</td>
                      <td>{b.CustomerName}</td>
                      <td><span className="badge bg-light text-dark border">{b.SalesRep || "-"}</span></td>
                      <td className="text-end">
                        {Number(b.TotalAmount || 0).toLocaleString("th-TH", { minimumFractionDigits: 2 })}
                      </td>
                      <td className="text-end fw-bold text-danger">
                        {Number(b.RemainingAmount || b.TotalAmount || 0).toLocaleString("th-TH", { minimumFractionDigits: 2 })}
                      </td>
                      <td className="text-center">
                        {b.Status === "Paid" ? (
                          <span className="badge bg-success">ชำระแล้ว</span>
                        ) : (
                          <span className="badge bg-warning text-dark">ค้างชำระ</span>
                        )}
                      </td>
                      <td className="text-center">
                        <button
                          className="btn btn-sm btn-success fw-semibold px-3 shadow-sm"
                          onClick={() => openModal(b.BillNo)}
                        >
                          <i className="bi bi-cash-stack me-1"></i> รับชำระเงิน
                        </button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Payment Modal */}
      {showModal && selectedBill && (
        <PaymentModal
          billNo={selectedBill}
          onClose={closeModal}
          onSuccess={handleSuccess}
        />
      )}

      {/* Upload Modal */}
      {showUploadModal && (
        <div className="modal fade show" style={{ display: "block", backgroundColor: "rgba(0,0,0,0.5)" }} tabIndex={-1}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content shadow-lg border-0">
              <div className="modal-header bg-primary text-white">
                <h5 className="modal-title fw-bold">
                  <i className="bi bi-cloud-arrow-up-fill me-2"></i> อัปโหลดไฟล์บิลขาย (CD Organizer / CSV)
                </h5>
                <button type="button" className="btn-close btn-close-white" onClick={() => setShowUploadModal(false)}></button>
              </div>
              <form onSubmit={handleUploadSubmit}>
                <div className="modal-body p-4">
                  <div className="mb-3">
                    <label className="form-label fw-bold text-muted small">เลือกไฟล์บิลขาย (.csv / .txt)</label>
                    <input
                      type="file"
                      className="form-control form-control-lg"
                      accept=".csv,.txt"
                      required
                      onChange={(e) => setUploadFile(e.target.files?.[0] || null)}
                    />
                    <div className="form-text small mt-2">
                      รองรับไฟล์ส่งออกจากโปรแกรม CD Organizer โดยระบบจะล็อกรูปแบบวันที่ DD/MM/YYYY อัตโนมัติ
                    </div>
                  </div>
                </div>
                <div className="modal-footer bg-light">
                  <button type="button" className="btn btn-secondary" onClick={() => setShowUploadModal(false)}>
                    ยกเลิก
                  </button>
                  <button type="submit" disabled={uploading} className="btn btn-primary fw-bold px-4">
                    {uploading ? "กำลังนำเข้าข้อมูล..." : "เริ่มอัปโหลด"}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
