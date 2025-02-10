// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Services
{
    public class EdFiBulkJsonPersister : IEdFiBulkJsonPersister
    {
        private readonly IEdFiOdsApiConfiguration _odsApiConfiguration;
        private readonly IEdFiVersionManager _edFiVersionManager;
        private readonly IAuthTokenManager _odsApiAuthTokenManager;
        private readonly ILogger<EdFiBulkJsonPersister> _logger;
        private readonly HttpClient _httpClient;



        public EdFiBulkJsonPersister(
            IEdFiOdsApiConfiguration odsApiConfiguration,
            IEdFiVersionManager edFiVersionManager,
            IAuthTokenManager odsApiAuthTokenManager,
            ILogger<EdFiBulkJsonPersister> logger,
            HttpClient httpClient)
        {
            _odsApiConfiguration = odsApiConfiguration;
            _edFiVersionManager = edFiVersionManager;
            _odsApiAuthTokenManager = odsApiAuthTokenManager;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<EdFiVersionModel> GetEdFiVersion()
        {
            return await _edFiVersionManager.GetEdFiVersion(_odsApiConfiguration);
        }

        public async Task<IList<IResponse>> PostEdFiBulkJson(EdFiBulkJsonModel edFiBulkJson, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(edFiBulkJson.Operation)
                && !edFiBulkJson.Operation.Equals("Upsert", StringComparison.InvariantCultureIgnoreCase))
            {
                // Note: Default is upsert.
                throw new NotSupportedException("Only Upserts Supported");
            }

            Check.NotEmpty(edFiBulkJson.Resource, nameof(edFiBulkJson.Resource));
            Check.NotNull(edFiBulkJson.Data, nameof(edFiBulkJson.Data));

            //var odsResourceUrl = EdFiBulkJsonPersisterHelper.ResolveOdsApiResourceUrl(
            //    _odsApiConfiguration.Url,
            //    edFiBulkJson.Schema,
            //    edFiBulkJson.Resource,
            //    _odsApiConfiguration.Version,
            //    _odsApiConfiguration.SchoolYear);
            var odsResourceUrl = await _edFiVersionManager.ResolveResourceUrl(_odsApiConfiguration, edFiBulkJson.Schema, edFiBulkJson.Resource).ConfigureAwait(false);

            _logger.LogDebug($"Url for Batch Json Model derived as: {odsResourceUrl}");

            var responses = new List<IResponse>();

            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogDebug(
                $"Beginning Post for Batch Json Model. Resource: {edFiBulkJson.Resource} Schema: {edFiBulkJson.Schema}");

            foreach (var resourceData in edFiBulkJson.Data)
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, odsResourceUrl);

                httpRequest.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    await _odsApiAuthTokenManager.GetTokenAsync().ConfigureAwait(false));

                // The request content is saved off as a string here instead of being read out of the
                // response object because .NET Full stack doesn't have the fix that was made for .Net Core.
                // See: https://github.com/dotnet/corefx/pull/19082
                string requestContent = resourceData.ToString(Formatting.None);

                httpRequest.Content = new StringContent(
                    requestContent,
                    new UTF8Encoding(),
                    "application/json");

                responses.Add(
                    await ProcessHttpResponseMessage(
                            await _httpClient.SendAsync(httpRequest, cancellationToken)
                                             .ConfigureAwait(false), requestContent)
                        .ConfigureAwait(false));
            }

            _logger.LogDebug(
                $"Successfully loaded {responses.Count(x => x.IsSuccess)} of {responses.Count} {edFiBulkJson.Resource} Resources in Batch Json Model.");

            return responses;
        }

        private async Task<IResponse> ProcessHttpResponseMessage(HttpResponseMessage httpResponseMessage, string requestContent)
        {
            if (httpResponseMessage.IsSuccessStatusCode)
                return new ResponseModel(
                    httpResponseMessage.IsSuccessStatusCode,
                    null,
                    null,
                    httpResponseMessage.StatusCode);

            var errorResponse = new ResponseModel(
                httpResponseMessage.IsSuccessStatusCode,
                httpResponseMessage.ReasonPhrase,
                httpResponseMessage.Content != null
                    ? await httpResponseMessage.Content.ReadAsStringAsync()
                                               .ConfigureAwait(false)
                    : null,
                httpResponseMessage.StatusCode);

            var logMessageBuilder = new StringBuilder();
            logMessageBuilder.AppendLine("While sending the following content to the ODS API:");
            logMessageBuilder.AppendLine(requestContent);
            logMessageBuilder.AppendLine("The following error occured:");
            logMessageBuilder.AppendLine($"HttpStatusCode: {errorResponse.StatusCode}");
            logMessageBuilder.AppendLine($"Message: {errorResponse.ErrorMessage}");
            logMessageBuilder.AppendLine($"Response: {errorResponse.Content}");

            _logger.LogError(logMessageBuilder.ToString());

            return errorResponse;
        }
    }
}
