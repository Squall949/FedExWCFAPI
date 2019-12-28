using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services.Protocols;
using FedExShippingWCF.RateService;
using Newtonsoft.Json;

namespace FedExShippingWCF
{
    public partial class FedExShippingService : IFedExShippingService
    {
        public string GetRatesByJson(string shippingRequest)
        {
            FedExWebServiceRequest request = JsonConvert.DeserializeObject<FedExWebServiceRequest>(shippingRequest);
            return JsonConvert.SerializeObject(GetRates(request));
        }

        public FedExRateResult GetRates(FedExWebServiceRequest shippingRequest)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            FedExRateResult result = new FedExRateResult();
            RateService.RateService service = new RateService.RateService();

            RateRequest request = new RateRequest();
            List<string> messages = new List<string>();

            SetBasicInfo(request, shippingRequest);
            SetShipmentDetails(request, shippingRequest);

            try
            {
                RateReply reply = service.getRates(request);
                //
                if (reply.HighestSeverity == NotificationSeverityType.SUCCESS || reply.HighestSeverity == NotificationSeverityType.NOTE || reply.HighestSeverity == NotificationSeverityType.WARNING)
                {
                    result.IsValidated = true;
                    result.NetCharge = reply.RateReplyDetails[0].RatedShipmentDetails[0].ShipmentRateDetail.TotalNetChargeWithDutiesAndTaxes.Amount;
                }
                else
                {
                    result.IsValidated = false;

                    foreach (Notification notification in reply.Notifications)
                    {
                        messages.Add(notification.Message);
                    }
                    result.ErrorMessages = messages.ToArray();
                }
            }
            catch (SoapException err)
            {
                messages.Add(err.Detail.InnerText);
                result.ErrorMessages = messages.ToArray();
                result.IsValidated = false;
            }
            catch (Exception err)
            {
                messages.Add(err.Message);
                result.ErrorMessages = messages.ToArray();
                result.IsValidated = false;
            }

            return result;
        }

        private void SetBasicInfo(RateRequest request, FedExWebServiceRequest shippingRequest)
        {
            request.ClientDetail = new ClientDetail();
            request.ClientDetail.AccountNumber = ConfigurationManager.AppSettings["ACCOUNTNUMBER"];
            request.ClientDetail.MeterNumber = ConfigurationManager.AppSettings["METERNUMBER"];

            request.WebAuthenticationDetail = new WebAuthenticationDetail();
            request.WebAuthenticationDetail.UserCredential = new WebAuthenticationCredential();
            request.WebAuthenticationDetail.UserCredential.Key = ConfigurationManager.AppSettings["USERCREDENTIAL_KEY"];
            request.WebAuthenticationDetail.UserCredential.Password = ConfigurationManager.AppSettings["USERCREDENTIAL_PASSWORD"];

            //request.TransactionDetail = new TransactionDetail();
            //request.TransactionDetail.CustomerTransactionId = "351551101288-1064681263022"; // This is a reference field for the customer.  Any value can be used and will be provided in the response.
            //
            request.Version = new VersionId();
            //main data
            request.RequestedShipment = new RequestedShipment();
            request.RequestedShipment.ShipTimestamp = DateTime.Now; // Shipping date and time
            request.RequestedShipment.DropoffType = DropoffType.REGULAR_PICKUP; //Drop off types are BUSINESS_SERVICE_CENTER, DROP_BOX, REGULAR_PICKUP, REQUEST_COURIER, STATION
            // Default Service Type
            //request.RequestedShipment.ServiceType = ServiceType.FEDEX_GROUND; // Service types are STANDARD_OVERNIGHT, PRIORITY_OVERNIGHT, FEDEX_GROUND ...

            switch (shippingRequest.ServiceType)
            {
                case ShippingServiceType.FEDEX_GROUND:
                    request.RequestedShipment.ServiceType = ServiceType.FEDEX_GROUND;
                    break;
                case ShippingServiceType.FEDEX_2_DAY:
                    request.RequestedShipment.ServiceType = ServiceType.FEDEX_2_DAY;
                    break;
                case ShippingServiceType.FEDEX_EXPRESS_SAVER:
                    request.RequestedShipment.ServiceType = ServiceType.FEDEX_EXPRESS_SAVER;
                    break;
                case ShippingServiceType.GROUND_HOME_DELIVERY:
                    request.RequestedShipment.ServiceType = ServiceType.GROUND_HOME_DELIVERY;
                    break;
                case ShippingServiceType.STANDARD_OVERNIGHT:
                    request.RequestedShipment.ServiceType = ServiceType.STANDARD_OVERNIGHT;
                    break;
                case ShippingServiceType.PRIORITY_OVERNIGHT:
                    request.RequestedShipment.ServiceType = ServiceType.PRIORITY_OVERNIGHT;
                    break;
                case ShippingServiceType.FEDEX_2_DAY_AM:
                    request.RequestedShipment.ServiceType = ServiceType.FEDEX_2_DAY_AM;
                    break;
                case ShippingServiceType.FEDEX_2_DAY_FREIGHT:
                    request.RequestedShipment.ServiceType = ServiceType.FEDEX_2_DAY_FREIGHT;
                    break;
                case ShippingServiceType.FEDEX_3_DAY_FREIGHT:
                    request.RequestedShipment.ServiceType = ServiceType.FEDEX_3_DAY_FREIGHT;
                    break;
                case ShippingServiceType.FIRST_OVERNIGHT:
                    request.RequestedShipment.ServiceType = ServiceType.FIRST_OVERNIGHT;
                    break;
                case ShippingServiceType.INTERNATIONAL_ECONOMY:
                    request.RequestedShipment.ServiceType = ServiceType.INTERNATIONAL_ECONOMY;
                    break;
                case ShippingServiceType.INTERNATIONAL_ECONOMY_FREIGHT:
                    request.RequestedShipment.ServiceType = ServiceType.INTERNATIONAL_ECONOMY_FREIGHT;
                    break;
                case ShippingServiceType.INTERNATIONAL_PRIORITY_FREIGHT:
                    request.RequestedShipment.ServiceType = ServiceType.INTERNATIONAL_PRIORITY_FREIGHT;
                    break;
            }

            request.RequestedShipment.PackagingType = PackagingType.YOUR_PACKAGING; // Packaging type FEDEX_BOK, FEDEX_PAK, FEDEX_TUBE, YOUR_PACKAGING, ...

            request.RequestedShipment.ServiceTypeSpecified = true;
            request.RequestedShipment.PackagingTypeSpecified = true;
        }

