// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Auth;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Services.Interfaces
{
    public interface ILearningStandardsDataValidator
    {
        Task<IResponse> ValidateConnection(IAuthApiManager learningStandardsProviderAuthTokenManager);
    }
}
