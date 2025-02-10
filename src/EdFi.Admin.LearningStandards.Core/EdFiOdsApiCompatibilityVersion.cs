// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

namespace EdFi.Admin.LearningStandards.Core
{
    public enum EdFiOdsApiCompatibilityVersion { Unknown = 0, v2 = 2, v3 = 3 }


    public enum EdFiWebApiVersion
    {
        Unknown = 0,
        v2x = 2,
        v3x = 3,
        v5x = 5,
        v6x = 6,
        v7x = 7,
    }

    public enum EdFiDataStandardVersion
    {
        Unknown = 0,
        DS2 = 20,
        DS3 = 30,
        DS4 = 40,
        DS5_0 = 50,
        DS5_1 = 51,
        DS5_2 = 52
    }
}
