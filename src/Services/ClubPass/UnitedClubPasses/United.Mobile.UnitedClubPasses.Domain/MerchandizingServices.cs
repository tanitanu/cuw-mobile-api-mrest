using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using System.Linq;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.UnitedClubPasses;
using United.Service.Presentation.ProductRequestModel;
using United.Services.FlightShopping.Common.Extensions;
using Reservation = United.Mobile.Model.UnitedClubPasses.Reservation;

namespace United.Mobile.UnitedClubPasses.Domain
{
    public class MerchandizingServices
    {
        private readonly IConfiguration _configuration;
        private readonly Utility _utility;

        public MerchandizingServices(IConfiguration configuration)
        {
            _configuration = configuration;
            _utility = new Utility(configuration);
        }
        public ProductPurchaseRequest GetOfferRequest(OTPPurchaseRequest request, string firstName, string lastName, double amountPaid, VormetricKeys vormetricKeys, PayPalPayor payPalPayor, Reservation reservationForMasterpass, Service.Presentation.PaymentModel.CreditCard creditCard, bool isBookingPath)
        {
            ProductPurchaseRequest offerRequest = new ProductPurchaseRequest();
            offerRequest.Requester = new Service.Presentation.CommonModel.ServiceClient();
            offerRequest.Requester.GUIDs = new Collection<Service.Presentation.CommonModel.UniqueIdentifier>();
            Service.Presentation.CommonModel.UniqueIdentifier uiCorrelation = new Service.Presentation.CommonModel.UniqueIdentifier();
            uiCorrelation.ID = "1420665359697010086025043118921";
            uiCorrelation.Name = "CORRELATIONID";
            offerRequest.Requester.GUIDs.Add(uiCorrelation);
            Service.Presentation.CommonModel.UniqueIdentifier uiTrans = new Service.Presentation.CommonModel.UniqueIdentifier();
            uiTrans.ID = "1420665359697010086025043118921";
            uiTrans.Name = "TRANSACTIONID";
            offerRequest.Requester.GUIDs.Add(uiTrans);

            Session session = new Session();
            if (session == null)
            {
                if (isBookingPath)
                {
                    throw new MOBUnitedException("Could not find your booking session.");
                }
                else
                {
                    throw new MOBUnitedException("Could not find United Club Pass Purchase session.");
                }
            }

            Service.Presentation.CommonModel.UniqueIdentifier uiToken = new Service.Presentation.CommonModel.UniqueIdentifier();
            if (_configuration.GetValue<bool>("EnableDPToken"))
            {
                uiToken.ID = ""; // DP token not required to bind in the request, as they are consuming from Dp directly.
            }
            uiToken.Name = "AUTHORIZATIONTOKEN";
            offerRequest.Requester.GUIDs.Add(uiToken);

            offerRequest.Requester.Requestor = new Service.Presentation.CommonModel.Requestor();

            if (_configuration.GetValue<bool>("EnabledMERCHChannels"))
            {
                string channelId = string.Empty;
                string channelName = string.Empty;
                string merchChannel = "BE";
                SetMerchandizeChannelValues(merchChannel, ref channelId, ref channelName);
                offerRequest.Requester.Requestor.ChannelID = channelId;
                offerRequest.Requester.Requestor.ChannelName = channelName;
            }
            else
            {
                offerRequest.Requester.Requestor.ChannelID = _configuration.GetSection("MerchandizeOffersServiceChannelID").Value.Trim();// "401";  //Changed per Praveen Vemulapalli email
                offerRequest.Requester.Requestor.ChannelName = _configuration.GetSection("MerchandizeOffersServiceChannelName").Value.Trim();//"MBE";  //Changed per Praveen Vemulapalli email
            }
            offerRequest.Requester.Requestor.ApplicationSource = _configuration.GetSection("MerchandizeOffersCSLServiceApplicationSource").Value.Trim();

            Reservation persistedReservation = new Reservation();
            offerRequest.Travelers = new Collection<Service.Presentation.ProductModel.ProductTraveler>();
            Service.Presentation.ProductModel.ProductTraveler traveler = new Service.Presentation.ProductModel.ProductTraveler();
            traveler.GivenName = firstName;
            traveler.Surname = lastName;
            traveler.ID = "1";
            traveler.PassengerTypeCode = "ADT";
            traveler.TravelerNameIndex = "1.1";
            offerRequest.Travelers.Add(traveler);

            offerRequest.CurrencyCode = "USD";
            // Added as part of Bug 128996 - Android: Unable to add PA, OTP, E+ (Ancillary Products) on RTI screen when we take GUAM as billing country : Issuf
            if (request != null && request.Address != null && request.Address.Country != null && !string.IsNullOrEmpty(request.Address.Country.Code))
            {
                offerRequest.CountryCode = request.Address.Country.Code;
            }
            offerRequest.PurchaseDetail = new Service.Presentation.ProductRequestModel.PurchaseDetail();
            offerRequest.PurchaseDetail.Cart = new Service.Presentation.ProductModel.Cart();
            offerRequest.PurchaseDetail.Cart.Products = new Collection<Service.Presentation.ProductModel.CartProduct>();
            Service.Presentation.ProductModel.CartProduct cart = new Service.Presentation.ProductModel.CartProduct();
            cart.ID = "1";
            cart.Code = "OTP";
            cart.Offers = new Collection<Service.Presentation.ProductModel.CartProductOffer>();
            Service.Presentation.ProductModel.CartProductOffer offer = new Service.Presentation.ProductModel.CartProductOffer();
            offer.GroupCode = "MERCH";
            offer.SubGroupCode = "OTP";
            offer.ID = "1";
            offer.PaymentOption = new Service.Presentation.ProductModel.ProductPaymentOption();
            offer.PaymentOption.Type = "Money";
            offer.PaymentOption.EDDCode = "PDP";
            offer.PaymentOption.PriceComponents = new Collection<Service.Presentation.ProductModel.ProductPriceComponent>();
            Service.Presentation.ProductModel.ProductPriceComponent ppc = new Service.Presentation.ProductModel.ProductPriceComponent();
            ppc.Price = new Service.Presentation.PriceModel.Price();
            ppc.Price.BasePrice = new Collection<Service.Presentation.CommonModel.Charge>();
            Service.Presentation.CommonModel.Charge charge = new Service.Presentation.CommonModel.Charge();
            charge.Currency = new Service.Presentation.CommonModel.Currency();
            charge.Currency.Code = "USD";
            charge.Amount = amountPaid;
            charge.Type = "Money";
            ppc.Price.BasePrice.Add(charge);

            ppc.Price.Totals = new Collection<Service.Presentation.CommonModel.Charge>();
            Service.Presentation.CommonModel.Charge total = new Service.Presentation.CommonModel.Charge();
            total.Currency = new Service.Presentation.CommonModel.Currency();
            total.Currency.Code = "USD";
            total.Amount = amountPaid;
            total.Type = "Money";
            ppc.Price.Totals.Add(total);

            ppc.Price.PointOfSale = "US";
            offer.PaymentOption.PriceComponents.Add(ppc);
            offer.Assocatiation = new Service.Presentation.ProductModel.ProductAssociation();

            offer.Assocatiation.TravelerRefIDs = new Collection<string>();
            offer.Assocatiation.TravelerRefIDs.Add("1");

            cart.Offers.Add(offer);
            offerRequest.PurchaseDetail.Cart.Products.Add(cart);

            offerRequest.PurchaseDetail.Payment = new Service.Presentation.ProductModel.CartPayment();
            offerRequest.PurchaseDetail.Payment.PaymentDetails = new Collection<Service.Presentation.ProductModel.CartPaymentDetail>();
            Service.Presentation.ProductModel.CartPaymentDetail pd = new Service.Presentation.ProductModel.CartPaymentDetail();
            pd.FormOfPayment = new Service.Presentation.PaymentModel.FormOfPayment();
            if (request.FormOfPayment == FormofPayment.CreditCard)
            {
                #region
                pd.FormOfPayment.CreditCard = new Service.Presentation.PaymentModel.CreditCard();
                pd.FormOfPayment.CreditCard.ExpirationDate = request.CreditCard.ExpireMonth.Length < 2 ? "0" + request.CreditCard.ExpireMonth + request.CreditCard.ExpireYear : request.CreditCard.ExpireMonth + request.CreditCard.ExpireYear;
                pd.FormOfPayment.CreditCard.Code = request.CreditCard.CardType;
                pd.FormOfPayment.CreditCard.AccountNumberToken = request.CreditCard.AccountNumberToken;
                pd.FormOfPayment.CreditCard.Name = request.CreditCard.CCName;
                pd.FormOfPayment.CreditCard.Type = new Service.Presentation.CommonModel.Genre();
                pd.FormOfPayment.CreditCard.Type.Key = "CC";
                pd.FormOfPayment.CreditCard.Amount = amountPaid;
                pd.FormOfPayment.CreditCard.Payor = new Service.Presentation.PersonModel.Person();
                pd.FormOfPayment.CreditCard.Payor.GivenName = request.CreditCard.CCName;
                ////ILE-1221 :HMenu - One Time Pass - Richa

                if (!string.IsNullOrEmpty(vormetricKeys.PersistentToken))
                {
                    pd.FormOfPayment.CreditCard.PersistentToken = vormetricKeys.PersistentToken;
                    if (!string.IsNullOrEmpty(vormetricKeys.SecurityCodeToken))
                    {
                        pd.FormOfPayment.CreditCard.SecurityCodeToken = vormetricKeys.SecurityCodeToken;
                    }
                    if (!string.IsNullOrEmpty(vormetricKeys.CardType) && string.IsNullOrEmpty(pd.FormOfPayment.CreditCard.Code))
                    {
                        pd.FormOfPayment.CreditCard.Code = vormetricKeys.CardType;
                    }
                }

                if (isBookingPath || (!isBookingPath && request.Address != null))
                {
                    pd.FormOfPayment.CreditCard.BillingAddress = new Service.Presentation.CommonModel.Address();
                    pd.FormOfPayment.CreditCard.BillingAddress.AddressLines = new Collection<string>();
                    pd.FormOfPayment.CreditCard.BillingAddress.AddressLines.Add(request.Address.Line1.Length > 35 ? request.Address.Line1.Substring(0, 35) : request.Address.Line1);
                    pd.FormOfPayment.CreditCard.BillingAddress.City = request.Address.City;
                    pd.FormOfPayment.CreditCard.BillingAddress.StateProvince = new Service.Presentation.CommonModel.StateProvince();
                    pd.FormOfPayment.CreditCard.BillingAddress.StateProvince.ShortName = request.Address.State.Code;
                    pd.FormOfPayment.CreditCard.BillingAddress.PostalCode = request.Address.PostalCode;
                    pd.FormOfPayment.CreditCard.BillingAddress.Country = new Service.Presentation.CommonModel.Country();
                    pd.FormOfPayment.CreditCard.BillingAddress.Country.CountryCode = request.Address.Country.Code;
                }
                pd.FormOfPayment.CreditCard.Currency = new Service.Presentation.CommonModel.Currency();
                pd.FormOfPayment.CreditCard.Currency.Code = "USD";

                pd.FormOfPayment.CreditCard.GroupID = request.SessionId;
                pd.FormOfPayment.CreditCard.OperationID = "1";
                #endregion
            }
            else if (payPalPayor != null & request.FormOfPayment == FormofPayment.PayPal || request.FormOfPayment == FormofPayment.PayPalCredit)
            {
                #region
                pd.FormOfPayment.PayPal = PayPalPayLoad(payPalPayor, amountPaid, request.PayPal);
                traveler.GivenName = pd.FormOfPayment.PayPal.Payor.GivenName;
                traveler.Surname = pd.FormOfPayment.PayPal.Payor.Surname;
                #endregion
            }
            else if (request.FormOfPayment == FormofPayment.Masterpass && reservationForMasterpass != null)
            {
                #region
                pd.FormOfPayment.CreditCard = MasterpassPayLoad(reservationForMasterpass.MasterpassSessionDetails, amountPaid, session.SessionId, request.Application, request.DeviceId, vormetricKeys);
                traveler.GivenName = pd.FormOfPayment.CreditCard.Payor.GivenName;
                traveler.Surname = pd.FormOfPayment.CreditCard.Payor.Surname;
                #endregion
            }
            else if (request.FormOfPayment == FormofPayment.ApplePay)
            {
                #region //**Apple Pay**//
                pd.FormOfPayment.CreditCard = creditCard;
                pd.FormOfPayment.CreditCard.Amount = amountPaid;
                #endregion //**Apple Pay**//
            }

            pd.Associations = new Collection<Service.Presentation.ProductModel.ProductOfferAssociation>();
            Service.Presentation.ProductModel.ProductOfferAssociation asso = new Service.Presentation.ProductModel.ProductOfferAssociation();
            asso.OfferID = "1";
            asso.ProductID = "1";
            pd.Associations.Add(asso);
            offerRequest.PurchaseDetail.Payment.PaymentDetails.Add(pd);

            return offerRequest;
        }


