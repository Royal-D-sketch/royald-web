import { NextResponse } from 'next/server';
import { supabase } from '@/lib/supabase';

/**
 * POST /api/payment
 * Handles payment submissions (Transfer or Check) from frontend.
 * Uses native FormData supported by Next.js App Router and Vercel.
 */
export async function POST(request: Request) {
  try {
    const formData = await request.formData();
    const billNo = formData.get('billNo') as string;
    const method = formData.get('method') as string;
    const amount = formData.get('amount') as string;
    const payDate = formData.get('payDate') as string;
    const note = (formData.get('note') as string) || null;
    const checkDate = (formData.get('checkDate') as string) || null;
    const bank = (formData.get('bank') as string) || null;
    const checkNo = (formData.get('checkNo') as string) || null;
    const file = formData.get('file') as File | null;

    if (!billNo || !method || !amount || !payDate) {
      return NextResponse.json({ error: 'Missing required fields' }, { status: 400 });
    }

    const amtNum = parseFloat(amount);
    if (isNaN(amtNum) || amtNum <= 0) {
      return NextResponse.json({ error: 'Invalid amount' }, { status: 400 });
    }

    // Fetch current remaining amount from Supabase
    const { data: bill, error: fetchErr } = await supabase
      .from('sales_bills')
      .select('remaining_amount, status')
      .eq('bill_no', billNo)
      .single();

    let newRemaining = 0;
    if (bill && !fetchErr) {
      newRemaining = Math.max(0, parseFloat(bill.remaining_amount || '0') - amtNum);
      const newStatus = newRemaining === 0 ? 'Paid' : bill.status;

      await supabase
        .from('sales_bills')
        .update({ remaining_amount: newRemaining, status: newStatus })
        .eq('bill_no', billNo);
    }

    // Handle file upload to Supabase Storage if present
    let fileUrl: string | null = null;
    if (file && file.size > 0) {
      const buffer = Buffer.from(await file.arrayBuffer());
      const fileName = `${Date.now()}_${file.name}`;
      const { data: storageData, error: storageErr } = await supabase.storage
        .from('payment_attachments')
        .upload(fileName, buffer, {
          contentType: file.type,
        });

      if (!storageErr && storageData) {
        const { data: publicUrlData } = supabase.storage
          .from('payment_attachments')
          .getPublicUrl(fileName);
        fileUrl = publicUrlData?.publicUrl || null;
      }
    }

    // Insert payment transaction record
    const paymentRecord = {
      bill_no: billNo,
      amount: amtNum,
      method,
      pay_date: payDate,
      note: note || null,
      file_url: fileUrl,
      check_date: checkDate || null,
      bank: bank || null,
      check_no: checkNo || null,
    };

    await supabase.from('payment_records').insert(paymentRecord);

    return NextResponse.json({
      success: true,
      message: 'Payment recorded successfully',
      remaining_amount: newRemaining,
    });
  } catch (err: any) {
    return NextResponse.json({ error: err.message || 'Internal server error' }, { status: 500 });
  }
}
