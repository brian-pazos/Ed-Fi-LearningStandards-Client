// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Auth
{
    public class EdFiOdsApiAuthTokenManagerFactory
        : IEdFiOdsApiAuthTokenManagerFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILoggerFactory _loggerFactory;

        public EdFiOdsApiAuthTokenManagerFactory(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            _httpClientFactory = httpClientFactory;
            _loggerFactory = loggerFactory;
        }

        public async Task<IAuthTokenManager> CreateEdFiOdsApiAuthTokenManager(IEdFiVersionManager edFiVersionManager, IEdFiOdsApiConfiguration edFiOdsApiConfiguration)
        {
            var version = await edFiVersionManager.GetEdFiVersion(edFiOdsApiConfiguration);
            switch (version.WebApiVersion)
            {
                case EdFiWebApiVersion.v2x:
                    return new EdFiOdsApiv2AuthTokenManager(
                        edFiOdsApiConfiguration,
                        _httpClientFactory.CreateClient(nameof(IAuthTokenManager)),
                        _loggerFactory.CreateLogger<EdFiOdsApiv2AuthTokenManager>());
                case EdFiWebApiVersion.v3x:
                case EdFiWebApiVersion.v5x:
                case EdFiWebApiVersion.v6x:
                case EdFiWebApiVersion.v7x:
                default: // assume latest version
                    return new EdFiOdsApiv3AuthTokenManager(
                        edFiOdsApiConfiguration,
                        edFiVersionManager,
                        _httpClientFactory.CreateClient(nameof(IAuthTokenManager)),
                        _loggerFactory.CreateLogger<EdFiOdsApiv3AuthTokenManager>());
            }

        }
    }
}
