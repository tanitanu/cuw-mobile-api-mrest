using System;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPOnTimeDOTMessages
    {
        public string CancellationPercentageMessage { get; set; } = string.Empty;
        public string DelayPercentageMessage { get; set; } = string.Empty;
        public string DelayAndCancellationPercentageMessage { get; set; } = string.Empty;
        public string DotMessagePopUpButtonCaption { get; set; } = string.Empty;

        //public string CancellationPercentageMessage
        //{
        //    get
        //    {
        //        return this.cancellationPercentageMessage;
        //    }
        //    set
        //    {
        //        this.cancellationPercentageMessage = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string DelayPercentageMessage
        //{
        //    get
        //    {
        //        return this.delayPercentageMessage;
        //    }
        //    set
        //    {
        //        this.delayPercentageMessage = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string DelayAndCancellationPercentageMessage
        //{
        //    get
        //    {
        //        return this.delayAndCancellationPercentageMessage;
        //    }
        //    set
        //    {
        //        this.delayAndCancellationPercentageMessage = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string DOTMessagePopUpButtonCaption
        //{
        //    get
        //    {
        //        return this.dotMessagePopUpButtonCaption;
        //    }
        //    set
        //    {
        //        this.dotMessagePopUpButtonCaption = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}
        
    }
}
