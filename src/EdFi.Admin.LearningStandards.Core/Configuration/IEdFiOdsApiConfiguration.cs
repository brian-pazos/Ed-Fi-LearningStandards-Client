// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Admin.LearningStandards.Core.Configuration
{
    public interface IEdFiOdsApiConfiguration
    {
        /// <summary>
        ///     Base URL for resource requests.
        /// </summary>
        string Url { get; }

        string AuthenticationUrl { get; }

        IAuthenticationConfiguration OAuthAuthenticationConfiguration { get; }

        EdFiOdsApiCompatibilityVersion Version { get; }

        int? SchoolYear { get; }

        string RoutingContextKey { get; }
    }
}