        public United.Service.Presentation.PaymentModel.CreditCard MasterpassPayLoad(MasterpassSessionDetails masterpassSession, double paymentAmount, string sessionId, MOBApplication application, string deviceId, VormetricKeys vormetricKeys)
        {
            Service.Presentation.PaymentModel.CreditCard creditCard = new Service.Presentation.PaymentModel.CreditCard();
            creditCard.Payor = new Service.Presentation.PersonModel.Person();

            creditCard.Amount = paymentAmount;
            creditCard.OperationID = sessionId;
            creditCard.AccountNumberToken = masterpassSession.AccountNumberToken;
            //MOBILE-1218 Booking - Checkout using MasterPass
            if (!string.IsNullOrEmpty(vormetricKeys.PersistentToken))
            {
                creditCard.PersistentToken = vormetricKeys.PersistentToken;
                if (!string.IsNullOrEmpty(vormetricKeys.SecurityCodeToken))
                {
                    creditCard.SecurityCodeToken = vormetricKeys.SecurityCodeToken;
                    creditCard.SecurityCode = vormetricKeys.SecurityCodeToken;
                }

                if (!string.IsNullOrEmpty(vormetricKeys.CardType) && string.IsNullOrEmpty(masterpassSession.Code))
                {
                    masterpassSession.Code = vormetricKeys.CardType;
                }
            }
            creditCard.ExpirationDate = masterpassSession.ExpirationDate;
            creditCard.Code = masterpassSession.Code;
            creditCard.Name = masterpassSession.Name;
            creditCard.Payor.GivenName = masterpassSession.GivenName;
            creditCard.Payor.Surname = masterpassSession.SurName;
            creditCard.Currency = new Service.Presentation.CommonModel.Currency();
            creditCard.Currency.Code = "USD";
            creditCard.Type = new Service.Presentation.CommonModel.Genre();
            creditCard.Type.DefaultIndicator = masterpassSession.MasterpassType.DefaultIndicator;
            creditCard.Type.Description = masterpassSession.MasterpassType.Description;
            creditCard.Type.Key = masterpassSession.MasterpassType.Key;
            creditCard.Type.Value = masterpassSession.MasterpassType.Val;
            creditCard.IsBinRangeValidation = "FALSE";
            creditCard.BillingAddress = AssignCreditCardBillingAddress(masterpassSession.BillingAddress);
            creditCard.CreditCardTypeCode = (United.Service.Presentation.CommonEnumModel.CreditCardTypeCode)masterpassSession.CreditCardTypeCode;
            creditCard.Description = (United.Service.Presentation.CommonEnumModel.CreditCardType)masterpassSession.Description;
            return creditCard;
        }
        public Service.Presentation.PaymentModel.PayPal PayPalPayLoad(PayPalPayor payPalPayor, double paymentAmount, PayPal mobPayPal)
        {
            Service.Presentation.PaymentModel.PayPal paypal = new Service.Presentation.PaymentModel.PayPal();
            paypal.Amount = paymentAmount;

            paypal.BillingAddress = AssignCreditCardBillingAddress(payPalPayor.PayPalBillingAddress);
            paypal.BillingAddress.Characteristic = new Collection<Service.Presentation.CommonModel.Characteristic>();
            paypal.BillingAddress.Status = new Service.Presentation.CommonModel.Status();
            paypal.BillingAddress.Status.Description = "NONE"; //**TBD-paypal**//
            paypal.Currency = new Service.Presentation.CommonModel.Currency();
            paypal.Currency.Code = mobPayPal.CurrencyCode;
            paypal.PayerID = mobPayPal.PayerID;
            paypal.TokenID = mobPayPal.PayPalTokenID;
            paypal.Payor = new Service.Presentation.PersonModel.Person();
            paypal.Payor.Contact = new Service.Presentation.PersonModel.Contact();
            paypal.Payor.Contact.Emails = new Collection<Service.Presentation.CommonModel.EmailAddress>();
            Service.Presentation.CommonModel.EmailAddress email = new Service.Presentation.CommonModel.EmailAddress();
            email.Address = payPalPayor.PayPalContactEmailAddress;
            paypal.Payor.Contact.Emails.Add(email);
            paypal.Payor.CustomerID = payPalPayor.PayPalCustomerID;
            paypal.Payor.GivenName = payPalPayor.PayPalGivenName;
            paypal.Payor.Surname = payPalPayor.PayPalSurName;
            paypal.Payor.Status = new Service.Presentation.CommonModel.Status();
            paypal.Payor.Status.Description = payPalPayor.PayPalStatus;
            paypal.Type = new Service.Presentation.CommonModel.Genre();
            paypal.Type.Key = "PP"; // check if we need to move this to web config.

            return paypal;
        }
        public Service.Presentation.CommonModel.Address AssignCreditCardBillingAddress(MOBAddress billingAddress)
        {
            Service.Presentation.CommonModel.Address address = null;
            if (billingAddress != null)
            {
                address = new Service.Presentation.CommonModel.Address();
                var addressLines = new Collection<string>();
                AddAddressLinesToCslBillingAddress(billingAddress.Line1, ref addressLines);
                AddAddressLinesToCslBillingAddress(billingAddress.Line2, ref addressLines);
                AddAddressLinesToCslBillingAddress(billingAddress.Line3, ref addressLines);
                address.AddressLines = addressLines;
                address.City = billingAddress.City;
                if (billingAddress.State != null)
                {
                    address.StateProvince = new Service.Presentation.CommonModel.StateProvince();
                    address.StateProvince.StateProvinceCode = !string.IsNullOrEmpty(billingAddress.State.Code) ? billingAddress.State.Code : billingAddress.State.Name;
                    address.StateProvince.Name = billingAddress.State.Name;
                    address.StateProvince.ShortName = billingAddress.State.Name;
                }
                if (billingAddress.Country != null)
                {
                    address.Country = new Service.Presentation.CommonModel.Country();
                    address.Country.CountryCode = billingAddress.Country.Code.ToUpper();
                }
                address.PostalCode = billingAddress.PostalCode;
            }
            return address;
        }

