// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Models.ABConnectApiModels;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Async;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace EdFi.Admin.LearningStandards.Core.Services
{
    public class AcademicBenchmarksLearningStandardsDataRetriever : ILearningStandardsDataRetriever, ILearningStandardsDataValidator
    {
        private readonly ILearningStandardsProviderConfiguration _learningStandardsProviderConfiguration;
        private readonly ILogger<AcademicBenchmarksLearningStandardsDataRetriever> _logger;
        private readonly HttpClient _httpClient;
        private readonly JsonSerializer _serializer = JsonSerializer.CreateDefault();
        private readonly ILearningStandardsDataMapper _dataMapper;

        public AcademicBenchmarksLearningStandardsDataRetriever(
            IOptionsSnapshot<AcademicBenchmarksOptions> academicBenchmarksOptionsSnapshot,
            ILogger<AcademicBenchmarksLearningStandardsDataRetriever> logger,
            IHttpClientFactory httpClientFactory,
            ILearningStandardsDataMapper dataMapper)
        {
            _learningStandardsProviderConfiguration = academicBenchmarksOptionsSnapshot.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient(nameof(ILearningStandardsDataRetriever));
            _dataMapper = dataMapper;
        }

        public event EventHandler<AsyncEnumerableOperationStatus> ProcessCountEvent
        {
            add => _processCount += value;
            remove => _processCount -= value;
        }

        private EventHandler<AsyncEnumerableOperationStatus> _processCount;

        private void ReportDefaultCount(Guid processingId, Uri requestUri)
        {
            _logger.LogInformation($"No record count was returned from the proxy for Uri: {requestUri?.AbsoluteUri}. Using Defaults.");
            _processCount?.Invoke(this, new AsyncEnumerableOperationStatus(processingId, _learningStandardsProviderConfiguration.DefaultReportedRecordCount));
        }

        public AsyncEnumerableOperation<EdFiBulkJsonModel> GetLearningStandardsDescriptors(
            EdFiVersionModel version,
            IChangeSequence syncStartSequence,
            IAuthApiManager learningStandardsProviderAuthApiManager,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(version, nameof(version));
            Check.NotNull(syncStartSequence, nameof(syncStartSequence));
            Check.NotNull(learningStandardsProviderAuthApiManager, nameof(learningStandardsProviderAuthApiManager));


            var uriBuilder = new UriBuilder(_learningStandardsProviderConfiguration.Url.TrimEnd('/'));

            // Append path segment
            uriBuilder.Path = $"{uriBuilder.Path}/standards";

            // Query parameters
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["facet"] = "disciplines.subjects,education_levels.grades";
            queryParams["limit"] = "0";

            uriBuilder.Query = queryParams.ToString();

            var processingId = Guid.NewGuid();
            ;
            return new AsyncEnumerableOperation<EdFiBulkJsonModel>(
                            processingId,
                            GetEdFiBulkAsyncEnumerable<ABConnectFacetsResponse>(
                                uriBuilder.Uri,
                                learningStandardsProviderAuthApiManager,
                                version,
                                cancellationToken)
                        );
        }

        public async Task<IChangesAvailableResponse> GetChangesAsync(
            IChangeSequence currentSequence,
            IAuthApiManager learningStandardsProviderAuthApiManager,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(currentSequence, nameof(currentSequence));
            Check.NotNull(learningStandardsProviderAuthApiManager, nameof(learningStandardsProviderAuthApiManager));

            try
            {
                // reverse sorting get last sequence number
                var uriBuilder = new UriBuilder(_learningStandardsProviderConfiguration.Url.TrimEnd('/'));

                // Append path segment
                uriBuilder.Path = $"{uriBuilder.Path}/events";

                // Query parameters
                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["fields[events]"] = "seq,section_guid,document_guid";
                queryParams["filter[events]"] = $"seq GT {currentSequence.Id}";
                queryParams["sort[events]"] = "-seq";
                queryParams["limit"] = "1";

                uriBuilder.Query = queryParams.ToString();

                var requestUri = uriBuilder.Uri;

                IAsyncEnumerable<ABConnectEventsResponse> eventsResponse = GetPaginatedAsyncEnumerable<ABConnectEventsResponse>(
                        uriBuilder.Uri,
                        learningStandardsProviderAuthApiManager,
                        cancellationToken);


                var eventModel = await eventsResponse.FirstOrDefaultAsync();
                if (eventModel == null)
                {
                    _logger.LogInformation($"[No response sent from url: {requestUri}");
                    throw new LearningStandardsHttpRequestException(
                        "No response was sent from the API when checking for change events.",
                        HttpStatusCode.OK,
                        string.Empty,
                        ServiceNames.AB);
                }

                return new ChangesAvailableResponse(
                    new ChangesAvailableInformation
                    {
                        Available = eventModel.Data.Any(),
                        MaxAvailable = new ChangeSequence
                        {
                            Id = eventModel.Data.Any() ? eventModel.Data.First().Attributes.Seq : currentSequence.Id,
                            Key = currentSequence.Key
                        },
                        Current = currentSequence
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return new ChangesAvailableResponse(ex.ToLearningStandardsResponse(), null);
            }
        }

        public async Task<IResponse> ValidateConnection(IAuthApiManager learningStandardsProviderAuthApiManager)
        {
            Check.NotNull(learningStandardsProviderAuthApiManager, nameof(learningStandardsProviderAuthApiManager));

            try
            {
                var uriBuilder = new UriBuilder(_learningStandardsProviderConfiguration.Url.TrimEnd('/'));

                // Append path segment
                uriBuilder.Path = $"{uriBuilder.Path}/standards";
                // Query parameters
                var queryParams = HttpUtility.ParseQueryString(string.Empty);
                queryParams["limit"] = "1";

                uriBuilder.Query = queryParams.ToString();

                var requestUri = uriBuilder.Uri;
                _logger.LogDebug($"Sending validation request to {requestUri.OriginalString}");

                var request = await learningStandardsProviderAuthApiManager.GetAuthenticatedRequestAsync(HttpMethod.Get, requestUri);

                //Todo: Resolve what type of response will actually come from this call.
                var httpResponse = await _httpClient.SendAsync(request).ConfigureAwait(false);
                string httpResponseContent = await httpResponse.ReadContentAsStringOrEmptyAsync().ConfigureAwait(false);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    string errorMessage = "There was an error sending the Academic Benchmarks status request.";

                    switch (httpResponse.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            errorMessage = $"The specified status url could not be found ({request.RequestUri}).";
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
                    $"Learning Standard's API response code:{httpResponse.StatusCode}",
                    httpResponse.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public AsyncEnumerableOperation<LearningStandardsSegmentModel> GetChangedSegments(
            EdFiVersionModel version,
            IChangeSequence syncStartSequence,
            IAuthApiManager learningStandardsProviderAuthApiManager,
            CancellationToken cancellationToken)
        {
            var sequenceStartNumber = syncStartSequence?.Id ?? 0;

            // get all sections
            var uriBuilder = new UriBuilder(_learningStandardsProviderConfiguration.Url.TrimEnd('/'));

            // Append path segment
            uriBuilder.Path = $"{uriBuilder.Path}/events";

            // Query parameters
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["filter[events]"] = $"seq GT {sequenceStartNumber}";
            queryParams["fields[events]"] = "seq,section_guid,document_guid";
            queryParams["limit"] = "100";

            uriBuilder.Query = queryParams.ToString();

            IAsyncEnumerable<ABConnectEventsResponse> changeEventsResponse = GetPaginatedAsyncEnumerable<ABConnectEventsResponse>(
                    uriBuilder.Uri,
                    learningStandardsProviderAuthApiManager,
                    cancellationToken);

            var sectionProcessId = Guid.NewGuid();
            var countReported = false;

            return new AsyncEnumerableOperation<LearningStandardsSegmentModel>(sectionProcessId, new AsyncEnumerable<LearningStandardsSegmentModel>(async yield =>
            {
                await changeEventsResponse.ForEachAsync(async sectionResp =>
                {
                    if (!countReported)
                    {
                        if (sectionResp.Meta.Count > 0)
                        {
                            _logger.LogInformation($"Total modified segments: {sectionResp.Meta.Count}");
                            // Signal count
                            _processCount?.Invoke(this, new AsyncEnumerableOperationStatus(sectionProcessId, sectionResp.Meta.Count));
                        }
                        else
                        {
                            ReportDefaultCount(sectionProcessId, uriBuilder.Uri);
                        }
                        countReported = true;
                    }

                    foreach (var item in sectionResp.Data)
                    {
                        var sectionId = item.Attributes.SectionGuid;
                        var documentId = item.Attributes.DocumentGuid;

                        await yield.ReturnAsync(new LearningStandardsSegmentModel
                        {
                            SectionId = sectionId,
                            DocumentId = documentId.Value
                        });
                    }

                }, cancellationToken);
            }));

        }

        public AsyncEnumerableOperation<EdFiBulkJsonModel> GetSegmentLearningStandards(
            EdFiVersionModel version,
            LearningStandardsSegmentModel segment,
            IAuthApiManager learningStandardsProviderAuthApiManager,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(version, nameof(version));
            Check.NotNull(segment, nameof(segment));
            Check.NotNull(learningStandardsProviderAuthApiManager, nameof(learningStandardsProviderAuthApiManager));

            var processingId = Guid.NewGuid();

            // get all sections
            IAsyncEnumerable<EdFiBulkJsonModel> asyncEnumerable = new AsyncEnumerable<EdFiBulkJsonModel>(
            async yield =>
            {
                // get all sections
                var uriBuilder = new UriBuilder(_learningStandardsProviderConfiguration.Url.TrimEnd('/'));

                // Append path segment
                uriBuilder.Path = $"{uriBuilder.Path}/standards";

                // Query parameters
                var queryParams = HttpUtility.ParseQueryString(string.Empty);

                var filterValue = $"document.guid EQ {segment.DocumentId}";
                if (segment.SectionId.HasValue)
                    filterValue = $"{filterValue} AND section.guid EQ {segment.SectionId}";

                queryParams["filter[standards]"] = filterValue;
                queryParams["fields[standards]"] = "status,education_levels.grades,disciplines.subjects,document.descr,document.adopt_year,document.publication.authorities.descr,section.descr,statement.combined_descr";
                queryParams["limit"] = "100";

                uriBuilder.Query = queryParams.ToString();

                var requestUri = uriBuilder.Uri;


                await GetEdFiBulkAsyncEnumerable<ABConnectStandardsResponse>(
                    requestUri,
                    learningStandardsProviderAuthApiManager,
                    version,
                    cancellationToken)
                .ForEachAsync(async (r) => await yield.ReturnAsync(r));

            });

            return new AsyncEnumerableOperation<EdFiBulkJsonModel>(processingId, asyncEnumerable);
        }



        private IAsyncEnumerable<TEntity> GetPaginatedAsyncEnumerable<TEntity>(
            Uri requestUri,
            IAuthApiManager learningStandardsProviderAuthApiManager,
            CancellationToken cancellationToken = default(CancellationToken)) where TEntity : class, ILearningStandardsApiResponseModel
        {

            var asyncEnumerable = new AsyncEnumerable<TEntity>(
                async yield =>
                {
                    var nextUri = requestUri;
                    do
                    {
                        var request = await learningStandardsProviderAuthApiManager.GetAuthenticatedRequestAsync(HttpMethod.Get, nextUri);

                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        // var httpRequestUri = request.RequestUri;

                        var httpResponse = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        string httpResponseContent = await httpResponse.ReadContentAsStringOrEmptyAsync().ConfigureAwait(false);

                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            string errorMessage = "There was an error sending the Academic Benchmarks changes request.";

                            switch (httpResponse.StatusCode)
                            {
                                case HttpStatusCode.NotFound:
                                    errorMessage = $"The specified status url could not be found ({nextUri}).";
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

                        var result = JsonConvert.DeserializeObject<TEntity>(httpResponseContent);

                        if (result == null)
                        {
                            _logger.LogInformation($"[{(int)httpResponse.StatusCode} {httpResponse.StatusCode}]: No response sent from url: {request.RequestUri.ToString()}");
                            throw new LearningStandardsHttpRequestException(
                                "No response was sent from the API when checking for change events.",
                                httpResponse.StatusCode,
                                httpResponseContent,
                                ServiceNames.AB);
                        }

                        // process
                        await yield.ReturnAsync(result);

                        nextUri = result.Links.Next != null ? new Uri(result.Links.Next) : null;

                    } while (nextUri != null);
                }
            );

            return asyncEnumerable;
        }

        private IAsyncEnumerable<EdFiBulkJsonModel> GetEdFiBulkAsyncEnumerable<U>(
            Uri requestUri,
            IAuthApiManager learningStandardsProviderAuthApiManager,
            EdFiVersionModel version,
            CancellationToken cancellationToken = default(CancellationToken)) where U : class, ILearningStandardsApiResponseModel
        {

            var asyncEnumerable = new AsyncEnumerable<EdFiBulkJsonModel>(
                async yield =>
                {
                    // Get paginated data as an IAsyncEnumerable
                    var responsePages = GetPaginatedAsyncEnumerable<U>(requestUri, learningStandardsProviderAuthApiManager, cancellationToken);

                    // Loop through each item and convert it to the target model
                    await responsePages.ForEachAsync(async pr =>
                    {
                        // Convert each item to EdFiBulkJsonModel
                        foreach (var edfiModel in _dataMapper.ToEdFiModel(version, pr))
                        {
                            await yield.ReturnAsync(edfiModel);
                        }
                    });

                }
            );

            return asyncEnumerable;
        }

    }
}
