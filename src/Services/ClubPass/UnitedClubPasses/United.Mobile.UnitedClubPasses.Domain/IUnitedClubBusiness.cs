using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.UnitedClubPasses;

namespace United.Mobile.UnitedClubPasses.Domain
{
    public interface IUnitedClubBusiness
    {
        Task<ClubPKDispenserPublicKeyResponse> GetPKDispenserPublicKey(ClubPKDispenserPublicKeyRequest request);

        Task<ClubPKDispenserPublicKeyResponse> GetPublicKey(ClubPKDispenserPublicKeyRequest request);

        Task<ClubMembershipResponse> GetUnitedClubMembershipV2(UnitedClubMembershipRequest ucmRequest);
    }
}
