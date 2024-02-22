using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.MoneyPlusMiles;
using United.Services.FlightShopping.Common;
using United.Services.FS.MoneyAndMiles.Models.Dto;
using United.Utility.Helper;
using United.Utility.HttpService;

namespace United.Mobile.DataAccess.Shopping
{
    public class MileagePricingService : IMileagePricingService
    {
        private readonly ICacheLog<MileagePricingService> _logger;
        private  IConfiguration _configuration;
        private  IHeaders _headers;
        private  ISessionHelperService _sessionHelperService;
        private  ICachingService _cachingService;
        private  IDPService _dPService;
        private  IFlightShoppingBaseClient _flightShoppingBaseClientService;
        private  IFeatureSettings _featureSettings;
        private readonly IShoppingClientService _shoppingClientService;

        public MileagePricingService()
        {

        }

        public MileagePricingService(ICacheLog<MileagePricingService> logger, IConfiguration configuration
            , IHeaders headers
            , ISessionHelperService sessionHelperService
            , ICachingService cachingService
            , IDPService dPService
            , IFlightShoppingBaseClient flightShoppingBaseClientService
            , IFeatureSettings featureSettings
            , IShoppingClientService shoppingClientService)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _sessionHelperService = sessionHelperService;
            _cachingService = cachingService;
            _flightShoppingBaseClientService = flightShoppingBaseClientService;
            _featureSettings = featureSettings;
            _shoppingClientService = shoppingClientService;
        }
        
