// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.VisualStudio.TestTools.WebTesting;

namespace Fabrikam.Ingress.Ingestion.LoadTests
{
    public class DeliveryRequestWebTest : WebTest
    {
        private const string ContextParamIngestUrl = "INGEST_URL";

        private const string DeliveryId = "42";
        private const string PackageId = "442";
        private const string PackageLocationDropOff = "0,37.4315730000000,-78.65689399999997";
        private const string PackageLocationPickup = "0,36.778261,-119.41793200000001";

        public DeliveryRequestWebTest()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = RemoteCertificateValidationCallBack;
        }

        public static bool RemoteCertificateValidationCallBack(
            object sender, 
            X509Certificate certificate, 
            X509Chain chain, 
            SslPolicyErrors sslPolicyErrors) => true;

        public override IEnumerator<WebTestRequest> GetRequestEnumerator()
        {
            Uri deliveryRequestUri = this.CreateDeliveryRequestUri();

            var deliveryRequest = new WebTestRequest(deliveryRequestUri)
            {
                Method = "POST",
                Body = CreateRandomHttpBodyString()
            };

            yield return deliveryRequest;
        }

        private Uri CreateDeliveryRequestUri()
        {
            if (!(this.Context[ContextParamIngestUrl].ToString() is var ingestUrl
                && !string.IsNullOrEmpty(ingestUrl)))
            {
                throw new ArgumentNullException($"{ContextParamIngestUrl} load test context param value can not be null");
            }

            if (!Uri.TryCreate(
                ingestUrl,
                UriKind.Absolute,
                out Uri ingestUri))
            {
                throw new ArgumentException($"{ingestUrl} is not a valid absolute URI");
            }

            if (!Uri.TryCreate(
                    ingestUri,
                    "/api/deliveryrequests",
                    out Uri deliveryRequestUri))
            {
                throw new ArgumentException($"{ingestUrl}/api/deliveryrequests is not a valid URI");
            }

            return deliveryRequestUri;
        }

        private static StringHttpBody CreateRandomHttpBodyString()
        {
            Guid tag = Guid.NewGuid();

            var httpBodyRequestWithRandomTag = 
                new StringHttpBody
            {
                ContentType = "application/json",
                InsertByteOrderMark = false,
                BodyString = $@"{{
    ""confirmationRequired"": ""FingerPrint"",
    ""deadline"": ""DeadlyQueueOfZombiatedDemons"",
    ""deliveryId"": ""{DeliveryId}"",
    ""dropOffLocation"": ""{PackageLocationDropOff}"",
    ""expedited"": true,
    ""ownerId"": ""1"",
    ""packageInfo"": {{
        ""packageId"": ""{PackageId}"",
        ""size"": ""Small"",
        ""tag"": ""{tag}"",
        ""weight"": 14
    }},
    ""pickupLocation"": ""{PackageLocationPickup}"",
    ""pickupTime"": ""2017-11-08T23:26:30.118Z""
}}"
            };

            return httpBodyRequestWithRandomTag;
        }
    }
}