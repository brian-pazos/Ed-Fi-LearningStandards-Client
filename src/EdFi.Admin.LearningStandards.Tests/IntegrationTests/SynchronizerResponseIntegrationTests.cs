// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Installers;
using EdFi.Admin.LearningStandards.Core.Models.ABConnectApiModels;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests.IntegrationTests
{
    [TestFixture]
    [Category("Interactive")]
    [Ignore("These tests are for interactive local use only. Comment out this line to use.")]
    public class SynchronizerResponseIntegrationTests
    {
        private const string _defaultOdsUrl = "https://api.testing-ed-fi.org/v3/api";

        private const string _oAuthKey = "1a2b3c4d5e6f7g8h9i0j";

        private const string _oAuthSecret = "j0i9h8g7f6e5d4c3b2a1";

        private const string DescriptorsRouteType = "Descriptors";

        private const string SyncRouteType = "Sync";

        private const string ChangesRouteType = "Changes";

        private Mock<IOptionsSnapshot<AcademicBenchmarksOptions>> _academicBenchmarksSnapshotOptionMock;

        private Mock<IAuthApiManager> _authTokenManager;

        private readonly NUnitConsoleLogger<LearningStandardsSynchronizer> _debugLogger = new NUnitConsoleLogger<LearningStandardsSynchronizer>(LogLevel.Information);

        private ILoggerProvider _loggerFactory;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var lf = new Mock<ILoggerProvider>();
            lf.Setup(m => m.CreateLogger(It.IsAny<string>()))
                .Returns(_debugLogger);
            _loggerFactory = lf.Object;

            _authTokenManager = new Mock<IAuthApiManager>();
            _authTokenManager.Setup(x => x.GetAuthenticatedRequestAsync(It.IsAny<HttpMethod>(), It.IsAny<Uri>(), It.IsAny<HttpContent>()))
                .Returns(Task.FromResult(new HttpRequestMessage()));

            _academicBenchmarksSnapshotOptionMock = new Mock<IOptionsSnapshot<AcademicBenchmarksOptions>>();
            _academicBenchmarksSnapshotOptionMock.Setup(x => x.Value)
                .Returns(
                    new AcademicBenchmarksOptions
                    { Url = "https://localhost:7777" });
        }

        [Test]
        public async Task Can_validate_double_failure()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("status", HttpStatusCode.InternalServerError)
                .AddRouteResponse("token", HttpStatusCode.Unauthorized);
            var clientConfiguration = new EdFiOdsApiClientConfiguration();
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateConfigurationAsync(authConfig, odsApiConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.IsNotEmpty(actual.ErrorMessage);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_use_availability_checks_with_default()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            var httpHandler = new MockJsonHttpMessageHandler()
                              .AddRouteResponse(
                                  $"{ChangesRouteType}/available",
                                  JObject.FromObject(
                                      new AcademicBenchmarksChangesAvailableModel
                                      { EventChangesAvailable = true, MaxSequenceId = 1000 }))
                              .AddRouteResponse("token", GetDefaultAccessCodeResponse(expiresIn: 3600));

            var clientConfiguration = new EdFiOdsApiClientConfiguration();
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var available = pluginConnector.LearningStandardsChangesAvailable;

            //Act
            var actual = await available.ChangesAvailableAsync(odsApiConfig, authConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.True(actual.IsSuccess);
            Assert.True(actual.ChangesAvailableInformation.Available);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_log_proper_unauthorized_message()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("status", HttpStatusCode.Unauthorized)
                .AddRouteResponse("token", HttpStatusCode.Unauthorized)
                .AddRouteResponse("sync", HttpStatusCode.Unauthorized);
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0, 1);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateConfigurationAsync(authConfig, odsApiConfig).ConfigureAwait(false);

            //Assert
            Assert.IsNotEmpty(_debugLogger.LogLines.Where(sd => sd.Contains("credentials were not valid")));
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.IsNotEmpty(actual.ErrorMessage);
        }

        [Test]
        public async Task Will_display_proper_progress_percentage()
        {
            // Arrange

            int testRescordCount = 157;

            var descriptorsResponse = JArray.Parse(TestCaseHelper.GetTestCaseTextFromFile("Valid-Descriptors-v3.txt"));
            var descriptorObj = (JObject)descriptorsResponse.First;
            var newDescriptorsResponse = new JArray();
            for (int i = 0; i < testRescordCount; i++)
            {
                newDescriptorsResponse.Add(descriptorObj.DeepClone());
            }

            var syncResponse = JArray.Parse(TestCaseHelper.GetTestCaseTextFromFile("Valid-Descriptors-v3.txt"));
            var syncObj = (JObject)syncResponse.First;
            var newSyncResponse = new JArray();
            for (int i = 0; i < testRescordCount; i++)
            {
                newSyncResponse.Add(syncObj.DeepClone());
            }

            //Http
            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();

            fakeHttpMessageHandler.AddRouteResponse($"{ChangesRouteType}/available", JObject.FromObject(new AcademicBenchmarksChangesAvailableModel { EventChangesAvailable = true, MaxSequenceId = 1000 }));
            fakeHttpMessageHandler.AddRouteResponse($"{DescriptorsRouteType}", newDescriptorsResponse, "X-Record-Count", (descriptorsResponse.Count * 8).ToString());
            fakeHttpMessageHandler.AddRouteResponse($"{SyncRouteType}", newSyncResponse, "X-Record-Count",
                (newSyncResponse.Count * 8).ToString());

            fakeHttpMessageHandler.AddRouteResponse("learningStandards", HttpStatusCode.OK);
            fakeHttpMessageHandler.AddRouteResponse("token", GetDefaultAccessCodeResponse(expiresIn: 3600));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);
            clientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            //Logging
            var odsLoggerFactory = new Mock<ILoggerFactory>();
            odsLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_debugLogger);
            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var learningStandardsLogger = new NUnitConsoleLogger<LearningStandardsSynchronizer>(LogLevel.Warning);
            var bulkJsonLogger = new NUnitConsoleLogger<EdFiBulkJsonPersister>(LogLevel.Warning);

            //Config
            IAuthenticationConfiguration abAuthConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            var learningStandardsAuthFactory = new Mock<ILearningStandardsProviderAuthApiManagerFactory>();
            learningStandardsAuthFactory.Setup(x => x.CreateLearningStandardsProviderAuthApiManager(It.IsAny<IAuthenticationConfiguration>()))
                .Returns(_authTokenManager.Object);
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(_defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            IEdFiOdsApiClientConfiguration odsApiClientConfiguration = new EdFiOdsApiClientConfiguration();
            var edfiTokenManager = clientFactoryMock.Object;
            IEdFiOdsApiAuthTokenManagerFactory edfiOdsTokenManagerFactory = new EdFiOdsApiAuthTokenManagerFactory(edfiTokenManager, odsLoggerFactory.Object);
            IEdFiBulkJsonPersisterFactory edFiBulkJsonPersister = new EdFiBulkJsonPersisterFactory(clientFactoryMock.Object, bulkJsonLogger);

            var defaultChangeSequencePersister = new DefaultChangeSequencePersister(new NUnitConsoleLogger<DefaultChangeSequencePersister>());

            var dataMapperMock = new Mock<ILearningStandardsDataMapper>();
            dataMapperMock.Setup(m => m.ToEdFiModel(It.IsAny<EdFiOdsApiCompatibilityVersion>(), It.IsAny<ILearningStandardsApiResponseModel>()))
                .Returns(new List<EdFiBulkJsonModel> { new EdFiBulkJsonModel() });

            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                _academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object,
                dataMapperMock.Object);

            //Synchronizer
            var synchronizer = new LearningStandardsSynchronizer(
                odsApiClientConfiguration,
                edfiOdsTokenManagerFactory,
                edFiBulkJsonPersister,
                sut,
                learningStandardsAuthFactory.Object,
                defaultChangeSequencePersister,
                new OptionsWrapper<LearningStandardsSynchronizationOptions>(
                    new LearningStandardsSynchronizationOptions()),
                learningStandardsLogger);

            // Act
            var prog = new TestProgress(_loggerFactory);
            var res = await synchronizer.SynchronizeAsync(odsApiConfig, abAuthConfig, null, default(CancellationToken), prog);

            Assert.IsNotNull(res);
            Assert.AreEqual(0, prog.PercentageUpdates.First());
            Assert.AreEqual(100, prog.PercentageUpdates.Last());
            Assert.IsTrue(prog.PercentageUpdates.All(al => al <= 100));
        }

        [Test]
        public async Task Will_error_if_missing_records()
        {
            // Arrange

            int testRecordCount = 157;
            int expectMoreRecordsSyncHeaderMultiplier = 500;

            var descriptorsResponse = JArray.Parse(TestCaseHelper.GetTestCaseTextFromFile("Valid-Descriptors-v3.txt"));
            var descriptorObj = (JObject)descriptorsResponse.First;
            var newDescriptorsResponse = new JArray();
            for (int i = 0; i < testRecordCount; i++)
            {
                newDescriptorsResponse.Add(descriptorObj.DeepClone());
            }

            var syncResponse = JArray.Parse(TestCaseHelper.GetTestCaseTextFromFile("Valid-Descriptors-v3.txt"));
            var syncObj = (JObject)syncResponse.First;
            var newSyncResponse = new JArray();
            for (int i = 0; i < testRecordCount; i++)
            {
                newSyncResponse.Add(syncObj.DeepClone());
            }

            //Http
            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();

            fakeHttpMessageHandler.AddRouteResponse(
                $"{ChangesRouteType}/available",
                JObject.FromObject(
                    new AcademicBenchmarksChangesAvailableModel
                    { EventChangesAvailable = true, MaxSequenceId = 1000 }));

            fakeHttpMessageHandler.AddRouteResponse(
                $"{DescriptorsRouteType}",
                newDescriptorsResponse,
                "X-Record-Count",
                (descriptorsResponse.Count * 8).ToString());
            fakeHttpMessageHandler.AddRouteResponse(
                $"{SyncRouteType}",
                newSyncResponse,
                "X-Record-Count",
                (syncResponse.Count * expectMoreRecordsSyncHeaderMultiplier).ToString());

            fakeHttpMessageHandler.AddRouteResponse("learningStandards", HttpStatusCode.OK);
            fakeHttpMessageHandler.AddRouteResponse("token", GetDefaultAccessCodeResponse(expiresIn: 3600));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);
            clientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            //Logging
            var odsLoggerFactory = new Mock<ILoggerFactory>();
            odsLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_debugLogger);
            var logger = new NUnitConsoleLogger<AcademicBenchmarksLearningStandardsDataRetriever>();

            var learningStandardsLogger = new NUnitConsoleLogger<LearningStandardsSynchronizer>(LogLevel.Warning);
            var bulkJsonLogger = new NUnitConsoleLogger<EdFiBulkJsonPersister>(LogLevel.Warning);

            //Config
            IAuthenticationConfiguration abAuthConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            var learningStandardsAuthFactory = new Mock<ILearningStandardsProviderAuthApiManagerFactory>();
            learningStandardsAuthFactory.Setup(x => x.CreateLearningStandardsProviderAuthApiManager(It.IsAny<IAuthenticationConfiguration>()))
                .Returns(_authTokenManager.Object);
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(_defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            IEdFiOdsApiClientConfiguration odsApiClientConfiguration = new EdFiOdsApiClientConfiguration();
            var edfiTokenManager = clientFactoryMock.Object;
            IEdFiOdsApiAuthTokenManagerFactory edfiOdsTokenManagerFactory = new EdFiOdsApiAuthTokenManagerFactory(edfiTokenManager, odsLoggerFactory.Object);
            IEdFiBulkJsonPersisterFactory edFiBulkJsonPersister = new EdFiBulkJsonPersisterFactory(clientFactoryMock.Object, bulkJsonLogger);

            var defaultChangeSequencePersister = new DefaultChangeSequencePersister(new NUnitConsoleLogger<DefaultChangeSequencePersister>());

            var dataMapperMock = new Mock<ILearningStandardsDataMapper>();
            dataMapperMock.Setup(m => m.ToEdFiModel(It.IsAny<EdFiOdsApiCompatibilityVersion>(), It.IsAny<ILearningStandardsApiResponseModel>()))
                .Returns(new List<EdFiBulkJsonModel> { new EdFiBulkJsonModel() });

            var sut = new AcademicBenchmarksLearningStandardsDataRetriever(
                _academicBenchmarksSnapshotOptionMock.Object,
                logger,
                clientFactoryMock.Object,
                dataMapperMock.Object);

            //Synchronizer
            var synchronizer = new LearningStandardsSynchronizer(
                odsApiClientConfiguration,
                edfiOdsTokenManagerFactory,
                edFiBulkJsonPersister,
                sut,
                learningStandardsAuthFactory.Object,
                defaultChangeSequencePersister,
                new OptionsWrapper<LearningStandardsSynchronizationOptions>(
                    new LearningStandardsSynchronizationOptions()),
                learningStandardsLogger);

            // Act
            var res = await synchronizer.SynchronizeAsync(odsApiConfig, abAuthConfig, null, default(CancellationToken));

            Assert.IsNotNull(res);
            Assert.IsFalse(res.IsSuccess);
            Assert.True(res.ErrorMessage.StartsWith("Not all expected records ("));
        }


        private LearningStandardsCorePluginConnector GetConfiguredTestConnector(
            HttpMessageHandler httpMessageHandler,
            EdFiOdsApiClientConfiguration edFiOdsApiClientConfiguration,
            Action<AcademicBenchmarksOptions> configureAbOptions = null)
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            return new LearningStandardsCorePluginConnector(
                serviceCollection,
                services =>
                {
                    //Configure AB options
                    services.ConfigureLearningStandardsProvider(configureAbOptions ?? (options =>
                    {
                        options.Url = "https://localhost:5000";
                        options.Retries = 0;
                    }));

                    //Configure all registered HttpClient Actions to use the mock message handler. This call
                    //should stack over the existing configurations.
                    services.ConfigureAll<HttpClientFactoryOptions>(options =>
                    {
                        options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = httpMessageHandler);
                    });

                    return services.BuildServiceProvider();
                },
                _loggerFactory,
                edFiOdsApiClientConfiguration
            );
        }

        private JObject GetDefaultAccessCodeResponse(string accessCode = null, int expiresIn = 30)
        {
            return new JObject
            {
                ["access_token"] = accessCode ?? Guid.NewGuid().ToString("N"),
                ["expires_in"] = expiresIn,
                ["token_type"] = "bearer"
            };
        }
    }
}
