using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.ReshopSelectTrip.Domain
{
    public class Reshopping
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        public Reshopping(IConfiguration configuration
            , ILogger logger
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility)
        {
            _configuration = configuration;
            _logger = logger;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
        }
        #region reshop change travelercsl build 
        public Reservation AssignPnrTravelerToReservation(MOBSHOPShopRequest request, Reservation persistedReservation, Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            //var cslReservation = United.Persist.FilePersist.Load<Service.Presentation.ReservationResponseModel.ReservationDetail>(request.SessionId, (new Service.Presentation.ReservationResponseModel.ReservationDetail()).GetType().FullName);
            var mobCPPhones = MapTravelerModel.GetMobCpPhones(cslReservation.Detail.TelephoneNumbers);
            var mobEmails = MapTravelerModel.GetMobEmails(cslReservation.Detail.EmailAddress);

            if (cslReservation.Detail.Travelers != null && cslReservation.Detail.Travelers.Count > 0)
            {
                persistedReservation.TravelerKeys = new List<string>();
            }
            persistedReservation.TravelersCSL = ConvertPnrTravelerToMobShopTraveler(request, cslReservation.Detail.Travelers.ToList(), persistedReservation.TravelerKeys, cslReservation.Detail.FlightSegments, request.Trips);
            AssignMainTCDIfNullForAnyTravler(persistedReservation.TravelersCSL, mobCPPhones, mobEmails);
            //FilePersist.Save<Reservation>(request.SessionId, persistedReservation.ObjectName, persistedReservation);
            return persistedReservation;
        }

        private SerializableDictionary<string, MOBCPTraveler> ConvertPnrTravelerToMobShopTraveler(MOBSHOPShopRequest request, List<Service.Presentation.ReservationModel.Traveler> cslTravelers, List<string> travelerKeys, Collection<Service.Presentation.SegmentModel.ReservationFlightSegment> flightSegments, List<MOBSHOPTripBase> trips)
        {
            List<MOBPNRPassenger> reshopTravelers = request.ReshopTravelers;
            SerializableDictionary<string, MOBCPTraveler> travelerCsl = null;

            if (reshopTravelers != null && reshopTravelers.Count > 0)
            {
                travelerCsl = new SerializableDictionary<string, MOBCPTraveler>();
                List<string> travelersIdList = null;
                //if (!request.ReshopTravelerSHARESPositions.IsNullOrEmpty())
                //{
                //    travelersIdList = request.ReshopTravelerSHARESPositions;
                //}
                //else
                //{
                travelersIdList = reshopTravelers.Select(t => t.SHARESPosition).ToList();
                //}

                var pnrCslTravelers = cslTravelers.Where(p => travelersIdList.Contains(p.Person.Key)).ToList();
                if (travelerKeys == null)
                {
                    travelerKeys = new List<string>();
                }
                travelerCsl = BuildMOBCPTravelersDictionaryFromCslPersonsList(pnrCslTravelers, travelerKeys, flightSegments, trips);
            }

            return travelerCsl;
        }

        private void AssignMainTCDIfNullForAnyTravler(SerializableDictionary<string, MOBCPTraveler> travelersCSL, List<MOBCPPhone> mobCPPhones, List<MOBEmail> mobEmails)
        {
            foreach (var traveler in travelersCSL)
            {
                //if ((traveler.Value.Phones == null || traveler.Value.Phones.Count == 0) && mobCPPhones.Count > 0)
                //{
                //    traveler.Value.Phones = mobCPPhones;
                //}
                //if ((traveler.Value.ReservationPhones == null || traveler.Value.ReservationPhones.Count == 0) && mobCPPhones.Count > 0)
                //{
                //    traveler.Value.ReservationPhones = mobCPPhones;
                //}                
                if ((traveler.Value.EmailAddresses == null || traveler.Value.EmailAddresses.Count == 0) && mobEmails.Count > 0)
                {
                    traveler.Value.EmailAddresses = mobEmails;
                }
                if ((traveler.Value.ReservationEmailAddresses == null || traveler.Value.ReservationEmailAddresses.Count == 0) && mobEmails.Count > 0)
                {
                    traveler.Value.ReservationEmailAddresses = mobEmails;
                }
            }
        }

        private SerializableDictionary<string, MOBCPTraveler> BuildMOBCPTravelersDictionaryFromCslPersonsList(List<Service.Presentation.ReservationModel.Traveler> cslTravelers, List<string> travelerKeys, Collection<Service.Presentation.SegmentModel.ReservationFlightSegment> flightSegments, List<MOBSHOPTripBase> trips)
        {
            SerializableDictionary<string, MOBCPTraveler> travelerCsl = new SerializableDictionary<string, MOBCPTraveler>();
            int paxIndex = 0;
            cslTravelers.ForEach(p =>
            {
                MOBCPTraveler mobCPTraveler = MapTravelerModel.MapCslPersonToMOBCPTravel(p, paxIndex, flightSegments, trips);
                travelerCsl.Add(mobCPTraveler.PaxIndex.ToString(), mobCPTraveler);
                travelerKeys.Add(mobCPTraveler.PaxIndex.ToString());
                paxIndex++;
            });

            return travelerCsl;
        }
        #endregion

    }
}
