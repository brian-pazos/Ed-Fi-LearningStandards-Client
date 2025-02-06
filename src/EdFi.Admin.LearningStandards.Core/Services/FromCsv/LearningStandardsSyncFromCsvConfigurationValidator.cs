// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces.FromCsv;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Services.FromCsv
{
    /// <inheritdoc />
    public class LearningStandardsSyncFromCsvConfigurationValidator : ILearningStandardsSyncFromCsvConfigurationValidator
    {
        private readonly IEdFiOdsApiAuthTokenManagerFactory _edFiOdsApiAuthTokenManagerFactory;

        private readonly IEdFiVersionManager _edFiVersionManager;

        private readonly ILogger<LearningStandardsSyncFromCsvConfigurationValidator> _logger;

        public LearningStandardsSyncFromCsvConfigurationValidator(
            IEdFiOdsApiAuthTokenManagerFactory edFiOdsApiAuthTokenManagerFactory,
            IEdFiVersionManager edFiVersionManager,
            ILogger<LearningStandardsSyncFromCsvConfigurationValidator> logger)
        {
            _edFiOdsApiAuthTokenManagerFactory = edFiOdsApiAuthTokenManagerFactory;
            _edFiVersionManager = edFiVersionManager;
            _logger = logger;
        }

        public async Task<IResponse> ValidateEdFiOdsApiConfigurationAsync(
            IEdFiOdsApiConfiguration edFiOdsApiConfiguration)
        {
            try
            {
                if (!edFiOdsApiConfiguration.Version.Equals(EdFiOdsApiCompatibilityVersion.v3))
                    throw new NotSupportedException(
                        "Sync from csv operation is not supported on ODS API version 2. Only supported from version 3 onwards.");

                string token = await (await _edFiOdsApiAuthTokenManagerFactory
                    .CreateEdFiOdsApiAuthTokenManager(_edFiVersionManager, edFiOdsApiConfiguration))
                    .GetTokenAsync()
                    .ConfigureAwait(false);

                return new ResponseModel(true, string.Empty, token, HttpStatusCode.OK);

            }
            catch (Exception ex)
            {
                return ex.ToLearningStandardsResponse();
            }
        }
    }
}
