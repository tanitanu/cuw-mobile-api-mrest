using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Service.Presentation.PriceModel;

namespace United.Mobile.Model.Shopping.Misc
{
    [Serializable()]
    public class Seat
    {

        public decimal PriceBeforeCouponApplied { get; set; } 

        public List<PriceAdjustment> Adjustments { get; set; } 
        public string SegmentIndex { get; set; }
      

        public decimal PriceAfterCouponApplied { get; set; } 

        public bool IsCouponApplied { get; set; } 
        public bool LimitedRecline { get; set; } 

        public string SeatAssignment { get; set; } 

        public string OldSeatAssignment { get; set; } 

        public string Attribute { get; set; }
      

        public string Origin { get; set; }
        
        public string Destination { get; set; } 
      
        public string FlightNumber { get; set; }
       
        public string DepartureDate { get; set; }
       
        public string TravelerSharesIndex { get; set; }
      

        public int Key { get; set; }

        public bool UAOperated { get; set; }
        private decimal price;

        public decimal Price
        {
            get { return this.price; }
            set { this.price = value; }
        }

        public decimal PriceAfterTravelerCompanionRules { get; set; }
        public string Currency { get; set; }
        private string programCode;
        public string ProgramCode
        {
            get { return this.programCode; }
            set { this.programCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper(); }
        }
        private string seatType;
        public string SeatType
        {
            get { return this.seatType; }
            set { this.seatType = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper(); }
        }

        public decimal OldSeatPrice { get; set; }
        private string oldSeatCurrency;

        public string OldSeatCurrency
        {
            get { return this.oldSeatCurrency; }
            set { this.oldSeatCurrency = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper(); }
        }


        public string OldSeatProgramCode { get; set; }
       

        public string OldSeatType { get; set; } 
      

        public string OldSeatEDDTransactionId { get; set; } 
     
        public bool IsEPA { get; set; } 

        public bool IsEPAFreeCompanion { get; set; }

        private string seatFeature;

        public string SeatFeature
        {
            get
            {
                return this.seatFeature;
            }
            set
            {
                this.seatFeature = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }

        public string PcuOfferOptionId { get; set; } 
        
        public Int32 Miles { get; set; } 
        public Int32 MilesAfterTravelerCompanionRules { get; set; } 
        public Int32 OldSeatMiles { get; set; } 
        public Int32 MilesBeforeCouponApplied { get; set; } 
        public Int32 MilesAfterCouponApplied { get; set; } 
        public string DisplayMiles { get; set; } 
        public string DisplayMilesAfterTravelerCompanionRules { get; set; } 
     
        public string DisplayOldSeatMiles { get; set; } 
      
        public string DisplayMilesBeforeCouponApplied { get; set; } 
        
        public string DisplayMilesAfterCouponApplied { get; set; } 

        public string PromotionalCouponCode { get; set; }
    }
}
