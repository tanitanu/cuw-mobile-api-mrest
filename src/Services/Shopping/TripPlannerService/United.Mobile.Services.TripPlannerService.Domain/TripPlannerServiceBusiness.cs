using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.TripPlannerService;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Model.TripPlannerService;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Extensions;
using United.TravelPlanner.Models;
using United.Utility.Helper;
using Application = United.Mobile.Model.TripPlannerService.Application;
using Version = United.Mobile.Model.TripPlannerService.Version;

namespace United.Mobile.Services.TripPlannerService.Domain
{
    public class TripPlannerServiceBusiness : ITripPlannerServiceBusiness
    {
        private readonly ICacheLog<TripPlannerServiceBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IAddTripPlanVotingService _addTripPlanVotingService;

        public TripPlannerServiceBusiness(ICacheLog<TripPlannerServiceBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IAddTripPlanVotingService addTripPlanVotingService)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _addTripPlanVotingService = addTripPlanVotingService;
        }

        public async Task<MOBTripPlanVoteResponse> AddTripPlanVoting(MOBTripPlanVoteRequest request)
        {
            MOBTripPlanVoteResponse response = new MOBTripPlanVoteResponse();

            response.VoteId = await AddVoteTripPlan(request);
            if (!string.IsNullOrEmpty(response.VoteId))
            { response.IsSuccess = true; }

            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;

            return await Task.FromResult(response);
        }

        public async Task<MOBTripPlanDeleteResponse> DeleteTripPlan(MOBTripPlanDeleteRequest request)
        {
            MOBTripPlanDeleteResponse response = new MOBTripPlanDeleteResponse();

            response.IsSuccess = await DeleteTripPlans(request);

            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;

            return await Task.FromResult(response);
        }

        public async Task<MOBTripPlanVoteResponse> DeleteTripPlanVoting(MOBTripPlanVoteRequest request)
        {
            MOBTripPlanVoteResponse response = new MOBTripPlanVoteResponse();

            response.IsSuccess = await DeleteVoteTripPlan(request);
            
            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;

            return await Task.FromResult(response);
        }

