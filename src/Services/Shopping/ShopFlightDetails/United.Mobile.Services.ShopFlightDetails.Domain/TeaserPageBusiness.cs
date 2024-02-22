using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.TeaserPage;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.TeaserPage;
using United.Services.FlightShopping.Common;
using United.Utility.Helper;
using StatusType = United.Services.FlightShopping.Common.StatusType;
using TeaserText = United.Services.FlightShopping.Common.TeaserText;

namespace United.Mobile.Services.ShopFlightDetails.Domain
{
    public class TeaserPageBusiness : ITeaserPageBusiness
    {
        private readonly ICacheLog<TeaserPageBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IHeaders _headers;
        private readonly IDPService _dPService;
        private readonly IGetTeaserColumnInfoService _getTeaserColumnInfoService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private HttpContext _httpContext;

        public TeaserPageBusiness(ICacheLog<TeaserPageBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IDPService dPService
            , IGetTeaserColumnInfoService getTeaserColumnInfoService
            , IShoppingUtility shoppingUtility
            , IFFCShoppingcs fFCShoppingcs)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _dPService = dPService;
            _getTeaserColumnInfoService = getTeaserColumnInfoService;
            _shoppingUtility = shoppingUtility;
            _fFCShoppingcs = fFCShoppingcs;

        }
        public async Task<MOBSHOPShoppingTeaserPageResponse> GetTeaserPage(MOBSHOPShoppingTeaserPageRequest request, HttpContext httpContext)
        {
            _httpContext = httpContext;
            MOBSHOPShoppingTeaserPageResponse response = new MOBSHOPShoppingTeaserPageResponse();
            if (!string.IsNullOrEmpty(request.CartID))
            {
                response = await GetTeaserColumnInfo(request);
            }
            return await Task.FromResult(response);


        }
        private async Task<MOBSHOPShoppingTeaserPageResponse> GetTeaserColumnInfo(MOBSHOPShoppingTeaserPageRequest request)
        {
            MOBSHOPShoppingTeaserPageResponse response = new MOBSHOPShoppingTeaserPageResponse();

            //string url = $"{ _flightShoppingServiceBaseUrl}/{getTeaserTextActionName}?cartId={request.CartID}&langCode={request.LanguageCode}&countryCode=US";
            //https://csmc.stage.api.united.com/8.0/flight/flightshopping/api/GetTeaserText?cartId=E9374435-6E37-4CE2-8E5E-FBED40C18169&langCode=en-US&countryCode=US

            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(request.SessionID, new Session().ObjectName, new List<string> { request.SessionID, new Session().ObjectName }).ConfigureAwait(false);

            if (session == null)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("BookingSessionExpiryMessageExceptionCode"), _configuration.GetValue<string>("BookingSessionExpiryMessage").ToString());
            }
            
            var countryCode = "US";
            var jsonResponse = await _getTeaserColumnInfoService.GetTeaserText<FareColumnContentInformationResponse>(session.Token, request.CartID, request.LanguageCode, countryCode, request.SessionID).ConfigureAwait(false);

            if (jsonResponse != null)
            {
                if (jsonResponse != null && jsonResponse.Status == StatusType.Success && jsonResponse.Errors == null
                    && jsonResponse.ColumnInformation != null && jsonResponse.ColumnInformation.Columns != null &&
                    jsonResponse.ColumnInformation.Columns.Any())
                {
                    response.Columns = await PopulateColumnsTeaserDetails(jsonResponse.ColumnInformation, session.SessionId);
                    if (_configuration.GetValue<bool>("EnableFooterDisclaimerMessage"))
                    {
                        List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID").ConfigureAwait(false);


                        if (session != null && session.IsAward == true)
                        {
                            response.FooterText = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("CompareFareTypesFooterDisclaimerAward"));
                        }
                        else
                        {
                            response.FooterText = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("CompareFareTypesFooterDisclaimerRevenue"));
                        }
                    }

                    return response;
                }
                else
                {
                    if (jsonResponse != null && jsonResponse.Errors != null)
                    {
                        throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }

                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

        }
        private async Task<List<MOBSHOPShoppingTripFareType>> PopulateColumnsTeaserDetails(ColumnInformation columnInfo, string sessionId)
        {
            List<MOBSHOPShoppingTripFareType> columns = null;
            var columnTypeList = new List<string>();
            if (_configuration.GetValue<bool>("EnableShoppingProductPersist"))
            {
                MOBSHOPShoppingProductList mOBSHOPShoppingProductList = new MOBSHOPShoppingProductList();
                var persistedShoppingProduct = await _sessionHelperService.GetSession<MOBSHOPShoppingProductList>(sessionId, mOBSHOPShoppingProductList.ObjectName, new List<string> { sessionId, mOBSHOPShoppingProductList.ObjectName }).ConfigureAwait(false);

                if (_configuration.GetValue<bool>("EnableTeaserTextFilter") && persistedShoppingProduct != null && persistedShoppingProduct.Columns != null)
                {
                    persistedShoppingProduct.Columns.ForEach(c =>
                    {
                        if (c != null && !string.IsNullOrWhiteSpace(c.ColumnID))
                        {
                            columnTypeList.Add(c.ColumnID);
                        }
                    });
                }
            }
            else
            {
                ShoppingResponse shop = new ShoppingResponse();
                var persistedShop = await _sessionHelperService.GetSession<ShoppingResponse>(sessionId, shop.ObjectName, new List<string> { sessionId, shop.ObjectName }).ConfigureAwait(false);

                if (_configuration.GetValue<bool>("EnableTeaserTextFilter") && persistedShop != null && persistedShop.Response != null
                    && persistedShop.Response.Availability != null && persistedShop.Response.Availability.Trip != null
                    && persistedShop.Response.Availability.Trip.Columns != null && persistedShop.Response.Availability.Trip.Columns.Any())
                {
                    persistedShop.Response.Availability.Trip.Columns.ForEach(c =>
                    {
                        if (c != null && !string.IsNullOrWhiteSpace(c.ColumnID))
                        {
                            columnTypeList.Add(c.ColumnID);
                        }
                    });
                }
            }

            // if we have columns...
            if (columnInfo != null && columnInfo.Columns != null && columnInfo.Columns.Count > 0)
            {
                columns = new List<MOBSHOPShoppingTripFareType>();
                foreach (Column column in columnInfo.Columns)
                {
                    // if we have teaser texts
                    if (column.TeaserTexts != null && column.TeaserTexts.Count > 0 && (columnTypeList.Count == 0 || columnTypeList.Exists(c => c == column.DescriptionId)))
                    {
                        MOBSHOPShoppingTripFareType tripFareType = new MOBSHOPShoppingTripFareType();
                        tripFareType.DataSourceLabel = column.DataSourceLabel;
                        tripFareType.Description = column.Description;
                        tripFareType.TeaserTexts = new List<MOBSHOPTeaserText>();

                        foreach (TeaserText teaser in column.TeaserTexts)
                        {
                            if (teaser.Text.ToUpper().Trim() != "NOTAPPLICABLE")
                            {
                                MOBSHOPTeaserText teaserText = new MOBSHOPTeaserText();
                                teaserText.Text = teaser.Text;
                                teaserText.Icon = GetFormatedUrl(_httpContext.Request.Host.Value,
                                                                 _httpContext.Request.Scheme,
                                                                 $"/images/rule-{teaser.Icon}.png").Replace("http:", "https:");
                                teaserText.LangCode = teaser.LangCode;
                                teaserText.IsPrimary = teaser.IsPrimary;
                                teaserText.SortIndex = teaser.SortIndex;
                                //teaserText.ItemType = teaser.GetType();
                                tripFareType.TeaserTexts.Add(teaserText);
                            }
                        }
                        columns.Add(tripFareType);
                    }
                }
            }
            return columns;
        }
        private static string GetFormatedUrl(string url, string scheme, string relativePath, bool ensureSSL = false)
        {
            var finalURL = $"{scheme}://{url}/shopflightdetailsservice/{relativePath.TrimStart(new[] { '/' })}";
            if (ensureSSL)
            {
                return finalURL.Replace("http:", "https:");
            }
            else
            {
                return finalURL;
            }
        }



    }
}
