using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.TeaserPage;

namespace United.Mobile.Services.ShopFlightDetails.Domain
{
    public interface ITeaserPageBusiness
    {
        Task<MOBSHOPShoppingTeaserPageResponse> GetTeaserPage(MOBSHOPShoppingTeaserPageRequest request, HttpContext httpcontext);
    }
}
