// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EdFi.Admin.LearningStandards.Core.Auth
{
    public class AcademicBenchmarksAuthApiManager : IAuthApiManager
    {
        private readonly IAuthenticationConfiguration _authenticationConfiguration;

        private readonly ILearningStandardsProviderConfiguration _learningStandardsProviderConfiguration;

        private readonly ILogger<AcademicBenchmarksAuthApiManager> _logger;

        private const int DefaultTimeWindow = 300;

        private string _token;

        private DateTime _utcExpiration;

        private string _signature;

        public AcademicBenchmarksAuthApiManager(
            IOptionsSnapshot<AcademicBenchmarksOptions> academicBenchmarksOptions,
            IAuthenticationConfiguration authenticationConfiguration,
            ILogger<AcademicBenchmarksAuthApiManager> logger)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(authenticationConfiguration, nameof(authenticationConfiguration));

            _learningStandardsProviderConfiguration = academicBenchmarksOptions?.Value;
            _authenticationConfiguration = authenticationConfiguration;
            _logger = logger;
        }


        /// <summary>
        /// Create a HttpRequest that includes authentication info
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="url"></param>
        /// <param name="queryStringValues"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task<HttpRequestMessage> GetAuthenticatedRequestAsync(
            HttpMethod httpMethod,
            Uri uri,
            HttpContent content = null
            )

        {
            var requestBuilder = new UriBuilder(uri);
            var queryString = HttpUtility.ParseQueryString(requestBuilder.Query);


            // add authentication to query
            var newExpiration = DateTime.UtcNow.AddMinutes(DefaultTimeWindow);
            var unixUtcExpiration = GetUnixTime(newExpiration);
            string sig = CreateSignature(_authenticationConfiguration.Secret, unixUtcExpiration);

            queryString["partner.id"] = _authenticationConfiguration.Key;
            queryString["auth.signature"] = sig;
            queryString["auth.expires"] = unixUtcExpiration.ToString();

            requestBuilder.Query = queryString.ToString();

            return Task.FromResult(new HttpRequestMessage(httpMethod, requestBuilder.Uri)
            {
                Content = content
            });
        }

        /// <summary>
        ///     Creates a base64 encoded HMACSHA256 signature
        /// </summary>
        /// <param name="partnerKey">The AB-Connect partner key</param>
        /// <param name="unixExpiration">The token expiration in UNIX time</param>
        /// <returns>base64 encoded HMACSHA256 string</returns>
        private string CreateSignature(string partnerKey, long unixExpiration, string userId = "")
        {
            var keyBytes = Encoding.UTF8.GetBytes(partnerKey);
            var messageBytes = Encoding.UTF8.GetBytes(string.Format("{0}\n{1}", unixExpiration.ToString(), userId));

            string signature;
            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                signature = Convert.ToBase64String(hmac.ComputeHash(messageBytes));
            }

            return signature;
        }

        /// <summary>
        ///     Creates a new token based on the specified parameters
        /// </summary>
        /// <param name="partnerId">The AB-Connect partner id</param>
        /// <param name="expiration">The token expiration in UNIX time</param>
        /// <param name="signature">The base64 encoded HMACSHA256 signature</param>
        /// <returns></returns>
        private string CreateBase64Token(string partnerId, long expiration, string signature)
        {
            Check.NotEmpty(partnerId, nameof(partnerId));
            Check.NotEmpty(signature, nameof(signature));

            JObject j = new JObject
            {
                ["auth"] = new JObject
                {
                    ["partner.id"] = partnerId,
                    ["auth.expires"] = expiration,
                    ["auth.signature"] = signature
                }
            };
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(j.ToString()));
        }

        private long GetUnixTime(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }

    }
}
