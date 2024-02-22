using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.FlightReservation
{
    [Serializable()]
    public class MOBReceiptByEmailRequest : MOBRequest
    {
        private string recordLocator = string.Empty;
        private string creationDate = string.Empty;
        private string emailAdress = string.Empty;
        private string emailAddress = string.Empty;

        public MOBReceiptByEmailRequest()
            : base()
        {
        }

        public string RecordLocator
        {
            get
            {
                return this.recordLocator;
            }
            set
            {
                this.recordLocator = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string CreationDate
        {
            get
            {
                return this.creationDate;
            }
            set
            {
                this.creationDate = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string EMailAddress
        {
            get
            {
                return this.emailAddress;
            }
            set
            {
                this.emailAddress = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string EMailAdress
        {
            get
            {
                return this.emailAdress;
            }
            set
            {
                this.emailAdress = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
    }
}