        private void SetShipmentDetails(RateRequest request, FedExWebServiceRequest shippingRequest)
        {
            request.RequestedShipment.CustomsClearanceDetail = new CustomsClearanceDetail();
            request.RequestedShipment.CustomsClearanceDetail.CustomsValue = new Money();
            request.RequestedShipment.CustomsClearanceDetail.CustomsValue.Currency = "USD";
            request.RequestedShipment.CustomsClearanceDetail.CustomsValue.Amount = Convert.ToDecimal(shippingRequest.Weight);

            //
            SetOrigin(request, shippingRequest);
            //
            SetDestination(request, shippingRequest);
            //
            SetPackageLineItems(request, shippingRequest);
            //
            request.RequestedShipment.PackageCount = shippingRequest.Qty.Trim();
        }

        private void SetOrigin(RateRequest request, FedExWebServiceRequest shippingRequest)
        {
            request.RequestedShipment.Shipper = new Party();
            request.RequestedShipment.Shipper.Address = new Address();

            List<string> streetLines = new List<string>();

            streetLines.Add(shippingRequest.ShippingAddress1.Trim());
            if (!string.IsNullOrEmpty(shippingRequest.ShippingAddress2.Trim()))
            {
                streetLines.Add(shippingRequest.ShippingAddress2.Trim());
            }
            request.RequestedShipment.Shipper.Address.StreetLines = streetLines.ToArray();
            request.RequestedShipment.Shipper.Address.City = shippingRequest.ShippingCity.Trim();
            request.RequestedShipment.Shipper.Address.StateOrProvinceCode = shippingRequest.ShippingState.Trim();
            request.RequestedShipment.Shipper.Address.PostalCode = shippingRequest.ShippingPostcode.Trim();
            request.RequestedShipment.Shipper.Address.CountryCode = shippingRequest.ShippingCountryCode.Trim();

            request.RequestedShipment.Shipper.Contact = new Contact();

            if (!string.IsNullOrEmpty(shippingRequest.ShippingCName.Trim()))
            {
                request.RequestedShipment.Shipper.Contact.CompanyName = shippingRequest.ShippingCName.Trim();
            }

            if (!string.IsNullOrEmpty(shippingRequest.ShippingTel.Trim()))
            {
                request.RequestedShipment.Shipper.Contact.PhoneNumber = shippingRequest.ShippingTel.Trim();
            }
        }