        private async Task<string> AddVoteTripPlan(MOBTripPlanVoteRequest request)
        {
            string url = string.Format("/{0}/{1}", "travelplanner","AddRating");

            Session session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            if (session == null) throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            Rating ratingRequest = GetRatingTripPlanRequest(request);
            string jsonRequest = JsonConvert.SerializeObject(ratingRequest);

            var jsonResponse = await MakeHTTPPost(request.SessionId, request.DeviceId, "AddVoteTripPlan", request.Application, session.Token, url, jsonRequest);
            var response = JsonConvert.DeserializeObject<TravelPlanRatingServiceResponse>(jsonResponse);
            if (response?.Status?.Equals("Successful", StringComparison.OrdinalIgnoreCase) ?? false && (response?.Error?.Count ?? 0) == 0)
            {
                if (_configuration.GetValue<bool>("EnableTripPlanPushNotifications")) //IS version check required
                {
                    await SendPushNotification(request, session);
                }
                return ratingRequest.ID;
            }
            else
            {
                if (response.Error != null && response.Error.Count > 0)
                {
                    string errorMessage = string.Empty;
                    response.Error.ForEach(e => errorMessage += " " + e.MajorDescription);

                    throw new MOBUnitedException(!string.IsNullOrEmpty(errorMessage.Trim()) ? errorMessage.Trim() : _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            //#endregion Service call
        }

        private Rating GetRatingTripPlanRequest(MOBTripPlanVoteRequest request)
        {
            return new Rating()
            {
                DeviceID = request.DeviceId,
                FirstFlightDepartDate = request.FirstFlightDepartDate?.ToDateTime(),
                ID = Guid.NewGuid().ToString(),
                MpID = request.MileagePlusId,
                ShoppingCartID = request.ShoppingCartId,
                TravelPlanID = request.TripPlanId,
                Type = RatingType.Vote
            };
        }

        private async Task SendPushNotification(MOBTripPlanVoteRequest request, Session session)
        {
            await System.Threading.Tasks.Task.Factory.StartNew(async() =>
            {
                List<LogEntry> _logEntries = new List<LogEntry>();
                try
                {
                    var tpCCEResponse = await _sessionHelperService.GetSession<TripPlanCCEResponse>(session.SessionId, new TripPlanCCEResponse().ObjectName, new List<string> { session.SessionId, new TripPlanCCEResponse().ObjectName }).ConfigureAwait(false);
                    if (tpCCEResponse == null)
                    {
                        _logger.LogError("RegisterAddVotePushNotification Failed to load TPCCe Response from couch base.");
                    }
                    else
                    {
                        //Load from TPCCe response once CCE is ready.
                        List<string> lstDeviceIDs = tpCCEResponse?.DeviceIds?.Split(',')?.Distinct()?.Where(d => d != request.DeviceId)?.ToList();

                        if ((lstDeviceIDs?.Count ?? 0) > 0)
                        {
                            string destination = tpCCEResponse.TripPlanTrips.FirstOrDefault(t => t?.CslShopRequest != null)?.CslShopRequest?.Trips?[0].Flights.Select(GetDestination).First();
                            if (!string.IsNullOrEmpty(destination))
                                await PostPushNotification(request, session, destination, lstDeviceIDs, _logEntries);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("RegisterAddVotePushNotification {@Exception}", JsonConvert.SerializeObject(ex));
                }
            });
        }

        private async Task PostPushNotification(MOBTripPlanVoteRequest request, Session session, string destination, List<string> lstDeviceIDs, List<LogEntry> _logEntries)
        {
            var url = string.Format("{0}{1}", "Notification/ProcessEvents");

            PushNotificationRequest createPushNotificationRequest = GetPushNotificationRequest(request, session, destination);

            foreach (var deviceID in lstDeviceIDs)
            {
                createPushNotificationRequest.Data.Recipients[0].Parameters["DeviceId"] = deviceID;

                string jsonRequest = JsonConvert.SerializeObject(createPushNotificationRequest, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings { ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() });

                try
                {
                    var jsonResponse = await MakeHTTPPost(request.SessionId, request.DeviceId, "PostPushNotification", request.Application, null, url, jsonRequest);

                    if (string.IsNullOrEmpty(jsonResponse)) throw new Exception("Error in PostPushNotification - Response is empty.");

                    var response = JsonConvert.DeserializeObject<PushNotificationResponse>(jsonResponse);

                    if ((response.Errors?.Count ?? 0) > 0) throw new Exception(string.Join(",", response?.Errors?.Select(e => e.Value)));

                }
                catch (Exception ex)
                {
                   _logger.LogError("RegisterAddVotePushNotification {@Exception}", JsonConvert.SerializeObject(ex));
                }
            }
        }

        private PushNotificationRequest GetPushNotificationRequest(MOBTripPlanVoteRequest request, Session session, string destination)
        {
            return new PushNotificationRequest()
            {
                TransactionId = request.TransactionId,
                Application = new Application()
                {
                    Id = request.Application.Id,
                    Version = new Version()
                    {
                        Major = int.Parse(request.Application.Version.Major.Split('.')[0]),
                        Minor = int.Parse(request.Application.Version.Major.Split('.')[1]),
                        Build = int.Parse(request.Application.Version.Major.Split('.')[2])
                    },
                    Name = request.Application.Name,
                    IsProduction = _configuration.GetValue<bool>("IsProd")
                },
                LanguageCode = request.LanguageCode,

                Data = new NotificationEnvelope()
                {
                    Recipients = new List<Recipient>()
                    {
                        new Recipient()
                        {
                            Type="BOOKING",
                            FeedbackToken=Guid.NewGuid().ToString(),
                            Parameters =  new Dictionary<string, string>()
                            {
                                {"DeviceId",request.DeviceId }
                            }
                        }
                    },
                    Notification = new Notification()
                    {
                        Type = "INFORMATION",
                        SubType = "TRIPPLANNER",
                        Badge = 0,
                        Sound = "default",
                        ContentAvaliable = false,
                        Category = "INFORMATION.BOOKING.TRIPPLANNER",
                        ThreadId = "TRIPPLANNER",
                        DestinationType = "TRIPPLANNER",
                        Alert = new Alert()
                        {
                            Title = _configuration.GetValue<string>("TripPlanPushNotificationTitle"),
                            Body = $"{_configuration.GetValue<string>("TripPlanPushNotificationBody")}{destination}."
                        },
                        Payload = new Dictionary<string, string>()
                        {
                            {"TpId",request.TripPlanId },
                            { "event","TRIPPLANNER"},
                            { "eventType","TRIPPLANNER" }
                        }
                    }
                }
            };
        }

        private string GetDestination(Flight arg)
        {
            if ((arg.Connections?.Count ?? 0) == 0)
                return arg.DestinationDescription?.Split(',')?[0] ?? arg.Destination;
            else return GetDestination(arg.Connections.Last());
        }

        private async Task<string> MakeHTTPPost(string sessionId, string deviceId, string action, MOBApplication application, string token, string path, string jsonRequest, bool isXMLRequest = false)
        {
            string jsonResponse = string.Empty;
            string applicationRequestType = isXMLRequest ? "xml" : "json";
            jsonResponse = await _addTripPlanVotingService.TripPlanVoting(token, sessionId, path, jsonRequest).ConfigureAwait(false);

            return jsonResponse;
        }

        public async Task<bool> DeleteTripPlans(MOBTripPlanDeleteRequest request)
        {
            //return true;
            //#region Service call
            string url = string.Format("/{0}/{1}/{2}", "travelplanner","RemoveByTravelPlanId",request.TripPlanId);
            
            Session session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            if (session == null) throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            var tpCCEResponse = await _sessionHelperService.GetSession<TripPlanCCEResponse>(session.SessionId, new TripPlanCCEResponse().ObjectName, new List<string> { session.SessionId, new TripPlanCCEResponse().ObjectName }).ConfigureAwait(false);

            if (!(tpCCEResponse?.IsPilot ?? false))
                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            var jsonResponse = await MakeHTTPDelete(request.SessionId, request.DeviceId, "DeleteTripPlan", request.Application.Id, request.Application.Version.Major, session.Token, url);

            TravelPlanServiceResponse response = JsonConvert.DeserializeObject<TravelPlanServiceResponse>(jsonResponse);
            if (response?.Status?.Equals("Successful", StringComparison.OrdinalIgnoreCase) ?? false && (response?.Error?.Count ?? 0) == 0)
            {
                return true;
            }
            else
            {
                if (response.Error != null && response.Error.Count > 0)
                {
                    string errorMessage = string.Empty;
                    response.Error.ForEach(e => errorMessage += " " + e.MajorDescription);

                    throw new MOBUnitedException(!string.IsNullOrEmpty(errorMessage.Trim()) ? errorMessage.Trim() : _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            //#endregion Service call
        }

        private async Task<string> MakeHTTPDelete(string sessionId, string deviceId, string action, int applicationId, string appVersion, string token, string url, bool isXMLRequest = false)
        {
            string jsonResponse = string.Empty;
            string applicationRequestType = isXMLRequest ? "xml" : "json";
            jsonResponse = await _addTripPlanVotingService.DeleteTripPlan(token, url, sessionId).ConfigureAwait(false);

            return jsonResponse;
        }

        private async Task<bool> DeleteVoteTripPlan(MOBTripPlanVoteRequest request)
        {
            //return true;
            //#region Service call
            string url = string.Format("/{0}", "travelplanner/RemoveRating?travelPlanId=" + request.TripPlanId+ "&shoppingCartId="+request.ShoppingCartId+ "&ratingId="+request.VoteId);
            Session session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            if (session == null) throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            var jsonResponse = await MakeHTTPDelete (request.SessionId, request.DeviceId, "DeleteVoteTripPlan", request.Application.Id, request.Application.Version.Major, session.Token, url);
            TravelPlanRatingServiceResponse response = JsonConvert.DeserializeObject<TravelPlanRatingServiceResponse>(jsonResponse);
            if (response?.Status?.Equals("Successful", StringComparison.OrdinalIgnoreCase) ?? false && (response?.Error?.Count ?? 0) == 0)
            {
                return true;
            }
            else
            {
                if (response.Error != null && response.Error.Count > 0)
                {
                    string errorMessage = string.Empty;
                    response.Error.ForEach(e => errorMessage += " " + e.MajorDescription);

                    throw new MOBUnitedException(!string.IsNullOrEmpty(errorMessage.Trim()) ? errorMessage.Trim() : _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            //#endregion Service call
        }

    }
}
