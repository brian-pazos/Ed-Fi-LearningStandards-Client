using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Services
{


    public class EdFiVersionManager : IEdFiVersionManager
    {
        private readonly HttpClient _httpClient;

        private readonly ILogger<EdFiVersionManager> _logger;

        private Dictionary<string, EdFiVersionModel> _apiVersionsCache;


        public EdFiVersionManager(IHttpClientFactory httpClientFactory, ILogger<EdFiVersionManager> logger)
        {
            Check.NotNull(httpClientFactory, nameof(httpClientFactory));
            Check.NotNull(logger, nameof(logger));

            _httpClient = httpClientFactory.CreateClient(nameof(IEdFiVersionManager));
            _logger = logger;

            _apiVersionsCache = new Dictionary<string, EdFiVersionModel>();

        }


        public async Task<EdFiVersionModel> GetEdFiVersion(IEdFiOdsApiConfiguration edFiOdsApiConfiguration, CancellationToken cancellationToken = default(CancellationToken))
        {
            var apiUrl = edFiOdsApiConfiguration.Url;
            if (!_apiVersionsCache.ContainsKey(apiUrl))
            {
                var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                var response = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
                string responseContent = await response.ReadContentAsStringOrEmptyAsync().ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = "There was an error sending the access code request.";

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.NotFound:
                            errorMessage = $"The specified access token oAuth url could not be found. ({req.RequestUri})";
                            break;
                        case HttpStatusCode.Unauthorized:
                            errorMessage = "The specified Ed-Fi ODS API credentials were not valid.";
                            break;
                    }

                    throw ExceptionFromResponse(errorMessage, response.StatusCode, responseContent);
                }

                var webApiInfo = JsonSerializer.Deserialize<EdFiWebApiInfo>(responseContent);
                if (string.IsNullOrWhiteSpace(webApiInfo.version))
                {
                    throw ExceptionFromResponse("The WebApi version field did not exist on the server response.", response.StatusCode, responseContent);
                }



                var version = new EdFiVersionModel(
                                    GetWeApiVersionFromString(webApiInfo.version),
                                    GetDataStandardVersionFromString(webApiInfo.dataModels?.FirstOrDefault(dm => dm.name == "Ed-Fi")?.version), webApiInfo);
                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter() },
                    WriteIndented = true
                };
                _logger.LogInformation($"WebApi: {apiUrl} Version:{Environment.NewLine}{JsonSerializer.Serialize(version, options)}");

                _apiVersionsCache.Add(apiUrl, version);

                return version;
            }

            return _apiVersionsCache[apiUrl];
        }


        private static string _tokenPath = "oauth/token";

        public async Task<Uri> ResolveAuthenticationUrl(IEdFiOdsApiConfiguration edFiOdsApiConfiguration)
        {
            var version = await GetEdFiVersion(edFiOdsApiConfiguration);
            switch (version.WebApiVersion)
            {
                case EdFiWebApiVersion.v2x:
                    throw new NotImplementedException("Legacy WebAPi Version 2.x not supported. Use Helper EdFiOdsApiConfigurationHelper.");

                case EdFiWebApiVersion.v3x:
                case EdFiWebApiVersion.v5x:
                case EdFiWebApiVersion.v6x:
                    return new Uri(EdFiOdsApiConfigurationHelper.ConcatUrlSegments(edFiOdsApiConfiguration.AuthenticationUrl, _tokenPath));

                case EdFiWebApiVersion.v7x:
                default: // assume newer api versions will use same routing as current latest version (v7.x)
                    return new Uri(EdFiOdsApiConfigurationHelper.ConcatUrlSegments(edFiOdsApiConfiguration.AuthenticationUrl, edFiOdsApiConfiguration.RoutingContextKey, _tokenPath));
            }
        }

        public async Task<Uri> ResolveResourceUrl(
            IEdFiOdsApiConfiguration edFiOdsApiConfiguration,
            string schema,
            string resource
            )
        {
            Check.NotEmpty(edFiOdsApiConfiguration.Url, nameof(edFiOdsApiConfiguration.Url));
            Check.NotEmpty(resource, nameof(resource));
            Check.NotEmpty(schema, nameof(schema));

            var version = await GetEdFiVersion(edFiOdsApiConfiguration);

            var sb = new StringBuilder(edFiOdsApiConfiguration.Url.TrimEnd('/'));

            switch (version.WebApiVersion)
            {
                case EdFiWebApiVersion.v2x:
                    throw new NotImplementedException("Legacy WebAPi Version 2.x not supported. Use Helper EdFiBulkJsonPersisterHelper.");

                case EdFiWebApiVersion.v3x:
                case EdFiWebApiVersion.v5x:
                case EdFiWebApiVersion.v6x:
                    sb.Append("/data/v3");
                    if (edFiOdsApiConfiguration.SchoolYear.HasValue)
                    {
                        sb.AppendFormat("/{0}", edFiOdsApiConfiguration.SchoolYear.Value);
                    }
                    sb.AppendFormat("/{0}", schema);
                    break;

                case EdFiWebApiVersion.v7x:
                default: // assume newer api versions will use same routing as current latest version (v7.x)
                    if (!string.IsNullOrWhiteSpace(edFiOdsApiConfiguration.RoutingContextKey))
                    {
                        sb.AppendFormat("/{0}", edFiOdsApiConfiguration.RoutingContextKey);
                    }
                    sb.Append("/data/v3");
                    sb.AppendFormat("/{0}", schema);
                    break;
            }
            sb.AppendFormat("/{0}", resource.TrimStart('/'));
            return new Uri(sb.ToString());
        }


        private EdFiWebApiVersion GetWeApiVersionFromString(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return EdFiWebApiVersion.Unknown;

            // Check the first character
            switch (versionString.Trim()[0])
            {
                case '2':
                    return EdFiWebApiVersion.v2x;
                case '3':
                    return EdFiWebApiVersion.v3x;
                case '5':
                    return EdFiWebApiVersion.v5x;
                case '6':
                    return EdFiWebApiVersion.v6x;
                case '7':
                    return EdFiWebApiVersion.v7x;

                default:
                    {
                        _logger.LogWarning("WebApi Version did not match, assume the latest version will work");
                        // If unable to find a specific version, assume the latest version will work
                        return EdFiWebApiVersion.v7x;
                    }

            }
        }

        private EdFiDataStandardVersion GetDataStandardVersionFromString(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
                return EdFiDataStandardVersion.Unknown;

            versionString = versionString.Trim();

            if (versionString.StartsWith("5.2"))
                return EdFiDataStandardVersion.DS5_2;
            if (versionString.StartsWith("5.1"))
                return EdFiDataStandardVersion.DS5_1;
            if (versionString.StartsWith("5.0"))
                return EdFiDataStandardVersion.DS5_0;
            if (versionString.StartsWith("4"))
                return EdFiDataStandardVersion.DS4;
            if (versionString.StartsWith("3"))
                return EdFiDataStandardVersion.DS3;
            if (versionString.StartsWith("2"))
                return EdFiDataStandardVersion.DS2;




            _logger.LogWarning("DataStandard Version did not match, assume the latest version will work");
            return EdFiDataStandardVersion.DS5_2;
        }

        private Exception ExceptionFromResponse(string message, HttpStatusCode statusCode, string responseContent)
        {
            var ex = new LearningStandardsHttpRequestException(message, statusCode, responseContent, ServiceNames.EdFi);
            _logger.LogError(ex.Message);
            return ex;
        }
    }

}
