using FedExShippingWCF.FedExShipService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace FedExShippingWCF
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IFedExShippingService
    {

        [OperationContract]
        string GetData(int value);

        [OperationContract]
        CompositeType GetDataUsingDataContract(CompositeType composite);

        // TODO: Add your service operations here
        [OperationContract]
        FedExWebServiceResult GetLabelByJson(string jsonTable);

        [OperationContract]
        FedExWebServiceResult GetLabel(FedExWebServiceRequest request);

        [OperationContract]
        string GetRatesByJson(string shippingRequest);

        [OperationContract]
        FedExRateResult GetRates(FedExWebServiceRequest shippingRequest);

        [OperationContract]
        FedExWebServiceResult ValidateAddress(FedExWebServiceRequest request);

        [OperationContract]
        FedExWebServiceResult ValidateServiceAvailability(FedExWebServiceRequest request);

        [OperationContract]
        FedExWebServiceResult CancelShipment(FedExCancelShipmentRequest request);
    }

    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }

        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
    }

    [DataContract]
    public enum ShippingServiceType
    {
        [EnumMember]
        NO_SET = 0,
        [EnumMember]
        FEDEX_GROUND = 92,
        [EnumMember]
        GROUND_HOME_DELIVERY = 90,
        [EnumMember]
        FEDEX_EXPRESS_SAVER = 20,
        [EnumMember]
        STANDARD_OVERNIGHT = 05,
        [EnumMember]
        PRIORITY_OVERNIGHT = 01,
        [EnumMember]
        FEDEX_2_DAY = 03,
        [EnumMember]
        FEDEX_2_DAY_AM = 49,
        [EnumMember]
        FEDEX_2_DAY_FREIGHT = 80,
        [EnumMember]
        FEDEX_3_DAY_FREIGHT = 83,
        [EnumMember]
        FIRST_OVERNIGHT = 06,
        [EnumMember]
        INTERNATIONAL_ECONOMY = 17,
        [EnumMember]
        INTERNATIONAL_ECONOMY_FREIGHT = 86,
        [EnumMember]
        INTERNATIONAL_PRIORITY_FREIGHT = 70
    }

    [DataContract]
    public class FedExWebServiceResult
    {
        bool isValidated = true;
        string[] errorMessages;
        string jsonResult;

        [DataMember]
        public bool IsValidated
        {
            get { return isValidated; }
            set { isValidated = value; }
        }

        [DataMember]
        public string[] ErrorMessages
        {
            get { return errorMessages; }
            set { errorMessages = value; }
        }

        [DataMember]
        public string JsonResult
        {
            get { return jsonResult; }
            set { jsonResult = value; }
        }
    }

    [DataContract]
    public class FedExRateResult : FedExWebServiceResult
    {
        decimal netCharge = 0;

        [DataMember]
        public decimal NetCharge
        {
            get { return netCharge; }
            set { netCharge = value; }
        }
    }

    [DataContract]
    public class FedExWebServiceRequest
    {
        ShippingServiceType serviceType;
        Commodity[] commodities;
        decimal? weight;
        decimal? unitPrice;
        string qty;
        string totalQty;
        string shippingAddress1;
        string shippingAddress2;
        string shippingCity;
        string shippingPostcode;
        string shippingState;
        string shippingCountryCode;
        string shippingCName;
        string shippingTel;
        string destAddress1;
        string destAddress2;
        string destCity;
        string destPostcode;
        string destState;
        string destCountryCode;
        string destCName;
        string destCompany;
        string destTel;
        string destEmail;
        string length;
        string width;
        string height;
        string pono;
        string reference;
        string podate;
        string id_num;
        bool isEmail;
        string emailFail;
        string description;
        string partno;
        string bill_to;
        string shippingAttention;
        string shippingAccNum;
        string tpCName;
        string tpAttention;
        string tpAddress1;
        string tpAddress2;
        string tpCity;
        string tpState;
        string tpPostcode;
        string tpCountry;
        string tpTel;
        string tpAccNum;
        string masterTrackNo;
        string sequenceNumber;
        string recipientAccNum;

        [DataMember]
        public ShippingServiceType ServiceType
        {
            get { return serviceType; }
            set { serviceType = value; }
        }

        [DataMember]
        public Commodity[] Commodity
        {
            get { return commodities; }
            set { commodities = value; }
        }

        [DataMember]
        public decimal? Weight
        {
            get { return weight == null ? 0 : weight; }
            set { weight = value; }
        }

        [DataMember]
        public decimal? UnitPrice
        {
            get { return unitPrice == null ? 0 : unitPrice; }
            set { unitPrice = value; }
        }

        [DataMember]
        public string Qty
        {
            get { return string.IsNullOrEmpty(qty) ? string.Empty : qty; }
            set { qty = value; }
        }

        [DataMember]
        public string TotalQty
        {
            get { return string.IsNullOrEmpty(totalQty) ? string.Empty : totalQty; }
            set { totalQty = value; }
        }

        [DataMember]
        public string ShippingAddress1
        {
            get { return string.IsNullOrEmpty(shippingAddress1) ? string.Empty : shippingAddress1; }
            set { shippingAddress1 = value; }
        }

        [DataMember]
        public string ShippingAddress2
        {
            get {
                return string.IsNullOrEmpty(shippingAddress2) ? string.Empty : shippingAddress2;
            }
            set { shippingAddress2 = value; }
        }

        [DataMember]
        public string ShippingCity
        {
            get { return string.IsNullOrEmpty(shippingCity) ? string.Empty : shippingCity; }
            set { shippingCity = value; }
        }

        [DataMember]
        public string ShippingPostcode
        {
            get { return string.IsNullOrEmpty(shippingPostcode) ? string.Empty : shippingPostcode; }
            set { shippingPostcode = value; }
        }

        [DataMember]
        public string ShippingState
        {
            get { return string.IsNullOrEmpty(shippingState) ? string.Empty : shippingState; }
            set { shippingState = value; }
        }

        [DataMember]
        public string ShippingCountryCode
        {
            get { return string.IsNullOrEmpty(shippingCountryCode) ? string.Empty : shippingCountryCode; }
            set { shippingCountryCode = value; }
        }

        [DataMember]
        public string ShippingCName
        {
            get { return string.IsNullOrEmpty(shippingCName) ? string.Empty : shippingCName; }
            set { shippingCName = value; }
        }

        [DataMember]
        public string ShippingTel
        {
            get { return string.IsNullOrEmpty(shippingTel) ? string.Empty : shippingTel; }
            set { shippingTel = value; }
        }

        [DataMember]
        public string DestAddress1
        {
            get { return string.IsNullOrEmpty(destAddress1) ? string.Empty : destAddress1; }
            set { destAddress1 = value; }
        }

        [DataMember]
        public string DestAddress2
        {
            get { return string.IsNullOrEmpty(destAddress2) ? string.Empty : destAddress2; }
            set { destAddress2 = value; }
        }

        [DataMember]
        public string DestCity
        {
            get { return string.IsNullOrEmpty(destCity) ? string.Empty : destCity; }
            set { destCity = value; }
        }

        [DataMember]
        public string DestPostcode
        {
            get { return string.IsNullOrEmpty(destPostcode) ? string.Empty : destPostcode; }
            set { destPostcode = value; }
        }

        [DataMember]
        public string DestState
        {
            get { return string.IsNullOrEmpty(destState) ? string.Empty : destState; }
            set { destState = value; }
        }

        [DataMember]
        public string DestCountryCode
        {
            get { return string.IsNullOrEmpty(destCountryCode) ? string.Empty : destCountryCode; }
            set { destCountryCode = value; }
        }

        [DataMember]
        public string DestCName
        {
            get { return string.IsNullOrEmpty(destCName) ? string.Empty : destCName; }
            set { destCName = value; }
        }

        [DataMember]
        public string DestTel
        {
            get { return string.IsNullOrEmpty(destTel) ? string.Empty : destTel; }
            set { destTel = value; }
        }

        [DataMember]
        public string DestCompany
        {
            get { return string.IsNullOrEmpty(destCompany) ? string.Empty : destCompany; }
            set { destCompany = value; }
        }

        [DataMember]
        public string DestEmail
        {
            get { return string.IsNullOrEmpty(destEmail) ? string.Empty : destEmail; }
            set { destEmail = value; }
        }

        [DataMember]
        public string Length
        {
            get { return string.IsNullOrEmpty(length) ? string.Empty : length; }
            set { length = value; }
        }

        [DataMember]
        public string Width
        {
            get { return string.IsNullOrEmpty(width) ? string.Empty : width; }
            set { width = value; }
        }

        [DataMember]
        public string Height
        {
            get { return string.IsNullOrEmpty(height) ? string.Empty : height; }
            set { height = value; }
        }

        [DataMember]
        public string PoNo
        {
            get { return string.IsNullOrEmpty(pono) ? string.Empty : pono; }
            set { pono = value; }
        }

        [DataMember]
        public string Reference
        {
            get { return string.IsNullOrEmpty(reference) ? string.Empty : reference; }
            set { reference = value; }
        }

        [DataMember]
        public string Podate
        {
            get { return string.IsNullOrEmpty(podate) ? string.Empty : podate; }
            set { podate = value; }
        }

        [DataMember]
        public string Id_num
        {
            get { return string.IsNullOrEmpty(id_num) ? string.Empty : id_num; }
            set { id_num = value; }
        }

        [DataMember]
        public bool IsEmail
        {
            get { return isEmail; }
            set { isEmail = value; }
        }

        [DataMember]
        public string EmailFail
        {
            get { return string.IsNullOrEmpty(emailFail) ? string.Empty : emailFail; }
            set { emailFail = value; }
        }

        [DataMember]
        public string Description
        {
            get { return string.IsNullOrEmpty(description) ? string.Empty : description; }
            set { description = value; }
        }

        [DataMember]
        public string Partno
        {
            get { return string.IsNullOrEmpty(partno) ? string.Empty : partno; }
            set { partno = value; }
        }

        [DataMember]
        public string Bill_to
        {
            get { return string.IsNullOrEmpty(bill_to) ? string.Empty : bill_to; }
            set { bill_to = value; }
        }

        [DataMember]
        public string ShippingAttention
        {
            get { return string.IsNullOrEmpty(shippingAttention) ? string.Empty : shippingAttention; }
            set { shippingAttention = value; }
        }

        [DataMember]
        public string ShippingAccNum
        {
            get { return string.IsNullOrEmpty(shippingAccNum) ? string.Empty : shippingAccNum; }
            set { shippingAccNum = value; }
        }

        [DataMember]
        public string TpCName
        {
            get { return string.IsNullOrEmpty(tpCName) ? string.Empty : tpCName; }
            set { tpCName = value; }
        }

        [DataMember]
        public string TpAttention
        {
            get { return string.IsNullOrEmpty(tpAttention) ? string.Empty : tpAttention; }
            set { tpAttention = value; }
        }

        [DataMember]
        public string TpAddress1
        {
            get { return string.IsNullOrEmpty(tpAddress1) ? string.Empty : tpAddress1; }
            set { tpAddress1 = value; }
        }

        [DataMember]
        public string TpAddress2
        {
            get { return string.IsNullOrEmpty(tpAddress2) ? string.Empty : tpAddress2; }
            set { tpAddress2 = value; }
        }

        [DataMember]
        public string TpCity
        {
            get { return string.IsNullOrEmpty(tpCity) ? string.Empty : tpCity; }
            set { tpCity = value; }
        }

        [DataMember]
        public string TpState
        {
            get { return string.IsNullOrEmpty(tpState) ? string.Empty : tpState; }
            set { tpState = value; }
        }

        [DataMember]
        public string TpPostcode
        {
            get { return string.IsNullOrEmpty(tpPostcode) ? string.Empty : tpPostcode; }
            set { tpPostcode = value; }
        }

        [DataMember]
        public string TpCountry
        {
            get { return string.IsNullOrEmpty(tpCountry) ? string.Empty : tpCountry; }
            set { tpCountry = value; }
        }

        [DataMember]
        public string TpTel
        {
            get { return string.IsNullOrEmpty(tpTel) ? string.Empty : tpTel; }
            set { tpTel = value; }
        }

        [DataMember]
        public string TpAccNum
        {
            get { return string.IsNullOrEmpty(tpAccNum) ? string.Empty : tpAccNum; }
            set { tpAccNum = value; }
        }

        [DataMember]
        public string MasterTrackNo
        {
            get { return string.IsNullOrEmpty(masterTrackNo) ? string.Empty : masterTrackNo; }
            set { masterTrackNo = value; }
        }

        [DataMember]
        public string SequenceNumber
        {
            get { return string.IsNullOrEmpty(sequenceNumber) ? "1" : sequenceNumber; }
            set { sequenceNumber = value; }
        }

        [DataMember]
        public string RecipientAccNum
        {
            get { return string.IsNullOrEmpty(recipientAccNum) ? string.Empty : recipientAccNum; }
            set { recipientAccNum = value; }
        }
    }

    [DataContract]
    public class FedExCancelShipmentRequest
    {
        TrackingIdType type;
        string trackingNumber;

        [DataMember]
        public TrackingIdType TrackingIdType
        {
            get { return type; }
            set { type = value; }
        }

        [DataMember]
        public string TrackingNumber
        {
            get { return trackingNumber; }
            set { trackingNumber = value; }
        }
    }

    [DataContract]
    public class CheckAddressLengthResult
    {
        string address1;
        string address2;
        bool isValidated = true;

        [DataMember]
        public bool IsValidated
        {
            get { return isValidated; }
            set { isValidated = value; }
        }

        [DataMember]
        public string Address1
        {
            get { return address1; }
            set { address1 = value; }
        }

        [DataMember]
        public string Address2
        {
            get { return address2; }
            set { address2 = value; }
        }
    }

    public class ShipmentResult
    {
        public string TrackNo { get; set; }
        public string Label { get; set; }
    }
}
