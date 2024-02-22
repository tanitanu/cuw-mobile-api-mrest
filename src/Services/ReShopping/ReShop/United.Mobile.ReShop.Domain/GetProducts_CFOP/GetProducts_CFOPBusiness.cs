using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.Model.Common;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.ProductRequestModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Utility.Helper;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using ProductOffer = United.Service.Presentation.ProductResponseModel.ProductOffer;
using United.Mobile.Model.Internal.Exception;

namespace United.Mobile.ReShop.Domain.GetProducts_CFOP
{
    public class GetProducts_CFOPBusiness : IGetProducts_CFOPBusiness
    {
        private readonly ICacheLog<GetProducts_CFOPBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IDPService _dPService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICachingService _cachingService;
        private readonly IHeaders _headers;
        private readonly IMerchandizingServices _merchandizingServices;
        private readonly IPurchaseMerchandizingService _purchaseMerchandizingService;

        public GetProducts_CFOPBusiness(ICacheLog<GetProducts_CFOPBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IShoppingSessionHelper shoppingSessionHelper
            , IDPService dPService
            , IDynamoDBService dynamoDBService
            , ICachingService cachingService
            , IHeaders headers
            , IMerchandizingServices merchandizingServices
            , IPurchaseMerchandizingService purchaseMerchandizingService)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shoppingSessionHelper = shoppingSessionHelper;
            _dPService = dPService;
            _dynamoDBService = dynamoDBService;
            _headers = headers;
            _cachingService = cachingService;
            _merchandizingServices = merchandizingServices;
            _purchaseMerchandizingService = purchaseMerchandizingService;
        }

        public async Task<MOBSHOPProductSearchResponse> GetProducts_CFOP(MOBSHOPProductSearchRequest request)
        {
            var response = new MOBSHOPProductSearchResponse();
            response.TransactionId = request.TransactionId;
            response.CartId = request.CartId;
            response.SessionId = request.SessionId;
            response.Flow = request.Flow;
            response.LanguageCode = request.LanguageCode;
            if (request != null && !string.IsNullOrEmpty(request.PointOfSale))
            {
                request.PointOfSale = request.PointOfSale.Trim().Length > 2 ? "US" : request.PointOfSale.Trim();
            }
            response.OfferSource = await GetOfferSource_CFOP(request);
            response.OfferSource.ClubDayPassOffer.ProductName = "United Club pass";
            return response;
        }
        public async Task<bool> ValidateEPlusVersion(int applicationID, string appVersion)
        {
            return await System.Threading.Tasks.Task.FromResult(GeneralHelper.IsApplicationVersionGreater(applicationID, appVersion, "AndroidEPlusVersion", "iPhoneEPlusVersion", "", "", true, _configuration));
        }

