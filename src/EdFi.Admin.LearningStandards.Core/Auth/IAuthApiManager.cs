using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Auth
{
    public interface IAuthApiManager
    {
        Task<HttpRequestMessage> GetAuthenticatedRequestAsync(
            HttpMethod httpMethod,
            Uri uri,
            HttpContent content = null);
    }
}
