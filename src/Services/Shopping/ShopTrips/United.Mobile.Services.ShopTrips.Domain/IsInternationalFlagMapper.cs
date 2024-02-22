﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Definition.Shopping;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.FlightReservation;

namespace United.Mobile.Services.ShopTrips
{
    public class IsInternationalFlagMapper
    {
        public bool IsInternationalFromResponse(FlightReservationResponse response)
        {
            if (response != null && response.Reservation != null && response.Reservation.FlightSegments != null)
                return response.Reservation.FlightSegments.FirstOrDefault(
                        IsInternational) != null;

            return false;
        }

        public bool IsInternational(ReservationFlightSegment item)
        {
            if (item != null && item.FlightSegment != null && !string.IsNullOrEmpty(item.FlightSegment.IsInternational))
                return item.FlightSegment.IsInternational.ToUpper().Trim() == "TRUE";

            return false;
        }

    }
}