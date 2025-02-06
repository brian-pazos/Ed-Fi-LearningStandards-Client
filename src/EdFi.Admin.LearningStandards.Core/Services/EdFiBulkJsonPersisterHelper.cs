// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System;
using System.Text;

namespace EdFi.Admin.LearningStandards.Core.Services
{
    public class EdFiBulkJsonPersisterHelper
    {

        [Obsolete("Use EdFiVersionManager instead.", true)]
        /// <summary>
        /// Creates an ODS API Uri based on the specified parameters.
        /// </summary>
        /// <param name="baseUrl">The base url for the intended ODS API instance</param>
        /// <param name="schema">The v3 specific schema to use</param>
        /// <param name="resource">The resource location</param>
        /// <param name="version">The API version to use</param>
        /// <param name="schoolYear">The specific school year to use. Note: Required in v2</param>
        /// <returns>A Uri that resolves to the specified ODS instance and version</returns>
        /// <exception cref="ArgumentException">description</exception>
        /// <exception cref="ArgumentNullException">description</exception>
        /// /// <exception cref="FormatException">description</exception>
        public static Uri ResolveOdsApiResourceUrl(
            string baseUrl,
            string schema,
            string resource,
            EdFiOdsApiCompatibilityVersion version,
            int? schoolYear)
        {
            Check.NotEmpty(baseUrl, nameof(baseUrl));
            Check.NotEmpty(resource, nameof(resource));

            var sb = new StringBuilder(baseUrl.TrimEnd('/'));

            switch (version)
            {
                case EdFiOdsApiCompatibilityVersion.v2:
                    Check.NotNull(schoolYear, nameof(schoolYear));

                    sb.Append("/api/v2.0");
                    sb.AppendFormat("/{0}", schoolYear);

                    break;
                case EdFiOdsApiCompatibilityVersion.v3:
                    Check.NotEmpty(schema, nameof(schema));

                    sb.Append("/data/v3");
                    if (schoolYear != null)
                    {
                        sb.AppendFormat("/{0}", schoolYear);
                    }
                    sb.AppendFormat("/{0}", schema);

                    break;
            }
            sb.AppendFormat("/{0}", resource.TrimStart('/'));


            return new Uri(sb.ToString());
        }
    }
}
