using System;
using System.Collections.Generic;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPOnTimePerformance
    {
        public string EffectiveDate { get; set; } = string.Empty;
        public string PctOnTimeCancelled { get; set; } = string.Empty;
        public string PctOnTimeDelayed { get; set; } = string.Empty;
        public string PctOnTimeMax { get; set; } = string.Empty;
        public string PctOnTimeMin { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public List<string> OnTimeNotAvailableMessage { get; set; }
        public SHOPOnTimeDOTMessages DotMessages { get; set; }

        //public string EffectiveDate
        //{
        //    get
        //    {
        //        return this.effectiveDate;
        //    }
        //    set
        //    {
        //        this.effectiveDate = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string PctOnTimeCancelled
        //{
        //    get
        //    {
        //        return this.pctOnTimeCancelled;
        //    }
        //    set
        //    {
        //        this.pctOnTimeCancelled = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string PctOnTimeDelayed
        //{
        //    get
        //    {
        //        return this.pctOnTimeDelayed;
        //    }
        //    set
        //    {
        //        this.pctOnTimeDelayed = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string PctOnTimeMax
        //{
        //    get
        //    {
        //        return this.pctOnTimeMax;
        //    }
        //    set
        //    {
        //        this.pctOnTimeMax = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string PctOnTimeMin
        //{
        //    get
        //    {
        //        return this.pctOnTimeMin;
        //    }
        //    set
        //    {
        //        this.pctOnTimeMin = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string Source
        //{
        //    get
        //    {
        //        return this.source;
        //    }
        //    set
        //    {
        //        this.source = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public List<string> OnTimeNotAvailableMessage
        //{
        //    get
        //    {
        //        return this.onTimeNotAvailableMessage;
        //    }
        //    set
        //    {
        //        this.onTimeNotAvailableMessage = value;
        //    }
        //}

        //public SHOPOnTimeDOTMessages DOTMessages
        //{
        //    get
        //    {
        //        return this.dotMessages;
        //    }
        //    set
        //    {
        //        this.dotMessages = value;
        //    }
        //}

        public SHOPOnTimePerformance()
        {
            OnTimeNotAvailableMessage = new List<string>();
        }
    }
}
