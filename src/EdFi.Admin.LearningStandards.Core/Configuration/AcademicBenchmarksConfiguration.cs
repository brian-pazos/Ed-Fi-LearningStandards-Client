// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Admin.LearningStandards.Core.Configuration
{
    public class AcademicBenchmarksOptions : ILearningStandardsProviderConfiguration
    {
        public string Url { get; set; } = "https://api.abconnect.certicaconnect.com/rest/v4.1/";

        public int Retries { get; set; } = 3;

        public int MaxSimultaneousRequests { get; set; } = 10;

        public int AuthorizationWindowSeconds { get; set; } = 300;

        public int DefaultReportedRecordCount { get; set; } = int.MaxValue;
    }
}
