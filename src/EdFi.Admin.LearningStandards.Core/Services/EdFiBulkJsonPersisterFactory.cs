// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace EdFi.Admin.LearningStandards.Core.Services
{
    public interface IEdFiBulkJsonPersisterFactory
    {
        IEdFiBulkJsonPersister CreateEdFiBulkJsonPersister(IAuthTokenManager authTokenManager, IEdFiOdsApiConfiguration odsApiConfiguration);
    }

    public class EdFiBulkJsonPersisterFactory : IEdFiBulkJsonPersisterFactory
    {
        private readonly IEdFiVersionManager _edFiVersionManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EdFiBulkJsonPersister> _logger;

        public EdFiBulkJsonPersisterFactory(IEdFiVersionManager versionManager, IHttpClientFactory httpClientFactory, ILogger<EdFiBulkJsonPersister> logger)
        {
            _edFiVersionManager = versionManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public IEdFiBulkJsonPersister CreateEdFiBulkJsonPersister(IAuthTokenManager authTokenManager, IEdFiOdsApiConfiguration odsApiConfiguration)
        {
            return new EdFiBulkJsonPersister(
                odsApiConfiguration,
                _edFiVersionManager,
                authTokenManager,
                _logger,
                _httpClientFactory.CreateClient(nameof(IEdFiBulkJsonPersister)));
        }
    }
}
