using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.ReShop;

namespace United.Mobile.ReShop.Domain.GetProducts_CFOP
{
    public interface IGetProducts_CFOPBusiness
    {
        Task<MOBSHOPProductSearchResponse> GetProducts_CFOP(MOBSHOPProductSearchRequest request);
        Task<bool> ValidateEPlusVersion(int applicationID, string appVersion);
    }
}
