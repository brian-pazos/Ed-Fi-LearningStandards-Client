// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Services
{
    /// <inheritdoc />
    public class LearningStandardsConfigurationValidator : ILearningStandardsConfigurationValidator
    {
        private readonly IEdFiOdsApiAuthTokenManagerFactory _edFiOdsApiAuthTokenManagerFactory;

        private readonly IEdFiVersionManager _edFiVersionManager;

        private readonly ILearningStandardsProviderAuthApiManagerFactory _learningStandardsProviderAuthTokenManagerFactory;

        private readonly ILearningStandardsDataValidator _learningStandardsDataValidator;

        private readonly ILogger<LearningStandardsConfigurationValidator> _logger;

        public LearningStandardsConfigurationValidator(
            IEdFiOdsApiAuthTokenManagerFactory edFiOdsApiAuthTokenManagerFactory,
            IEdFiVersionManager edFiVersionManager,
            ILearningStandardsProviderAuthApiManagerFactory learningStandardsProviderAuthTokenManagerFactory,
            ILearningStandardsDataValidator learningStandardsDataValidator,
            ILogger<LearningStandardsConfigurationValidator> logger)
        {
            _edFiOdsApiAuthTokenManagerFactory = edFiOdsApiAuthTokenManagerFactory;
            _edFiVersionManager = edFiVersionManager;
            _learningStandardsProviderAuthTokenManagerFactory = learningStandardsProviderAuthTokenManagerFactory;
            _learningStandardsDataValidator = learningStandardsDataValidator;
            _logger = logger;
        }

        public async Task<IResponse> ValidateConfigurationAsync(
            IAuthenticationConfiguration learningStandardsAuthenticationConfiguration,
            IEdFiOdsApiConfiguration edFiOdsApiConfiguration)
        {
            var taskList = new List<Task<IResponse>>
            {
                ValidateLearningStandardProviderConfigurationAsync(learningStandardsAuthenticationConfiguration),
                ValidateEdFiOdsApiConfigurationAsync(edFiOdsApiConfiguration)
            };

            var tasks = await Task.WhenAll(taskList).ConfigureAwait(false);
            return ResponseModel.Aggregate(tasks);
        }

        public async Task<IResponse> ValidateLearningStandardProviderConfigurationAsync(IAuthenticationConfiguration learningStandardsAuthenticationConfiguration)
        {
            try
            {
                return await _learningStandardsDataValidator.ValidateConnection(_learningStandardsProviderAuthTokenManagerFactory
                        .CreateLearningStandardsProviderAuthApiManager(learningStandardsAuthenticationConfiguration))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return ex.ToLearningStandardsResponse();
            }
        }

        public async Task<IResponse> ValidateEdFiOdsApiConfigurationAsync(IEdFiOdsApiConfiguration edFiOdsApiConfiguration)
        {
            try
            {
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
