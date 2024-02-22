using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.Shopping;
using United.Utility.Helper;
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
        private readonly IHeaders _headers;

        public UpdateProfileBusiness(ICacheLog<UpdateProfileBusiness> logger
            , IConfiguration configuration
            , IShoppingSessionHelper shoppingSessionHelper
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , ITravelerUtility travelerUtility
            , IFormsOfPayment formsOfPayment
            , ICachingService cachingService
            , IHeaders headers
            )
        {
            _logger = logger;
            _configuration = configuration;
            _shoppingSessionHelper = shoppingSessionHelper;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _formsOfPayment = formsOfPayment;
            _travelerUtility = travelerUtility;
            _cachingService = cachingService;
            _headers = headers;
        }

        public async Task<MOBChangeEmailResponse> ReshopSaveEmail_CFOP(MOBChangeEmailRequest request)
        {
            MOBChangeEmailResponse response = new MOBChangeEmailResponse();
            response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);

            Reservation persistedReservation = new Reservation();
            persistedReservation = await _sessionHelperService.GetSession<Reservation>(_headers.ContextValues.SessionId, persistedReservation.ObjectName, new List<string>() { _headers.ContextValues.SessionId, persistedReservation.ObjectName });
            if (persistedReservation.ReservationEmail != null)
            {
                await _sessionHelperService.SaveSession<MOBEmail>(persistedReservation.ReservationEmail, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, persistedReservation.ReservationEmail.ToString() }, persistedReservation.ReservationEmail.ToString());
            }
            persistedReservation.ReservationEmail = request.MobEmail;
            persistedReservation.ReservationEmail.EmailAddress = persistedReservation.ReservationEmail.EmailAddress.ToLower();
            await _sessionHelperService.SaveSession<Reservation>(persistedReservation, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName);
            Session session = null;
            session = await _shoppingSessionHelper.GetValidateSession(request.SessionId, false, true);
            session.Flow = request.Flow;
            response.Flow = request.Flow;
            response.Reservation =await _shoppingUtility.GetReservationFromPersist(response.Reservation, request.SessionId).ConfigureAwait(false);
            response.SessionId = request.SessionId;

            MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, persistShoppingCart.ObjectName, new List<string>() { session.SessionId, persistShoppingCart.ObjectName });

            if (persistedReservation != null)
            {
                bool isDefault = false;
                var tupleRes = await _formsOfPayment.GetEligibleFormofPayments(request, session, persistShoppingCart, request.CartId, request.Flow, isDefault).ConfigureAwait(false);
                response.EligibleFormofPayments = tupleRes.response;
                isDefault = tupleRes.isDefault;
                persistShoppingCart.Flow = request.Flow;
                persistShoppingCart= await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Reservation, persistShoppingCart, request);
                response.ShoppingCart = persistShoppingCart;
                await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName);
            }
            
            return response;
        }

        public async Task<MOBSHOPReservationResponse> ReshopSaveAddress_CFOP(MOBChangeAddressRequest request)
        {
            //controllerUtility.StartWatchAndResetLogEntries();
            MOBSHOPReservationResponse response = new MOBSHOPReservationResponse();
            response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
            //try
            //{
            Reservation persistedReservation = new Reservation();
            //persistedReservation = Persist.FilePersist.Load<Reservation>(request.SessionId, persistedReservation.ObjectName);
            persistedReservation = await _sessionHelperService.GetSession<Reservation>(_headers.ContextValues.SessionId, persistedReservation.ObjectName, new List<string>() { _headers.ContextValues.SessionId, persistedReservation.ObjectName });

            persistedReservation.ReservationEmail = request.MobEmail;
            persistedReservation.ReservationEmail.EmailAddress = persistedReservation.ReservationEmail.EmailAddress.ToLower();
            if (persistedReservation.Reshop != null)
            {
                //if (_traceSwitch.TraceInfo)
                //{
                //    reShopping.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBChangeAddressRequest>(request.SessionId, "ReshopSaveAddress_CFOP", "Request - " + persistedReservation.Reshop.RecordLocator, request.Application.Id, request.Application.Version.Major, request.DeviceId, request, true, false));
                //}
                persistedReservation.Reshop.RefundAddress = request.MobAddress;
                persistedReservation.Reshop.RefundAddress.State.Code = !string.IsNullOrEmpty(request.MobAddress.State.Code) ? request.MobAddress.State.Code : request.MobAddress.State.Name;
            }
            //FilePersist.Save<Reservation>(request.SessionId, persistedReservation.ObjectName, persistedReservation);
            await _sessionHelperService.SaveSession<Reservation>(persistedReservation, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName);
            response.Reservation = await _shoppingUtility.GetReservationFromPersist(response.Reservation, request.SessionId);
            Session session = null;
            session = await _shoppingSessionHelper.GetValidateSession(request.SessionId, false, true);
            MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
            //persistShoppingCart = Persist.FilePersist.Load<MOBShoppingCart>(request.SessionId, persistShoppingCart.GetType().ToString());
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(_headers.ContextValues.SessionId, persistShoppingCart.ObjectName, new List<string>() { _headers.ContextValues.SessionId, persistShoppingCart.ObjectName});


            if (persistedReservation != null)
            {
                bool isDefault = false;
                var tupleRes =await _formsOfPayment.GetEligibleFormofPayments(request, session, persistShoppingCart, request.CartId, request.Flow, isDefault).ConfigureAwait(false);
                response.EligibleFormofPayments = tupleRes.response;
                isDefault = tupleRes.isDefault;
                persistShoppingCart.Flow = request.Flow;
                persistShoppingCart = await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Reservation, persistShoppingCart, request);
                response.ShoppingCart = persistShoppingCart;
                //United.Persist.FilePersist.Save<MOBShoppingCart>(request.SessionId, persistShoppingCart.GetType().ToString(), persistShoppingCart);
                await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, _headers.ContextValues.SessionId, new List<string>() { _headers.ContextValues.SessionId, persistShoppingCart.ObjectName}, persistShoppingCart.ObjectName);
            }

            //}
            //catch (United.Definition.MOBUnitedException uaex)
            //{
            //    response.Exception = controllerUtility.MOBUnitedExceptionHandler(request.SessionId, request.DeviceId, "ReshopSaveAddress_CFOP", request.Application, uaex);
            //}
            //catch (System.Exception ex)
            //{
            //    response.Exception = controllerUtility.SystemExceptionHandeler(request.SessionId, request.DeviceId, "ReshopSaveAddress_CFOP", request.Application, ex);
            //}

            //controllerUtility.StopWatchAndWriteLogs<MOBSHOPReservationResponse>(request.SessionId, request.DeviceId, "ReshopSaveAddress_CFOP", response, request.Application);
            return response;
        }
    }
}
