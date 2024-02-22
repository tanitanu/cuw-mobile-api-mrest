using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.HomePageContent;
namespace United.Mobile.Model.Internal.EmployeeProfile
{
    public class EmployeeTravelType
    {
        public bool IsTermsAndConditionsAccepted{ get; set; }
        public int NumberOfPassengersInJA{ get; set; }
        public string TermsAndConditions { get; set; }
        public int TaxType{ get; set; }
        public string VerbiageDescription { get; set; }
        public List<EmpTravelTypeItem> EmpTravelTypes{ get; set; }
    }
}