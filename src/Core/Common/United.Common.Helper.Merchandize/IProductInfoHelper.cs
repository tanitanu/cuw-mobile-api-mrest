using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model;
using United.Mobile.Model.Shopping;
using United.Services.FlightShopping.Common.FlightReservation;
using MOBItem = United.Mobile.Model.Common.MOBItem;
using MOBMobileCMSContentMessages = United.Mobile.Model.Common.MOBMobileCMSContentMessages;
using SeatChangeState = United.Mobile.Model.Shopping.SeatChangeState;

namespace United.Common.Helper.Merchandize
{
    public interface IProductInfoHelper
    {
        Task<List<ProdDetail>> ConfirmationPageProductInfo(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isPost, MOBApplication application, SeatChangeState state = null, string flow = "VIEWRES", string sessionId = "");
        Task<List<MOBMobileCMSContentMessages>> GetProductBasedTermAndConditions(string sessionId, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isPost);
        Task<List<MOBItem>> GetCaptions(string key);
        Task<List<MOBMobileCMSContentMessages>> GetProductBasedTermAndConditions(United.Service.Presentation.ProductResponseModel.ProductOffer productVendorOffer, FlightReservationResponse flightReservationResponse, bool isPost, bool isGetCartInfo = false);
        void AddCouponDetails(List<ProdDetail> prodDetails, Services.FlightShopping.Common.FlightReservation.FlightReservationResponse cslFlightReservationResponse, bool isPost, string flow, MOBApplication application);
        string GetCommonSeatCode(string seatCode);
        //List<MOBTypeOption> GetBagsLineItems(MOBBag bagCount);
        bool IsEnableOmniCartMVP2Changes(int applicationId, string appVersion, bool isDisplayCart);
    }
}
