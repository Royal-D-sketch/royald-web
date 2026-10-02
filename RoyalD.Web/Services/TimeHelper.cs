using System;

namespace RoyalD.Web.Services
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo ThaiZone = InitThaiZone();

        private static TimeZoneInfo InitThaiZone()
        {
            try
            {
                // Linux / IANA standard (e.g. Render, Docker, Ubuntu)
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
            }
            catch
            {
                try
                {
                    // Windows standard
                    return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
                }
                catch
                {
                    // Generic fallback UTC+7
                    return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
                }
            }
        }

        /// <summary>
        /// Current DateTime in Thailand (UTC+7).
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ThaiZone);

        /// <summary>
        /// Current Date in Thailand.
        /// </summary>
        public static DateTime Today => Now.Date;

        /// <summary>
        /// Converts any DateTime (UTC, Local, or Unspecified UTC from DB) to Thailand Time (UTC+7).
        /// </summary>
        public static DateTime ToThaiTime(DateTime dt)
        {
            DateTime utc;
            if (dt.Kind == DateTimeKind.Utc)
            {
                utc = dt;
            }
            else if (dt.Kind == DateTimeKind.Local)
            {
                utc = dt.ToUniversalTime();
            }
            else // DateTimeKind.Unspecified
            {
                utc = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utc, ThaiZone);
        }
    }
}
