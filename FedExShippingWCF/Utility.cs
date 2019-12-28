using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services.Protocols;
using FedExShippingWCF.FedExAddressValidationService;
using FedExShippingWCF.FedExAvailabilityValidationService;
using Newtonsoft.Json;

namespace FedExShippingWCF
{
    public partial class FedExShippingService : IFedExShippingService
    {
        #region Address Validation

        public FedExWebServiceResult ValidateAddress(FedExWebServiceRequest shippingRequest)
        {
            AddressValidationService service = new AddressValidationService();

            AddressValidationRequest request = CreateAddressValidationRequest(shippingRequest);
            FedExWebServiceResult verifyResult = new FedExWebServiceResult();
            List<string> messages = new List<string>();

            try
            {
                // Call the AddressValidationService passing in an AddressValidationRequest and returning an AddressValidationReply
                AddressValidationReply reply = service.addressValidation(request);
                //
                if (reply.HighestSeverity == FedExAddressValidationService.NotificationSeverityType.SUCCESS || reply.HighestSeverity == FedExAddressValidationService.NotificationSeverityType.NOTE || reply.HighestSeverity == FedExAddressValidationService.NotificationSeverityType.WARNING)
                {
                    verifyResult.IsValidated = true;
                }
                else
                {
                    verifyResult.IsValidated = false;

                    foreach (FedExAddressValidationService.Notification notification in reply.Notifications)
                    {
                        messages.Add(notification.Message);
                    }
                    verifyResult.ErrorMessages = messages.ToArray();
                }
            }
            catch (SoapException err)
            {
                messages.Add(err.Detail.InnerText);
                verifyResult.ErrorMessages = messages.ToArray();
                verifyResult.IsValidated = false;
            }
            catch (Exception err)
            {
                messages.Add(err.Message);
                verifyResult.ErrorMessages = messages.ToArray();
                verifyResult.IsValidated = false;
            }

            return verifyResult;
        }

        private AddressValidationRequest CreateAddressValidationRequest(FedExWebServiceRequest req)
        {
            AddressValidationRequest request = new AddressValidationRequest();
            request.ClientDetail = new FedExAddressValidationService.ClientDetail();
            request.ClientDetail.AccountNumber = ConfigurationManager.AppSettings["ACCOUNTNUMBER"];
            request.ClientDetail.MeterNumber = ConfigurationManager.AppSettings["METERNUMBER"];

            request.WebAuthenticationDetail = new FedExAddressValidationService.WebAuthenticationDetail();
            request.WebAuthenticationDetail.UserCredential = new FedExAddressValidationService.WebAuthenticationCredential();
            request.WebAuthenticationDetail.UserCredential.Key = ConfigurationManager.AppSettings["USERCREDENTIAL_KEY"];
            request.WebAuthenticationDetail.UserCredential.Password = ConfigurationManager.AppSettings["USERCREDENTIAL_PASSWORD"];
            //
            request.Version = new FedExAddressValidationService.VersionId();
            //
            SetAddress(request, req);
            //
            return request;
        }

        private void SetAddress(AddressValidationRequest request, FedExWebServiceRequest req)
        {
            request.AddressesToValidate = new AddressToValidate[1];
            request.AddressesToValidate[0] = new AddressToValidate();
            request.AddressesToValidate[0].ClientReferenceId = "ClientReferenceId1";
            request.AddressesToValidate[0].Address = new FedExAddressValidationService.Address();

            List<string> streetLines = new List<string>();
            streetLines.Add(req.DestAddress1.Trim());

            if (!string.IsNullOrEmpty(req.DestAddress2.Trim()))
            {
                streetLines.Add(req.DestAddress2.Trim());
            }
            request.AddressesToValidate[0].Address.StreetLines = streetLines.ToArray();

            request.AddressesToValidate[0].Address.PostalCode = req.DestPostcode.Trim();
            request.AddressesToValidate[0].Address.City = req.DestCity.Trim();
            request.AddressesToValidate[0].Address.StateOrProvinceCode = req.DestState.Trim();
            request.AddressesToValidate[0].Address.CountryCode = req.DestCountryCode.Trim();
        }

        #endregion

        #region Service Validation

