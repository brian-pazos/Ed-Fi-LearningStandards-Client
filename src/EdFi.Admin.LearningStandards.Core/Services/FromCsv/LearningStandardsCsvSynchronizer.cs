// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces.FromCsv;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Async;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Services.FromCsv
{
    public class LearningStandardsCsvSynchronizer : ILearningStandardsCsvSynchronizer
    {
        private const int LearningStandardsMaxProgressPercentage = 100;

        private readonly IEdFiBulkJsonPersisterFactory _bulkJsonPersisterFactory;

        private readonly ConcurrentDictionary<Guid, int> _countsByProcessId = new ConcurrentDictionary<Guid, int>();

        private readonly ILogger<LearningStandardsCsvSynchronizer> _logger;
        private readonly ILearningStandardsCsvDataRetriever _learningStandardsCsvDataRetriever;

        private readonly IEdFiOdsApiAuthTokenManagerFactory _odsApiAuthTokenManagerFactory;

        private readonly IEdFiOdsApiClientConfiguration _odsApiClientConfiguration;

        private readonly IEdFiVersionManager _edFiVersionManager;

        public LearningStandardsCsvSynchronizer(
            IEdFiOdsApiClientConfiguration odsApiClientConfiguration,
            IEdFiVersionManager versionManager,
            IEdFiOdsApiAuthTokenManagerFactory odsApiAuthTokenManagerFactory,
            IEdFiBulkJsonPersisterFactory bulkJsonPersisterFactory,
            ILogger<LearningStandardsCsvSynchronizer> logger,
            ILearningStandardsCsvDataRetriever learningStandardsCsvDataRetriever)
        {
            _odsApiClientConfiguration = odsApiClientConfiguration;
            _odsApiAuthTokenManagerFactory = odsApiAuthTokenManagerFactory;
            _bulkJsonPersisterFactory = bulkJsonPersisterFactory;
            _logger = logger;
            _learningStandardsCsvDataRetriever = learningStandardsCsvDataRetriever;
            _learningStandardsCsvDataRetriever.ProcessCountEvent += OnCountEvent;
            _edFiVersionManager = versionManager;
        }

        public async Task<IResponse> SynchronizeAsync(IEdFiOdsApiConfiguration odsApiConfiguration,
         ILearningStandardsSynchronizationFromCsvOptions options,
         CancellationToken cancellationToken,
         IProgress<LearningStandardsSynchronizerProgressInfo> progress = null)
        {
            Check.NotNull(odsApiConfiguration, nameof(odsApiConfiguration));
            Check.NotNull(options, nameof(options));

            var bulkJsonPersister = _bulkJsonPersisterFactory.CreateEdFiBulkJsonPersister(
                await _odsApiAuthTokenManagerFactory.CreateEdFiOdsApiAuthTokenManager(_edFiVersionManager, odsApiConfiguration),
                odsApiConfiguration);

            if (string.IsNullOrEmpty(options.ResourcesMetaDataUri))
            {
                options.ResourcesMetaDataUri = SwaggerMetaDataUriHelper.GetUri(odsApiConfiguration);
            }

            _logger.LogInformation("Synchronization process starting.");
            var processId = default(Guid);
            var results = new ConcurrentBag<IEnumerable<IResponse>>();
            progress?.Report(new LearningStandardsSynchronizerProgressInfo("Synchronization", "Starting", 0));
            int recordCounter = 0;

            try
            {
                var learningStandardsProcess = await _learningStandardsCsvDataRetriever.GetLearningStandards(
                    options, cancellationToken);

                processId = learningStandardsProcess.ProcessId;

                await learningStandardsProcess.AsyncEntityEnumerable
                    .ParallelForEachAsync(
                        async model =>
                        {
                            var response = await bulkJsonPersister
                                .PostEdFiBulkJson(
                                    model,
                                    cancellationToken)
                                .ConfigureAwait(false);

                            results.Add(response);

                            int totalRecordCount;
                            while (!_countsByProcessId.TryGetValue(processId, out totalRecordCount))
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                            int currentRecordCount = Interlocked.Add(
                                ref recordCounter,
                                response.Count);

                            int progressPercentage = Convert.ToInt32(
                                (LearningStandardsMaxProgressPercentage / (double)totalRecordCount) * currentRecordCount);

                            progressPercentage = progressPercentage >= LearningStandardsMaxProgressPercentage
                                ? LearningStandardsMaxProgressPercentage
                                : progressPercentage;

                            _logger.LogDebug($"Current progress percentage: {progressPercentage}");

                            progress?.Report(
                                new
                                    LearningStandardsSynchronizerProgressInfo(
                                        "Synchronization",
                                        "In Progress",
                                        progressPercentage));

                        }, _odsApiClientConfiguration.MaxSimultaneousRequests,
                        cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured during learning standards synchronization.");
                return ex.ToLearningStandardsResponse();
            }
            finally
            {
                _countsByProcessId.TryRemove(processId, out int _);
            }
            return ResponseModel.Aggregate(results.SelectMany(sm => sm));
        }

        private void OnCountEvent(object o, AsyncEnumerableOperationStatus e)
        {
            _countsByProcessId.AddOrUpdate(e.ProcessId, e.TotalCount, (g, i) => e.TotalCount);
        }
    }
}
