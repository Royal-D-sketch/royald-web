"use client";

import { useEffect } from "react";
import Providers from "../components/Providers";

export default function RootLayout({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    // Watermark style injected
    const style = document.createElement("style");
    style.id = "royal-d-watermark";
    style.innerHTML = `
      body::before {
        content: "เอกสารลับ ห้ามเผยแพร่ บจก.รอแยล-ดี";
        position: fixed;
        top: 0; left: 0;
        width: 100vw; height: 100vh;
        pointer-events: none;
        color: rgba(0, 0, 0, 0.04);
        font-size: 38px;
        font-weight: bold;
        display: flex;
        align-items: center;
        justify-content: center;
        text-align: center;
        transform: rotate(-25deg);
        z-index: 99999;
      }
    `;
    if (!document.getElementById("royal-d-watermark")) {
      document.head.appendChild(style);
    }

    // Anti-Screen Capture (DLP)
    const blockScreen = () => {
      document.body.style.transition = "background 0.1s";
      document.body.style.background = "#000000";
      setTimeout(() => {
        document.body.style.background = "";
      }, 3000);
    };

    const keyHandler = (e: KeyboardEvent) => {
      // PrintScreen, Shift+Cmd+3/4/5 (Mac), or Win+Shift+S / Ctrl+Shift+S
      if (
        e.key === "PrintScreen" ||
        e.keyCode === 44 ||
        (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === "s") ||
        (e.metaKey && e.shiftKey && (e.key === "3" || e.key === "4" || e.key === "5"))
      ) {
        blockScreen();
      }
    };

    window.addEventListener("keyup", keyHandler);
    window.addEventListener("keydown", keyHandler);

    return () => {
      window.removeEventListener("keyup", keyHandler);
      window.removeEventListener("keydown", keyHandler);
    };
  }, []);

  return (
    <html lang="th">
      <head>
        <title>Royal-D Sales & Debtor Management System</title>
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <link
          rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css"
        />
        <link
          rel="stylesheet"
          href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css"
        />
      </head>
      <body style={{ backgroundColor: "#f8fafc", minHeight: "100vh" }}>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
