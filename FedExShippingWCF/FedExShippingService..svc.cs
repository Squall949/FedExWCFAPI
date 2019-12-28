using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Configuration;
using FedExShippingWCF.FedExShipService;
using System.Web.Services.Protocols;
using System.Net;
using Newtonsoft.Json;

namespace FedExShippingWCF
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public partial class FedExShippingService : IFedExShippingService
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        public FedExWebServiceResult GetLabelByJson(string jsonTable)
        {
            FedExWebServiceResult result = new FedExWebServiceResult();
            ShipService service = new ShipService();

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            DataTable dt = new DataTable();
            DataTable res = new DataTable();

            dt = JsonConvert.DeserializeObject<DataTable>(jsonTable);

            if (dt.Rows.Count == 0)
            {
                result.IsValidated = false;
                result.ErrorMessages = new String[1] { "Data not found!" };

                return result;
            }

            res.Columns.Add("id_num", typeof(int));
            res.Columns.Add("state", typeof(string));
            res.Columns.Add("errorMessage", typeof(string));
            res.Columns.Add("trackno", typeof(string));
            res.Columns.Add("label", typeof(string));

            foreach (DataRow dr in dt.Rows)
            {
                DataRow resRow = res.NewRow();

                resRow.SetField("id_num", dr["id_num"]);
                resRow.SetField("state", !string.IsNullOrWhiteSpace(dr["state"].ToString()) ? dr["state"].ToString().Trim() : "");

                // Start to request infomation of shipment via FedEx Web Service
                ProcessShipmentRequest request = new ProcessShipmentRequest();
                SetBasicInfo(request, dr);
                // set up details
                SetShipmentDetails(request, dr);

                try
                {
                    ProcessShipmentReply reply = service.processShipment(request);

                    if (reply.HighestSeverity == NotificationSeverityType.SUCCESS || reply.HighestSeverity == NotificationSeverityType.NOTE || reply.HighestSeverity == NotificationSeverityType.WARNING)
                    {
                        foreach (var packageDetail in reply.CompletedShipmentDetail.CompletedPackageDetails)
                        {
                            foreach (var part in packageDetail.Label.Parts)
                            {
                                resRow.SetField("label", Convert.ToBase64String(part.Image));
                            }
                        }

                        resRow.SetField("trackno", reply.CompletedShipmentDetail.MasterTrackingId.TrackingNumber);
                        resRow.SetField("errorMessage", "");


                    }
                    else
                    {
                        string messages = "";

                        foreach (Notification notification in reply.Notifications)
                        {
                            messages += notification.Message + "  ";
                        }
                        resRow.SetField("errorMessage", messages);
                    }
                }
                catch (SoapException err)
                {
                    resRow.SetField("errorMessage", err.Detail.InnerText);
                }
                catch (Exception err)
                {
                    resRow.SetField("errorMessage", err.Message);
                }

                res.Rows.Add(resRow);
            }

            result.JsonResult = JsonConvert.SerializeObject(res);

            return result;
        }

        private void SetBasicInfo(ProcessShipmentRequest request, DataRow dr)
        {
            request.ClientDetail = new ClientDetail();
            // AccountNumber for testing
            request.ClientDetail.AccountNumber = ConfigurationManager.AppSettings["ACCOUNTNUMBER"];
            //if (!string.IsNullOrEmpty(dr["sf_fdxaccnum"].ToString()))
            //{
            //    request.ClientDetail.AccountNumber = dr["sf_fdxaccnum"].ToString();
            //}

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
            // request from customers
            ShippingServiceType serviceCode = (ShippingServiceType) Convert.ToInt32(dr["service_type"]);

            switch (serviceCode)
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
        }

        private void SetShipmentDetails(ProcessShipmentRequest request, DataRow dr)
        {
            request.RequestedShipment.CustomsClearanceDetail = new CustomsClearanceDetail();
            request.RequestedShipment.CustomsClearanceDetail.CustomsValue = new Money();
            request.RequestedShipment.CustomsClearanceDetail.CustomsValue.Currency = "USD";
            request.RequestedShipment.CustomsClearanceDetail.CustomsValue.Amount = Convert.ToDecimal(dr["weight"]);

            request.RequestedShipment.ShippingChargesPayment = new Payment();

            if (dr["bill_to"].ToString() == "3")
            {
                request.RequestedShipment.ShippingChargesPayment.PaymentType = PaymentType.THIRD_PARTY;
            }

            request.RequestedShipment.ShippingChargesPayment.Payor = new Payor();
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty = new Party();

            if (request.RequestedShipment.ShippingChargesPayment.PaymentType == PaymentType.THIRD_PARTY)
            {
                request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.AccountNumber = dr["tp_fdxaccnum"].ToString();
                // for test
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["TEST_MODE"]))
                    request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.AccountNumber = ConfigurationManager.AppSettings["ACCOUNTNUMBER"];
            }

            // Label
            request.RequestedShipment.LabelSpecification = new LabelSpecification();
            request.RequestedShipment.LabelSpecification.LabelFormatType = LabelFormatType.COMMON2D;
            request.RequestedShipment.LabelSpecification.ImageType = ShippingDocumentImageType.PNG;
            request.RequestedShipment.LabelSpecification.ImageTypeSpecified = true;
            request.RequestedShipment.LabelSpecification.LabelStockType = LabelStockType.PAPER_4X6;
            request.RequestedShipment.LabelSpecification.LabelStockTypeSpecified = true;

            // Notification
            if (!string.IsNullOrWhiteSpace(dr["email"].ToString()))
            {
                request.RequestedShipment.SpecialServicesRequested = new ShipmentSpecialServicesRequested();
                request.RequestedShipment.SpecialServicesRequested.SpecialServiceTypes = new ShipmentSpecialServiceType[] { ShipmentSpecialServiceType.EVENT_NOTIFICATION };
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail = new ShipmentEventNotificationDetail();
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications = new ShipmentEventNotificationSpecification[1];
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0] = new ShipmentEventNotificationSpecification();
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail = new NotificationDetail();
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail.NotificationType = NotificationType.EMAIL;
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail.NotificationTypeSpecified = dr["isemail"].ToString().Trim() == "Y";
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail.EmailDetail = new EMailDetail();
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail.EmailDetail.EmailAddress = dr["email"].ToString().Trim();
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].Role = ShipmentNotificationRoleType.RECIPIENT;
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].RoleSpecified = dr["isemail"].ToString().Trim() == "Y";
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].Events = new NotificationEventType[2] { NotificationEventType.ON_DELIVERY, NotificationEventType.ON_SHIPMENT };
            }
            

            // Commodity
            if (dr["sf_country"].ToString().Trim() == "US" && (dr["country"].ToString().Trim() == "CA" || dr["state"].ToString().Trim().ToUpper() == "PR"))
            {
                SetCommodity(request, dr);
            }

            //
            SetOrigin(request, dr);
            //
            SetDestination(request, dr);
            //
            SetPackageLineItems(request, dr);
            //
            request.RequestedShipment.PackageCount = dr["qty"].ToString().Trim();
        }

        private void SetCommodity(ProcessShipmentRequest request, DataRow dr)
        {
            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment = new Payment();
            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment.PaymentType = request.RequestedShipment.ShippingChargesPayment.PaymentType;

            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment.Payor = new Payor();
            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment.Payor.ResponsibleParty = new Party();
            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment.Payor.ResponsibleParty.AccountNumber = request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.AccountNumber;

            request.RequestedShipment.CustomsClearanceDetail.Commodities = new Commodity[1];
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0] = new Commodity();
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].NumberOfPieces = "1";
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].CountryOfManufacture = dr["mf_country"].ToString();
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].Weight = new Weight();
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].Weight.Units = WeightUnits.LB;
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].Weight.Value = Convert.ToDecimal(dr["weight"]);
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].Description = dr["description"].ToString();
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].QuantityUnits = "EA";
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].Quantity = 1;
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].QuantitySpecified = true;
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].UnitPrice = new Money();
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].UnitPrice.Currency = "USD";
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].UnitPrice.Amount = Convert.ToDecimal(dr["unit_price"]);
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].Purpose = (CommodityPurposeType)Convert.ToInt32(dr["purpose"]);
            request.RequestedShipment.CustomsClearanceDetail.Commodities[0].PartNumber = dr["commodity_partno"].ToString(); 
        }

        private void SetOrigin(ProcessShipmentRequest request, DataRow dr)
        {
            request.RequestedShipment.Shipper = new Party();
            request.RequestedShipment.Shipper.Address = new Address();

            List<string> streetLines = new List<string>();
            streetLines.Add(dr["sf_address1"].ToString().Trim());

            if (!string.IsNullOrEmpty(dr["sf_address2"].ToString().Trim()))
            {
                streetLines.Add(dr["sf_address2"].ToString().Trim());
            }

            request.RequestedShipment.Shipper.Address.StreetLines = streetLines.ToArray();

            request.RequestedShipment.Shipper.Address.City = dr["sf_city"].ToString().Trim();
            request.RequestedShipment.Shipper.Address.StateOrProvinceCode = dr["sf_state"].ToString().Trim();
            request.RequestedShipment.Shipper.Address.PostalCode = dr["sf_postcode"].ToString().Trim();
            request.RequestedShipment.Shipper.Address.CountryCode = dr["sf_country"].ToString().Trim();
            request.RequestedShipment.Shipper.Contact = new Contact();

            if (!string.IsNullOrEmpty(dr["sf_cname"].ToString().Trim()))
            {
                request.RequestedShipment.Shipper.Contact.CompanyName = dr["sf_cname"].ToString().Trim();
            }

            if (!string.IsNullOrEmpty(dr["sf_tel"].ToString().Trim()))
            {
                request.RequestedShipment.Shipper.Contact.PhoneNumber = dr["sf_tel"].ToString().Trim();
            }
        }

        private void SetDestination(ProcessShipmentRequest request, DataRow dr)
        {
            request.RequestedShipment.Recipient = new Party();
            request.RequestedShipment.Recipient.Address = new Address();

            List<string> streetLines = new List<string>();
            streetLines.Add(dr["address1"].ToString().Trim());

            if (!string.IsNullOrEmpty(dr["address2"].ToString().Trim()))
            {
                streetLines.Add(dr["address2"].ToString().Trim());
            }

            request.RequestedShipment.Recipient.Address.StreetLines = streetLines.ToArray();

            request.RequestedShipment.Recipient.Address.City = dr["city"].ToString().Trim();
            request.RequestedShipment.Recipient.Address.StateOrProvinceCode = dr["state"].ToString().Trim();
            request.RequestedShipment.Recipient.Address.PostalCode = dr["postcode"].ToString().Trim();
            request.RequestedShipment.Recipient.Address.CountryCode = dr["country"].ToString().Trim();
            request.RequestedShipment.Recipient.Contact = new Contact();

            if (!string.IsNullOrEmpty(dr["cname"].ToString().Trim()))
            {
                request.RequestedShipment.Recipient.Contact.PersonName = dr["cname"].ToString().Trim();
            }
            if (!string.IsNullOrEmpty(dr["company"].ToString().Trim()))
            {
                request.RequestedShipment.Recipient.Contact.CompanyName = dr["company"].ToString().Trim();
            }

            if (!string.IsNullOrEmpty(dr["tel"].ToString().Trim()))
            {
                request.RequestedShipment.Recipient.Contact.PhoneNumber = dr["tel"].ToString().Trim();
            }

            // HOME_DELIVERY needs to check the residential address
            request.RequestedShipment.Recipient.Address.Residential = request.RequestedShipment.ServiceType == ServiceType.GROUND_HOME_DELIVERY;
            request.RequestedShipment.Recipient.Address.ResidentialSpecified = request.RequestedShipment.ServiceType == ServiceType.GROUND_HOME_DELIVERY;
        }

        private static void SetPackageLineItems(ProcessShipmentRequest request, DataRow dr)
        {
            request.RequestedShipment.RequestedPackageLineItems = new RequestedPackageLineItem[1];
            request.RequestedShipment.RequestedPackageLineItems[0] = new RequestedPackageLineItem();
            request.RequestedShipment.RequestedPackageLineItems[0].SequenceNumber = "1"; // package sequence number
            request.RequestedShipment.RequestedPackageLineItems[0].GroupPackageCount = "1";

            // package weight
            request.RequestedShipment.RequestedPackageLineItems[0].Weight = new Weight();
            request.RequestedShipment.RequestedPackageLineItems[0].Weight.Units = WeightUnits.LB;
            request.RequestedShipment.RequestedPackageLineItems[0].Weight.Value = Convert.ToDecimal(dr["weight"]);

            // package dimensions
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions = new Dimensions();
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Length = dr["length"].ToString();
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Width = dr["width"].ToString();
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Height = dr["height"].ToString();
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Units = LinearUnits.IN;

            request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences = new CustomerReference[2];

            // if there is Purchase Order (PO) number
            if (!string.IsNullOrEmpty(dr["pono"].ToString().Trim()))
            {
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[0] = new CustomerReference();
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[0].Value = dr["pono"].ToString().Trim();
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[0].CustomerReferenceType = CustomerReferenceType.P_O_NUMBER;
            }
            // if there is reference number
            if (!string.IsNullOrEmpty(dr["reference"].ToString().Trim()))
            {
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[1] = new CustomerReference();
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[1].Value = dr["reference"].ToString().Trim();
                request.RequestedShipment.RequestedPackageLineItems[0].CustomerReferences[1].CustomerReferenceType = CustomerReferenceType.CUSTOMER_REFERENCE;
            }

            //request.RequestedShipment.RequestedPackageLineItems[0].PhysicalPackaging = PhysicalPackagingType.BAG;
        }

        public FedExWebServiceResult CancelShipment(FedExCancelShipmentRequest cancelRequest)
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            FedExWebServiceResult result = new FedExWebServiceResult();
            DeleteShipmentRequest request = new DeleteShipmentRequest();

            request.ClientDetail = new ClientDetail();
            request.ClientDetail.AccountNumber = ConfigurationManager.AppSettings["ACCOUNTNUMBER"];
            request.ClientDetail.MeterNumber = ConfigurationManager.AppSettings["METERNUMBER"];

            request.WebAuthenticationDetail = new WebAuthenticationDetail();
            request.WebAuthenticationDetail.UserCredential = new WebAuthenticationCredential();
            request.WebAuthenticationDetail.UserCredential.Key = ConfigurationManager.AppSettings["USERCREDENTIAL_KEY"];
            request.WebAuthenticationDetail.UserCredential.Password = ConfigurationManager.AppSettings["USERCREDENTIAL_PASSWORD"];
            request.Version = new VersionId();
            request.TrackingId = new TrackingId();
            request.TrackingId.TrackingIdType = cancelRequest.TrackingIdType;
            request.TrackingId.TrackingIdTypeSpecified = true;
            request.TrackingId.TrackingNumber = cancelRequest.TrackingNumber;

            List<string> messages = new List<string>();
            ShipService service = new ShipService();

            try
            {
                ShipmentReply reply = service.deleteShipment(request);

                if (reply.HighestSeverity == NotificationSeverityType.SUCCESS || reply.HighestSeverity == NotificationSeverityType.NOTE || reply.HighestSeverity == NotificationSeverityType.WARNING)
                {
                    result.IsValidated = true;
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

        public FedExWebServiceResult GetLabel(FedExWebServiceRequest request)
        {
            FedExWebServiceResult result = new FedExWebServiceResult();
            ProcessShipmentRequest processReq = new ProcessShipmentRequest();
            ShipService service = new ShipService();
            ShipmentResult label = new ShipmentResult();
            List<string> messages = new List<string>();

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            SetBasicInfo(processReq, request);
            SetShipmentDetails(processReq, request);

            try
            {
                ProcessShipmentReply reply = service.processShipment(processReq);

                if (reply.HighestSeverity == NotificationSeverityType.SUCCESS || reply.HighestSeverity == NotificationSeverityType.NOTE || reply.HighestSeverity == NotificationSeverityType.WARNING)
                {
                    foreach (var packageDetail in reply.CompletedShipmentDetail.CompletedPackageDetails)
                    {
                        foreach (var part in packageDetail.Label.Parts)
                        {
                            label.Label = Convert.ToBase64String(part.Image);
                            label.TrackNo = packageDetail.TrackingIds[0].TrackingNumber;
                            result.JsonResult = JsonConvert.SerializeObject(label);
                        }
                    }

                    messages.Add("");
                }
                else
                {
                    string message = "";

                    foreach (Notification notification in reply.Notifications)
                    {
                        message += notification.Message + "  ";
                    }

                    messages.Add(message);

                    result.IsValidated = false;
                }
            }
            catch (SoapException err)
            {
                messages.Add(err.Detail.InnerText);
                result.IsValidated = false;

            }
            catch (Exception err)
            {
                messages.Add(err.Message);
                result.IsValidated = false;
            }

            result.ErrorMessages = messages.ToArray();

            return result;
        }

        private void SetBasicInfo(ProcessShipmentRequest request, FedExWebServiceRequest shippingRequest)
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
        }

        private void SetShipmentDetails(ProcessShipmentRequest request, FedExWebServiceRequest shippingRequest)
        {
            request.RequestedShipment.CustomsClearanceDetail = new CustomsClearanceDetail();
            request.RequestedShipment.CustomsClearanceDetail.CustomsValue = new Money();
            request.RequestedShipment.CustomsClearanceDetail.CustomsValue.Currency = "USD";
            request.RequestedShipment.CustomsClearanceDetail.CustomsValue.Amount = Convert.ToDecimal(shippingRequest.Weight);

            request.RequestedShipment.ShippingChargesPayment = new Payment();
            request.RequestedShipment.ShippingChargesPayment.Payor = new Payor();
            request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty = new Party();

            switch (shippingRequest.Bill_to)
            {
                case "1":
                    request.RequestedShipment.ShippingChargesPayment.PaymentType = PaymentType.SENDER;
                    request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.AccountNumber = string.IsNullOrEmpty(shippingRequest.ShippingAccNum) ? ConfigurationManager.AppSettings["ACCOUNTNUMBER"] : shippingRequest.ShippingAccNum;
                    break;
                case "2":
                    request.RequestedShipment.ShippingChargesPayment.PaymentType = PaymentType.RECIPIENT;
                    request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.AccountNumber = shippingRequest.RecipientAccNum;
                    break;
                case "3":
                    request.RequestedShipment.ShippingChargesPayment.PaymentType = PaymentType.THIRD_PARTY;
                    request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.AccountNumber = ConfigurationManager.AppSettings["ACCOUNTNUMBER"];
                    break;
            }
            
            if (!Convert.ToBoolean(ConfigurationManager.AppSettings["TEST_MODE"]) && request.RequestedShipment.ShippingChargesPayment.PaymentType == PaymentType.THIRD_PARTY)
            {
                request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.AccountNumber = shippingRequest.TpAccNum;
            }

            // Label
            request.RequestedShipment.LabelSpecification = new LabelSpecification();
            request.RequestedShipment.LabelSpecification.LabelFormatType = LabelFormatType.COMMON2D;
            request.RequestedShipment.LabelSpecification.ImageType = ShippingDocumentImageType.PNG;
            request.RequestedShipment.LabelSpecification.ImageTypeSpecified = true;
            request.RequestedShipment.LabelSpecification.LabelStockType = LabelStockType.PAPER_4X6;
            request.RequestedShipment.LabelSpecification.LabelStockTypeSpecified = true;

            // Notification
            request.RequestedShipment.SpecialServicesRequested = new ShipmentSpecialServicesRequested();

            if (shippingRequest.IsEmail && !string.IsNullOrWhiteSpace(shippingRequest.DestEmail))
            {
                request.RequestedShipment.SpecialServicesRequested.SpecialServiceTypes = new ShipmentSpecialServiceType[] { ShipmentSpecialServiceType.EVENT_NOTIFICATION };
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail = new ShipmentEventNotificationDetail();
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications = new ShipmentEventNotificationSpecification[1];
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0] = new ShipmentEventNotificationSpecification();
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail = new NotificationDetail();
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail.NotificationType = NotificationType.EMAIL;
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail.NotificationTypeSpecified = true;
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail.EmailDetail = new EMailDetail();
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].NotificationDetail.EmailDetail.EmailAddress = shippingRequest.DestEmail;
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].Role = ShipmentNotificationRoleType.RECIPIENT;
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].RoleSpecified = true;
                request.RequestedShipment.SpecialServicesRequested.EventNotificationDetail.EventNotifications[0].Events = new NotificationEventType[2] { NotificationEventType.ON_DELIVERY, NotificationEventType.ON_SHIPMENT };
            }

            // Commodity
            if (shippingRequest.Commodity != null)
            {
                SetCommodity(request, shippingRequest);
            }

            //
            SetOrigin(request, shippingRequest);
            //
            SetDestination(request, shippingRequest);
            //
            SetPackageLineItems(request, shippingRequest);
            //
            request.RequestedShipment.PackageCount = string.IsNullOrEmpty(shippingRequest.TotalQty) ? shippingRequest.Qty : shippingRequest.TotalQty;
            // multiple packages
            if (!string.IsNullOrEmpty(shippingRequest.MasterTrackNo))
            {
                request.RequestedShipment.MasterTrackingId = new TrackingId();
                request.RequestedShipment.MasterTrackingId.TrackingNumber = shippingRequest.MasterTrackNo;
            }
            // international multiple packages
            if (shippingRequest.Commodity != null && shippingRequest.Commodity.Length > 1)
            {
                decimal value = 0;

                foreach (Commodity comd in shippingRequest.Commodity)
                {
                    value += comd.Weight.Value;
                }

                request.RequestedShipment.TotalWeight = new Weight();
                request.RequestedShipment.TotalWeight.Value = value;
                request.RequestedShipment.TotalWeight.Units = WeightUnits.LB;
            }
        }

        private void SetCommodity(ProcessShipmentRequest request, FedExWebServiceRequest shippingRequest)
        {
            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment = new Payment();
            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment.PaymentType = request.RequestedShipment.ShippingChargesPayment.PaymentType;

            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment.Payor = new Payor();
            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment.Payor.ResponsibleParty = new Party();
            request.RequestedShipment.CustomsClearanceDetail.DutiesPayment.Payor.ResponsibleParty.AccountNumber = request.RequestedShipment.ShippingChargesPayment.Payor.ResponsibleParty.AccountNumber;

            request.RequestedShipment.CustomsClearanceDetail.Commodities = shippingRequest.Commodity;
        }

        private void SetOrigin(ProcessShipmentRequest request, FedExWebServiceRequest shippingRequest)
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

        private void SetDestination(ProcessShipmentRequest request, FedExWebServiceRequest shippingRequest)
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

        private static void SetPackageLineItems(ProcessShipmentRequest request, FedExWebServiceRequest shippingRequest)
        {
            request.RequestedShipment.RequestedPackageLineItems = new RequestedPackageLineItem[1];
            request.RequestedShipment.RequestedPackageLineItems[0] = new RequestedPackageLineItem();
            request.RequestedShipment.RequestedPackageLineItems[0].SequenceNumber = shippingRequest.SequenceNumber; // package sequence number
            request.RequestedShipment.RequestedPackageLineItems[0].GroupPackageCount = "1";

            // package weight
            request.RequestedShipment.RequestedPackageLineItems[0].Weight = new Weight();
            request.RequestedShipment.RequestedPackageLineItems[0].Weight.Units = WeightUnits.LB;
            request.RequestedShipment.RequestedPackageLineItems[0].Weight.Value = Convert.ToDecimal(shippingRequest.Weight);

            // package dimensions
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions = new Dimensions();
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Length = shippingRequest.Length;
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Width = shippingRequest.Width;
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Height = shippingRequest.Height;
            request.RequestedShipment.RequestedPackageLineItems[0].Dimensions.Units = LinearUnits.IN;

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
