using EdFi.Admin.LearningStandards.Core.Models.ABConnectApiModels;
using System.Collections.Generic;

namespace EdFi.Admin.LearningStandards.Core.Services.Interfaces
{
    public interface ILearningStandardsDataMapper
    {
        IEnumerable<EdFiBulkJsonModel> ToEdFiModel(
            EdFiOdsApiCompatibilityVersion version,
            ILearningStandardsApiResponseModel response);
    }
}
