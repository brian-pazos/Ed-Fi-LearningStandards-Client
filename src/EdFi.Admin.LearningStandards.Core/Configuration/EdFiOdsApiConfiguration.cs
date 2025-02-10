// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Admin.LearningStandards.Core.Configuration
{
    public class EdFiOdsApiConfiguration : IEdFiOdsApiConfiguration
    {
        /// <summary>
        /// EdFiOdsApiConfiguration for the ODS API to be populated with LearningStandards
        /// </summary>
        /// <param name="url">The base url for the ODS API </param>
        /// <param name="version">The major version of the ODS API</param>
        /// <param name="oAuthAuthenticationConfiguration">The OAuth authentication credentials</param>
        /// <param name="schoolYear">The school year (if applicable), required for v2</param>
        /// <param name="authenticationUrl">Optionally specify the path to the base url for authorization. (Should not include /oauth route)</param>
        public EdFiOdsApiConfiguration(
            string url,
            EdFiOdsApiCompatibilityVersion version,
            IAuthenticationConfiguration oAuthAuthenticationConfiguration,
            int? schoolYear = null,
            string routingContextKey = null,
            string authenticationUrl = null)
        {
            Url = url;
            Version = version;
            OAuthAuthenticationConfiguration = oAuthAuthenticationConfiguration;
            SchoolYear = schoolYear;
            RoutingContextKey = routingContextKey;
            AuthenticationUrl = authenticationUrl ?? url;
        }

        public string Url { get; }

        public string AuthenticationUrl { get; }

        public IAuthenticationConfiguration OAuthAuthenticationConfiguration { get; }

        public int? SchoolYear { get; }

        public string RoutingContextKey { get; }

        public EdFiOdsApiCompatibilityVersion Version { get; }
    }
}
