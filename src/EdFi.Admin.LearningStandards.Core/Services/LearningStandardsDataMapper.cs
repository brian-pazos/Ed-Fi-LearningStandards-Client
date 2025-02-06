using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Models.ABConnectApiModels;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace EdFi.Admin.LearningStandards.Core.Services
{
    public class LearningStandardsDataMapper : ILearningStandardsDataMapper
    {
        private readonly ILearningStandardsProviderConfiguration _learningStandardsProviderConfiguration;
        private readonly ILogger<LearningStandardsDataMapper> _logger;

        // private readonly Dictionary<EdFiDataStandardVersion, IEdFiDataStandardVersionHandler> _versionHandlers;

        public LearningStandardsDataMapper(
            IOptionsSnapshot<AcademicBenchmarksOptions> academicBenchmarksOptionsSnapshot,
            ILogger<LearningStandardsDataMapper> logger)
        {
            _learningStandardsProviderConfiguration = academicBenchmarksOptionsSnapshot.Value;
            _logger = logger;

            //    // Register handlers for each version
            //    _versionHandlers = new Dictionary<EdFiDataStandardVersion, IEdFiDataStandardVersionHandler>
            //{
            //    { EdFiDataStandardVersion.DS3, new EdFiDataStandardV3Handler(_learningStandardsProviderConfiguration, logger) },
            //    { EdFiDataStandardVersion.DS4, new EdFiDataStandardV3Handler(_learningStandardsProviderConfiguration, logger) },
            //    { EdFiDataStandardVersion.DS5_0, new EdFiDataStandardV3Handler(_learningStandardsProviderConfiguration, logger) },
            //    { EdFiDataStandardVersion.DS5_1, new EdFiDataStandardV3Handler(_learningStandardsProviderConfiguration, logger) },
            //    { EdFiDataStandardVersion.DS5_2, new EdFiDataStandardV3Handler(_learningStandardsProviderConfiguration, logger) }
            //};
        }


        public IEnumerable<EdFiBulkJsonModel> ToEdFiModel(
                    EdFiVersionModel version,
                    ILearningStandardsApiResponseModel response)
        {
            //if (_versionHandlers.TryGetValue(version.DataStandardVersion, out var handler))
            //{
            //    return handler.MapResponse(response);
            //}
            switch (version.DataStandardVersion)
            {
                case EdFiDataStandardVersion.DS2:
                    throw new NotImplementedException("Mapping to EdFi DataStandard version 2.0 has not been implemented.");
                case EdFiDataStandardVersion.DS3:
                case EdFiDataStandardVersion.DS4:
                case EdFiDataStandardVersion.DS5_0:
                case EdFiDataStandardVersion.DS5_1:
                case EdFiDataStandardVersion.DS5_2:
                default:
                    return new EdFiDataStandardV3Handler(_learningStandardsProviderConfiguration, _logger).MapResponse(response);
            }

            throw new NotImplementedException($"Mapping to ODS version:{version} is not implemented.");
        }
    }

}
