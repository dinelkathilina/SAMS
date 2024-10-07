using System;
using System.Security.Cryptography;
using System.Text;

namespace SAMS.Utilities // Replace SAMS with your project's namespace
{
    public static class SessionCodeGenerator
    {
        private static readonly TimeZoneInfo sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");

        public static string GenerateSessionCode(int courseId, int lectureHallId, DateTime startTime)
        {
            // Convert the startTime to Sri Lanka time zone
            var sriLankaTime = TimeZoneInfo.ConvertTimeFromUtc(startTime.ToUniversalTime(), sriLankaTimeZone);

            // Generate a random component
            var randomComponent = GenerateRandomComponent();

            // Format the date and time
            var dateTimeComponent = sriLankaTime.ToString("yyMMddHHmm");

            // Combine all components
            return $"{courseId:D3}-{lectureHallId:D2}-{dateTimeComponent}-{randomComponent}";
        }

        private static string GenerateRandomComponent()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var stringBuilder = new StringBuilder(6);
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (stringBuilder.Length < 6)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    stringBuilder.Append(chars[(int)(num % (uint)chars.Length)]);
                }
            }

            return stringBuilder.ToString();
        }
    }
}