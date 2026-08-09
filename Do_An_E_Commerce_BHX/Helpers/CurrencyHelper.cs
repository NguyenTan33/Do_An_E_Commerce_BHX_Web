using System;
using System.Globalization;
using System.Text;

namespace Do_An_E_Commerce_BHX.Helpers
{
    /// <summary>
    /// Class tiện ích xử lý định dạng tiền tệ VNĐ và đọc số tiền thành chữ Việt Nam
    /// </summary>
    public static class CurrencyHelper
    {
        private static readonly CultureInfo VnCulture = new CultureInfo("vi-VN");

        /// <summary>
        /// Định dạng số tiền decimal sang chuỗi VNĐ chuẩn (VD: 330.000 ₫)
        /// </summary>
        public static string FormatVND(this decimal amount, bool includeSymbol = true)
        {
            string formatted = amount.ToString("N0", VnCulture);
            return includeSymbol ? formatted + " ₫" : formatted;
        }

        /// <summary>
        /// Định dạng số tiền double sang chuỗi VNĐ chuẩn
        /// </summary>
        public static string FormatVND(this double amount, bool includeSymbol = true)
        {
            return FormatVND((decimal)amount, includeSymbol);
        }

        /// <summary>
        /// Định dạng số tiền float sang chuỗi VNĐ chuẩn
        /// </summary>
        public static string FormatVND(this float amount, bool includeSymbol = true)
        {
            return FormatVND((decimal)amount, includeSymbol);
        }

        /// <summary>
        /// Định dạng số tiền int sang chuỗi VNĐ chuẩn
        /// </summary>
        public static string FormatVND(this int amount, bool includeSymbol = true)
        {
            return FormatVND((decimal)amount, includeSymbol);
        }

        /// <summary>
        /// Định dạng rút gọn cho thẻ giá (VD: 350k, 1.5M)
        /// </summary>
        public static string FormatVNDShort(this decimal amount)
        {
            if (amount >= 1000000000)
                return (amount / 1000000000m).ToString("0.#") + "Tỷ";
            if (amount >= 1000000)
                return (amount / 1000000m).ToString("0.#") + "Tr";
            if (amount >= 1000)
                return (amount / 1000m).ToString("0.#") + "k";
            return amount.ToString("N0", VnCulture) + " ₫";
        }

        /// <summary>
        /// Chuyển đổi chuỗi số tiền từ giao diện về decimal an toàn
        /// </summary>
        public static decimal ParseVND(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0m;
            string clean = input.Replace("₫", "").Replace("VNĐ", "").Replace("vnd", "").Replace(".", "").Replace(",", "").Trim();
            return decimal.TryParse(clean, out decimal val) ? val : 0m;
        }

        /// <summary>
        /// Đọc số tiền bằng chữ Tiếng Việt cho hóa đơn thanh toán
        /// </summary>
        public static string ToVietnameseWords(this decimal totalAmount)
        {
            long number = (long)Math.Round(totalAmount);
            if (number == 0) return "Không đồng";
            if (number < 0) return "Am " + ToVietnameseWords(Math.Abs(number));

            string[] digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
            string[] units = { "", "ngàn", "triệu", "tỷ", "ngàn tỷ", "triệu tỷ" };

            var sb = new StringBuilder();
            int unitIndex = 0;

            while (number > 0)
            {
                int group = (int)(number % 1000);
                if (group > 0)
                {
                    string groupText = FormatThreeDigits(group, number >= 1000, digits);
                    string unitText = units[unitIndex];
                    if (!string.IsNullOrEmpty(unitText))
                    {
                        groupText += " " + unitText;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Insert(0, groupText + " ");
                    }
                    else
                    {
                        sb.Append(groupText);
                    }
                }
                number /= 1000;
                unitIndex++;
            }

            string result = sb.ToString().Trim();
            if (result.Length > 0)
            {
                result = char.ToUpper(result[0]) + result.Substring(1) + " đồng";
            }
            return result;
        }

        private static string FormatThreeDigits(int number, bool hasHigherGroups, string[] digits)
        {
            int hundreds = number / 100;
            int tens = (number % 100) / 10;
            int units = number % 10;

            var sb = new StringBuilder();

            if (hundreds > 0 || hasHigherGroups)
            {
                sb.Append(digits[hundreds]).Append(" trăm");
            }

            if (tens > 1)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(digits[tens]).Append(" mươi");
                if (units == 1) sb.Append(" mốt");
                else if (units == 5) sb.Append(" lăm");
                else if (units > 0) sb.Append(" ").Append(digits[units]);
            }
            else if (tens == 1)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append("mười");
                if (units == 5) sb.Append(" lăm");
                else if (units > 0) sb.Append(" ").Append(digits[units]);
            }
            else if (units > 0)
            {
                if (sb.Length > 0)
                {
                    if (hundreds > 0 || hasHigherGroups) sb.Append(" lẻ ");
                }
                sb.Append(digits[units]);
            }

            return sb.ToString().Trim();
        }
    }
}