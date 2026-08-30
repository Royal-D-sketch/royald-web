"use client";

import { useState } from "react";
import { signIn } from "next-auth/react";
import { useRouter, useSearchParams } from "next/navigation";
import toast, { Toaster } from "react-hot-toast";

export default function LoginPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const callbackUrl = searchParams.get("callbackUrl") || "/sales-dashboard";

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username || !password) {
      toast.error("กรุณากรอกชื่อผู้ใช้และรหัสผ่าน");
      return;
    }

    setLoading(true);
    try {
      const res = await signIn("credentials", {
        redirect: false,
        username,
        password,
        callbackUrl,
      });

      if (res?.error) {
        toast.error(res.error || "เข้าสู่ระบบไม่สำเร็จ");
      } else if (res?.ok) {
        toast.success("เข้าสู่ระบบสำเร็จ กำลังพาไปหน้าหลัก...");
        router.push(callbackUrl);
        router.refresh();
      }
    } catch (err) {
      toast.error("เกิดข้อผิดพลาดในการเชื่อมต่อ");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0f172a 100%)",
        padding: "20px",
        position: "relative",
        overflow: "hidden",
      }}
    >
      <Toaster position="top-center" />

      {/* Background glowing circle decoration */}
      <div
        style={{
          position: "absolute",
          width: "400px",
          height: "400px",
          borderRadius: "50%",
          background: "radial-gradient(circle, rgba(59,130,246,0.15) 0%, rgba(0,0,0,0) 70%)",
          top: "-100px",
          right: "-100px",
          pointerEvents: "none",
        }}
      />
      <div
        style={{
          position: "absolute",
          width: "350px",
          height: "350px",
          borderRadius: "50%",
          background: "radial-gradient(circle, rgba(234,179,8,0.1) 0%, rgba(0,0,0,0) 70%)",
          bottom: "-50px",
          left: "-50px",
          pointerEvents: "none",
        }}
      />

      <div
        className="card shadow-lg"
        style={{
          width: "100%",
          maxWidth: "420px",
          backgroundColor: "#ffffff",
          borderRadius: "16px",
          border: "1px solid rgba(255,255,255,0.1)",
          overflow: "hidden",
          zIndex: 10,
        }}
      >
        <div
          style={{
            background: "linear-gradient(135deg, #1e3a8a 0%, #2563eb 100%)",
            padding: "32px 24px 24px",
            textAlign: "center",
            color: "#ffffff",
          }}
        >
          <div
            style={{
              width: "64px",
              height: "64px",
              backgroundColor: "rgba(255,255,255,0.15)",
              backdropFilter: "blur(4px)",
              borderRadius: "50%",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              margin: "0 auto 16px",
              boxShadow: "0 4px 12px rgba(0,0,0,0.15)",
            }}
          >
            <svg
              width="32"
              height="32"
              viewBox="0 0 24 24"
              fill="none"
              stroke="#fbbf24"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
              <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
            </svg>
          </div>
          <h4 style={{ margin: 0, fontWeight: 700, letterSpacing: "0.5px" }}>
            ROYAL-D SYSTEM
          </h4>
          <p
            style={{
              margin: "4px 0 0",
              fontSize: "13px",
              opacity: 0.85,
            }}
          >
            ระบบติดตามบิลขายและการ์ดลูกหนี้ (v2.0)
          </p>
        </div>

        <div className="card-body" style={{ padding: "32px 28px" }}>
          <form onSubmit={handleSubmit}>
            <div className="mb-3">
              <label
                className="form-label"
                style={{ fontSize: "14px", fontWeight: 600, color: "#334155" }}
              >
                ชื่อผู้ใช้งาน (Username) <span style={{ color: "#ef4444" }}>*</span>
              </label>
              <div className="input-group">
                <input
                  type="text"
                  className="form-control"
                  style={{
                    borderRadius: "8px",
                    padding: "10px 14px",
                    borderColor: "#cbd5e1",
                    fontSize: "14px",
                  }}
                  placeholder="เช่น admin หรือ รหัสผู้แทนขาย"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  autoFocus
                  required
                />
              </div>
            </div>

            <div className="mb-4">
              <label
                className="form-label"
                style={{ fontSize: "14px", fontWeight: 600, color: "#334155" }}
              >
                รหัสผ่าน (Password) <span style={{ color: "#ef4444" }}>*</span>
              </label>
              <div className="input-group">
                <input
                  type={showPassword ? "text" : "password"}
                  className="form-control"
                  style={{
                    borderTopLeftRadius: "8px",
                    borderBottomLeftRadius: "8px",
                    padding: "10px 14px",
                    borderColor: "#cbd5e1",
                    fontSize: "14px",
                  }}
                  placeholder="กรอกรหัสผ่านเพื่อเข้าใช้งาน"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  style={{
                    borderTopRightRadius: "8px",
                    borderBottomRightRadius: "8px",
                    borderColor: "#cbd5e1",
                  }}
                  onClick={() => setShowPassword(!showPassword)}
                >
                  {showPassword ? "ซ่อน" : "แสดง"}
                </button>
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="btn btn-primary w-100"
              style={{
                background: "linear-gradient(135deg, #1e40af 0%, #2563eb 100%)",
                border: "none",
                borderRadius: "8px",
                padding: "12px",
                fontWeight: 600,
                fontSize: "15px",
                boxShadow: "0 4px 12px rgba(37,99,235,0.3)",
                transition: "all 0.2s ease",
              }}
            >
              {loading ? (
                <span>
                  <span
                    className="spinner-border spinner-border-sm me-2"
                    role="status"
                    aria-hidden="true"
                  ></span>
                  กำลังตรวจสอบสิทธิ์...
                </span>
              ) : (
                "เข้าสู่ระบบ (Sign In)"
              )}
            </button>
          </form>

          <div
            style={{
              marginTop: "24px",
              paddingTop: "16px",
              borderTop: "1px solid #f1f5f9",
              textAlign: "center",
              fontSize: "12px",
              color: "#64748b",
            }}
          >
            🔒 ระบบความปลอดภัย Data Loss Prevention (DLP) บจก. รอแยล-ดี
          </div>
        </div>
      </div>
    </div>
  );
}
