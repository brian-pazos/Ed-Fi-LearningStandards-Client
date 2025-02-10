using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Services.Interfaces
{
    public interface IEdFiVersionManager
    {
        Task<EdFiVersionModel> GetEdFiVersion(IEdFiOdsApiConfiguration edFiOdsApiConfiguration, CancellationToken cancellationToken = default(CancellationToken));

        Task<Uri> ResolveAuthenticationUrl(IEdFiOdsApiConfiguration edFiOdsApiConfiguration);

        Task<Uri> ResolveResourceUrl(IEdFiOdsApiConfiguration edFiOdsApiConfiguration, string schema, string resource);

    }
}
