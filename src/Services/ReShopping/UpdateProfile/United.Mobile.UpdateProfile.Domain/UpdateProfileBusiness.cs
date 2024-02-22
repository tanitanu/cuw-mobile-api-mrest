using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.Shopping;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.UpdateProfile.Domain
{
    public class UpdateProfileBusiness : IUpdateProfileBusiness
    {
        private readonly ICacheLog<UpdateProfileBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ITravelerUtility _travelerUtility;
       private readonly IFormsOfPayment _formsOfPayment;
        private readonly ICachingService _cachingService;

        public UpdateProfileBusiness(ICacheLog<UpdateProfileBusiness> logger
            , IConfiguration configuration
            , IShoppingSessionHelper shoppingSessionHelper
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , ITravelerUtility travelerUtility
            , IFormsOfPayment formsOfPayment
            , ICachingService cachingService)
        {
            _logger = logger;
            _configuration = configuration;
            _shoppingSessionHelper = shoppingSessionHelper;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _formsOfPayment = formsOfPayment;
            _travelerUtility = travelerUtility;
            _cachingService = cachingService;
        }

        public async Task<MOBChangeEmailResponse> ReshopSaveEmail_CFOP(MOBChangeEmailRequest request)
        {
            MOBChangeEmailResponse response = new MOBChangeEmailResponse();
            response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);

            Reservation persistedReservation = new Reservation();
            persistedReservation =  await _sessionHelperService.GetSession<Reservation>(Headers.ContextValues.SessionId, persistedReservation.ObjectName, new List<string>() { Headers.ContextValues.SessionId, persistedReservation.ObjectName });
            if (persistedReservation.ReservationEmail != null)
            {
                await _sessionHelperService.SaveSession<MOBEmail>(persistedReservation.ReservationEmail, Headers.ContextValues.SessionId, new List<string>() { Headers.ContextValues.SessionId, persistedReservation.ReservationEmail.ToString() }, persistedReservation.ReservationEmail.ToString());
            }
            persistedReservation.ReservationEmail = request.MobEmail;
            persistedReservation.ReservationEmail.EmailAddress = persistedReservation.ReservationEmail.EmailAddress.ToLower();
            await _sessionHelperService.SaveSession<Reservation>(persistedReservation, Headers.ContextValues.SessionId, new List<string>() { Headers.ContextValues.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName);
            Session session = null;
            session = _shoppingSessionHelper.GetValidateSession(request.SessionId, false, true);
            session.Flow = request.Flow;
            response.Flow = request.Flow;
            response.Reservation = _shoppingUtility.GetReservationFromPersist(response.Reservation, request.SessionId).Result;
            response.SessionId = request.SessionId;

            MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, ObjectNames.MOBShoppingCart, new List<string>() { session.SessionId, ObjectNames.MOBShoppingCart });

            if (persistedReservation != null)
            {
                bool isDefault = false;
                response.EligibleFormofPayments = _formsOfPayment.GetEligibleFormofPayments(request, session, persistShoppingCart, request.CartId, request.Flow, ref isDefault);
                persistShoppingCart.Flow = request.Flow;
                _travelerUtility.ReservationToShoppingCart_DataMigration(response.Reservation, ref persistShoppingCart, request);
                response.ShoppingCart = persistShoppingCart;
                await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, Headers.ContextValues.SessionId, new List<string>() { Headers.ContextValues.SessionId, ObjectNames.MOBShoppingCart }, ObjectNames.MOBShoppingCart);
            }

            return response;
        }
    }
}
