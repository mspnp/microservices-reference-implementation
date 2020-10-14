using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Fabrikam.DroneDelivery.WebSite.Handlers
{
    public class IgnoreSSLValidateDelegatingHandler : DelegatingHandler
    {
        private readonly X509CertificateCollection _certificates = new X509CertificateCollection();
        public IgnoreSSLValidateDelegatingHandler()
        {
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var inner = InnerHandler;
            while (inner is DelegatingHandler)
            {
                inner = ((DelegatingHandler)inner).InnerHandler;
            }
            // inner is HttpClientHandler
            if (inner is HttpClientHandler httpClientHandler)
            {
                if (httpClientHandler.ServerCertificateCustomValidationCallback == null)
                {
                    httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                    {
                        return true;
                    };
                }
            }
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
