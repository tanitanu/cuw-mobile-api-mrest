﻿using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Booking;
using United.Mobile.Model.Shopping.FormofPayment;

namespace United.Common.Helper.FOP
{
    public interface IRegisterCFOP
    {
        Task<CheckOutResponse> RegisterFormsOfPayments_CFOP(CheckOutRequest checkOutRequest);
        void GetInterlineRedirectLink(List<MOBBKTraveler> bookingTravelerInfo, List<TripSegment> segments, string pointOfSale, MOBRequest mobRequest, string recordLocator, string lastname, List<MOBItem> catalog);
        void GetInterlineRedirectLink(List<TripSegment> segments, string pointOfSale, MOBRequest mobRequest, string recordLocator, string lastname, List<MOBItem> catalog);
        void AddPaymentNew(string transactionId,
                                     int applicationId,
                                     string applicationVersion,
                                     string paymentType,
                                     double amount,
                                     string currencyCode,
                                     int mileage,
                                     string remark,
                                     string insertBy,
                                     bool isTest,
                                     string sessionId,
                                     string deviceId,
                                     string recordLocator,
                                     string mileagePlusNumber,
                                     string formOfPayment,
                                     string restAPIVersion = "");
    }
}
