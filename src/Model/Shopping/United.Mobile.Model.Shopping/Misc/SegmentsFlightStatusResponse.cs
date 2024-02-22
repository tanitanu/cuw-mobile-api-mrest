using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.Model.Shopping.Misc
{
    [Serializable()]
    public class SegmentsFlightStatusResponse : MOBResponse
    {
        public List<FlightStatusInfo> FlightStatusInfoList { get; set; }
       
    }
}
