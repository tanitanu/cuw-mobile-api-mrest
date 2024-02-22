﻿using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;

namespace United.Common.Helper.FSRHandler
{
    public class FSRSeasonalDestinationSuggestFutureDate : FSRSeasonalSuggestFutureDateBase
    {
        private readonly IConfiguration _configuration;
        public FSRSeasonalDestinationSuggestFutureDate(
            ShopResponse cslShopResponse
            , MOBSHOPShopRequest restShopRequest
            , IConfiguration configuration)
            : base(cslShopResponse, restShopRequest)
        {
            _configuration = configuration;
        }

        public override bool ShouldExecuteRule()
        {
            return base.ShouldExecuteRule()
                && _cslShopResponse.Errors != null
                && _cslShopResponse.Errors.Any(warning => warning.MinorCode.Equals(ArrivalFutureTravelDateErrorMinorCode));
        }

        protected override MOBSHOPShopRequest GetSearchNearByButtonUpdatedRequest(MOBSHOPShopRequest restShopRequest)
        {
            restShopRequest.Trips[_tripIndex].SearchNearbyDestinationAirports = true;
            restShopRequest.CameFromFSRHandler = true;
            restShopRequest.SessionId = null;
            restShopRequest.GetNonStopFlightsOnly = true;
            restShopRequest.GetFlightsWithStops = false;

            return restShopRequest;
        }

        protected override Tuple<DateTime, DateTime> GetNewTripDurationWindowForRT(MOBSHOPShopRequest restShopRequest)
        {
            var departureDate = Convert.ToDateTime(restShopRequest.Trips[0].DepartDate).Date;
            var returnDate = Convert.ToDateTime(restShopRequest.Trips[1].DepartDate).Date;

            return CalculateNewTripDurationWindowForRT(Tuple.Create(departureDate, DateTime.MinValue), Tuple.Create(returnDate, FutureTravelDate));
        }

        protected override string GetSeasonalSuggestFutureDateBodyMessage()
        {
            string seasonalBodyMsgTmpl = _configuration.GetValue<string>("FSRNoResultsSuggestFutureDateDestMsgbody");
            return string.Format(seasonalBodyMsgTmpl, _cslShopResponse.Trips[_tripIndex].DestinationDecoded, FutureTravelDate.ToString(DateToStringFormat));
        }

        protected override MOBFSREnhancementType GetFRSEnhancementType()
        {
            return MOBFSREnhancementType.NoResultsDestinationSeasonal;
        }
    }
}
