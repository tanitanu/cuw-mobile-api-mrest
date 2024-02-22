using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.FlightStatus;

namespace United.Mobile.Model.Shopping
{
    [Serializable()]
    public class BagTagHistory
    {
        private MOBAirport destination;
        private string psgrFirstName = string.Empty;
        private string psgrLastName = string.Empty;
        private string pnr = string.Empty;
        private MOBAirport origin;
        private string bagTagNum = string.Empty;
        private List<BagHistory> bagHistory;

        public BagTagHistory()
        {
        }

        public MOBAirport Destination
        {
            get 
            { 
                return this.destination; 
            }
            set 
            { 
                this.destination = value; 
            }
        }

        public string PsgrFirstName
        {
            get
            {
                return this.psgrFirstName;
            }
            set
            {
                this.psgrFirstName = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string PsgrLastName
        {
            get
            {
                return this.psgrLastName;
            }
            set
            {
                this.psgrLastName = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string PNR
        {
            get
            {
                return this.pnr;
            }
            set
            {
                this.pnr = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public MOBAirport Origin
        {
            get 
            { 
                return this.origin; 
            }
            set 
            { 
                this.origin = value; 
            }
        }

        public string BagTagNum
        {
            get
            {
                return this.bagTagNum;
            }
            set
            {
                this.bagTagNum = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public List<BagHistory> BagHistory
        {
            get
            {
                return this.bagHistory;
            }
            set
            {
                this.bagHistory = value;
            }
        }
    }
}
