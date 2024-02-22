using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.Fitbit
{
    public class TripSegment
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int SegmentNumber { get; set; } 
        public string SegmentOrigin { get; set; } = string.Empty;
        public string SegmentDestination { get; set; } = string.Empty;
        public bool GenerateTripPass { get; set; }
    }
}