        private void SetDestination(RateRequest request, FedExWebServiceRequest shippingRequest)
        {
            request.RequestedShipment.Recipient = new Party();
            request.RequestedShipment.Recipient.Address = new Address();

            List<string> streetLines = new List<string>();
            streetLines.Add(shippingRequest.DestAddress1.Trim());

            if (!string.IsNullOrEmpty(shippingRequest.DestAddress2.Trim()))
            {
                streetLines.Add(shippingRequest.DestAddress2.Trim());
            }

            request.RequestedShipment.Recipient.Address.StreetLines = streetLines.ToArray();

            request.RequestedShipment.Recipient.Address.City = shippingRequest.DestCity.Trim();
            request.RequestedShipment.Recipient.Address.StateOrProvinceCode = shippingRequest.DestState.Trim();
            request.RequestedShipment.Recipient.Address.PostalCode = shippingRequest.DestPostcode.Trim();
            request.RequestedShipment.Recipient.Address.CountryCode = shippingRequest.DestCountryCode.Trim();
            request.RequestedShipment.Recipient.Contact = new Contact();

            if (!string.IsNullOrEmpty(shippingRequest.DestCName.Trim()))
            {
                request.RequestedShipment.Recipient.Contact.PersonName = shippingRequest.DestCName.Trim();
            }
            if (!string.IsNullOrEmpty(shippingRequest.DestCompany.Trim()))
            {
                request.RequestedShipment.Recipient.Contact.CompanyName = shippingRequest.DestCompany.Trim();
            }

            if (!string.IsNullOrEmpty(shippingRequest.DestTel.Trim()))
            {
                request.RequestedShipment.Recipient.Contact.PhoneNumber = shippingRequest.DestTel.Trim();
            }

            // HOME_DELIVERY needs to check the residential address
            request.RequestedShipment.Recipient.Address.Residential = request.RequestedShipment.ServiceType == ServiceType.GROUND_HOME_DELIVERY;
            request.RequestedShipment.Recipient.Address.ResidentialSpecified = request.RequestedShipment.ServiceType == ServiceType.GROUND_HOME_DELIVERY;
        }

        private static void SetPackageLineItems(RateRequest request, FedExWebServiceRequest shippingRequest)
        {
            request.RequestedShipment.RequestedPackageLineItems = new RequestedPackageLineItem[1];
            request.RequestedShipment.RequestedPackageLineItems[0] = new RequestedPackageLineItem();
            request.RequestedShipment.RequestedPackageLineItems[0].SequenceNumber = "1"; // package sequence number
            request.RequestedShipment.RequestedPackageLineItems[0].GroupPackageCount = "1";

            // package weight
            request.RequestedShipment.RequestedPackageLineItems[0].Weight = new Weight();
            request.RequestedShipment.RequestedPackageLineItems[0].Weight.Units = WeightUnits.LB;
            request.RequestedShipment.RequestedPackageLineItems[0].Weight.UnitsSpecified = true;
            request.RequestedShipment.RequestedPackageLineItems[0].Weight.Value = Convert.ToDecimal(shippingRequest.Weight);
            request.RequestedShipment.RequestedPackageLineItems[0].Weight.ValueSpecified = true;

            // package dimensions
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions = new Dimensions();
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Length = shippingRequest.Length;
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Width = shippingRequest.Width;
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Height = shippingRequest.Height;
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Units = LinearUnits.IN;
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.UnitsSpecified = true;

            request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences = new CustomerReference[2];

            // if there is Purchase Order (PO) number
            if (!string.IsNullOrEmpty(shippingRequest.PoNo.Trim()))
            {
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[0] = new CustomerReference();
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[0].Value = shippingRequest.PoNo.Trim();
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[0].CustomerReferenceType = CustomerReferenceType.P_O_NUMBER;
            }
            // if there is reference number
            if (!string.IsNullOrEmpty(shippingRequest.Reference.Trim()))
            {
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[1] = new CustomerReference();
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[1].Value = shippingRequest.Reference.Trim();
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[1].CustomerReferenceType = CustomerReferenceType.CUSTOMER_REFERENCE;
            }

            //request.RequestedShipment.RequestedPackageLineItems[0].PhysicalPackaging = PhysicalPackagingType.BAG;
        }
    }
}