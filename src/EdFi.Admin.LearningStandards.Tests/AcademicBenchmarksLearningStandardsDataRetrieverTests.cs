// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Models.ABConnectApiModels;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests
{
    [TestFixture]
    public class AcademicBenchmarksLearningStandardsDataRetrieverTests
    {
        private Mock<IAuthApiManager> _authTokenManager;
        private Mock<IOptionsSnapshot<AcademicBenchmarksOptions>> _academicBenchmarksSnapshotOptionMock;
        private const string DescriptorsRouteType = "Descriptors";
        private const string ChangesRouteType = "Changes";
        private const string SyncRouteType = "Sync";
        private const string StatusRouteType = "Status";
        private const string SectionFailureType = "Section";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {

            _academicBenchmarksSnapshotOptionMock = new Mock<IOptionsSnapshot<AcademicBenchmarksOptions>>();
            _academicBenchmarksSnapshotOptionMock.Setup(x => x.Value)
                                                 .Returns(
                                                     new AcademicBenchmarksOptions
                                                     {
                                                         Url = "https://localhost:7777"
                                                     });

            _authTokenManager = new Mock<IAuthApiManager>();
            _authTokenManager.Setup(x => x.GetAuthenticatedRequestAsync(It.IsAny<HttpMethod>(), It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                             .ReturnsAsync((HttpMethod method, Uri passedUri, HttpContent content)
                                 => new HttpRequestMessage(method, passedUri) { Content = content });

        }

        //[TestCaseSource(typeof(FileBasedTestCases), nameof(FileBasedTestCases.ValidProxyResponseTestCases))]
        //public void When_receiving_proxy_response(
        //    string routeType,
        //    EdFiOdsApiCompatibilityVersion version,
        //    int bulkJsonObjectCount,
        //    string syncResponse
        //    )
        //{
        //    var syncOptions = new LearningStandardsSynchronizationOptions();

        //    var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
        //    fakeHttpMessageHandler.AddRouteResponse($"{ChangesRouteType}/available", JObject.FromObject(new AcademicBenchmarksChangesAvailableModel { EventChangesAvailable = true, MaxSequenceId = 1000 }));
        //    fakeHttpMessageHandler.AddRouteResponse($"{routeType}", JToken.Parse(syncResponse));

        //    var clientFactoryMock = new Mock<IHttpClientFactory>();
        //    var httpClient = new HttpClient(fakeHttpMessageHandler);

        //    clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
        //                     .Returns(httpClient);

        //    var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

        //    var blankChangeSequence = new ChangeSequence();

        //    var mockLearningStandardsDataMapper = new Mock<ILearningStandardsDataMapper>();
        //    mockLearningStandardsDataMapper
        //        .Setup(m => m.ToEdFiModel(It.IsAny<EdFiOdsApiCompatibilityVersion>(), It.IsAny<ILearningStandardsApiResponseModel>()))
        //        .Returns(new List<EdFiBulkJsonModel> { new EdFiBulkJsonModel() });

        //    var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
        //        _academicBenchmarksSnapshotOptionMock.Object,
        //        logger,
        //        clientFactoryMock.Object,
        //        mockLearningStandardsDataMapper.Object);

        //    // Act

        //    System.Collections.Async.IAsyncEnumerator<EdFiBulkJsonModel> result;
        //    switch (routeType)
        //    {
        //        //case SyncRouteType:
        //        //    result = sut.GetLearningStandards(version, blankChangeSequence, _authTokenManager.Object, default(CancellationToken)).AsyncEntityEnumerable
        //        //                .GetAsyncEnumeratorAsync()
        //        //                .Result;
        //        //    break;
        //        case DescriptorsRouteType:
        //            result = sut.GetLearningStandardsDescriptors(version, blankChangeSequence, _authTokenManager.Object, default(CancellationToken)).AsyncEntityEnumerable
        //                        .GetAsyncEnumeratorAsync()
        //                        .Result;
        //            break;
        //        default:
        //            result = null;
        //            break;
        //    }

        //    // Assert
        //    Assert.NotNull(result);
        //    int actualCount = 0;

        //    while (result.MoveNextAsync().Result)
        //    {
        //        actualCount++;
        //        var actual = result.Current;
        //        Assert.IsInstanceOf<EdFiBulkJsonModel>(actual);
        //        Assert.IsNotNull(actual.Operation);
        //        Assert.IsNotNull(actual.Resource);
        //        Assert.IsNotNull(actual.Data);
        //    }

        //    Assert.AreEqual(bulkJsonObjectCount, actualCount);
        //}

        //[TestCaseSource(typeof(FileBasedTestCases), nameof(FileBasedTestCases.InvalidProxyResponseTestCases))]
        //public void When_receiving_invalid_proxy_response(string routeType, EdFiOdsApiCompatibilityVersion version, int validBulkJsonObjectCount, string testCaseFailureType, string syncResponse)
        //{
        //    var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
        //    fakeHttpMessageHandler.AddRouteResponse($"{ChangesRouteType}/available", JObject.FromObject(new AcademicBenchmarksChangesAvailableModel { EventChangesAvailable = true, MaxSequenceId = 1000 }));
        //    fakeHttpMessageHandler.AddRouteResponse($"{routeType}", JToken.Parse(syncResponse));

        //    var clientFactoryMock = new Mock<IHttpClientFactory>();
        //    var httpClient = new HttpClient(fakeHttpMessageHandler);

        //    clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
        //                     .Returns(httpClient);

        //    var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

        //    var blankChangeSequence = new ChangeSequence();

        //    var mockLearningStandardsDataMapper = new Mock<ILearningStandardsDataMapper>();
        //    mockLearningStandardsDataMapper.Setup(m => m.ToEdFiModel(It.IsAny<EdFiOdsApiCompatibilityVersion>(), It.IsAny<ILearningStandardsApiResponseModel>()))
        //        .Returns(new List<EdFiBulkJsonModel> { new EdFiBulkJsonModel() });


        //    var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
        //        _academicBenchmarksSnapshotOptionMock.Object,
        //        logger,
        //        clientFactoryMock.Object,
        //        mockLearningStandardsDataMapper.Object);

        //    // Act

        //    System.Collections.Async.IAsyncEnumerator<EdFiBulkJsonModel> result;
        //    switch (routeType)
        //    {
        //        //case SyncRouteType:
        //        //    result = sut.GetLearningStandards(version, blankChangeSequence, _authTokenManager.Object, default(CancellationToken)).AsyncEntityEnumerable
        //        //                .GetAsyncEnumeratorAsync()
        //        //                .Result;
        //        //    break;
        //        case DescriptorsRouteType:
        //            result = sut.GetLearningStandardsDescriptors(version, blankChangeSequence, _authTokenManager.Object, default(CancellationToken)).AsyncEntityEnumerable
        //                        .GetAsyncEnumeratorAsync()
        //                        .Result;
        //            break;
        //        default:
        //            result = null;
        //            break;
        //    }

        //    // Assert
        //    Assert.NotNull(result);

        //    for (int i = 0; i < validBulkJsonObjectCount; i++)
        //    {
        //        bool b = result.MoveNextAsync()
        //                       .Result;
        //    }

        //    Func<Task> moveNext = async () => await result.MoveNextAsync().ConfigureAwait(false);
        //    if (testCaseFailureType == SectionFailureType)
        //    {
        //        moveNext.Should().Throw<Exception>().WithInnerException<JsonSerializationException>();
        //    }
        //    else
        //    {
        //        moveNext.Should().Throw<Exception>().WithInnerException<ArgumentException>();
        //    }
        //}

        //[TestCase(StatusRouteType, null)]
        //[TestCase(DescriptorsRouteType, EdFiOdsApiCompatibilityVersion.v2)]
        //[TestCase(DescriptorsRouteType, EdFiOdsApiCompatibilityVersion.v3)]
        //[TestCase(SyncRouteType, EdFiOdsApiCompatibilityVersion.v2)]
        //[TestCase(SyncRouteType, EdFiOdsApiCompatibilityVersion.v3)]
        //public void When_proxy_returns_an_http_error(string routeType, EdFiOdsApiCompatibilityVersion version)
        //{
        //    // Arrange
        //    var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();

        //    var clientFactoryMock = new Mock<IHttpClientFactory>();
        //    var httpClient = new HttpClient(fakeHttpMessageHandler);

        //    clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
        //                     .Returns(httpClient);

        //    var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

        //    var blankChangeSequence = new ChangeSequence();


        //    var mockLearningStandardsDataMapper = new Mock<ILearningStandardsDataMapper>();
        //    mockLearningStandardsDataMapper.Setup(m => m.ToEdFiModel(It.IsAny<EdFiOdsApiCompatibilityVersion>(), It.IsAny<ILearningStandardsApiResponseModel>()))
        //        .Returns(new List<EdFiBulkJsonModel> { new EdFiBulkJsonModel() });

        //    var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
        //        _academicBenchmarksSnapshotOptionMock.Object,
        //        logger,
        //        clientFactoryMock.Object,
        //        mockLearningStandardsDataMapper.Object);

        //    // Act => Assert
        //    switch (routeType)
        //    {
        //        //case SyncRouteType:
        //        //    Assert.CatchAsync<HttpRequestException>(() => sut.GetLearningStandards(version, blankChangeSequence, _authTokenManager.Object, default(CancellationToken)).AsyncEntityEnumerable.ForEachAsync(x => { }));
        //        //    break;
        //        case DescriptorsRouteType:
        //            Assert.CatchAsync<HttpRequestException>(() => sut.GetLearningStandardsDescriptors(version, blankChangeSequence, _authTokenManager.Object, default(CancellationToken)).AsyncEntityEnumerable.ForEachAsync(x => { }));
        //            break;
        //        case StatusRouteType:
        //            Assert.CatchAsync<LearningStandardsHttpRequestException>(() => sut.ValidateConnection(_authTokenManager.Object));
        //            break;
        //        default:
        //            Assert.Fail($"{nameof(routeType)}: {routeType} not found.");
        //            break;
        //    }
        //}

        //[TestCaseSource(typeof(FileBasedTestCases), nameof(FileBasedTestCases.ValidProxyChangeResponseTestCases))]
        //public void Can_get_changes_available_response(string routeType, ChangeSequence remoteChangeSequence, string syncResponse)
        //{
        //    // Arrange
        //    var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
        //    fakeHttpMessageHandler.AddRouteResponse($"{routeType}/available", JToken.Parse(syncResponse));

        //    var clientFactoryMock = new Mock<IHttpClientFactory>();
        //    var httpClient = new HttpClient(fakeHttpMessageHandler);

        //    clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
        //        .Returns(httpClient);

        //    var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

        //    var mockLearningStandardsDataMapper = new Mock<ILearningStandardsDataMapper>();
        //    mockLearningStandardsDataMapper.Setup(m => m.ToEdFiModel(It.IsAny<EdFiOdsApiCompatibilityVersion>(), It.IsAny<ILearningStandardsApiResponseModel>()))
        //        .Returns(new List<EdFiBulkJsonModel> { new EdFiBulkJsonModel() });


        //    var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
        //        _academicBenchmarksSnapshotOptionMock.Object,
        //        logger,
        //        clientFactoryMock.Object,
        //        mockLearningStandardsDataMapper.Object);

        //    var currentSequence = new ChangeSequence { Id = 1200 };
        //    var expected = remoteChangeSequence.Id > currentSequence.Id;

        //    // Act
        //    var result = sut.GetChangesAsync(currentSequence, _authTokenManager.Object, default(CancellationToken)).Result;

        //    // Assert
        //    Assert.NotNull(result);
        //    Assert.IsTrue(result.ChangesAvailableInformation.Available == expected);
        //}

        [Test]
        public void Can_handle_invalid_changes_response()
        {
            // Arrange
            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{ChangesRouteType}/available", new JValue(string.Empty));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
                .Returns(httpClient);

            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var mockLearningStandardsDataMapper = new Mock<ILearningStandardsDataMapper>();
            mockLearningStandardsDataMapper.Setup(m => m.ToEdFiModel(It.IsAny<EdFiOdsApiCompatibilityVersion>(), It.IsAny<ILearningStandardsApiResponseModel>()))
                .Returns(new List<EdFiBulkJsonModel> { new EdFiBulkJsonModel() });

            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                _academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object,
                mockLearningStandardsDataMapper.Object);

            var currentSequence = new ChangeSequence { Id = 1200 };

            // Act
            var result = sut.GetChangesAsync(currentSequence, _authTokenManager.Object, default(CancellationToken)).Result;

            // Assert
            Assert.False(result.IsSuccess);
            Assert.IsNotEmpty(result.ErrorMessage);
        }


        // ----------------------------------------------------------------
        // ----------------------------------------------------------------
        // ----------------------------------------------------------------
        // ----------------------------------------------------------------

        [Test]
        public async Task Validate_connection_success()
        {
            // Arrange
            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"standards", ABConnectApiFileBasedTestCases.ValidApiResponse_LearningStandards());

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
                .Returns(httpClient);

            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var mockLearningStandardsDataMapper = new Mock<ILearningStandardsDataMapper>();
            mockLearningStandardsDataMapper.Setup(m => m.ToEdFiModel(It.IsAny<EdFiOdsApiCompatibilityVersion>(), It.IsAny<ILearningStandardsApiResponseModel>()))
                .Returns(new List<EdFiBulkJsonModel> { new EdFiBulkJsonModel() });

            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                _academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object,
                mockLearningStandardsDataMapper.Object);

            var currentSequence = new ChangeSequence { Id = 1200 };

            // Act
            var result = await sut.ValidateConnection(_authTokenManager.Object);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.IsEmpty(result.ErrorMessage);
        }


        [TestCaseSource(typeof(ABConnectApiFileBasedTestCases), nameof(ABConnectApiFileBasedTestCases.ValidApiResponse_DescriptorsTestCases))]
        public void Get_learning_standard_descriptors(
            string routeType,
            EdFiOdsApiCompatibilityVersion version,
            int bulkJsonObjectCount,
            string syncResponse
            )
        {
            var syncOptions = new LearningStandardsSynchronizationOptions();

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{routeType}", JToken.Parse(syncResponse));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var blankChangeSequence = new ChangeSequence();

            //var mockLearningStandardsDataMapper = new Mock<ILearningStandardsDataMapper>();
            //mockLearningStandardsDataMapper
            //    .Setup(m => m.ToEdFiModel(It.IsAny<EdFiOdsApiCompatibilityVersion>(), It.IsAny<ILearningStandardsApiResponseModel>()))
            //    .Returns(new List<EdFiBulkJsonModel> {
            //        new EdFiBulkJsonModel(),
            //        new EdFiBulkJsonModel(),
            //        new EdFiBulkJsonModel()
            //    });

            var mapper = new LearningStandardsDataMapper(
                _academicBenchmarksSnapshotOptionMock.Object,
                new NUnitConsoleLogger<LearningStandardsDataMapper>());


            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                _academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object,
                mapper);

            // Act

            System.Collections.Async.IAsyncEnumerator<EdFiBulkJsonModel> result = sut.GetLearningStandardsDescriptors(
                            version,
                            blankChangeSequence,
                            _authTokenManager.Object,
                            default(CancellationToken))
                .AsyncEntityEnumerable
                                .GetAsyncEnumeratorAsync()
                                .Result;


            // Assert
            Assert.NotNull(result);
            int actualCount = 0;

            while (result.MoveNextAsync().Result)
            {
                actualCount++;
                var actual = result.Current;
                Assert.IsInstanceOf<EdFiBulkJsonModel>(actual);
                Assert.IsNotNull(actual.Operation);
                Assert.IsNotNull(actual.Resource);
                Assert.IsNotNull(actual.Data);

                switch (actual.Resource)
                {
                    case "gradeLevelDescriptors":
                        Assert.AreEqual(actual.Data.Count, 14);
                        break;
                    case "academicSubjectDescriptors":
                        Assert.AreEqual(actual.Data.Count, 20);
                        break;
                    case "publicationStatusDescriptors":
                        Assert.AreEqual(actual.Data.Count, 5);
                        break;
                    default:
                        //resource 
                        Assert.True(false);
                        break;
                }
            }
        }

        [TestCaseSource(typeof(ABConnectApiFileBasedTestCases), nameof(ABConnectApiFileBasedTestCases.ValidApiResponse_SegmentsChangesTestCases))]
        public void Get_modified_segments(
            string routeType,
            EdFiOdsApiCompatibilityVersion version,
            int bulkJsonObjectCount,
            string syncResponse
            )
        {
            var syncOptions = new LearningStandardsSynchronizationOptions();

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{routeType}", JToken.Parse(syncResponse));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var blankChangeSequence = new ChangeSequence();

            var mapper = new LearningStandardsDataMapper(
                _academicBenchmarksSnapshotOptionMock.Object,
                new NUnitConsoleLogger<LearningStandardsDataMapper>());


            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                _academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object,
                mapper);

            // Act
            System.Collections.Async.IAsyncEnumerator<LearningStandardsSegmentModel> result = sut.GetChangedSegments(
                            version,
                            blankChangeSequence,
                            _authTokenManager.Object,
                            default(CancellationToken))
                .AsyncEntityEnumerable
                                .GetAsyncEnumeratorAsync()
                                .Result;

            // Assert
            Assert.NotNull(result);
            int actualCount = 0;

            while (result.MoveNextAsync().Result)
            {
                actualCount++;
                var actual = result.Current;
                Assert.IsInstanceOf<LearningStandardsSegmentModel>(actual);
            }

            Assert.AreEqual(100, actualCount);
        }

        //[TestCaseSource(typeof(FileBasedTestCases), nameof(FileBasedTestCases.ValidApiResponse_LearningStandardsTestCases))]
        //public void Get_segment_learning_standards_test_cases(
        //    string routeType,
        //    EdFiOdsApiCompatibilityVersion version,
        //    int bulkJsonObjectCount,
        //    string syncResponse
        //    )
        //{
        //    var syncOptions = new LearningStandardsSynchronizationOptions();

        //    var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
        //    fakeHttpMessageHandler.AddRouteResponse($"{routeType}", JToken.Parse(syncResponse));

        //    var clientFactoryMock = new Mock<IHttpClientFactory>();
        //    var httpClient = new HttpClient(fakeHttpMessageHandler);

        //    clientFactoryMock.Setup(x => x.CreateClient(nameof(ILearningStandardsDataRetriever)))
        //                     .Returns(httpClient);

        //    var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

        //    var segment = new LearningStandardsSegmentModel
        //    {
        //        DocumentId = Guid.NewGuid(),
        //        SectionId = Guid.NewGuid()
        //    };

        //    var mapper = new LearningStandardsDataMapper(
        //        _academicBenchmarksSnapshotOptionMock.Object,
        //        new NUnitConsoleLogger<LearningStandardsDataMapper>());


        //    var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
        //        _academicBenchmarksSnapshotOptionMock.Object,
        //        logger,
        //        clientFactoryMock.Object,
        //        mapper);

        //    // Act

        //    System.Collections.Async.IAsyncEnumerator<EdFiBulkJsonModel> result = sut.GetSegmentLearningStandards(
        //                    version,
        //                    segment,
        //                    _authTokenManager.Object,
        //                    default(CancellationToken))
        //        .AsyncEntityEnumerable
        //                        .GetAsyncEnumeratorAsync()
        //                        .Result;


        //    // Assert
        //    Assert.NotNull(result);
        //    int actualCount = 0;

        //    while (result.MoveNextAsync().Result)
        //    {
        //        actualCount++;
        //        var actual = result.Current;
        //        Assert.IsInstanceOf<EdFiBulkJsonModel>(actual);
        //    }

        //    Assert.AreEqual(10, actualCount);
        //}



        private class FileBasedTestCases
        {
            public static object[] ValidProxyResponseTestCases()
            {
                return new object[]
                       {
                           new object[]
                           {
                               DescriptorsRouteType,
                               EdFiOdsApiCompatibilityVersion.v2,
                               2,
                               TestCaseHelper.GetTestCaseTextFromFile("Valid-Descriptors-v2.txt")
                           },
                           new object[]
                           {
                               DescriptorsRouteType,
                               EdFiOdsApiCompatibilityVersion.v3,
                               3,
                               TestCaseHelper.GetTestCaseTextFromFile("Valid-Descriptors-v3.txt")
                           },
                           new object[]
                           {
                               SyncRouteType,
                               EdFiOdsApiCompatibilityVersion.v2,
                               1,
                               TestCaseHelper.GetTestCaseTextFromFile("Valid-Sync-v2.txt")
                           },
                           new object[]
                           {
                               SyncRouteType,
                               EdFiOdsApiCompatibilityVersion.v3,
                               1,
                               TestCaseHelper.GetTestCaseTextFromFile("Valid-Sync-v3.txt")
                           }
                       };
            }

            public static object[] ValidProxyChangeResponseTestCases()
            {
                return new object[]
                {
                    new object[]
                    {
                        ChangesRouteType,
                        new ChangeSequence { Id = 1234 },
                        TestCaseHelper.GetTestCaseTextFromFile("Valid-Changes-Available.txt")
                    },
                    new object[]
                    {
                        ChangesRouteType,
                        new ChangeSequence { Id = 1200 },
                        TestCaseHelper.GetTestCaseTextFromFile("Valid-Changes-Not-Available.txt")
                    }
                };
            }

            public static object[] InvalidProxyResponseTestCases()
            {
                return new[]
                       {
                           new TestCaseData(
                                   DescriptorsRouteType,
                                   EdFiOdsApiCompatibilityVersion.v2,
                                   0,
                                   SectionFailureType,
                                   TestCaseHelper.GetTestCaseTextFromFile(
                                       "InvalidResponse-SectionFailure.txt"))
                               .SetName(
                                   $"{DescriptorsRouteType}-{EdFiOdsApiCompatibilityVersion.v2}-{SectionFailureType}-Failure"),
                           new TestCaseData(
                                   DescriptorsRouteType,
                                   EdFiOdsApiCompatibilityVersion.v3,
                                   0,
                                   SectionFailureType,
                                   TestCaseHelper.GetTestCaseTextFromFile(
                                       "InvalidResponse-SectionFailure.txt"))
                               .SetName(
                                   $"{DescriptorsRouteType}-{EdFiOdsApiCompatibilityVersion.v3}-{SectionFailureType}-Failure"),
                           new TestCaseData(
                                   SyncRouteType,
                                   EdFiOdsApiCompatibilityVersion.v2,
                                   0,
                                   SectionFailureType,
                                   TestCaseHelper.GetTestCaseTextFromFile(
                                       "InvalidResponse-SectionFailure.txt"))
                               .SetName(
                                   $"{SyncRouteType}-{EdFiOdsApiCompatibilityVersion.v2}-{SectionFailureType}-Failure"),
                           new TestCaseData(

                                   SyncRouteType,
                                   EdFiOdsApiCompatibilityVersion.v3,
                                   0,
                                   SectionFailureType,
                                   TestCaseHelper.GetTestCaseTextFromFile(
                                       "InvalidResponse-SectionFailure.txt"))
                               .SetName(
                                   $"{SyncRouteType}-{EdFiOdsApiCompatibilityVersion.v3}-{SectionFailureType}-Failure"),

                           new TestCaseData(

                                   DescriptorsRouteType,
                                   EdFiOdsApiCompatibilityVersion.v2,
                                   1,
                                   "Data",
                                   TestCaseHelper.GetTestCaseTextFromFile(
                                       "InvalidResponse-Descriptor-v2-Failure.txt"))
                               .SetName(
                                   $"{DescriptorsRouteType}-{EdFiOdsApiCompatibilityVersion.v2}-Data-Failure"),
                           new TestCaseData(
                               DescriptorsRouteType,
                               EdFiOdsApiCompatibilityVersion.v3,
                               2,
                               "Data",
                               TestCaseHelper.GetTestCaseTextFromFile(
                                   "InvalidResponse-Descriptor-v3-Failure.txt")).SetName(
                               $"{DescriptorsRouteType}-{EdFiOdsApiCompatibilityVersion.v3}-Data-Failure"),
                           new TestCaseData(
                                   SyncRouteType,
                                   EdFiOdsApiCompatibilityVersion.v2,
                                   1,
                                   SectionFailureType,
                                   TestCaseHelper.GetTestCaseTextFromFile(
                                       "InvalidResponse-Sync-v2-Failure.txt"))
                               .SetName(
                                   $"{SyncRouteType}-{EdFiOdsApiCompatibilityVersion.v2}-{SectionFailureType}-Late-Failure"),
                           new TestCaseData(
                                   SyncRouteType,
                                   EdFiOdsApiCompatibilityVersion.v3,
                                   1,
                                   SectionFailureType,
                                   TestCaseHelper.GetTestCaseTextFromFile(
                                       "InvalidResponse-Sync-v3-Failure.txt"))
                               .SetName(
                                   $"{SyncRouteType}-{EdFiOdsApiCompatibilityVersion.v3}-{SectionFailureType}-Late-Failure"),
                       };
            }


        }
    }
}
