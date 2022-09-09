using System.Net.Http;

namespace EdFi.Admin.LearningStandards.Tests.Utilities
{
    public class MockHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name = "")
        {
            return new HttpClient();
        }
    }
}
