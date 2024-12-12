// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace EdFi.Admin.LearningStandards.Core.Auth
{
    public class AcademicBenchmarksAuthApiManagerFactory : ILearningStandardsProviderAuthApiManagerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AcademicBenchmarksAuthApiManager> _logger;

        public AcademicBenchmarksAuthApiManagerFactory(
            IServiceProvider serviceProvider,
            ILogger<AcademicBenchmarksAuthApiManager> logger)
        {
            Check.NotNull(logger, nameof(logger));

            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IAuthApiManager CreateLearningStandardsProviderAuthApiManager(
            IAuthenticationConfiguration authenticationConfiguration)
        {
            Check.NotNull(authenticationConfiguration, nameof(authenticationConfiguration));

            return new AcademicBenchmarksAuthApiManager(
                _serviceProvider.GetRequiredService<IOptionsSnapshot<AcademicBenchmarksOptions>>(),
                authenticationConfiguration,
                _logger);
        }
    }
}
