using System;
namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class ClubMembershipResponse : MOBResponse
    {
        public ClubMembership Membership { get; set; }
    }
}
