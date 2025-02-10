// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Auth
{
    public class EdFiOdsApiv3AuthTokenManager : IAuthTokenManager
    {
        private readonly IEdFiOdsApiConfiguration _edFiOdsApiConfiguration;

        private readonly IEdFiVersionManager _edFiVersionManager;

        private readonly HttpClient _httpClient;

        private readonly ILogger<EdFiOdsApiv3AuthTokenManager> _logger;

        private readonly TimeSpan _refreshWindow = TimeSpan.FromMinutes(5);

        private string _token;
        private string _webApiVersion;

        private DateTime _utcExpiration = DateTime.MinValue;

        public EdFiOdsApiv3AuthTokenManager(IEdFiOdsApiConfiguration edFiOdsApiConfiguration, IEdFiVersionManager edFiVersionManager, HttpClient httpClient, ILogger<EdFiOdsApiv3AuthTokenManager> logger)
        {
            Check.NotNull(edFiOdsApiConfiguration, nameof(edFiOdsApiConfiguration));
            Check.NotNull(edFiVersionManager, nameof(edFiVersionManager));
            Check.NotNull(httpClient, nameof(httpClient));
            Check.NotNull(logger, nameof(logger));

            _edFiOdsApiConfiguration = edFiOdsApiConfiguration;
            _edFiVersionManager = edFiVersionManager;
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Attempts to retrieve an Ed-Fi ODS API v3 token.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<string> GetTokenAsync()
        {
            bool refreshNeeded = _token == null;

            if (_token == null || DateTime.UtcNow >= _utcExpiration - _refreshWindow)
            {
                string description = refreshNeeded ? "was not found" : "has expired";
                _logger.LogDebug($"An existing access token {description}. Starting refresh");

                await RefreshTokenAsync().ConfigureAwait(false);
            }
            else
            {
                _logger.LogDebug($"An existing access token was found, expiring at {_utcExpiration}");
            }

            return _token;
        }

        private async Task<HttpRequestMessage> GetAccessTokenRequest()
        {
            // var uri = EdFiOdsApiConfigurationHelper.ResolveAuthenticationUrl(_edFiOdsApiConfiguration.Version, _edFiOdsApiConfiguration.AuthenticationUrl, "/token");
            var uri = await _edFiVersionManager.ResolveAuthenticationUrl(_edFiOdsApiConfiguration);

            var ret = new HttpRequestMessage(HttpMethod.Post, uri);
            ret.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_edFiOdsApiConfiguration.OAuthAuthenticationConfiguration.Key}:{_edFiOdsApiConfiguration.OAuthAuthenticationConfiguration.Secret}")));
            ret.Content = new JsonHttpContent(new JObject
            {
                { "grant_type", "client_credentials" }
            });
            return ret;
        }

        private async Task RefreshTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var accessTokenRequest = await GetAccessTokenRequest();

                _logger.LogDebug($"Sending access token request to {accessTokenRequest.RequestUri}");

                var accessTokenResult = await _httpClient.SendAsync(accessTokenRequest, cancellationToken).ConfigureAwait(false);
                string responseContent = await accessTokenResult.ReadContentAsStringOrEmptyAsync().ConfigureAwait(false);

                if (!accessTokenResult.IsSuccessStatusCode)
                {
                    string errorMessage = "There was an error sending the access code request.";

                    switch (accessTokenResult.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            errorMessage = $"The specified access token oAuth url could not be found. ({accessTokenRequest.RequestUri})";
                            break;
                        case HttpStatusCode.Unauthorized:
                            errorMessage = "The specified Ed-Fi ODS API credentials were not valid.";
                            break;
                    }

                    throw ExceptionFromResponse(errorMessage, accessTokenResult.StatusCode, responseContent);
                }

                var tokenData = await accessTokenResult.Content.ReadAsJTokenAsync<JObject>(cancellationToken).ConfigureAwait(false);
                if (!tokenData.ContainsKey("access_token"))
                {
                    throw ExceptionFromResponse("The access token field did not exist on the server response.", accessTokenResult.StatusCode, responseContent);
                }

                _token = tokenData.Value<string>("access_token");

                //API v3 uses static expiration value
                int expiresIn = tokenData.Value<int>("expires_in");
                _utcExpiration = DateTime.UtcNow.AddSeconds(expiresIn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        private Exception ExceptionFromResponse(string message, HttpStatusCode statusCode, string responseContent)
        {
            var ex = new LearningStandardsHttpRequestException(message, statusCode, responseContent, ServiceNames.EdFi);
            _logger.LogError(ex.Message);
            return ex;
        }

    }
}
