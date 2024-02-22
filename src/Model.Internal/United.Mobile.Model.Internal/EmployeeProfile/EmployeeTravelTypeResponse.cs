using System.Collections.Generic;

namespace United.Mobile.Model.Internal.EmployeeProfile
{
    public class EmployeeTravelTypeResponse
    {
        public string EResTransactionId { get; set; }
        public string SessionId { get; set; }
        public EmployeeTravelType EmpTravelType { get; set; }
        public bool IsPayrollDeduct { get; set; }
        public int AdvanceBookingDays { get; set; }
        public string DisplayEmployeeId { get; set; }
        public List<DependentInfo> DependentInfos { get; set; }
        public Name EmployeeName { get; set; }
        public string EmployeeDateOfBirth { get; set; }
    }
}
