// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces.FromCsv;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace EdFi.Admin.LearningStandards.Core.Services.FromCsv
{
    public class CsvLearningStandardsDataRetriever : ILearningStandardsCsvDataRetriever
    {
        private readonly ILogger<CsvLearningStandardsDataRetriever> _logger;
        private readonly IMetaDataRetriever _metaDataRetriever;
        private readonly ICsvFileProcessor _csvFileProcessor;
        private readonly IDataMappingProcess _dataMappingProcess;
        private EventHandler<AsyncEnumerableOperationStatus> _processCount;

        public CsvLearningStandardsDataRetriever(
            ILogger<CsvLearningStandardsDataRetriever> logger,
            IMetaDataRetriever metaDataRetriever,
            ICsvFileProcessor csvFileProcessor,
            IDataMappingProcess dataMappingProcess)
        {
            _logger = logger;
            _metaDataRetriever = metaDataRetriever;
            _csvFileProcessor = csvFileProcessor;
            _dataMappingProcess = dataMappingProcess;
        }
        
        public event EventHandler<AsyncEnumerableOperationStatus> ProcessCountEvent
        {
            add => _processCount += value;
            remove => _processCount -= value;
        }

        public async Task<AsyncEnumerableOperation<EdFiBulkJsonModel>> GetLearningStandards(
            ILearningStandardsSynchronizationFromCsvOptions options,
            CancellationToken cancellationToken = default)
        {
            var inputRows = _csvFileProcessor.GetRows(options.InputCsvFullPath).ToList();
            var invalidRowsExceptions = _csvFileProcessor.InvalidRowsExceptions;
            if (invalidRowsExceptions != null && invalidRowsExceptions.Any())
            {
                var sb = new StringBuilder();
                sb.Append($"Invalid rows found on file: {options.InputCsvFullPath}. Invalid rows details: ");
                foreach (var innerException in invalidRowsExceptions)
                {
                    sb.Append(innerException.Message + ",");
                }
                throw new Exception(sb.ToString());
            }

            if (inputRows == null || !inputRows.Any())
            {
                string error = $"No records to process, file: {options.InputCsvFullPath}";
                _logger.LogError(error);
                throw new Exception(error);
            }

            var metadata = await _metaDataRetriever.GetMetadata(options.ResourcesMetaDataUri,
                options.ForceMetaDataReload);
            var dataMappers = _dataMappingProcess.GetDataMappings().ToArray();

            int startPosition = 0;
            const int recordsToFetch = 100;
            int totalRecords = inputRows.Count();
            var processingId = Guid.NewGuid();

            System.Collections.Async.IAsyncEnumerable<EdFiBulkJsonModel> asyncEnumerable = new AsyncEnumerable<EdFiBulkJsonModel>(
                async yield =>
                {
                    _processCount?.Invoke(this,
                        new AsyncEnumerableOperationStatus(processingId, totalRecords));
                    while (startPosition < totalRecords)
                    {
                        var mappedRows = new List<JObject>();
                        var batch = inputRows.Skip(startPosition).Take(recordsToFetch);
                        mappedRows.AddRange(batch.Select(row =>
                            _dataMappingProcess.ApplyMap(metadata, dataMappers, row)));

                        var bulkJsonModel = new EdFiBulkJsonModel
                        {
                            Schema = "ed-fi",
                            Resource = "learningStandards",
                            Operation = "upsert",
                            Data = mappedRows
                        };

                        await yield
                            .ReturnAsync(bulkJsonModel)
                            .ConfigureAwait(false);

                        Interlocked.Add(ref startPosition, recordsToFetch);
                    }
                });
            return new AsyncEnumerableOperation<EdFiBulkJsonModel>(processingId, asyncEnumerable);
        }
    }
}