        public async Task<MOBMoneyPlusMilesOptionsResponse> GetMoneyPlusMilesOptions(Session session, MOBMoneyPlusMilesOptionsRequest request)
        {
            MOBMoneyPlusMilesOptionsResponse mOBMoneyPlusMilesOptionsResponse = new MOBMoneyPlusMilesOptionsResponse();
            string cslActionName = string.Format("{0}/{1}", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLShoppingMoneyAndMiles"), _configuration.GetValue<string>("CSLShoppingMoneyAndMilesOptions"));
            CultureInfo ci = null;
            GetMoneyAndMilesRequest cslRequest = new GetMoneyAndMilesRequest
            { 
                CartId = request.CartId,
                LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson(),
                MileageBalance = Convert.ToInt32(request.MileageBalance)
            };
            
                cslRequest.LoyaltyPerson.LoyaltyProgramMemberID = request.MileagePlusAccountNumber;
            if (!string.IsNullOrEmpty(request.MileagePlusAccountNumber))
            {
                if (request.PremierStatusLevel == -1)
                {
                    request.PremierStatusLevel = 0;// General Member
                }
                cslRequest.LoyaltyPerson.LoyaltyProgramMemberTierLevel = (Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel)request.PremierStatusLevel;
                cslRequest.LoyaltyPerson.AccountBalances = new Collection<Service.Presentation.CommonModel.LoyaltyAccountBalance>();
                Service.Presentation.CommonModel.LoyaltyAccountBalance balance = new Service.Presentation.CommonModel.LoyaltyAccountBalance();
                int.TryParse(request.MileageBalance, out int bal);
                balance.Balance = bal;
                balance.BalanceType = Service.Presentation.CommonEnumModel.LoyaltyAccountBalanceType.MilesBalance;
                cslRequest.LoyaltyPerson.AccountBalances.Add(balance);
            }
            string jsonRequest = JsonConvert.SerializeObject(cslRequest);

            GetMoneyAndMilesResponse cslResponse = new GetMoneyAndMilesResponse();
            cslResponse = await _flightShoppingBaseClientService.PostAsync<GetMoneyAndMilesResponse>(session.Token, session.SessionId, cslActionName, jsonRequest);


            if (cslResponse != null)
            {
                List<MOBMoneyPlusMilesShopFlightDetails> flights = new List<MOBMoneyPlusMilesShopFlightDetails>();

                if (cslResponse.MoneyAndMilesOptions?.Count > 0)
                {
                    MOBMoneyPlusMilesShopFlightDetails flight1;
                    foreach (var moneyAndMilesOption in cslResponse.MoneyAndMilesOptions)
                    {
                        if (await _featureSettings.GetFeatureSettingValue("EnableMoneyPlusMilesFastFollower").ConfigureAwait(false))
                        {
                            ci = TopHelper.GetCultureInfo(moneyAndMilesOption.Options?.FirstOrDefault()?.Prices?.FirstOrDefault(c => c.PricingType.ToUpper() == "FARE")?.Currency);
                        }
                        flight1  = new MOBMoneyPlusMilesShopFlightDetails();
                        flight1.FlightHash = moneyAndMilesOption.FlightHash;
                        List<MOBMoneyPlusMilesShopProduct> mOBMoneyPlusMilesShopProducts = new List<MOBMoneyPlusMilesShopProduct>();

                        foreach (var option in moneyAndMilesOption.Options)
                        {
                            if (await _featureSettings.GetFeatureSettingValue("EnableMoneyPlusMilesFastFollower").ConfigureAwait(false))
                            {
                                mOBMoneyPlusMilesShopProducts.Add(
                                  new MOBMoneyPlusMilesShopProduct
                                  {
                                      ProductId = option.ProductId,
                                      MoneyPlusMilesOptionId = option.OptionId,
                                      Price = TopHelper.FormatAmountForDisplay(option.Prices.FirstOrDefault(c => c.PricingType.ToUpper() == "FARE").Amount.ToString(), ci, true, false),
                                      MilesDisplayValue = "+" + FormatAwardAmountForDisplay(option.Prices.FirstOrDefault(c => c.PricingType.ToUpper() == "MILES").Amount.ToString(), true)
                                  });
                            }
                            else
                            {
                            mOBMoneyPlusMilesShopProducts.Add(
                                new MOBMoneyPlusMilesShopProduct
                                {
                                    ProductId = option.ProductId,
                                    MoneyPlusMilesOptionId = option.OptionId,
                                    Price = "$" + option.Prices.FirstOrDefault(c => c.PricingType.ToUpper() == "FARE").Amount.ToString(),
                                    MilesDisplayValue = "+" + FormatAwardAmountForDisplay(option.Prices.FirstOrDefault(c => c.PricingType.ToUpper() == "MILES").Amount.ToString(), true)
                                });
                            }

                        }
                        flight1.Products = mOBMoneyPlusMilesShopProducts;
                        flights.Add(flight1);
                    }
                    mOBMoneyPlusMilesOptionsResponse.Flights = flights;
                }
                if (cslResponse.Errors?.Count > 0)
                {
                    if (await _featureSettings.GetFeatureSettingValue("EnableMoneyPlusMilesFastFollower").ConfigureAwait(false))
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in cslResponse.Errors)
                        {
                            errorMessage = errorMessage + " " + error.ErrorMessage;
                            if (!string.IsNullOrEmpty(error.ErrorCode) && error.ErrorCode.Trim().Equals("63211"))
                                throw new MOBUnitedException(_configuration.GetValue<string>("FSRMoneyPlusMilesLowMilesMessage").ToString());
                        }
                        throw new System.Exception(errorMessage);
                    }
                }
            }
            else
            {
              //  throw new MOBUnitedException(ConfigurationManager.AppSettings["Booking2OGenericExceptionMessage"]);
            }
            return mOBMoneyPlusMilesOptionsResponse;
        }

        public async Task<T> GetCSLMoneyAndMilesFareWheel<T>(Session session, MOBSHOPShopRequest shopRequest, ShopRequest request)
        {
            MoneyAndMilesFareWheelRequest moneyAndMilesFareWheelRequest = new MoneyAndMilesFareWheelRequest
            {
                CartId = session.CartId,
                LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson { LoyaltyProgramMemberID = request.LoyaltyPerson?.LoyaltyProgramMemberID },
                MileageBalance = (int)request.LoyaltyPerson?.AccountBalances?.FirstOrDefault()?.Balance
            };
            string cslActionName = string.Format("{0}/{1}", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLShoppingMoneyAndMiles"), _configuration.GetValue<string>("CSLShoppingMoneyAndMilesFareWheel"));
            string calendarJsonRequest = JsonConvert.SerializeObject(moneyAndMilesFareWheelRequest);
            T calendarResponse = await _flightShoppingBaseClientService.PostAsync<T>(session.Token, session.SessionId, cslActionName, calendarJsonRequest);

            return calendarResponse;
        }

        private string FormatAwardAmountForDisplay(string amt, bool truncate = true)
        {
            string newAmt = amt;
            decimal amount = 0;
            decimal.TryParse(amt, out amount);

            try
            {
                if (amount > -1)
                {
                    if (truncate)
                    {
                        try
                        {
                            if (amount > 999)
                            {
                                amount = amount / 1000;
                                if (amount % 1 > 0)
                                {
                                    newAmt = string.Format("{0:n1}", amount) + "K miles";
                                }
                                else
                                {
                                    newAmt = string.Format("{0:n0}", amount) + "K miles";
                                }
                            }
                            else
                            {
                                newAmt = string.Format("{0:n0}", amount) + " miles";
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            newAmt = string.Format("{0:n0}", amount) + " miles";
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return newAmt;
        }

        public async Task<MOBFSRMileagePricingResponse> GetApplyMileagePricing(Session session, MOBFSRMileagePricingRequest request)
        {
            MOBFSRMileagePricingResponse mOBFSRMileagePricingResponse = new MOBFSRMileagePricingResponse();
            if (request != null && request.PricingType.Equals(PricingType.ETC.ToString()))
            {
                await ApplyETCTravelCredits(session, request, mOBFSRMileagePricingResponse);
            }
            return mOBFSRMileagePricingResponse;
        }

        private async Task ApplyETCTravelCredits(Session session, MOBFSRMileagePricingRequest request, MOBFSRMileagePricingResponse mOBFSRMileagePricingResponse)
        {
            string cslActionName = "/ApplyTravelCredits";
            CultureInfo ci = TopHelper.GetCultureInfo("en-US");
            TravelCreditsRequest cslRequest = new TravelCreditsRequest
            {
                CartId = request.CartId,
                TravelCreditDetails = new TravelCreditDetails
                {
                    LoyaltyId = request.MileagePlusAccountNumber,
                    TotalCreditsToBeApplied = request.MileageBalance,
                    TravelCertificates = new List<Services.FlightShopping.Common.TravelCertificate>()
                }                
            };

            try
            {
                string jsonRequest = JsonConvert.SerializeObject(cslRequest);

                TravelCreditsResponse cslResponse = new TravelCreditsResponse();
                cslResponse = await _shoppingClientService.PostAsync<TravelCreditsResponse>(session.Token, session.SessionId, cslActionName, cslRequest);


                if (cslResponse != null)
                {
                    List<MOBFSRMileagePricingShopFlightDetails> flights = new List<MOBFSRMileagePricingShopFlightDetails>();

                    if (cslResponse.FlightDetails?.Count > 0)
                    {
                        MOBFSRMileagePricingShopFlightDetails flight1;
                        foreach (var flightDetails in cslResponse.FlightDetails)
                        {
                            //ci = TopHelper.GetCultureInfo(flightDetails.Options?.FirstOrDefault()?.Prices?.FirstOrDefault(c => c.PricingType.ToUpper() == "FARE")?.Currency);
                            flight1 = new MOBFSRMileagePricingShopFlightDetails();
                            flight1.FlightHash = flightDetails.FlightHash;
                            List<MOBFSRMileagePricingShopProduct> mOBFSRMileagePricingShopProduct = new List<MOBFSRMileagePricingShopProduct>();

                            foreach (var option in flightDetails.ProductPrices)
                            {
                                if(option != null && option.ProductId != null && option.DisplayPrice != null && option.StrikeThroughPrice != null) 
                                { 
                                    mOBFSRMileagePricingShopProduct.Add(
                                    new MOBFSRMileagePricingShopProduct
                                    {
                                        ProductId = option.ProductId,
                                        Price = TopHelper.FormatAmountForDisplay(option.StrikeThroughPrice.ToString(), ci, true, false),
                                        DisplayValue = TopHelper.FormatAmountForDisplay(option.DisplayPrice.ToString(), ci, true, false)
                                    });
                                }
                            }
                            flight1.Products = mOBFSRMileagePricingShopProduct;
                            flights.Add(flight1);
                        }
                        mOBFSRMileagePricingResponse.Flights = flights;
                    }
                    if (cslResponse.Errors?.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in cslResponse.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10036"))
                                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage").ToString());
                        }
                        throw new System.Exception(errorMessage);
                    }
                }
                else
                {
                    //  throw new MOBUnitedException(ConfigurationManager.AppSettings["Booking2OGenericExceptionMessage"]);
                }
            }
            catch (Exception ex)
            {
                throw new System.Exception(ex.Message);
            }
        }
    }
}
