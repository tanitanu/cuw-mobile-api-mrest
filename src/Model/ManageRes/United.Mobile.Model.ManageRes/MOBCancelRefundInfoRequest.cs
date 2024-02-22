using System;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.ManageRes
{
    [Serializable]
    public class MOBCancelRefundInfoRequest : MOBModifyReservationRequest
    {
        private string pnr = string.Empty;
        private string lastName = string.Empty;
        private bool isVersionAllowAwardCancel = false;
        private Boolean override24HrFlex;
        public string Token {set; get;}    

        public bool IsAward { set; get; }
        public bool isPNRELF { set; get; }
        public string PNR
        {
            get { return pnr; }
            set { pnr = value; }
        }

        public string LastName
        {
            get { return lastName; }
            set { lastName = value; }
        }

        public bool IsVersionAllowAwardCancel
        {
            get { return isVersionAllowAwardCancel; }
            set { isVersionAllowAwardCancel = value; }
        }

        public Boolean Override24HrFlex
        {
            get { return override24HrFlex; }
            set { override24HrFlex = value; }
        }
    }
}