        public Collection<string> AddAddressLinesToCslBillingAddress(string line, ref Collection<string> lines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                var linesAfterSplit = line.Replace("\r\n", "|").Replace("\n", "|").Split('|').ToCollection();
                lines = lines.Concat(linesAfterSplit).ToCollection();
            }
            return lines;
        }

        public void SetMerchandizeChannelValues(string merchChannel, ref string channelId, ref string channelName)
        {
            channelId = string.Empty;
            channelName = string.Empty;

            if (string.IsNullOrEmpty(merchChannel))
            {
                switch (merchChannel)
                {
                    case "MOBBE":
                        channelId = _configuration.GetSection("MerchandizeOffersServiceMOBBEChannelID").Value.Trim();
                        channelName = _configuration.GetSection("MerchandizeOffersServiceMOBBEChannelName").Value.Trim();
                        break;
                    case "MOBMYRES":
                        channelId = _configuration.GetSection("MerchandizeOffersServiceMOBMYRESChannelID").Value.Trim();
                        channelName = _configuration.GetSection("MerchandizeOffersServiceMOBMYRESChannelName").Value.Trim();
                        break;
                    case "MOBWLT":
                        channelId = _configuration.GetSection("MerchandizeOffersServiceMOBWLTChannelID").Value.Trim();
                        channelName = _configuration.GetSection("MerchandizeOffersServiceMOBWLTChannelName").Value.Trim();
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
