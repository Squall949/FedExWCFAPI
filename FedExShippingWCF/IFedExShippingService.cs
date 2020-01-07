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

    public class FedExWebServiceContact
    {
        public string Name { get; set; }
        public string Company { get; set; }
        public string Tel { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string Postcode { get; set; }
        public string State { get; set; }
        public string CountryCode { get; set; }
        public string Email { get; set; }
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
        FedExWebServiceContact shipper;
        FedExWebServiceContact recipient;
        FedExWebServiceContact thirdParty;
        string length;
        string width;
        string height;
        string pono;
        string reference;
        string podate;
        string id_num;
        bool isEmail;
        string description;
        string partno;
        string bill_to;
        string shippingAccNum;
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
        public FedExWebServiceContact Shipper
        {
            get { return shipper == null ? new FedExWebServiceContact() : shipper; }
            set { shipper = value; }
        }

        [DataMember]
        public FedExWebServiceContact Recipient
        {
            get { return recipient == null ? new FedExWebServiceContact() : recipient; }
            set { recipient = value; }
        }

        [DataMember]
        public FedExWebServiceContact ThirdParty
        {
            get { return thirdParty == null ? new FedExWebServiceContact() : thirdParty; }
            set { thirdParty = value; }
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
        public string ShippingAccNum
        {
            get { return string.IsNullOrEmpty(shippingAccNum) ? string.Empty : shippingAccNum; }
            set { shippingAccNum = value; }
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
