// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Auth
{
    public class EdFiOdsApiv2AuthTokenManager : IAuthTokenManager
    {
        private readonly IEdFiOdsApiConfiguration _edFiOdsApiConfiguration;

        private readonly HttpClient _httpClient;

        private readonly ILogger<EdFiOdsApiv2AuthTokenManager> _logger;

        private int _slidingExpiration = 1800;

        private string _token;

        private DateTime _utcExpiration = DateTime.MinValue;

        public EdFiOdsApiv2AuthTokenManager(IEdFiOdsApiConfiguration edFiOdsApiConfiguration, HttpClient httpClient, ILogger<EdFiOdsApiv2AuthTokenManager> logger)
        {
            Check.NotNull(edFiOdsApiConfiguration, nameof(edFiOdsApiConfiguration));
            Check.NotNull(httpClient, nameof(httpClient));
            Check.NotNull(logger, nameof(logger));

            _edFiOdsApiConfiguration = edFiOdsApiConfiguration;
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        ///     Attempts to retrieve an Ed-Fi ODS API v2 token.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<string> GetTokenAsync()
        {
            bool refreshNeeded = _token == null;

            if (_token == null || DateTime.UtcNow >= _utcExpiration)
            {
                string description = refreshNeeded ? "was not found" : "has expired";
                _logger.LogDebug($"An existing access token {description}. Starting refresh");

                await RefreshTokenAsync().ConfigureAwait(false);
            }
            else
            {
                _utcExpiration = DateTime.UtcNow.AddSeconds(_slidingExpiration);
                _logger.LogDebug($"An existing access token was found. Adding {_slidingExpiration} seconds to sliding expiration. Token now expires at {_utcExpiration}");
            }

            return _token;
        }

        private async Task RefreshTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                //1. Get auth token
                string authorizationToken = await GetAuthorizationCodeAsync(cancellationToken).ConfigureAwait(false);
                var accessTokenRequest = GetAccessTokenRequest(authorizationToken);

                _logger.LogDebug($"Sending access token request to {accessTokenRequest.RequestUri}");

                //2. Get access token
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

                //3. Extract token information
                var tokenData = await accessTokenResult.Content.ReadAsJTokenAsync<JObject>(cancellationToken).ConfigureAwait(false);
                if (!tokenData.ContainsKey("access_token"))
                {
                    throw ExceptionFromResponse("The access token field did not exist on the server response.", accessTokenResult.StatusCode, responseContent);
                }

                _token = tokenData.Value<string>("access_token");

                //Calculate the default sliding window sent by the API. Most likely 30 minutes.
                var compTime = DateTime.UtcNow;
                int expiresIn = tokenData.Value<int>("expires_in");
                _utcExpiration = compTime.AddSeconds(expiresIn);
                _slidingExpiration = (_utcExpiration - compTime).Seconds;

                _logger.LogDebug($"Sliding expiration value calculated as {_slidingExpiration} from original token value {expiresIn}, expiring on {_utcExpiration}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        private async Task<string> GetAuthorizationCodeAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var authorizeRequest = GetAuthorizationRequest();
            _logger.LogDebug($"Sending authorization code request to {authorizeRequest.RequestUri}");
            var result = await _httpClient.SendAsync(authorizeRequest, cancellationToken).ConfigureAwait(false);
            string responseContent = await result.ReadContentAsStringOrEmptyAsync().ConfigureAwait(false);
            if (!result.IsSuccessStatusCode)
            {
                string errorMessage = "There was an error sending the authorization code request.";

                switch (result.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                        errorMessage = "The specified authorization code oAuth url could not be found.";
                        break;
                    case HttpStatusCode.Unauthorized:
                        errorMessage = "The specified Ed-Fi ODS API credentials were not valid.";
                        break;
                }

                throw ExceptionFromResponse(errorMessage, result.StatusCode, responseContent);
            }

            var authData = await result.Content.ReadAsJTokenAsync<JObject>(cancellationToken).ConfigureAwait(false);

            if (authData.ContainsKey("code"))
            {
                return authData.Value<string>("code");
            }
            throw ExceptionFromResponse($"The authorization code could not be retrieved when requesting {authorizeRequest.RequestUri}.", result.StatusCode, responseContent);
        }

        private HttpRequestMessage GetAuthorizationRequest()
        {
            var uri = EdFiOdsApiConfigurationHelper.ResolveAuthenticationUrl(_edFiOdsApiConfiguration.Version, _edFiOdsApiConfiguration.AuthenticationUrl, "/authorize");
            return new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "Client_id", _edFiOdsApiConfiguration.OAuthAuthenticationConfiguration.Key },
                    { "Response_type", "code" }
                })
            };
        }

        private HttpRequestMessage GetAccessTokenRequest(string authorizationCode)
        {
            var uri = EdFiOdsApiConfigurationHelper.ResolveAuthenticationUrl(_edFiOdsApiConfiguration.Version, _edFiOdsApiConfiguration.AuthenticationUrl, "/token");
            return new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new JsonHttpContent(new JObject
                {
                    ["Client_id"] = _edFiOdsApiConfiguration.OAuthAuthenticationConfiguration.Key,
                    ["Client_secret"] = _edFiOdsApiConfiguration.OAuthAuthenticationConfiguration.Secret,
                    ["Code"] = authorizationCode,
                    ["Grant_type"] = "authorization_code"
                })
            };
        }

        private Exception ExceptionFromResponse(string message, HttpStatusCode statusCode, string responseContent)
        {
            var ex = new LearningStandardsHttpRequestException(message, statusCode, responseContent, ServiceNames.EdFi);
            _logger.LogError(ex.Message);
            return ex;
        }

    }
}
