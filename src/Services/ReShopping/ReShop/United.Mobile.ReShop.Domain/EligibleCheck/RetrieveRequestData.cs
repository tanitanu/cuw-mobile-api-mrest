using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.Model.Common;
using United.Mobile.Model.ReShop;
using United.Services.FlightShopping.Common.Extensions;

namespace United.Mobile.EligibleCheck.Domain
{
    public class RetrieveRequestData
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly AirportDynamoDB _airportDynamoDB;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;

        public RetrieveRequestData(
             IConfiguration configuration
            , IDynamoDBService dynamoDBService
            , IHeaders headers
            , IFeatureSettings featureSettings
            )
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
            _headers = headers;
            _airportDynamoDB = new AirportDynamoDB(_configuration, _dynamoDBService);
            _featureSettings = featureSettings;
        }

        public async Task<List<MOBPNRSegment>> GetAllSegments
            (MOBRequest request, Service.Presentation.ReservationResponseModel.ReservationDetail cslresponse)
        {
            //TODO null check
            List<MOBPNRSegment> segments = new List<MOBPNRSegment>();

            var flightsegments = cslresponse.Detail.FlightSegments;

            if (flightsegments == null || !flightsegments.Any()) return null;

            string confirmedActionCodes = _configuration.GetValue<string>("flightSegmentTypeCode");

            flightsegments.ToList().ForEach(async cslseg =>
            {
                if (confirmedActionCodes.IndexOf
                        (cslseg.FlightSegment.FlightSegmentType.Substring(0, 2), StringComparison.Ordinal) != -1)
                {
                    MOBPNRSegment segment = new MOBPNRSegment
                    {
                        SegmentNumber = cslseg.SegmentNumber,
                        TripNumber = cslseg.TripNumber,

                        ScheduledArrivalDateTime = cslseg.EstimatedArrivalTime,
                        ScheduledDepartureDateTime = cslseg.EstimatedDepartureTime,
                        ScheduledArrivalDateTimeGMT = cslseg.EstimatedArrivalUTCTime,
                        ScheduledDepartureDateTimeGMT = cslseg.EstimatedDepartureUTCTime,
                    };

                    if (cslseg.FlightSegment != null)
                    {
                        if (cslseg.FlightSegment.ArrivalAirport != null)
                        {
                            string airportName = string.Empty;
                            string cityName = string.Empty;
                            var tupleRes = await _airportDynamoDB.GetAirportCityName(cslseg.FlightSegment.ArrivalAirport.IATACode, _headers.ContextValues.SessionId, airportName, cityName);
                            airportName = tupleRes.airportName;
                            cityName = tupleRes.cityName;

                            segment.Arrival = new MOBAirport
                            {
                                Code = cslseg.FlightSegment.ArrivalAirport.IATACode,
                                City = cityName,
                                Name = airportName
                            };
                        }

                        if (cslseg.FlightSegment.DepartureAirport != null)
                        {
                            string airportName = string.Empty;
                            string cityName = string.Empty;
                            var tupleFlightRes = await _airportDynamoDB.GetAirportCityName(cslseg.FlightSegment.DepartureAirport.IATACode, _headers.ContextValues.SessionId, airportName, cityName);
                            airportName = tupleFlightRes.airportName;
                            cityName = tupleFlightRes.cityName;

                            segment.Departure = new MOBAirport
                            {
                                Code = cslseg.FlightSegment.DepartureAirport.IATACode,
                                City = cityName,
                                Name = airportName
                            };
                        }

                        segment.Aircraft = new MOBAircraft
                        {
                            Code = cslseg.FlightSegment.Equipment != null
                            ? cslseg.FlightSegment.Equipment.Model.Fleet : string.Empty,

                            LongName = cslseg.FlightSegment.Equipment != null
                            ? cslseg.FlightSegment.Equipment.Model.Description : string.Empty,

                            ShortName = cslseg.FlightSegment.Equipment != null
                            ? cslseg.FlightSegment.Equipment.Model.STIAircraftType : string.Empty,
                        };

                        segment.FlightTime
                        = EligibilityCheckHelper.GetTravelTime(request.Application.Version.Major, cslseg.FlightSegment.JourneyDuration);

                        segment.FlightNumber = cslseg.FlightSegment.FlightNumber;
                        //segment.FlightTime = Utility.GetCharactersticValue(                       

                        segment.OperationoperatingCarrier = new MOBAirline
                        {
                            Code = cslseg.FlightSegment.OperatingAirlineCode,
                            FlightNumber = cslseg.FlightSegment.OperatingAirlineFlightNumber,
                            Name = cslseg.FlightSegment.OperatingAirlineName,
                        };

                        var marketingcarrier = cslseg.FlightSegment.MarketedFlightSegment?.FirstOrDefault();

                        if (marketingcarrier != null)
                        {
                            segment.MarketingCarrier = new MOBAirline
                            {
                                Code = marketingcarrier.MarketingAirlineCode,
                                FlightNumber = marketingcarrier.FlightNumber,
                                Name = marketingcarrier.Description,
                            };
                        }
                    }

                    DateTime arrivaldate;
                    DateTime.TryParse(cslseg.EstimatedArrivalTime, out arrivaldate);
                    segment.FormattedScheduledArrivalDateTime = cslseg.EstimatedArrivalTime;
                    segment.FormattedScheduledArrivalDate = arrivaldate.ToShortDateString();

                    DateTime departuredate;
                    DateTime.TryParse(cslseg.EstimatedDepartureTime, out departuredate);
                    segment.FormattedScheduledDepartureDateTime = cslseg.EstimatedDepartureTime;
                    segment.FormattedScheduledDepartureDate = departuredate.ToShortDateString();

                    segment.ClassOfService = cslseg?.BookingClass?.Code;
                    segment.CabinType = cslseg?.BookingClass?.Cabin?.Name;
                    segment.ClassOfServiceDescription = cslseg?.BookingClass?.Cabin?.Description;
                    segments.Add(segment);
                }
            });
            return segments;
        }

        public async Task<List<MOBTrip>> GetAllTrips
            (Service.Presentation.ReservationResponseModel.ReservationDetail cslresponse, bool isAward)
        {
            List<MOBTrip> trips = new List<MOBTrip>();

            var flightsegments = cslresponse.Detail.FlightSegments;

            if (flightsegments == null || !flightsegments.Any()) return null;

            if (flightsegments != null && flightsegments.Any())
            {
                flightsegments = flightsegments.OrderBy(x => x.TripNumber).ToCollection();

                int mintripnumber = Convert.ToInt32(flightsegments.Where
                    (x => !string.IsNullOrEmpty(x.TripNumber))?.Select(o => o.TripNumber).First());

                int maxtripnumber = Convert.ToInt32(flightsegments.Where
                    (x => !string.IsNullOrEmpty(x.TripNumber))?.Select(o => o.TripNumber).Last());

                for (int i = mintripnumber; i <= maxtripnumber; i++)
                {
                    bool isUsed = false;
                    MOBTrip pnrTrip = new MOBTrip();

                    var totalTripSegments = flightsegments.Where(o => o.TripNumber == i.ToString());

                    if (totalTripSegments == null) continue;

                    pnrTrip.Index = i;

                    foreach (United.Service.Presentation.SegmentModel.ReservationFlightSegment segment in totalTripSegments)
                    {
                        if (!string.IsNullOrEmpty(segment.TripNumber) && Convert.ToInt32(segment.TripNumber) == i)
                        {
                            if (await _featureSettings.GetFeatureSettingValue("PartiallyFlownCheckinSSWWFix").ConfigureAwait(false) && segment.FlightSegment?.FlightStatuses?.Count() > 0)
                            {
                                var status = segment?.FlightSegment?.FlightStatuses?.FirstOrDefault(fs => fs.Description == "ETicket Coupon Status");
                                if (status != null && status.StatusType!=null)
                                {
                                    isUsed = status.StatusType.Equals("USED");
                                }
                            }
                            string airportName = string.Empty;
                            string cityName = string.Empty;

                            pnrTrip.CabinType = EligibilityCheckHelper.GetHighestCabin(totalTripSegments, isAward);

                            if (segment.SegmentNumber == totalTripSegments.Min(x => x.SegmentNumber))
                            {
                                pnrTrip.Origin = segment.FlightSegment.DepartureAirport.IATACode;

                                var tupleFlightRes = await _airportDynamoDB.GetAirportCityName(pnrTrip.Origin, _headers.ContextValues.SessionId, airportName, cityName);
                                airportName = tupleFlightRes.airportName;
                                cityName = tupleFlightRes.cityName;
                                pnrTrip.OriginName = airportName;

                                pnrTrip.DepartureTime = segment.FlightSegment.DepartureDateTime;

                                pnrTrip.DepartureTimeGMT = segment.FlightSegment.DepartureUTCDateTime;

                            }
                            if (segment.SegmentNumber == totalTripSegments.Max(x => x.SegmentNumber))
                            {
                                pnrTrip.Destination = segment.FlightSegment.ArrivalAirport.IATACode;

                                var tupleResponse = await _airportDynamoDB.GetAirportCityName(pnrTrip.Destination, _headers.ContextValues.SessionId, airportName, cityName);
                                airportName = tupleResponse.airportName;
                                cityName = tupleResponse.cityName;
                                pnrTrip.DestinationName = airportName;

                                pnrTrip.ArrivalTime = segment.FlightSegment.ArrivalDateTime;

                                pnrTrip.ArrivalTimeGMT = segment.FlightSegment.ArrivalUTCDateTime;
                            }

                        }
                    }
                    if(!isUsed) trips.Add(pnrTrip);
                }
            }
            return trips;
        }

    }
}
