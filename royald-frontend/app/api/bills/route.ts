import { NextResponse } from 'next/server';
import { supabase } from '@/lib/supabase';

/**
 * GET /api/bills
 * Fetches sales bills directly from Supabase database.
 */
export async function GET() {
  try {
    const { data: bills, error } = await supabase
      .from('sales_bills')
      .select('*')
      .order('bill_date', { ascending: false })
      .limit(200);

    if (!error && bills && bills.length > 0) {
      const formatted = bills.map((b: any) => ({
        BillNo: b.bill_no,
        Date: b.bill_date || b.created_at,
        CustomerName: b.customer_name || 'ลูกค้าทั่วไป',
        TotalAmount: b.total_amount || b.remaining_amount || 0,
        RemainingAmount: b.remaining_amount,
        Status: b.status || 'Outstanding',
        SalesRep: b.sales_rep || '-',
        Province: b.province || '-',
      }));
      return NextResponse.json(formatted);
    }
  } catch (err) {
    console.error("Supabase bills fetch error:", err);
  }

  return NextResponse.json([]);
}
