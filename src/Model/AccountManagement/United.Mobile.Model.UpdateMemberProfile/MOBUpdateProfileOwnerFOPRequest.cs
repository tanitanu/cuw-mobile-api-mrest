﻿using System;
using United.Mobile.Model.Common;

namespace United.Mobile.Model.UpdateMemberProfile
{
    [Serializable()]
    public class MOBUpdateProfileOwnerFOPRequest : MOBRequest
    {
        private string sessionId = string.Empty;
        private int customerId;
        private string hashPinCode = string.Empty;
        private string mileagePlusNumber = string.Empty;
        private bool updateInsertCreditCardInfo;
        private MOBCPTraveler traveler;
        private bool isSavedToProfile = false;
        private string transactionPath = string.Empty;
        private string redirectViewName = string.Empty;
        private string cartId = string.Empty;
        private string flow = string.Empty;
        private string amount;
        private bool clientCardType = false;
        private bool isCCSelectedForContactless;
        private string token = string.Empty;
        private bool isPartnerProvision = false;
        private string accountReferenceIdentifier;
        private bool isExistCreditCard = false;
        private string partnerRequestIdentifier;

        public string Token
        {
            get
            {
                return token;
            }
            set
            {
                this.token = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public bool IsCCSelectedForContactless
        {
            get { return isCCSelectedForContactless; }
            set { isCCSelectedForContactless = value; }
        }

        //public MOBPersistFormofPaymentRequest FOPInfo
        //{
        //    get { return fopInfo; }
        //    set { fopInfo = value; }
        //}

        public string CartId
        {
            get { return cartId; }
            set { this.cartId = value; }
        }
        public string Flow
        {
            get { return flow; }
            set { this.flow = value; }
        }
        public string Amount
        {
            get { return amount; }
            set { this.amount = value; }
        }
        public bool IsSavedToProfile
        {
            get { return isSavedToProfile; }
            set { isSavedToProfile = value; }
        }

        public string RedirectViewName
        {
            get { return redirectViewName; }
            set { redirectViewName = value; }
        }
        public string TransactionPath
        {
            get { return transactionPath; }
            set { transactionPath = value; }
        }

        public string SessionId
        {
            get
            {
                return sessionId;
            }
            set
            {
                this.sessionId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string MileagePlusNumber
        {
            get
            {
                return mileagePlusNumber;
            }
            set
            {
                this.mileagePlusNumber = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }

        public int CustomerId
        {
            get
            {
                return customerId;
            }
            set
            {
                this.customerId = value;
            }
        }

        public string HashPinCode
        {
            get
            {
                return hashPinCode;
            }
            set
            {
                this.hashPinCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
        public MOBCPTraveler Traveler
        {
            get
            {
                return this.traveler;
            }
            set
            {
                this.traveler = value;
            }
        }
        public bool ClientCardType
        {
            get
            {
                return this.clientCardType;
            }
            set
            {
                this.clientCardType = value;
            }
        }
        public bool UpdateInsertCreditCardInfo
        {
            get { return this.updateInsertCreditCardInfo; }
            set { this.updateInsertCreditCardInfo = value; }
        }

        public bool IsPartnerProvision
        {
            get { return this.isPartnerProvision; }
            set { this.isPartnerProvision = value; }
        }

        public string AccountReferenceIdentifier
        {
            get { return this.accountReferenceIdentifier; }
            set { this.accountReferenceIdentifier = value; }
        }

        public bool IsExistCreditCard
        {
            get { return this.isExistCreditCard; }
            set { this.isExistCreditCard = value; }
        }

        public string PartnerRequestIdentifier
        {
            get { return this.partnerRequestIdentifier; }
            set { this.partnerRequestIdentifier = value; }
        }
    }
}