        public FedExWebServiceResult ValidateServiceAvailability(FedExWebServiceRequest shippingRequest)
        {
            ValidationAvailabilityAndCommitmentService service = new ValidationAvailabilityAndCommitmentService();

            ServiceAvailabilityRequest request = CreateServiceAvailabilityValidationRequest(shippingRequest);

            FedExWebServiceResult verifyResult = new FedExWebServiceResult();
            List<string> messages = new List<string>();

            try
            {
                // Call the AddressValidationService passing in an AddressValidationRequest and returning an AddressValidationReply
                ServiceAvailabilityReply reply = service.serviceAvailability(request);
                //
                if (reply.HighestSeverity == FedExAvailabilityValidationService.NotificationSeverityType.SUCCESS || reply.HighestSeverity == FedExAvailabilityValidationService.NotificationSeverityType.NOTE || reply.HighestSeverity == FedExAvailabilityValidationService.NotificationSeverityType.WARNING)
                {
                    verifyResult.IsValidated = true;
                }
                else
                {
                    verifyResult.IsValidated = false;

                    foreach (FedExAvailabilityValidationService.Notification notification in reply.Notifications)
                    {
                        messages.Add(notification.Message);
                    }
                    verifyResult.ErrorMessages = messages.ToArray();
                }
            }
            catch (SoapException err)
            {
                messages.Add(err.Detail.InnerText);
                verifyResult.ErrorMessages = messages.ToArray();
                verifyResult.IsValidated = false;
            }
            catch (Exception err)
            {
                messages.Add(err.Message);
                verifyResult.ErrorMessages = messages.ToArray();
                verifyResult.IsValidated = false;
            }

            return verifyResult;
        }

        private ServiceAvailabilityRequest CreateServiceAvailabilityValidationRequest(FedExWebServiceRequest req)
        {
            ServiceAvailabilityRequest request = new ServiceAvailabilityRequest();
            request.ClientDetail = new FedExAvailabilityValidationService.ClientDetail();
            request.ClientDetail.AccountNumber = ConfigurationManager.AppSettings["ACCOUNTNUMBER"];
            request.ClientDetail.MeterNumber = ConfigurationManager.AppSettings["METERNUMBER"];

            request.WebAuthenticationDetail = new FedExAvailabilityValidationService.WebAuthenticationDetail();
            request.WebAuthenticationDetail.UserCredential = new FedExAvailabilityValidationService.WebAuthenticationCredential();
            request.WebAuthenticationDetail.UserCredential.Key = ConfigurationManager.AppSettings["USERCREDENTIAL_KEY"];
            request.WebAuthenticationDetail.UserCredential.Password = ConfigurationManager.AppSettings["USERCREDENTIAL_PASSWORD"];
            //
            request.Version = new FedExAvailabilityValidationService.VersionId();
            //
            request.Origin = new FedExAvailabilityValidationService.Address(); // Origin information
            request.Origin.PostalCode = req.ShippingPostcode.Trim();
            request.Origin.CountryCode = req.ShippingCountryCode.Trim();
            //
            request.Destination = new FedExAvailabilityValidationService.Address(); // Destination information
            request.Destination.PostalCode = req.DestPostcode.Trim();
            request.Destination.CountryCode = req.DestCountryCode.Trim();
            //
            request.ShipDate = DateTime.Now; // Shipping date and time

            //request.CarrierCode = FedExAvailabilityValidationService.CarrierCodeType.FDXE; // Carrier code types are FDXC(Cargo), FDXE(Express), FDXG(Ground), FXCC(Custom Critical), FXFX(Freight)

            //If a service is specified it will be checked, if no service is specified all available services will be returned
            ShippingServiceType serviceCode = (ShippingServiceType)Convert.ToInt32(req.ServiceType);
            request.Service = (FedExAvailabilityValidationService.ServiceType) Enum.Parse(typeof(FedExAvailabilityValidationService.ServiceType), serviceCode.ToString());
            request.ServiceSpecified = true;

            request.Packaging = FedExAvailabilityValidationService.PackagingType.YOUR_PACKAGING; // Packaging type FEDEX_BOX, FEDEX_PAK, FEDEX_TUBE, YOUR_PACKAGING, ...
            request.PackagingSpecified = true;

            return request;
        }

        #endregion
    }
}