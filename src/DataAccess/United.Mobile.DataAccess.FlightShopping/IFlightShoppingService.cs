using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace United.Mobile.DataAccess.FlightShopping
{
    public interface IFlightShoppingService
    {
        Task<(T response, long callDuration)> GetShop<T>(string token, string sessionId, string action, string shopRequest);
        Task<string> GetEconomyEntitlement(string token, string action, string request, string sessionId);
        Task<string> GetFareColumnEntitlements(string token, string action, string request, string sessionId);
        Task<T> ShopFareWheelInfo<T>(string token, string sessionId, string logAction, string requestData);
        Task<T> GetBundles<T>(string token, string sessionId, string logAction, string requestData);
        Task<T> SelectFareWheel<T>(string token, string action, string sessionId, string requestData);
        Task<T> GetOnTimePerformanceInfo<T>(string token, string MarketingCarrier, string FlightNumber, string Origin, string Destination, string DepartureDateTime, string sessionId);
        Task<string> GetProducts(string token, string requestData, string sessionId);
        Task<string> FarePriceRules(string token, string action, string request, string sessionId);
        Task<string> GetColumnInfo(string token, string sessionId, string cartId, string langCode, string countryCode);
        Task<string> OrganizeResults(string token, string requestData, string sessionId);
        Task<string> GetShopPinDown(string token, string action, string request, string sessionId);
        Task<T> GetBasicEconomyEntitlement<T>(string token, string action, string request, string sessionId);
        Task<T> GetAmenitiesInfo<T>(string token, string action, string request, string sessionId);
        Task<T> GetUserSession<T>(string token, string action, string request, string sessionId);
        Task<T> GetBookingDetailsV2<T>(string token, string action, string request, string sessionId);
        Task<T> GetMetaBookingDetails<T>(string token, string action, string request, string sessionId);
        Task<T> SelectTrip<T>(string token, string sessionId, string logAction, string requestData);
        Task<T> AmenitiesForFlights<T>(string token, string sessionId, string action, string requestData);
        Task<T> ShopBookingDetails<T>(string token, string sessionId, string action, string requestData);
        Task<T> MetaSyncUserSession<T>(string token, string sessionId, string action, string requestData);
        Task<(T response, long callDuration)> GetLmxQuote<T>(string token, string sessionId, string cartId, string hashList);
        Task<(T response, long callDuration)> UpdateAmenitiesIndicators<T>(string token, string sessionId, string jsonRequest);
        Task<(T response, long callDuration)> GetAmenitiesForFlights<T>(string token, string sessionId, string jsonRequest);
        Task<(T response, long callDuration)> ShopValidateSpecialPricing<T>(string token, string sessionId, string jsonRequest);
        Task<string> MetaSyncUserSession(string token, string sessionId, string action, string requestData);
        Task<string> GetCartInformation(string token, string action, string request, string sessionId);

        Task<T> FlightCarbonEmission<T>(string token, string sessionId, string logAction, string requestData);
        // Task<T> ShopValidateSpecialPricing<T>(string token, string sessionId, string action, string requestData);

    }
}
