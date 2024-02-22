using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Payment;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using FormofPaymentOption = United.Mobile.Model.Common.Shopping.FormofPaymentOption;
using MOBShoppingCart = United.Mobile.Model.Shopping.MOBShoppingCart;
using MOBSHOPReservation = United.Mobile.Model.Shopping.MOBSHOPReservation;

namespace United.Common.Helper.FOP
{
    public interface IFormsOfPayment
    {
        Task<(List<FormofPaymentOption> response, bool isDefault)> EligibleFormOfPayments(FOPEligibilityRequest request, Session session, bool isDefault, bool IsMilesFOPEnabled = false, List<LogEntry> eligibleFoplogEntries = null);

        Task<List<ProdDetail>> GetProductDetailsFromCartID(Session session, SelectTripRequest request);

        Task<MOBFOPAcquirePaymentTokenResponse> GetPayPalToken(MOBFOPAcquirePaymentTokenRequest request, Session session);

        Task<MOBFOPAcquirePaymentTokenResponse> GetMasterpassToken(MOBFOPAcquirePaymentTokenRequest request, Session session);
        Task<MOBPersistFormofPaymentResponse> PersistFormofPaymentDetails(MOBPersistFormofPaymentRequest request, Session session);
        Task<MOBRegisterOfferResponse> RegisterOffers(MOBRegisterOfferRequest request, Session session);
        Task<MOBPersistFormofPaymentResponse> GetCreditCardToken(MOBPersistFormofPaymentRequest request, Session session);
        string GetPaymentTargetForRegisterFop(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isCompleteFarelockPurchase = false);
        Task<(List<FormofPaymentOption> response, bool isDefault)> GetEligibleFormofPayments(MOBRequest request, Session session, MOBShoppingCart shoppingCart, string cartId, string flow, bool isDefault, MOBSHOPReservation reservation = null, bool IsMilesFOPEnabled = false, SeatChangeState persistedState = null);
    }
}
