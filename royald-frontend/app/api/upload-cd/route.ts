import { NextResponse } from 'next/server';
import { supabase } from '@/lib/supabase';
import { parse, format, isValid } from 'date-fns';

/** Helper to parse dates from various formats and return ISO string (yyyy-MM-dd) or null */
function parseDate(raw: string): string | null {
  const patterns = ['yyyy-MM-dd', 'dd/MM/yyyy', 'MM/dd/yyyy', 'dd-MM-yyyy'];
  for (const p of patterns) {
    const parsed = parse(raw, p, new Date());
    if (isValid(parsed)) {
      return format(parsed, 'yyyy-MM-dd');
    }
  }
  return null;
}


/**
 * POST /api/upload-cd
 * Accepts Excel/CSV text or form data, extracts CD bills, and batch inserts into Supabase sales_bills
 */
export async function POST(request: Request) {
  try {
    const formData = await request.formData();
    const file = formData.get('file') as File | null;

    if (!file) {
      return NextResponse.json({ error: 'ไม่พบไฟล์ที่อัปโหลด' }, { status: 400 });
    }

    const text = await file.text();
    const lines = text.split(/\r?\n/).filter((l) => l.trim().length > 0);

    const billsToInsert: any[] = [];
    const now = new Date().toISOString();

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const cols = line.split(',').map((c) => c.replace(/^"|"$/g, '').trim());

      // Parse simple CSV rows
      if (cols.length >= 3) {
        const billNo = cols[0];
        if (billNo && billNo.length >= 3 && !billNo.toLowerCase().includes('เลขที่') && !billNo.toLowerCase().includes('bill')) {
          let billDate = new Date().toISOString().split('T')[0];
          if (cols[1]) {
            const parsed = parseDate(cols[1]);
            if (parsed) {
              billDate = parsed;
            } else {
              billDate = null; // will be handled as missing date
            }
          }

          const custName = cols[2] || 'ลูกค้าทั่วไป';
          const amount = parseFloat(cols[3]?.replace(/,/g, '') || '0') || 0;
          const salesRep = cols[4] || '-';
          const province = cols[5] || '-';

          billsToInsert.push({
            bill_no: billNo,
            bill_date: billDate,
            customer_name: custName,
            total_amount: amount,
            remaining_amount: amount,
            status: 'Outstanding',
            sales_rep: salesRep,
            province: province,
            created_at: now,
          });
        }
      }
    }

    if (billsToInsert.length > 0) {
      // Upsert into Supabase in batches of 100
      const batchSize = 100;
      for (let i = 0; i < billsToInsert.length; i += batchSize) {
        const batch = billsToInsert.slice(i, i + batchSize);
        await supabase.from('sales_bills').upsert(batch, { onConflict: 'bill_no' });
      }
    }

    return NextResponse.json({
      success: true,
      message: `นำเข้าข้อมูลบิลสำเร็จ ${billsToInsert.length} รายการ`,
      count: billsToInsert.length,
    });
  } catch (err: any) {
    return NextResponse.json({ error: err.message || 'Error processing file' }, { status: 500 });
  }
}
