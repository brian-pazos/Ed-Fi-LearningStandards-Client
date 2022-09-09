using System.Net.Http;
using System.Threading.Tasks;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces.FromCsv;
using Microsoft.Extensions.Logging;

namespace EdFi.Admin.LearningStandards.Core.Services.FromCsv
{
    public class SwaggerDocumentRetriever : ISwaggerDocumentRetriever
    {
        private readonly ILogger<SwaggerDocumentRetriever> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public SwaggerDocumentRetriever(ILogger<SwaggerDocumentRetriever> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> LoadJsonString(string metaDataUri)
        {
            string swaggerDocument;
            var httpClient = _httpClientFactory.CreateClient();

            using (var response = await httpClient.GetAsync(metaDataUri))
            {
                using (var content = response.Content)
                {
                    _logger.LogInformation($"Loading swagger document from {metaDataUri}.");
                    swaggerDocument = await content.ReadAsStringAsync();
                }
            }
            return swaggerDocument;
        }
    }
}
