using System.Globalization;
using United.Utility.Helper;

namespace United.Mobile.Model.Travelers
{
    public class UnitedCurrency
    {
        public UnitedCurrency()
        {
        }

        public decimal Amount { get; set; }

        public string Currency { get; set; }

        public string Price1 { get { return GeneralHelper.FormatCurrency(Amount.ToString()) ; } }

        public string Price2 { get { return Amount.ToString("C", new CultureInfo("en-US")); } }

        public string Price3 { get { return string.Format("{0:c}", Amount); } }

        public string Price4 { get { return GetPriceAfterChaseCredit(Amount, "$160.20"); } }

        public string Price5 { get { return string.Format(new CultureInfo("en-US"), "{0:c}", Amount); } }
        public bool IsICUMode { get { return ICUMode(); } }

        public static bool ICUMode()
        {
            SortVersion sortVersion = CultureInfo.InvariantCulture.CompareInfo.Version;
            byte[] bytes = sortVersion.SortId.ToByteArray();
            int version = bytes[3] << 24 | bytes[2] << 16 | bytes[1] << 8 | bytes[0];
            return version != 0 && version == sortVersion.FullVersion;
        }
        public string GetPriceAfterChaseCredit(decimal price, string chaseCrediAmount)
        {
            decimal creditAmt = 0;
                decimal.TryParse(chaseCrediAmount, System.Globalization.NumberStyles.AllowCurrencySymbol | System.Globalization.NumberStyles.AllowDecimalPoint, null, out creditAmt);
            CultureInfo culture = new System.Globalization.CultureInfo("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            return string.Format(culture, "{0:C}", price - creditAmt);

            //return (Convert.ToDecimal(price - creditAmt)).ToString("C2", CultureInfo.CurrentCulture);
        }
    }
}