        private async Task<OfferSource> GetOfferSource_CFOP(MOBSHOPProductSearchRequest request)
        {
            OfferSource offerSource = null;
            try
            {
                if (request != null && request.ProductCodes != null)
                {
                    Session session = new Session();
                    session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);
                    if (session != null && session.IsMeta)
                    {
                        Reservation reservation = new Reservation();
                        reservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, reservation.ObjectName, new List<string> { request.SessionId, reservation.ObjectName }).ConfigureAwait(false);
                        offerSource = new OfferSource();
                        offerSource.OfferHeaderDescription = _configuration.GetValue<string>("OfferHeaderDescription");
                        offerSource.ClubDayPassOffer = await GetClubDayPassOffer_CFOP(request, session);
                    }
                    else
                    {
                        ShoppingResponse shopResponse = new ShoppingResponse();
                        var shoppingResponse = await _sessionHelperService.GetSession<ShoppingResponse>(request.SessionId, shopResponse.ObjectName, new List<string> { request.SessionId, shopResponse.ObjectName }).ConfigureAwait(false);
                        offerSource = new OfferSource();
                        offerSource.OfferHeaderDescription = _configuration.GetValue<string>("OfferHeaderDescription");
                        offerSource.ClubDayPassOffer = await GetClubDayPassOffer_CFOP(request, session);
                    }
                    if (session.IsReshopChange)
                        offerSource.ProductOffer = null;
                }
            }
            catch (MOBUnitedException uaex)
            {
                throw uaex;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            return offerSource;
        }

        private async Task<ClubDayPassOffer> GetClubDayPassOffer_CFOP(MOBSHOPProductSearchRequest request, Session session)
        {
            ClubDayPassOffer clubDayPassOffer = new ClubDayPassOffer();
            try
            {
                var productOffers = new Service.Presentation.ProductResponseModel.ProductOffer();
                productOffers = await GetMerchOffersDetailsforOTP(session, request);
                clubDayPassOffer.ProductCode = productOffers.Offers.FirstOrDefault().ProductInformation.ProductDetails.FirstOrDefault().Product.Code;
                string offerDescriptionHeaderConfigurationKey = "UnitedClubUnitedClubDayPassOfferDescriptionHeader" + request.PointOfSale;
                string offerDescriptionHeader = string.Empty;
                if (request.Application.Id == 2)
                {
                    var documentLibrary = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
                    List<string> docTitles = new List<string>() { offerDescriptionHeaderConfigurationKey };
                    var docs = await documentLibrary.GetNewLegalDocumentsForTitles(docTitles, session.SessionId /* Headers.ContextValues.SessionId*/);

                    if (docs != null && docs.Count > 0 && docs.Find(item => item.Title == offerDescriptionHeaderConfigurationKey) != null)
                    {
                        offerDescriptionHeader = docs.Find(item => item.Title == offerDescriptionHeaderConfigurationKey).LegalDocument;
                    }

                }
                else
                {
                    offerDescriptionHeader = _configuration.GetValue<string>(offerDescriptionHeaderConfigurationKey);
                }
                if (!string.IsNullOrEmpty(offerDescriptionHeader))
                {
                    clubDayPassOffer.OfferDescriptionHeader = offerDescriptionHeader;
                }
                string offerDescriptionConfigurationKey = "UnitedClubUnitedClubDayPassOfferDescription" + request.PointOfSale;
                string offerDescription = _configuration.GetValue<string>(offerDescriptionConfigurationKey);
                if (!string.IsNullOrEmpty(offerDescription))
                {
                    clubDayPassOffer.OfferDescription = offerDescription;
                }

                string descriptionsConfigurationKey = "UnitedClubDayPassDescriptions" + request.PointOfSale;
                string descriptionsString = _configuration.GetValue<string>(descriptionsConfigurationKey);
                if (!string.IsNullOrEmpty(descriptionsString))
                {
                    string[] descriptionsArray = descriptionsString.Split('|');
                    clubDayPassOffer.Descriptions = new List<string>();
                    foreach (var description in descriptionsArray)
                    {
                        clubDayPassOffer.Descriptions.Add(description);
                    }
                }

                string termsAndConditionsConfigurationKey = "UnitedClubUnitedClubDayPassTermsAndConditions" + request.PointOfSale;
                string termsAndConditionsString = _configuration.GetValue<string>(termsAndConditionsConfigurationKey);
                if (!string.IsNullOrEmpty(termsAndConditionsString))
                {
                    string[] termsAndConditionsArray = termsAndConditionsString.Split('|');
                    clubDayPassOffer.TermsAndConditions = new List<string>();
                    foreach (var termAndCondition in termsAndConditionsArray)
                    {
                        clubDayPassOffer.TermsAndConditions.Add(termAndCondition);
                    }
                }
                //TODO
                //Get numbers from merch, will break when price obj has less mapping data                
                string totalBaseFare
                    = String.Format("{0:0.00}", productOffers.Offers.FirstOrDefault().ProductInformation.ProductDetails.FirstOrDefault().Product.SubProducts.FirstOrDefault().Prices.FirstOrDefault().PaymentOptions.FirstOrDefault().PriceComponents.FirstOrDefault().Price.BasePrice.FirstOrDefault().Amount);
                string displayValue
                    = String.Format("{0:0.00}", productOffers.Offers.FirstOrDefault().ProductInformation.ProductDetails.FirstOrDefault().Product.SubProducts.FirstOrDefault().Prices.FirstOrDefault().PaymentOptions.FirstOrDefault().PriceComponents.FirstOrDefault().Price.BasePrice.FirstOrDefault().Amount);

                string priceConfigurationKey = "UnitedClubUnitedClubDayPassRegularPrice" + request.PointOfSale;
                string priceString = _configuration.GetValue<string>(priceConfigurationKey);
                if (!string.IsNullOrEmpty(priceString))
                {
                    if (clubDayPassOffer.Prices == null)
                    {
                        clubDayPassOffer.Prices = new List<MOBSHOPPrice>();
                    }

                    string[] priceStringArray = priceString.Split('|');
                    if (priceStringArray.Length == 4)
                    {
                        MOBSHOPPrice price = new MOBSHOPPrice();
                        price.ProductId = productOffers.Offers.FirstOrDefault().ProductInformation.ProductDetails.FirstOrDefault().Product.SubProducts.FirstOrDefault().Prices.FirstOrDefault().ID;
                        if (price.ProductId == "0")
                        {
                            price.ProductId = Guid.NewGuid().ToString();
                        }
                        price.CurrencyCode = priceStringArray[0];
                        price.TotalBaseFare = totalBaseFare;
                        price.DisplayValue = displayValue;
                        price.PriceType = priceStringArray[3];
                        clubDayPassOffer.Prices.Add(price);
                    }
                }

                string discountdPriceKey = "UnitedClubUnitedClubDayPassDiscountedPrice" + request.PointOfSale;
                string discountdPriceString = _configuration.GetValue<string>(discountdPriceKey);
                if (!string.IsNullOrEmpty(discountdPriceString))
                {
                    if (clubDayPassOffer.Prices == null)
                    {
                        clubDayPassOffer.Prices = new List<MOBSHOPPrice>();
                    }
                    string[] priceStringArray = discountdPriceString.Split('|');
                    if (priceStringArray.Length == 4)
                    {
                        MOBSHOPPrice price = new MOBSHOPPrice();
                        price.ProductId = Guid.NewGuid().ToString();
                        price.CurrencyCode = priceStringArray[0];
                        price.TotalBaseFare = totalBaseFare;
                        price.DisplayValue = displayValue;
                        price.PriceType = priceStringArray[3];
                        clubDayPassOffer.Prices.Add(price);
                    }
                }
            }
            catch (MOBUnitedException uaex)
            {
                throw uaex;
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            finally
            {
                _logger.LogInformation("GetProducts_CFOP {@clubDayPassOffer}", JsonConvert.SerializeObject(clubDayPassOffer));
            }

            return clubDayPassOffer;
        }

        private async Task<ProductOffer> GetMerchOffersDetailsforOTP(Session session, MOBSHOPProductSearchRequest request)
        {
            GetOffers response = null;
            string token = session.Token;
            Reservation reservation = new Reservation();
            reservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, reservation.ObjectName, new List<string> { request.SessionId, reservation.ObjectName }).ConfigureAwait(false);
            var cslrequest = await BuildMerchOffersRequestforOTP(reservation);
            string jsonRequest = JsonConvert.SerializeObject(cslrequest);
            response = (await _purchaseMerchandizingService.GetMerchOfferInfo<GetOffers>(token, "getoffers", jsonRequest, _headers.ContextValues.SessionId).ConfigureAwait(false)).response;

            if (response != null)
            {
                if (response != null && response.Offers != null && response.Response.Error == null)
                {
                    await _sessionHelperService.SaveSession<GetOffers>(response, session.SessionId, new List<string> { session.SessionId, response.ObjectName }, response.ObjectName).ConfigureAwait(false);
                }
                else
                    response = null;
            }
            else
                response = null;

            return response;
        }

        private async Task<Service.Presentation.ProductRequestModel.ProductOffer> BuildMerchOffersRequestforOTP(Reservation reservation)
        {
            var offerRequest = new Service.Presentation.ProductRequestModel.ProductOffer();
            offerRequest.CurrencyCode = reservation.Prices.FirstOrDefault().CurrencyCode;
            List<ReservationFlightSegment> segments = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservation.CSLReservationJSONFormat);
            offerRequest.CountryCode = reservation.PointOfSale;
            offerRequest.TicketingCountryCode = reservation.PointOfSale;
            offerRequest.Requester = Requester();
            offerRequest.Filters = new Collection<ProductFilter>() { new ProductFilter { IsIncluded = "true", ProductCode = "OTP" } };
            offerRequest.AirportCode = segments.FirstOrDefault().FlightSegment.ArrivalAirport.IATACode;
            if (reservation.IsReshopChange)
            {
                ReservationDetail reservationDetail = new ReservationDetail();
                reservationDetail = await _sessionHelperService.GetSession<ReservationDetail>(reservation.SessionId, reservationDetail.GetType().FullName, new List<string> { reservation.SessionId, reservationDetail.GetType().FullName }).ConfigureAwait(false);
                //offerRequest.FlightSegments = _merchandizingServices.ProductFlightSegments(segments.ToCollection(), reservationDetail.Detail.Prices);
            }
            Service.Presentation.ProductModel.ProductTraveler traveler = new Service.Presentation.ProductModel.ProductTraveler();
            var travelers = reservation.TravelersCSL.Select(x => new Service.Presentation.ProductModel.ProductTraveler()
            {
                GivenName = x.Value.FirstName,
                Surname = x.Value.LastName,
                ID = Convert.ToString(1), //Sending Id always as "1".So that merch will echo back same Id all the time in the response.            
                PassengerTypeCode = x.Value.TravelerTypeCode,
                TravelerNameIndex = x.Value.TravelerNameIndex
            }).FirstOrDefault();

            // In implement of CFOP integration with RESHOP, We are hardcodeing travelers ID to get Product offers.
            // After implemention of these feature in RESHOP we can remove it. Traveler->PAXID
            //if (reservation.IsReshopChange)
            //{
            //    travelers.ID = Convert.ToString(1);
            //}
            offerRequest.Travelers = new Collection<Service.Presentation.ProductModel.ProductTraveler>();
            offerRequest.Travelers.Add(travelers);
            return offerRequest;
        }

        private Service.Presentation.CommonModel.ServiceClient Requester()
        {
            return new ServiceClient()
            {
                Requestor = new Requestor()
                {
                    ChannelID = _configuration.GetValue<string>("MerchandizeOffersServiceChannelID"), //1301
                    ChannelName = _configuration.GetValue<string>("MerchandizeOffersServiceChannelName"), //"MMR",
                    LanguageCode = "en"
                }
            };
        }
    }
}
