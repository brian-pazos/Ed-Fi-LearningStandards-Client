using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Models.ABConnectApiModels;
using System.Collections.Generic;

namespace EdFi.Admin.LearningStandards.Core.Services.Interfaces
{
    public interface ILearningStandardsDataMapper
    {
        IEnumerable<EdFiBulkJsonModel> ToEdFiModel(
            EdFiVersionModel version,
            ILearningStandardsApiResponseModel response);
    }
}
