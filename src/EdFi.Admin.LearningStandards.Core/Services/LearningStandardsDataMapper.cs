using EdFi.Admin.LearningStandards.Core.Configuration;
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
        private readonly Dictionary<EdFiOdsApiCompatibilityVersion, IEdFiDataStandardVersionHandler> _versionHandlers;

        public LearningStandardsDataMapper(
            IOptionsSnapshot<AcademicBenchmarksOptions> academicBenchmarksOptionsSnapshot,
            ILogger<LearningStandardsDataMapper> logger)
        {
            _learningStandardsProviderConfiguration = academicBenchmarksOptionsSnapshot.Value;
            _logger = logger;

            // Register handlers for each version
            _versionHandlers = new Dictionary<EdFiOdsApiCompatibilityVersion, IEdFiDataStandardVersionHandler>
        {
            { EdFiOdsApiCompatibilityVersion.v3, new EdFiDataStandardV3Handler(_learningStandardsProviderConfiguration, logger) }
        };
        }


        public IEnumerable<EdFiBulkJsonModel> ToEdFiModel(
                    EdFiOdsApiCompatibilityVersion version,
                    ILearningStandardsApiResponseModel response)
        {
            if (_versionHandlers.TryGetValue(version, out var handler))
            {
                return handler.MapResponse(response);
            }

            throw new NotImplementedException($"Mapping to ODS version:{version} is not implemented.");
        }
    }

}
