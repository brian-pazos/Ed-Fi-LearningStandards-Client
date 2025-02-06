// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using CommandLine;
using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Configuration;
using System;
using System.Linq;

namespace EdFi.Admin.LearningStandards.CLI
{
    public class LearningStandardsCLIBaseOptions
    {
        //Required segments
        [Option("ab-connect-id", Required = true, HelpText = "The Academic Benchmarks AB Connect ID to use.")]
        public string AcademicBenchmarksConnectId { get; set; }

        [Option("ab-connect-key", Required = true, HelpText = "The Academic Benchmarks AB Connect Key to use.")]
        public string AcademicBenchmarksConnectKey { get; set; }

        [Option("ed-fi-url", Required = true, HelpText = "The Ed-Fi ODS url to use.")]
        public string EdFiUrl { get; set; }

        [Option("ed-fi-key", Required = true, HelpText = "The Ed-Fi ODS API key to use.")]
        public string EdFiKey { get; set; }

        [Option("ed-fi-secret", Required = true, HelpText = "The Ed-Fi ODS API secret to use.")]
        public string EdFiSecret { get; set; }

        //Optional segments
        [Option("ab-auth-window", Required = false, HelpText = "The buffer window, in seconds to use when refreshing an upcoming token expiration. Defaults to 300.")]
        public int AcademicBenchmarksAuthorizationWindow { get; set; } = 300;

        [Option("ab-retry-limit", Required = false, HelpText = "The number of retry attempts the application will make in case of failure. Defaults to 3.")]
        public int AcademicBenchmarksRetryLimit { get; set; } = 3;

        [Option("ed-fi-auth-url", Required = false, HelpText = "The Ed-Fi ODS authentication url to use. Defaults to the oauth section of the provided base url.")]
        public string EdFiAuthenticationUrl { get; set; }

        [Option("ed-fi-version", Required = false, HelpText = "The Ed-Fi ODS version to use. Defaults to latest version.")]
        public EdFiOdsApiCompatibilityVersion EdFiCompatibilityVersion { get; set; } = Enum.GetValues(typeof(EdFiOdsApiCompatibilityVersion)).Cast<EdFiOdsApiCompatibilityVersion>().Max();

        [Option("ed-fi-school-year", Required = false, HelpText = "The school year used when querying the Ed-Fi ODS API. Applicable only to ODS version 6.x and earlier.")]
        public int? EdFiSchoolYear { get; set; }

        [Option("ed-fi-routing-context-key", Required = false, HelpText = "The routing context key used when querying the Ed-Fi ODS API. Applicable only to ODS version 7.x.")]
        public string EdFiRoutingContextKey { get; set; }


        [Option("ed-fi-retry-limit", Required = false, HelpText = "The number of retry attempts the application will make in case of failure. Defaults to 2.")]
        public int EdFiRetryLimit { get; set; } = 2;

        [Option("ed-fi-simultaneous-request-limit", Required = false, HelpText = "The number of simultaneous requests allowed during synchronization. Defaults to 4.")]
        public int EdFiMaxSimultaneousRequests { get; set; } = 4;

        [Option("ab-proxy-url", Hidden = true)]
        public string AcademicBenchmarksProxyUrl { get; set; }

        [Option("change-sequence-store-path", Required = false, HelpText = "Specifies the file path for storing change sequence ids.")]
        public string ChangeSequenceStorePath { get; set; } = "cst.json";

        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('u', "unattended", Required = false, HelpText = "If enabled, the application will close immediately when finished.")]
        public bool Unattended { get; set; }

        public EdFiOdsApiConfiguration ToEdFiOdsApiConfiguration()
        {
            var authResult = new AuthenticationConfiguration(EdFiKey, EdFiSecret);
            return new EdFiOdsApiConfiguration(
                EdFiUrl,
                EdFiCompatibilityVersion,
                authResult,
                EdFiSchoolYear,
                EdFiRoutingContextKey,
                EdFiAuthenticationUrl);
        }

        public AuthenticationConfiguration ToAcademicBenchmarksAuthenticationConfiguration()
        {
            return new AuthenticationConfiguration(AcademicBenchmarksConnectId, AcademicBenchmarksConnectKey);
        }

        public EdFiOdsApiClientConfiguration ToEdFiOdsApiClientConfiguration()
        {
            return new EdFiOdsApiClientConfiguration(EdFiRetryLimit, EdFiMaxSimultaneousRequests);
        }
    }
}
