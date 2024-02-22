using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Travelers;
using MOBFOPTravelBankDetails = United.Mobile.Model.Shopping.FormofPayment.MOBFOPTravelBankDetails;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Common.Helper.Traveler
{
    public class TravelBank
    {
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ITravelerUtility _travelerUtility;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly ICachingService _cachingService;
        private readonly IDPService _dPService;
        private readonly IHeaders _headers;

        public TravelBank(IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , ITravelerUtility travelerUtility
            , IFFCShoppingcs fFCShoppingcs
            , IShoppingUtility shoppingUtility            
            , ICachingService cachingService
            , IDPService dPService
            , IPKDispenserService pKDispenserService
            , IHeaders headers)
        {
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _travelerUtility = travelerUtility;
            _fFCShoppingcs = fFCShoppingcs;
            _shoppingUtility = shoppingUtility;
            _cachingService = cachingService;
            _dPService = dPService;
            _pKDispenserService = pKDispenserService;
            _headers = headers;
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, _headers); ;
        }
        public async Task<MOBFOPTravelBankDetails> PopulateTravelBankData(Session session, MOBSHOPReservation reservation, MOBRequest request)
        {
            MOBFOPTravelBankDetails travelBankDetails = null;
            if (reservation != null && reservation.IsSignedInWithMP)
            {
                double travelBankBalance = await _travelerUtility.GetTravelBankBalance(session.SessionId);
                if (travelBankBalance > 0)
                {
                    MOBCPTraveler mobCPTraveler = await _travelerUtility.GetProfileOwnerTravelerCSL(session.SessionId);
                    travelBankDetails = new MOBFOPTravelBankDetails
                    {
                        ApplyTBContentMessage = await GetTBContentMessages(session, request),
                        TBBalance = travelBankBalance,
                        DisplayTBBalance = (travelBankBalance).ToString("C2", CultureInfo.CurrentCulture),
                        TBApplied = 0,
                        DisplaytbApplied = "$0.00",
                        RemainingBalance = travelBankBalance,
                        DisplayRemainingBalance = (travelBankBalance).ToString("C2", CultureInfo.CurrentCulture),
                        DisplayAvailableBalanceAsOfDate = $"{"Balance as of "}{ DateTime.Now.ToString("MM/dd/yyyy") }",
                        PayorFirstName = mobCPTraveler.FirstName,
                        PayorLastName = mobCPTraveler.LastName,
                        MPNumber = mobCPTraveler.MileagePlus.MileagePlusId
                        //TravelBanks = new List<MOBFOPTravelBank> { }
                    };
                }
            }
            return travelBankDetails;
        }

        private async Task<List<MOBMobileCMSContentMessages>> GetTBContentMessages(Session session, MOBRequest request)
        {
            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            return SwapTitleAndLocation(GetSDLContentMessages(lstMessages, "RTI.TravelBank.Apply"));
        }

        public static List<MOBMobileCMSContentMessages> SwapTitleAndLocation(List<MOBMobileCMSContentMessages> cmsList)
        {
            foreach (var item in cmsList)
            {
                string location = item.LocationCode;
                item.LocationCode = item.Title;
                item.Title = location;
            }
            return cmsList;
        }
        public static List<MOBMobileCMSContentMessages> GetSDLContentMessages(List<CMSContentMessage> lstMessages, string title)
        {
            List<MOBMobileCMSContentMessages> messages = new List<MOBMobileCMSContentMessages>();
            messages.AddRange(ShopStaticUtility.GetSDLMessageFromList(lstMessages, title));
            return messages;
        }

        public async Task<FOPResponse> TravelBankCredit(Session session, MOBFOPTravelerBankRequest request, bool isCustomerCall)
        {
            var bookingPathReservation = new Reservation();
            var tupleResponse = await _travelerUtility.LoadBasicFOPResponse(session, bookingPathReservation);
            FOPResponse response = tupleResponse.Item1;
            bookingPathReservation = tupleResponse.bookingPathReservation;
            response.Flow = request.Flow;

            var travelBankDetails = response.ShoppingCart.FormofPaymentDetails.TravelBankDetails;
            if (travelBankDetails == null)
            {
                travelBankDetails = new MOBFOPTravelBankDetails();
            }
            var scRESProduct = response.ShoppingCart.Products.Find(p => p.Code == "RES");
            double prodValue = Convert.ToDouble(scRESProduct.ProdTotalPrice);
            prodValue = Math.Round(prodValue, 2, MidpointRounding.AwayFromZero);

            travelBankDetails.TBBalance = await _travelerUtility.GetTravelBankBalance(session.SessionId);
            travelBankDetails.DisplayTBBalance = (travelBankDetails.TBBalance).ToString("C2", CultureInfo.CurrentCulture);
            travelBankDetails.TBApplied = request.AppliedAmount > prodValue ? prodValue : request.AppliedAmount;
            travelBankDetails.TBApplied = request.IsRemove ? 0 : travelBankDetails.TBApplied;
            if (isCustomerCall)
                travelBankDetails.TBAppliedByCustomer = travelBankDetails.TBApplied;
            else if (travelBankDetails.TBAppliedByCustomer > travelBankDetails.TBApplied && travelBankDetails.TBAppliedByCustomer <= prodValue)
                travelBankDetails.TBApplied = travelBankDetails.TBAppliedByCustomer;

            travelBankDetails.DisplaytbApplied = (travelBankDetails.TBApplied).ToString("C2", CultureInfo.CurrentCulture);
            travelBankDetails.RemainingBalance = travelBankDetails.TBBalance > 0 ? travelBankDetails.TBBalance - travelBankDetails.TBApplied : 0;
            travelBankDetails.DisplayRemainingBalance = (travelBankDetails.RemainingBalance).ToString("C2", CultureInfo.CurrentCulture);
            travelBankDetails.DisplayAvailableBalanceAsOfDate = $"{"Balance as of "}{ DateTime.Now.ToString("MM/dd/yyyy") }";

            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            travelBankDetails.ApplyTBContentMessage = GetSDLContentMessages(lstMessages, "RTI.TravelBank.Apply");
            //travelBankDetails.ReviewTBContentMessage = GetSDLContentMessages(lstMessages, "RTI.TravelBank.Review");
            travelBankDetails.LearnmoreTermsandConditions = GetSDLContentMessages(lstMessages, "RTI.TravelBank.Learnmore");
            SwapTitleAndLocation(travelBankDetails.ApplyTBContentMessage);
            //SwapTitleAndLocation(travelBankDetails.ReviewTBContentMessage);
            SwapTitleAndLocation(travelBankDetails.LearnmoreTermsandConditions);
            UpdateCertificateAmountInTotalPrices(bookingPathReservation.Prices, (request.IsRemove ? 0 : travelBankDetails.TBApplied), "TB", "TravelBank cash");
            response.Reservation.Prices = bookingPathReservation.Prices;

            _shoppingUtility.AssignIsOtherFOPRequired(response.ShoppingCart.FormofPaymentDetails, response.Reservation.Prices);
            AssignFormOfPaymentType(response.ShoppingCart.FormofPaymentDetails, response.Reservation.Prices, response.ShoppingCart.FormofPaymentDetails.IsOtherFOPRequired, travelBankDetails.TBApplied, MOBFormofPayment.TB);
            response.ShoppingCart.FormofPaymentDetails.TravelBankDetails = travelBankDetails;
            response.PkDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major, request.DeviceId, session.SessionId, session.Token,session.CatalogItems);
            await _sessionHelperService.SaveSession(bookingPathReservation, session.SessionId, new List<string> { session.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
            await _sessionHelperService.SaveSession(response.ShoppingCart, session.SessionId, new List<string> { session.SessionId, response.ShoppingCart.ObjectName }, response.ShoppingCart.ObjectName).ConfigureAwait(false);

            return response;
        }

        public void UpdateCertificateAmountInTotalPrices(List<MOBSHOPPrice> prices, double certificateTotalAmount, string fopType, string fopDescription)
        {
            var ffcPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == fopType);
            var grandtotal = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "GRAND TOTAL");

            if (ffcPrice == null && certificateTotalAmount > 0)
            {
                ffcPrice = new MOBSHOPPrice();
                prices.Add(ffcPrice);
            }
            else if (ffcPrice != null)
            {
                ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(grandtotal, ffcPrice.Value, false);
            }

            if (certificateTotalAmount > 0)
            {
                _fFCShoppingcs.UpdateCertificatePrice(ffcPrice, certificateTotalAmount, fopType, fopDescription, isAddNegative: true);
                ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(grandtotal, certificateTotalAmount);
            }
            else
            {
                prices.Remove(ffcPrice);
            }
        }
        public void AssignFormOfPaymentType(MOBFormofPaymentDetails formofPaymentDetails, List<MOBSHOPPrice> prices, bool isOtherFOPRequired, double fopAmount, MOBFormofPayment fopPayment)
        {
            //need to update only when TravelFutureFlightCredit is added as formofpayment.          
            if (fopAmount > 0)
            {
                if (!isOtherFOPRequired)
                {
                    formofPaymentDetails.FormOfPaymentType = fopPayment.ToString();
                    if (!string.IsNullOrEmpty(formofPaymentDetails.CreditCard?.Message) &&
                        _configuration.GetValue<string>("CreditCardDateExpiredMessage").IndexOf(formofPaymentDetails.CreditCard?.Message, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        formofPaymentDetails.CreditCard = null;
                    }
                }
                else
                {
                    formofPaymentDetails.FormOfPaymentType = MOBFormofPayment.CreditCard.ToString();
                }
            }
        }
    }
}