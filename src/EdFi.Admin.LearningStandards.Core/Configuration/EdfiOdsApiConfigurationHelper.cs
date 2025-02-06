// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Linq;

namespace EdFi.Admin.LearningStandards.Core.Configuration
{
    public class EdFiOdsApiConfigurationHelper
    {
        public static Uri ResolveAuthenticationUrl(EdFiOdsApiCompatibilityVersion edFiOdsApiCompatibilityVersion, string url)
        {
            return ResolveAuthenticationUrl(edFiOdsApiCompatibilityVersion, url, string.Empty);
        }

        [Obsolete("Only used in ODS WebAPI 2.x. Use EdFiVersionManager instead.")]
        public static Uri ResolveAuthenticationUrl(EdFiOdsApiCompatibilityVersion edFiOdsApiCompatibilityVersion, string baseUrl, string path)
        {
            Check.NotEmpty(baseUrl, nameof(baseUrl));

            return new Uri(ConcatUrlSegments(baseUrl, "oauth", path));
        }




        public static string ConcatUrlSegments(params string[] segments)
        {
            return string.Join("/", segments.Where(sl => !string.IsNullOrEmpty(sl)).Select(sl => sl.Trim('/')));
        }
    }
}
