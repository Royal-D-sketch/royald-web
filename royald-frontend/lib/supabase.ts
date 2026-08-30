// lib/supabase.ts
import { createClient } from "@supabase/supabase-js";

// Supabase credentials are taken from environment variables set in Vercel.
const supabaseUrl = process.env.NEXT_PUBLIC_SUPABASE_URL || "";
const supabaseAnonKey = process.env.NEXT_PUBLIC_SUPABASE_ANON_KEY || "";

export const supabase = createClient(supabaseUrl, supabaseAnonKey);
