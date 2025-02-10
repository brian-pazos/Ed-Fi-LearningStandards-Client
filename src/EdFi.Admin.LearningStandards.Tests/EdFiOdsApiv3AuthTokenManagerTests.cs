// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests
{
    [TestFixture]
    public class EdFiOdsApiv3AuthTokenManagerTests
    {
        private const string _defaultOdsUrl = "https://api.testing-ed-fi.org/v3/api";

        private const string _expectedAccessToken = "940934d3a405492c99572db9329fc081";

        private const string _oAuthKey = "1a2b3c4d5e6f7g8h9i0j";

        private const string _oAuthSecret = "j0i9h8g7f6e5d4c3b2a1";

        private ILoggerFactory _loggerFactory;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var lf = new Mock<ILoggerFactory>();
            lf.Setup(m => m.CreateLogger(It.IsAny<string>()))
                .Returns(new NUnitConsoleLogger<EdFiOdsApiv3AuthTokenManagerTests>());
            _loggerFactory = lf.Object;
        }

        [Test]
        public async Task Token_manager_can_retrieve_access_token()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(_defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            var logger = _loggerFactory.CreateLogger<EdFiOdsApiv3AuthTokenManager>();
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken));
            var httpClient = GetConfiguredClient(httpHandler);

            // Get the last path segment
            string lastSegment = new Uri(_defaultOdsUrl).Segments[^1].TrimEnd('/');

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{lastSegment}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            clientFactoryMock.Setup(x => x.CreateClient(nameof(IEdFiVersionManager)))
                             .Returns(new HttpClient(fakeHttpMessageHandler));

            var versionManager = new EdFiVersionManager(clientFactoryMock.Object, new NUnitConsoleLogger<EdFiVersionManager>());

            var manager = new EdFiOdsApiv3AuthTokenManager(odsApiConfig, versionManager, httpClient, logger);

            //Act
            string actual = await manager.GetTokenAsync().ConfigureAwait(false);

            //Assert
            Assert.NotNull(actual);
            Assert.AreEqual(_expectedAccessToken, actual);

            //2 API calls are made for the initial token: 1st being the authorize call, 2nd being the token call.
            Assert.AreEqual(1, httpHandler.CallCount);
        }

        [Test]
        public async Task Token_manager_can_retrieve_access_token_after_expiration()
        {
            //Arrange
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(_defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            var logger = _loggerFactory.CreateLogger<EdFiOdsApiv3AuthTokenManager>();
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(expiresIn: 2))
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(expiresIn: 2));
            var httpClient = GetConfiguredClient(httpHandler);

            // Get the last path segment
            string lastSegment = new Uri(_defaultOdsUrl).Segments[^1].TrimEnd('/');

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{lastSegment}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            clientFactoryMock.Setup(x => x.CreateClient(nameof(IEdFiVersionManager)))
                             .Returns(new HttpClient(fakeHttpMessageHandler));

            var versionManager = new EdFiVersionManager(clientFactoryMock.Object, new NUnitConsoleLogger<EdFiVersionManager>());


            var manager = new EdFiOdsApiv3AuthTokenManager(odsApiConfig, versionManager, httpClient, logger);

            //Act
            string firstActual = await manager.GetTokenAsync().ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            string secondActual = await manager.GetTokenAsync().ConfigureAwait(false);

            //Assert
            Assert.NotNull(firstActual);
            Assert.NotNull(secondActual);
            Assert.AreNotEqual(firstActual, secondActual);
            Assert.AreEqual(2, httpHandler.CallCount);
        }

        [Test]
        public async Task Token_manager_will_retry_after_failed_attempt()
        {
            //Arrange
            var apiDelay = TimeSpan.FromSeconds(1);

            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(_oAuthKey, _oAuthSecret);
            IEdFiOdsApiConfiguration odsApiConfig = new EdFiOdsApiConfiguration(_defaultOdsUrl, EdFiOdsApiCompatibilityVersion.v3, authConfig);
            var logger = _loggerFactory.CreateLogger<EdFiOdsApiv3AuthTokenManager>();
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("token", HttpStatusCode.InternalServerError, new JObject(), apiDelay)
                .AddRouteResponse("token", GetDefaultAccessCodeResponse());
            var httpClient = GetConfiguredClient(httpHandler);

            // Get the last path segment
            string lastSegment = new Uri(_defaultOdsUrl).Segments[^1].TrimEnd('/');

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{lastSegment}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            clientFactoryMock.Setup(x => x.CreateClient(nameof(IEdFiVersionManager)))
                             .Returns(new HttpClient(fakeHttpMessageHandler));

            var versionManager = new EdFiVersionManager(clientFactoryMock.Object, new NUnitConsoleLogger<EdFiVersionManager>());

            var manager = new EdFiOdsApiv3AuthTokenManager(odsApiConfig, versionManager, httpClient, logger);

            //Act
            string firstActual = await manager.GetTokenAsync().ConfigureAwait(false);

            //Assert
            Assert.NotNull(firstActual);
            Assert.AreEqual(2, httpHandler.CallCount);
        }

        private HttpClient GetConfiguredClient(HttpMessageHandler handler, Func<PolicyBuilder<HttpResponseMessage>, IAsyncPolicy<HttpResponseMessage>> configurePolicy = null)
        {
            var serviceProvider = new ServiceCollection()
                .AddHttpClient(nameof(EdFiOdsApiv3AuthTokenManagerTests))
                .ConfigurePrimaryHttpMessageHandler(() => handler)
                .AddTransientHttpErrorPolicy(p =>
                    configurePolicy?.Invoke(p) ?? p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(600)))
                .Services.BuildServiceProvider();

            var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            return factory.CreateClient(nameof(EdFiOdsApiv3AuthTokenManagerTests));
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
