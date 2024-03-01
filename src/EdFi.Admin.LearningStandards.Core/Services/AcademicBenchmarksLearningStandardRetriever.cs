// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Async;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace EdFi.Admin.LearningStandards.Core.Services
{
    public class AcademicBenchmarksLearningStandardsDataRetriever : ILearningStandardsDataRetriever, ILearningStandardsDataValidator
    {
        private readonly ILearningStandardsProviderConfiguration _learningStandardsProviderConfiguration;
        private readonly ILogger<AcademicBenchmarksLearningStandardsDataRetriever> _logger;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializer _serializer = JsonSerializer.CreateDefault();

        public AcademicBenchmarksLearningStandardsDataRetriever(
            IOptionsSnapshot<AcademicBenchmarksOptions> academicBenchmarksOptionsSnapshot,
            ILogger<AcademicBenchmarksLearningStandardsDataRetriever> logger,
            IHttpClientFactory httpClientFactory)
        {
            _learningStandardsProviderConfiguration = academicBenchmarksOptionsSnapshot.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient(nameof(ILearningStandardsDataRetriever));
        }

        public event EventHandler<AsyncEnumerableOperationStatus> ProcessCountEvent
        {
            add => _processCount += value;
            remove => _processCount -= value;
        }

        private EventHandler<AsyncEnumerableOperationStatus> _processCount;

        private AsyncEnumerableOperation<EdFiBulkJsonModel> GetEdFiBulkAsyncEnumerable(
            Uri requestUri,
            EdFiOdsApiCompatibilityVersion version,
            IChangeSequence syncStartSequence,
            IAuthTokenManager learningStandardsProviderAuthTokenManager,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // https://odetocode.com/blogs/scott/archive/2018/01/11/streaming-content-in-asp-net-core-2.aspx
            // https://www.codeproject.com/Articles/1180464/Large-JSON-Array-Streaming-in-ASP-NET-Web-API

            string query = $"edfiVersion={version}&syncFromEventSequenceId={syncStartSequence.Id}";
            var qualifiedRequestUri = new UriBuilder(requestUri) { Query = query }.Uri;

            var request = new HttpRequestMessage(HttpMethod.Get, qualifiedRequestUri);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // The request content is saved off as a string here instead of being read out of the
            // response object because .NET Full stack doesn't have the fix that was made for .Net Core and a reference to:
            // https://github.com/dotnet/corefx/pull/19082
            var httpRequestUri = request.RequestUri;

            var processingId = Guid.NewGuid();

            // Once C# 8 is available consider https://github.com/Dasync/AsyncEnumerable#what-happens-when-c-80-is-released
            IAsyncEnumerable<EdFiBulkJsonModel> asyncEnumerable = new AsyncEnumerable<EdFiBulkJsonModel>(
                async yield =>
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        await learningStandardsProviderAuthTokenManager.GetTokenAsync().ConfigureAwait(false));

                    using (var response = await _httpClient
                                                .SendAsync(
                                                    request,
                                                    HttpCompletionOption.ResponseHeadersRead,
                                                    cancellationToken)
                                                .ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            string errorContent = await response.ReadContentAsStringOrEmptyAsync().ConfigureAwait(false);
                            var ex = new LearningStandardsHttpRequestException($"An error occurred while trying to connect with {httpRequestUri}", response.StatusCode, errorContent, ServiceNames.AB);
                            _logger.LogError(ex);
                            _logger.LogInformation($"[{(int)response.StatusCode} {response.StatusCode}]: {errorContent}");
                            throw ex;
                        }

                        if(response.Headers.TryGetValues("X-Record-Count", out var recordCountValues))
                        {
                            string countText = recordCountValues.FirstOrDefault();

                            if (!string.IsNullOrWhiteSpace(countText)
                                && int.TryParse(countText, out int count))
                            {
                                _logger.LogInformation($"Total records expected from proxy: {count}");
                                // Signal count
                                _processCount?.Invoke(this, new AsyncEnumerableOperationStatus(processingId, count));
                            }
                            else
                            {
                                ReportDefaultCount(processingId, qualifiedRequestUri);
                            }
                        }
                        else
                        {
                            ReportDefaultCount(processingId, qualifiedRequestUri);
                        }

                        try
                        {
                            using (var httpContentStream = await response.Content.ReadAsStreamAsync()
                                .ConfigureAwait(false))
                            {
                                _logger.LogInformation("Successfully connected to AB web service. Starting retrieval");

                                using (var httpContentStreamReader = new StreamReader(httpContentStream))
                                {
                                    using (var httpJsonTextContentReader =
                                        new JsonTextReader(httpContentStreamReader))
                                    {
                                        while (await httpJsonTextContentReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                        {
                                            if (httpJsonTextContentReader.TokenType != JsonToken.StartArray
                                                && httpJsonTextContentReader.TokenType != JsonToken.EndArray)
                                            {
                                                // If performance is an issue look at using TPL Dataflow here too.
                                                await yield
                                                    .ReturnAsync(
                                                        _serializer.Deserialize<EdFiBulkJsonModel>(
                                                            httpJsonTextContentReader))
                                                    .ConfigureAwait(false);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex);
                            throw new Exception("An unexpected error occurred while streaming Learning Standards. Please try again. If this problem persists, please contact support.", ex);
                        }
                    }
                });

            return new AsyncEnumerableOperation<EdFiBulkJsonModel>(processingId,asyncEnumerable);
        }

        private void ReportDefaultCount(Guid processingId, Uri requestUri)
        {
            _logger.LogInformation($"No record count was returned from the proxy for Uri: {requestUri?.AbsoluteUri}. Using Defaults.");
            _processCount?.Invoke(this, new AsyncEnumerableOperationStatus(processingId, _learningStandardsProviderConfiguration.DefaultReportedRecordCount));
        }

        public AsyncEnumerableOperation<EdFiBulkJsonModel> GetLearningStandardsDescriptors(
            EdFiOdsApiCompatibilityVersion version,
            IChangeSequence syncStartSequence,
            IAuthTokenManager learningStandardsProviderAuthTokenManager,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(version, nameof(version));
            Check.NotNull(syncStartSequence, nameof(syncStartSequence));
            Check.NotNull(learningStandardsProviderAuthTokenManager, nameof(learningStandardsProviderAuthTokenManager));

            return GetEdFiBulkAsyncEnumerable(
                new Uri(new Uri(_learningStandardsProviderConfiguration.Url), "Descriptors"),
                version,
                syncStartSequence,
                learningStandardsProviderAuthTokenManager,
                cancellationToken);
        }

        public AsyncEnumerableOperation<EdFiBulkJsonModel> GetLearningStandards(
            EdFiOdsApiCompatibilityVersion version,
            IChangeSequence syncStartSequence,
            IAuthTokenManager learningStandardsProviderAuthTokenManager,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(version, nameof(version));
            Check.NotNull(syncStartSequence, nameof(syncStartSequence));
            Check.NotNull(learningStandardsProviderAuthTokenManager, nameof(learningStandardsProviderAuthTokenManager));

            return GetEdFiBulkAsyncEnumerable(
                new Uri(new Uri(_learningStandardsProviderConfiguration.Url), "Sync"),
                version,
                syncStartSequence,
                learningStandardsProviderAuthTokenManager,
                cancellationToken);
        }

        public async Task<IChangesAvailableResponse> GetChangesAsync(
            IChangeSequence currentSequence,
            IAuthTokenManager learningStandardsProviderAuthTokenManager,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(currentSequence, nameof(currentSequence));
            Check.NotNull(learningStandardsProviderAuthTokenManager, nameof(learningStandardsProviderAuthTokenManager));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{_learningStandardsProviderConfiguration.Url.TrimEnd('/')}/changes/available?clientChangeId={currentSequence.Id}"));

                string authToken = await learningStandardsProviderAuthTokenManager.GetTokenAsync().ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                var requestUri = request.RequestUri;

                _logger.LogDebug($"Sending changes request to {requestUri}");

                var httpResponse = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                string httpResponseContent = await httpResponse.ReadContentAsStringOrEmptyAsync().ConfigureAwait(false);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    string errorMessage = "There was an error sending the Academic Benchmarks changes request.";

                    switch (httpResponse.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            errorMessage = $"The specified status url could not be found ({requestUri}).";
                            break;
                        case HttpStatusCode.Unauthorized:
                            errorMessage = "The specified Academic Benchmark credentials were not valid.";
                            break;
                    }

                    var ex = new LearningStandardsHttpRequestException(errorMessage, httpResponse.StatusCode, httpResponseContent, ServiceNames.AB);
                    _logger.LogInformation($"[{(int)httpResponse.StatusCode} {httpResponse.StatusCode}]: {httpResponseContent}");
                    _logger.LogError(ex.Message);

                    throw ex;
                }

                var result = JsonConvert.DeserializeObject<AcademicBenchmarksChangesAvailableModel>(httpResponseContent);

                if (result == null)
                {
                    _logger.LogInformation($"[{(int)httpResponse.StatusCode} {httpResponse.StatusCode}]: No response sent from url: {request.RequestUri.ToString()}");
                    throw new LearningStandardsHttpRequestException(
                        "No response was sent from the API when checking for change events.",
                        httpResponse.StatusCode,
                        httpResponseContent,
                        ServiceNames.AB);
                }

                return new ChangesAvailableResponse(
                    new ChangesAvailableInformation
                    {
                        Available = result.EventChangesAvailable,
                        MaxAvailable = new ChangeSequence
                                       { Id = result.MaxSequenceId, Key = currentSequence.Key },
                        Current = currentSequence
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return new ChangesAvailableResponse(ex.ToLearningStandardsResponse(), null);
            }
        }

        public async Task<IResponse> ValidateConnection(IAuthTokenManager learningStandardsProviderAuthTokenManager)
        {
            Check.NotNull(learningStandardsProviderAuthTokenManager, nameof(learningStandardsProviderAuthTokenManager));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{_learningStandardsProviderConfiguration.Url.TrimEnd('/')}/validate/authentication"));

                string authToken = await learningStandardsProviderAuthTokenManager.GetTokenAsync().ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                // The request content is saved off as a string here instead of being read out of the
                // response object because .NET Full stack doesn't have the fix that was made for .Net Core and a reference to:
                // https://github.com/dotnet/corefx/pull/19082
                var requestUri = request.RequestUri;

                _logger.LogDebug($"Sending validation request to {requestUri}");

                //Todo: Resolve what type of response will actually come from this call.
                var httpResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
                string httpResponseContent = await httpResponse.ReadContentAsStringOrEmptyAsync().ConfigureAwait(false);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    string errorMessage = "There was an error sending the Academic Benchmarks status request.";

                    switch (httpResponse.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            errorMessage = $"The specified status url could not be found ({requestUri}).";
                            break;
                        case HttpStatusCode.Unauthorized:
                            errorMessage = "The specified Academic Benchmark credentials were not valid.";
                            break;
                    }

                    var ex = new LearningStandardsHttpRequestException(errorMessage, httpResponse.StatusCode, httpResponseContent, ServiceNames.AB);
                    _logger.LogInformation($"[{(int)httpResponse.StatusCode} {httpResponse.StatusCode}]: {httpResponseContent}");
                    _logger.LogError(ex.Message);

                    throw ex;
                }

                return new ResponseModel(
                    httpResponse.IsSuccessStatusCode,
                    string.Empty,
                    httpResponseContent,
                    httpResponse.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }
    }
}
