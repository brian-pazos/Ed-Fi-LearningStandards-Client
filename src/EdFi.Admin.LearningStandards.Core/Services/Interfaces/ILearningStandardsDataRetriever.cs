// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Core.Services.Interfaces
{
    public interface ILearningStandardsDataRetriever
    {
        AsyncEnumerableOperation<EdFiBulkJsonModel> GetLearningStandardsDescriptors(
            EdFiOdsApiCompatibilityVersion version,
            IChangeSequence syncStartSequence,
            IAuthApiManager learningStandardsProviderAuthTokenManager,
            CancellationToken cancellationToken = default);

        AsyncEnumerableOperation<LearningStandardsSegmentModel> GetChangedSegments(
            EdFiOdsApiCompatibilityVersion version,
            IChangeSequence syncStartSequence,
            IAuthApiManager learningStandardsProviderAuthTokenManager,
            CancellationToken cancellationToken = default);


        AsyncEnumerableOperation<EdFiBulkJsonModel> GetSegmentLearningStandards(
            EdFiOdsApiCompatibilityVersion version,
            LearningStandardsSegmentModel section,
            IAuthApiManager learningStandardsProviderAuthTokenManager,
            CancellationToken cancellationToken = default);

        Task<IChangesAvailableResponse> GetChangesAsync(
            IChangeSequence currentSequence,
            IAuthApiManager learningStandardsProviderAuthTokenManager,
            CancellationToken cancellationToken = default);

        event EventHandler<AsyncEnumerableOperationStatus> ProcessCountEvent;
    }
}
