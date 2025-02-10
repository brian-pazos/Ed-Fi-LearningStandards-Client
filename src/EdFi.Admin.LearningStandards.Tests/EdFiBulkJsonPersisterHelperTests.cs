// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests
{
    [TestFixture]
    public class EdFiBulkJsonPersisterHelperTests
    {
        private const string _v2BaseUrl = "https://api.ed-fi.org/v2.5.0/api";

        private const string _v3BaseUrl = "https://api.ed-fi.org/v3.0/api";

        private const string _resource = "classPeriods";

        //[Test]
        //public void Can_resolve_v2_address()
        //{
        //    //Arrange
        //    string schema = string.Empty;
        //    var version = EdFiOdsApiCompatibilityVersion.v2;
        //    int? schoolYear = 2018;

        //    string expected = $"{_v2BaseUrl}/api/v2.0/{schoolYear}/{_resource}";

        //    //Act
        //    var actual = EdFiBulkJsonPersisterHelper.ResolveOdsApiResourceUrl(_v2BaseUrl, schema, _resource, version, schoolYear);

        //    //Assert
        //    Assert.AreEqual(expected, actual.ToString());
        //}

        //[Test]
        //public void Version_2_throws_on_null_school_year()
        //{
        //    //Arrange
        //    var version = EdFiOdsApiCompatibilityVersion.v2;

        //    //Act -> Assert
        //    Assert.Throws<ArgumentNullException>(() =>
        //    {
        //        var actual = EdFiBulkJsonPersisterHelper.ResolveOdsApiResourceUrl(_v2BaseUrl, string.Empty, _resource, version, null);
        //    });
        //}

        //[Test]
        //public void Version_2_throws_on_improper_base_url()
        //{
        //    //Arrange
        //    var version = EdFiOdsApiCompatibilityVersion.v2;

        //    //Act -> Assert
        //    //The original .net standard exception is a UriFormatException,
        //    //however, in portable libraries, the base FormatException is thrown instead.
        //    Assert.Catch<FormatException>(() =>
        //    {
        //        var actual = EdFiBulkJsonPersisterHelper.ResolveOdsApiResourceUrl("/api.ed-fi.org", string.Empty, _resource, version, 2018);
        //    });
        //}

        //[Test]
        //public void Version_2_throws_on_empty_resource()
        //{
        //    //Arrange
        //    string resource = string.Empty;
        //    var version = EdFiOdsApiCompatibilityVersion.v2;

        //    //Act -> Assert
        //    Assert.Throws<ArgumentException>(() =>
        //    {
        //        var actual = EdFiBulkJsonPersisterHelper.ResolveOdsApiResourceUrl(_v2BaseUrl, string.Empty, resource, version, 2018);
        //    });
        //}

        [Test]
        public async Task Can_resolve_v3_address()
        {
            //Arrange
            var odsApiConfig = new EdFiOdsApiConfiguration(
                _v3BaseUrl,
                EdFiOdsApiCompatibilityVersion.Unknown,
                new AuthenticationConfiguration("key", "secret")
                );


            string schema = "ed-fi";
            string expected = $"{_v3BaseUrl}/data/v3/ed-fi/{_resource}";


            // Get the last path segment
            string lastSegment = new Uri(_v3BaseUrl).Segments[^1].TrimEnd('/');

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{lastSegment}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(IEdFiVersionManager)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<EdFiVersionManager>();

            var versionManager = new EdFiVersionManager(clientFactoryMock.Object, logger);

            // Act
            var uri = await versionManager.ResolveResourceUrl(odsApiConfig, schema, _resource);
            var actual = uri.OriginalString;

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public async Task Version_3_ODSv6x_includes_year_when_provided()
        {
            //Arrange
            var odsApiConfig = new EdFiOdsApiConfiguration(
                _v3BaseUrl,
                EdFiOdsApiCompatibilityVersion.Unknown,
                new AuthenticationConfiguration("key", "secret"),
                2025
                );


            string schema = "ed-fi";
            string expected = $"{_v3BaseUrl}/data/v3/{odsApiConfig.SchoolYear}/ed-fi/{_resource}";

            // Get the last path segment
            string lastSegment = new Uri(_v3BaseUrl).Segments[^1].TrimEnd('/');

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{lastSegment}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv6x-Info-Response.json")));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(IEdFiVersionManager)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<EdFiVersionManager>();

            var versionManager = new EdFiVersionManager(clientFactoryMock.Object, logger);

            // Act
            var uri = await versionManager.ResolveResourceUrl(odsApiConfig, schema, _resource);
            var actual = uri.OriginalString;

            //Assert
            Assert.AreEqual(expected, actual.ToString());
        }

        [Test]
        public async Task Version_3_ODSv7x_includes_routingContext_when_provided()
        {
            //Arrange
            var odsApiConfig = new EdFiOdsApiConfiguration(
                _v3BaseUrl,
                EdFiOdsApiCompatibilityVersion.Unknown,
                new AuthenticationConfiguration("key", "secret"),
                routingContextKey: "2026"
                );


            string schema = "ed-fi";
            string expected = $"{_v3BaseUrl}/{odsApiConfig.RoutingContextKey}/data/v3/ed-fi/{_resource}";

            // Get the last path segment
            string lastSegment = new Uri(_v3BaseUrl).Segments[^1].TrimEnd('/');

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{lastSegment}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(IEdFiVersionManager)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<EdFiVersionManager>();

            var versionManager = new EdFiVersionManager(clientFactoryMock.Object, logger);

            // Act
            var uri = await versionManager.ResolveResourceUrl(odsApiConfig, schema, _resource);
            var actual = uri.OriginalString;

            //Assert
            Assert.AreEqual(expected, actual.ToString());
        }


        [Test]
        public async Task Version_3_throws_on_empty_schema()
        {
            //Arrange
            var odsApiConfig = new EdFiOdsApiConfiguration(
                _v3BaseUrl,
                EdFiOdsApiCompatibilityVersion.Unknown,
                new AuthenticationConfiguration("key", "secret"),
                routingContextKey: "key"
                );


            string schema = string.Empty;
            string expected = $"{_v3BaseUrl}/{odsApiConfig.RoutingContextKey}/data/v3/ed-fi/{_resource}";

            // Get the last path segment
            string lastSegment = new Uri(_v3BaseUrl).Segments[^1].TrimEnd('/');

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{lastSegment}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(IEdFiVersionManager)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<EdFiVersionManager>();

            var versionManager = new EdFiVersionManager(clientFactoryMock.Object, logger);

            //Act
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await versionManager.ResolveResourceUrl(odsApiConfig, schema, _resource);
            });
        }

        [Test]
        public void Version_3_throws_on_improper_base_url()
        {
            //Arrange
            var odsApiConfig = new EdFiOdsApiConfiguration(
                "/api.ed-fi.org",
                EdFiOdsApiCompatibilityVersion.Unknown,
                new AuthenticationConfiguration("key", "secret"),
                2018
                );


            string schema = string.Empty;
            string expected = $"{_v3BaseUrl}/data/v3/{odsApiConfig.SchoolYear}/ed-fi/{_resource}";

            // Get the last path segment
            string lastSegment = new Uri(_v3BaseUrl).Segments[^1].TrimEnd('/');

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{lastSegment}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(IEdFiVersionManager)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<EdFiVersionManager>();

            var versionManager = new EdFiVersionManager(clientFactoryMock.Object, logger);

            //Act -> Assert
            //The original .net standard exception is a UriFormatException,
            //however, in portable libraries, the base FormatException is thrown instead.
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var actual = await versionManager.ResolveResourceUrl(odsApiConfig, schema, _resource);
            });
        }

        [Test]
        public void Version_3_throws_on_empty_resource()
        {
            //Arrange
            var odsApiConfig = new EdFiOdsApiConfiguration(
                _v3BaseUrl,
                EdFiOdsApiCompatibilityVersion.Unknown,
                new AuthenticationConfiguration("key", "secret"),
                2018
                );


            string schema = string.Empty;
            string expected = $"{_v3BaseUrl}/data/v3/{odsApiConfig.SchoolYear}/ed-fi/{_resource}";

            // Get the last path segment
            string lastSegment = new Uri(_v3BaseUrl).Segments[^1].TrimEnd('/');

            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse($"{lastSegment}", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")));

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient(fakeHttpMessageHandler);

            clientFactoryMock.Setup(x => x.CreateClient(nameof(IEdFiVersionManager)))
                             .Returns(httpClient);

            var logger = new NUnitConsoleLogger<EdFiVersionManager>();

            var versionManager = new EdFiVersionManager(clientFactoryMock.Object, logger);

            //Act -> Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var actual = await versionManager.ResolveResourceUrl(odsApiConfig, schema, string.Empty);
            });
        }
    }
}
