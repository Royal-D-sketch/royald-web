using System;
using System.Text;

namespace RoyalD.Web.Services
{
    public static class ThaiBahtTextHelper
    {
        private static readonly string[] ThaiDigits = { "ศูนย์", "หนึ่ง", "สอง", "สาม", "สี่", "ห้า", "หก", "เจ็ด", "แปด", "เก้า" };
        private static readonly string[] ThaiPositions = { "", "สิบ", "ร้อย", "พัน", "หมื่น", "แสน", "ล้าน" };

        public static string ToThaiBahtText(decimal amount)
        {
            if (amount == 0) return "ศูนย์บาทถ้วน";

            bool isNegative = amount < 0;
            amount = Math.Abs(Math.Round(amount, 2));

            long baht = (long)Math.Floor(amount);
            int satang = (int)Math.Round((amount - baht) * 100);

            var sb = new StringBuilder();
            if (isNegative) sb.Append("ลบ");

            if (baht > 0)
            {
                sb.Append(ConvertIntegerToThai(baht));
                sb.Append("บาท");
            }

            if (satang > 0)
            {
                sb.Append(ConvertIntegerToThai(satang));
                sb.Append("สตางค์");
            }
            else
            {
                sb.Append("ถ้วน");
            }

            return sb.ToString();
        }

        private static string ConvertIntegerToThai(long number)
        {
            if (number == 0) return ThaiDigits[0];

            var sb = new StringBuilder();
            string strNum = number.ToString();
            int len = strNum.Length;

            for (int i = 0; i < len; i++)
            {
                int digit = strNum[i] - '0';
                int pos = (len - i - 1) % 6;

                if (digit != 0)
                {
                    if (pos == 1 && digit == 1)
                    {
                        // 10 -> สิบ
                    }
                    else if (pos == 1 && digit == 2)
                    {
                        sb.Append("ยี่");
                    }
                    else if (pos == 0 && digit == 1 && len > 1 && (strNum[i - 1] - '0') != 0)
                    {
                        sb.Append("เอ็ด");
                    }
                    else
                    {
                        sb.Append(ThaiDigits[digit]);
                    }

                    sb.Append(ThaiPositions[pos]);
                }

                if (pos == 0 && i < len - 1)
                {
                    sb.Append("ล้าน");
                }
            }

            return sb.ToString();
        }
    }
}
