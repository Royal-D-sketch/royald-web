'use client';
import React, { useState } from 'react';
import { useRouter } from 'next/navigation';

export default function TestUpload() {
  const [file, setFile] = useState<File | null>(null);
  const [message, setMessage] = useState<string>('');
  const router = useRouter();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) {
      setMessage('กรุณาเลือกไฟล์');
      return;
    }
    const formData = new FormData();
    formData.append('file', file);
    try {
      const res = await fetch('/api/upload-cd', {
        method: 'POST',
        body: formData,
      });
      const data = await res.json();
      if (res.ok) {
        setMessage(`สำเร็จ: ${data.message}`);
      } else {
        setMessage(`ผิดพลาด: ${data.error || 'ไม่ทราบสาเหตุ'}`);
      }
    } catch (err) {
      setMessage('เกิดข้อผิดพลาดขณะส่งไฟล์');
    }
  };

  return (
    <div style={{ padding: '2rem' }}>
      <h1>ทดสอบอัปโหลดไฟล์ CD</h1>
      <form onSubmit={handleSubmit}>
        <input
          type="file"
          accept=".csv,.txt"
          onChange={e => setFile(e.target.files ? e.target.files[0] : null)}
        />
        <button type="submit" style={{ marginLeft: '1rem' }}>
          ส่งไฟล์
        </button>
      </form>
      {message && <p style={{ marginTop: '1rem' }}>{message}</p>}
    </div>
  );
}
