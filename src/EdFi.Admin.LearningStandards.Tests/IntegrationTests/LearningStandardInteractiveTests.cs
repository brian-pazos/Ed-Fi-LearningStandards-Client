// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using System.Collections.Async;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EdFi.Admin.LearningStandards.Tests.IntegrationTests
{
    [TestFixture]
    [Category("Interactive")]
    [Ignore("These tests are for interactive local use only. Comment out this line to use.")]
    public class LearningStandardInteractiveTests
    {
        private const string AbClientId = "abtestsmall";
        private const string AbSecret = "<Your secret here>";
        private const string ProxyUrl = "https://abproxy.datahub.inscloudgate.net/api/";

        [TestCase(EdFiOdsApiCompatibilityVersion.v2)]
        [TestCase(EdFiOdsApiCompatibilityVersion.v3)]
        public async Task Interactive_AB_Sync_Test(EdFiOdsApiCompatibilityVersion version)
        {
            var academicBenchmarksSnapshotOptionMock =
                new Mock<IOptionsSnapshot<AcademicBenchmarksOptions>>();
            academicBenchmarksSnapshotOptionMock.Setup(x => x.Value)
                                                .Returns(
                                                    new AcademicBenchmarksOptions
                                                    {
                                                        Url = ProxyUrl
                                                    });

            var syncOptions = new LearningStandardsSynchronizationOptions();

            var authTokenManager = new AcademicBenchmarksAuthTokenManager(
                academicBenchmarksSnapshotOptionMock.Object,
                new AuthenticationConfiguration(AbClientId, AbSecret),
                new NUnitConsoleLogger<AcademicBenchmarksAuthTokenManager>());

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient();

            clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object);

            var count = 0;

            var collector = new List<string>();
            // Act /Assert
            await sut.GetLearningStandards(version, new ChangeSequence(), authTokenManager, default(CancellationToken)).AsyncEntityEnumerable
                     .ForEachAsync(
                         actual =>
                         {
                             count++;
                             Assert.IsInstanceOf<EdFiBulkJsonModel>(actual);
                             Assert.IsNotNull(actual.Operation);
                             Assert.IsNotNull(actual.Resource);
                             Assert.IsNotNull(actual.Data);
                             collector.Add(JsonConvert.SerializeObject(actual,Formatting.Indented));
                         }).ConfigureAwait(false);

            File.WriteAllLines($@"C:\temp\ls_out_{version}.txt", collector.Take(1));

            // Assert
            Assert.AreEqual(63, count);
        }

        [TestCase(EdFiOdsApiCompatibilityVersion.v2, 988335, 0)]
        [TestCase(EdFiOdsApiCompatibilityVersion.v3, 823197, 1)]
        [TestCase(EdFiOdsApiCompatibilityVersion.v3, 780965, 2)]
        [TestCase(EdFiOdsApiCompatibilityVersion.v3, 780964, 3)]
        public async Task Interactive_AB_Change_Sequence_Sync_Test(EdFiOdsApiCompatibilityVersion version, int sequenceId, int expectedCount)
        {
            var academicBenchmarksSnapshotOptionMock =
                new Mock<IOptionsSnapshot<AcademicBenchmarksOptions>>();
            academicBenchmarksSnapshotOptionMock.Setup(x => x.Value)
                                                .Returns(
                                                    new AcademicBenchmarksOptions
                                                    {
                                                        Url = ProxyUrl
                                                    });

            var authTokenManager = new AcademicBenchmarksAuthTokenManager(
                academicBenchmarksSnapshotOptionMock.Object,
                new AuthenticationConfiguration(AbClientId, AbSecret),
                new NUnitConsoleLogger<AcademicBenchmarksAuthTokenManager>());

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient();

            clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object);

            var count = 0;

            var collector = new List<string>();
            // Act /Assert
            await sut.GetLearningStandards(version, new ChangeSequence(), authTokenManager, default(CancellationToken)).AsyncEntityEnumerable
                     .ForEachAsync(
                         actual =>
                         {
                             count++;
                             Assert.IsInstanceOf<EdFiBulkJsonModel>(actual);
                             Assert.IsNotNull(actual.Operation);
                             Assert.IsNotNull(actual.Resource);
                             Assert.IsNotNull(actual.Data);
                             collector.Add(JsonConvert.SerializeObject(actual,Formatting.Indented));
                         }).ConfigureAwait(false);

            File.WriteAllLines($@"C:\temp\ls_out_{version}.txt", collector.Take(1));

            // Assert
            Assert.AreEqual(expectedCount, count);
        }

        [TestCase(EdFiOdsApiCompatibilityVersion.v2)]
        [TestCase(EdFiOdsApiCompatibilityVersion.v3)]
        public async Task Interactive_AB_Descriptor_Test(EdFiOdsApiCompatibilityVersion version)
        {
            var academicBenchmarksSnapshotOptionMock =
                new Mock<IOptionsSnapshot<AcademicBenchmarksOptions>>();
            academicBenchmarksSnapshotOptionMock.Setup(x => x.Value)
                                                .Returns(
                                                    new AcademicBenchmarksOptions
                                                    {
                                                        Url = ProxyUrl
                                                    });

            var syncOptions = new LearningStandardsSynchronizationOptions();

            var authTokenManager = new AcademicBenchmarksAuthTokenManager(
                academicBenchmarksSnapshotOptionMock.Object,
                new AuthenticationConfiguration(AbClientId, AbSecret),
                new NUnitConsoleLogger<AcademicBenchmarksAuthTokenManager>());

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient();

            clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object);

            var count = 0;
            var collector = new List<string>();

            // Act /Assert
            await sut.GetLearningStandardsDescriptors(version, new ChangeSequence(), authTokenManager, default(CancellationToken)).AsyncEntityEnumerable
                     .ForEachAsync(
                         actual =>
                         {
                             count++;
                             Assert.IsInstanceOf<EdFiBulkJsonModel>(actual);
                             Assert.IsNotNull(actual.Operation);
                             Assert.IsNotNull(actual.Resource);
                             Assert.IsNotNull(actual.Data);
                             collector.Add(JsonConvert.SerializeObject(actual));
                         }).ConfigureAwait(false);

            File.WriteAllLines($@"C:\temp\ls_desc_out_{version}.txt", collector);

            switch (version)
            {
                // Assert
                case EdFiOdsApiCompatibilityVersion.v2:
                    Assert.AreEqual(2, count);
                    break;
                case EdFiOdsApiCompatibilityVersion.v3:
                    Assert.AreEqual(3, count);
                    break;
                default:
                    Assert.Fail($"Version: {version} not handled.");
                    break;
            }
        }

        [Test]
        public async Task Interactive_AB_Status_Test()
        {
            var academicBenchmarksSnapshotOptionMock =
                new Mock<IOptionsSnapshot<AcademicBenchmarksOptions>>();
            academicBenchmarksSnapshotOptionMock.Setup(x => x.Value)
                                                .Returns(
                                                    new AcademicBenchmarksOptions
                                                    {
                                                        Url = ProxyUrl
                                                    });

            var authTokenManager = new AcademicBenchmarksAuthTokenManager(
                academicBenchmarksSnapshotOptionMock.Object,
                new AuthenticationConfiguration(AbClientId, AbSecret),
                new NUnitConsoleLogger<AcademicBenchmarksAuthTokenManager>());

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient();

            clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object);

            // Act
            var response = await sut.ValidateConnection(authTokenManager).ConfigureAwait(false);

            // Assert
            Assert.True(response.IsSuccess);
        }
    }
}
