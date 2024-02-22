using System;

namespace United.Mobile.Model.FeedBack
{

    [Serializable]
    public class MOBPromoFeedbackResponse : MOBResponse
    {
        private MOBPromoFeedbackRequest request;
        private bool succeed;

        public MOBPromoFeedbackRequest Request
        {
            get
            {
                return this.request;
            }
            set
            {
                this.request = value;
            }
        }

        public bool Succeed
        {
            get
            {
                return this.succeed;
            }
            set
            {
                this.succeed = value;
            }
        }
    }
}
