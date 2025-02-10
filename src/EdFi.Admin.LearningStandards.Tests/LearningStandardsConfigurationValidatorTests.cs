// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Installers;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests
{
    [TestFixture]
    public class LearningStandardsConfigurationValidatorTests
    {
        private const string _defaultOdsUrl = "https://api.testing-ed-fi.org/v3/api";

        private const string _expectedAuthCode = "5a531101727f4ac7b1494bb9dea544a9";

        private const string _expectedAccessToken = "940934d3a405492c99572db9329fc081";

        private const string _oAuthKey = "1a2b3c4d5e6f7g8h9i0j";

        private const string _oAuthSecret = "j0i9h8g7f6e5d4c3b2a1";

        private ILoggerProvider _loggerFactory;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var lf = new Mock<ILoggerProvider>();
            lf.Setup(m => m.CreateLogger(It.IsAny<string>()))
                .Returns(new NUnitConsoleLogger<LearningStandardsConfigurationValidator>());
            _loggerFactory = lf.Object;
        }

        [Test]
        public async Task Can_validate_success()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);

            string apiInfoPath = new Uri(odsApiConfig.Url).Segments[^1].TrimEnd('/');


            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("standards", ABConnectApiFileBasedTestCases.ValidApiResponse_LearningStandards())
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken))
                .AddRouteResponse($"{apiInfoPath}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));

            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateConfigurationAsync(authConfig, odsApiConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(true, actual.IsSuccess);
            Assert.IsNotEmpty(actual.Content);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_validate_single_failure()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("validate/authentication", HttpStatusCode.InternalServerError)
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken));
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
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
        public async Task Can_validate_double_failure()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("validate/authentication", HttpStatusCode.InternalServerError)
                .AddRouteResponse("token", HttpStatusCode.Unauthorized);
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
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
        public async Task Can_validate_ods_v3_success()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);

            string apiInfoPath = new Uri(odsApiConfig.Url).Segments[^1].TrimEnd('/');

            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("validate/authentication", GetDefaultProxyResponse())
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken))
                .AddRouteResponse($"{apiInfoPath}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateEdFiOdsApiConfigurationAsync(odsApiConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(true, actual.IsSuccess);
            Assert.IsNotEmpty(actual.Content);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_validate_ods_v3_failure()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("token", HttpStatusCode.InternalServerError);
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateEdFiOdsApiConfigurationAsync(odsApiConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.IsNotEmpty(actual.ErrorMessage);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_validate_ods_v2_success()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v2, authConfig);

            string apiInfoPath = new Uri(odsApiConfig.Url).Segments[^1].TrimEnd('/');

            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("authorize", GetDefaultAuthorizationResponse())
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken))
                .AddRouteResponse($"{apiInfoPath}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));



            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateEdFiOdsApiConfigurationAsync(odsApiConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(true, actual.IsSuccess);
            Assert.IsNotEmpty(actual.Content);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_validate_ods_v2_failure()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                _defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v2, authConfig);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("authorize", HttpStatusCode.InternalServerError);
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateEdFiOdsApiConfigurationAsync(odsApiConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.IsNotEmpty(actual.ErrorMessage);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_validate_ods_v2_url_failure()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                "DEFAULTURL", EdFiOdsApiCompatibilityVersion.v2, authConfig);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("authorize", HttpStatusCode.InternalServerError);
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateEdFiOdsApiConfigurationAsync(odsApiConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.IsNotEmpty(actual.ErrorMessage);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_validate_configuration_with_transient_error()
        {
            //Arrange
            var consoleStringListWriter = new ConsoleStringListWriter();
            Console.SetOut(consoleStringListWriter);

            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(
                "http://localhost", EdFiOdsApiCompatibilityVersion.v2, authConfig);
            //Socket Exception 10061: Connection Refused
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("*", new SocketException(10061));
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateEdFiOdsApiConfigurationAsync(odsApiConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.IsNotEmpty(actual.ErrorMessage);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_validate_learning_standards_success()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("standards", ABConnectApiFileBasedTestCases.ValidApiResponse_LearningStandards());
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateLearningStandardProviderConfigurationAsync(authConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(true, actual.IsSuccess);
            Assert.IsNotEmpty(actual.Content);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_validate_learning_standards_failure()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("validate/authentication", HttpStatusCode.InternalServerError);
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration);
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateLearningStandardProviderConfigurationAsync(authConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.IsNotEmpty(actual.ErrorMessage);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public async Task Can_validate_learning_standards_url_failure()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("validate/authentication", HttpStatusCode.InternalServerError);
            var clientConfiguration = new EdFiOdsApiClientConfiguration(0);
            var pluginConnector = GetConfiguredTestConnector(httpHandler, clientConfiguration, options => options.Url = "PROXYURL");
            var validator = pluginConnector.LearningStandardsConfigurationValidator;

            //Act
            var actual = await validator.ValidateLearningStandardProviderConfigurationAsync(authConfig).ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.IsNotEmpty(actual.ErrorMessage);

            Console.WriteLine(actual.ToString());
        }

        [Test]
        public void Validator_is_registered_using_service_collection_extensions()
        {
            //Arrange
            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLearningStandardsServices(new EdFiOdsApiClientConfiguration());
            var serviceProvider = serviceCollection.BuildServiceProvider();

            //Act
            var actual = serviceProvider.GetService<ILearningStandardsConfigurationValidator>();

            //Assert
            Assert.IsNotNull(actual);
        }

        [Test]
        public void Validator_is_registered_using_core_plugin_connector()
        {
            //Arrange
            IServiceCollection serviceCollection = new ServiceCollection();

            //Act
            var actual = new LearningStandardsCorePluginConnector(
                serviceCollection,
                (services) => services.BuildServiceProvider(),
                _loggerFactory,
                new EdFiOdsApiClientConfiguration());

            //Assert
            Assert.IsNotNull(actual.LearningStandardsConfigurationValidator);
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
                    services.ConfigureLearningStandardsProvider<AcademicBenchmarksOptions>(configureAbOptions ?? (options =>
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

        private JObject GetDefaultAuthorizationResponse()
        {
            return new JObject { ["code"] = _expectedAuthCode };
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

        private JObject GetDefaultProxyResponse()
        {
            return new JObject
            {
                ["status"] = "OK"
            };
        }
    }
}
