using System.Collections.Generic;

namespace United.Mobile.Model.CSLModels
{

    public class TravelerAwsRequest
    {
        public TravelerData TravelerData { get; set; }
        public ContactData ContactData { get; set; }
        public List<int> SpecialRequestIds { get; set; }
        public List<TravelerRewardProgram> RewardProgramData { get; set; }
        public int MealId { get; set; }
        public int SeatSideId { get; set; }
    }

    public class AddEmail
    {
        public string Type { get; set; }
        public string Address { get; set; }
    }

    public class AddPhone
    {
        public string Type { get; set; }
        public string CountryPhoneNumber { get; set; }
        public string CountryCode { get; set; }
        public string Number { get; set; }
        public string DeviceType { get; set; }
    }

    public class ContactData
    {
        public List<AddPhone> AddPhone { get; set; }
        public List<EditPhone> EditPhone { get; set; }
        public List<AddEmail> AddEmail { get; set; }
        public List<EditEmail> EditEmail { get; set; }
    }

    public class EditEmail
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public string Address { get; set; }
    }

    public class EditPhone
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public string CountryPhoneNumber { get; set; }
        public string CountryCode { get; set; }
        public string Number { get; set; }
        public string DeviceType { get; set; }
    }

    public class TravelerRewardProgram
    {
        public int ProgramId { get; set; }
        public string ProgramMemberId { get; set; }
        public bool? IsUpdate { get; set; }
    }

    public class TravelerSecure
    {
        public string Type { get; set; }
        public string Number { get; set; }
    }

    public class TravelerData
    {
        public string TravelerKey { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Suffix { get; set; }
        public string BirthDate { get; set; }
        public string Gender { get; set; }
        public List<TravelerSecure> SecureTraveler { get; set; }
        public string Nationality { get; set; }
        public string CountryOfResidence { get; set; }
        public string UpdateId { get; set; }
        public string InsertId { get; set; }
    }
}
