using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.Shopping
{
    [Serializable]
    public class MOBStyledText
    {

        public string Text { get; set; } = string.Empty;
        public string TextColor { get; set; } = MOBStyledColor.Black.GetDescription();

        public bool IsItalic { get; set; }
        public string BackgroundColor { get; set; } = MOBStyledColor.Clear.GetDescription();

        public string SortPriority { get; set; } = string.Empty;
        public int FontSize { get; set; } 

        public string Key { get; set; }
        public MOBMobileCMSContentMessages BadgeToolTipContent { get; set; }
    }


    public enum MOBStyledColor
    {
        [Description("#FF 000000")]
        Black,
        [Description("#FF FFC558")]
        Yellow,
        [Description("#FF 1D7642")]
        Green,
        [Description("#00 000000")]
        Clear,
        [Description("#FF FFFFFF")]
        White,
        [Description("#FF 1B7742")]
        AlertGreen,
        [Description("#024 000000")]
        Blue,
    }

    public enum MOBFlightBadgeSortOrder
    {
        CovidTestRequired,
        PremierSavings,
        ChaseCardHolder,
        MemberSavings,
        WheelChairFits
    }

    public enum MOBFlightProductBadgeSortOrder
    {
        NoLongerAvailable,
        Specialoffer,
        MixedCabin,
        YADiscounted,
        CorporateDiscounted,
        OutOfPolicy,
        MyUADiscounted,
        BreakFromBusiness,
        SaverAward
    }

    public enum MOBFlightProductAwardType
    {
        Saver,
        Standard
    }

    public enum MOBFlightProductAwardStrikeThroughType
    {
        CH, // Chase card
        PE // Premier Elite
    }
    public static class LinqHelper
    {
        public static string GetDescription<T>(this T e) where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttribute = memInfo[0]
                            .GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

                        if (descriptionAttribute != null)
                        {
                            return descriptionAttribute.Description;
                        }
                    }
                }
            }

            return null; // could also return string.Empty
        }
    }
}