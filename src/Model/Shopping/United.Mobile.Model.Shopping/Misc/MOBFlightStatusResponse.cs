using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace United.Mobile.Model.Shopping
{
    [Serializable]
    public class FlightStatusResponse : MOBResponse
    {
        private FlightStatusInfo flightStatusInfo;


        public FlightStatusResponse()
            : base()
        {
        }


        public FlightStatusInfo FlightStatusInfo
        {
            get
            {
                return this.flightStatusInfo;
            }
            set
            {
                this.flightStatusInfo = value;
            }
        }
    }
}
