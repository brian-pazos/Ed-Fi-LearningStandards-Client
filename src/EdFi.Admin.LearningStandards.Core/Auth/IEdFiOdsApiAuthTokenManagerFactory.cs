// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Auth
{
    public interface IEdFiOdsApiAuthTokenManagerFactory
    {
        Task<IAuthTokenManager> CreateEdFiOdsApiAuthTokenManager(IEdFiVersionManager edFiVersionManager, IEdFiOdsApiConfiguration edFiOdsApiConfiguration);
    }
}
