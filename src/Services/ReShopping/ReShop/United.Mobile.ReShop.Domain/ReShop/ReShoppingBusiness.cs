using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Services.Shopping.Domain;
using United.Utility.Helper;

namespace United.Mobile.ReShop.Domain
{
    public class ReShoppingBusiness : IReShoppingBusiness
    {
        private readonly ICacheLog<ReShoppingBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IShoppingBusiness _shopBusiness;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private HttpContext _httpContext;

        public ReShoppingBusiness(ICacheLog<ReShoppingBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IShoppingBusiness shopBusiness
            , IShoppingUtility shoppingUtility
            , IShoppingSessionHelper shoppingSessionHelper)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shopBusiness = shopBusiness;
            _shoppingUtility = shoppingUtility;
            _shoppingSessionHelper = shoppingSessionHelper;
        }

        public async Task<ShopResponse> ReShop(MOBSHOPShopRequest request, HttpContext httpContext = null)
        {
            request.IsReshopChange = true;

            var tupleRes = await GetEmployeeDiscountId(request);
            request = tupleRes.request;

            var reshopResponse = await _shopBusiness.GetShop(request, _httpContext ?? httpContext);
            if (reshopResponse.Availability != null)
            {
                reshopResponse.Availability.FareWheel = null;
            }

            if (!_configuration.GetValue<bool>("DisableShopRequestFlow"))
            {
                if (reshopResponse?.ShopRequest != null)
                    reshopResponse.ShopRequest.Flow = Utility.Enum.FlowType.RESHOP.ToString();

            }

            if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination") && !request.AwardTravel)
            {
                try
                {
                    int changeLOFcount = 0;
                    if (request?.Trips != null && request.Trips.Any())
                    {
                        changeLOFcount = request.Trips.Where(x => x.ChangeType == MOBSHOPTripChangeType.ChangeFlight).Count()
                            + request.Trips.Where(x => x.ChangeType == MOBSHOPTripChangeType.AddFlight).Count();

                        if (changeLOFcount > 1 && reshopResponse.Availability?.Reservation?.Reshop != null)
                        {
                            if (_configuration.GetValue<bool>("EnableFSRHeadingBugfix"))
                            {
                                if (changeLOFcount == 2)
                                {
                                    reshopResponse.Availability.Reservation.Reshop.FsrSubHeading = "Roundtrip";
                                }
                                else
                                {
                                    reshopResponse.Availability.Reservation.Reshop.FsrSubHeading = "Multicity";
                                }
                            }
                            else {
                                reshopResponse.Availability.Reservation.Reshop.FsrSubHeading = "Roundtrip";
                            }
                            

                            var persistedReservation = new Reservation();
                            persistedReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, persistedReservation.ObjectName, new List<string>() { request.SessionId, persistedReservation.ObjectName });
                            if (persistedReservation != null)
                            {
                                if (_configuration.GetValue<bool>("EnableFSRHeadingBugfix"))
                                {
                                    if (changeLOFcount == 2)
                                    {
                                        persistedReservation.Reshop.FsrSubHeading = "Roundtrip";
                                    }
                                    else
                                    {
                                        persistedReservation.Reshop.FsrSubHeading = "Multicity";
                                    }
                                }
                                else
                                {
                                    persistedReservation.Reshop.FsrSubHeading = "Roundtrip";
                                }
                                await _sessionHelperService.SaveSession<Reservation>(persistedReservation, request.SessionId, new List<string>() { request.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName);
                            }
                        }
                    }
                }
                catch { }
            }

            return reshopResponse;
        }

        private async Task<(bool response, MOBSHOPShopRequest request)> GetEmployeeDiscountId(MOBSHOPShopRequest request)
        {
            United.Service.Presentation.ReservationResponseModel.ReservationDetail pNRByRecordLocatorResponse = new United.Service.Presentation.ReservationResponseModel.ReservationDetail();
            pNRByRecordLocatorResponse = await _sessionHelperService.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(request.SessionId, pNRByRecordLocatorResponse.GetType().FullName, new List<string> { request.SessionId, pNRByRecordLocatorResponse.GetType().FullName }).ConfigureAwait(false);

            if (pNRByRecordLocatorResponse == null || pNRByRecordLocatorResponse.Detail == null)
                return (false, request);
            if (pNRByRecordLocatorResponse.Detail.Prices == null)
                return (false, request);
            foreach (United.Service.Presentation.PriceModel.Price price in pNRByRecordLocatorResponse.Detail.Prices)
            {
                if (price.PassengerTypeCode == "EMP")
                {
                    request.EmployeeDiscountId = price.PassengerTypeCode;
                    return (true, request);
                }
            }
            return (false, request);
        }
    }
}
