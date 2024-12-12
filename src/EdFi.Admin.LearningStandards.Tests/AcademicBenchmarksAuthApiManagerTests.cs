// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace EdFi.Admin.LearningStandards.Tests
{
    [TestFixture]
    public class AcademicBenchmarksAuthApiManagerTests
    {
        private ILogger<AcademicBenchmarksAuthApiManager> _logger;

        private IServiceProvider _serviceProvider;

        private Uri _uri = new Uri("https://api.abconnect.com");

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _logger = new NUnitConsoleLogger<AcademicBenchmarksAuthApiManager>();

            var academicBenchmarksSnapshotOption = new Mock<IOptionsSnapshot<AcademicBenchmarksOptions>>();
            academicBenchmarksSnapshotOption.Setup(x => x.Value)
                .Returns(new AcademicBenchmarksOptions());

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(IOptionsSnapshot<AcademicBenchmarksOptions>)))
                .Returns(academicBenchmarksSnapshotOption.Object);

            _serviceProvider = serviceProvider.Object;
        }

        [Test]
        public async Task Get_authenticated_request_async_generates_value()
        {
            //Arrange
            var factory = new AcademicBenchmarksAuthApiManagerFactory(_serviceProvider, _logger);
            var partnerId = "userKey";
            var partnerSecret = "userSecret";
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(partnerId, partnerSecret);
            var tokenManager = factory.CreateLearningStandardsProviderAuthApiManager(authConfig);

            //Act
            var result = await tokenManager.GetAuthenticatedRequestAsync(HttpMethod.Get, _uri).ConfigureAwait(false);

            // Act & Assert
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task Get_authenticated_request_async_contains_required_fields()
        {
            //Arrange
            var factory = new AcademicBenchmarksAuthApiManagerFactory(_serviceProvider, _logger);
            var partnerId = "userKey";
            var partnerSecret = "userSecret";
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(partnerId, partnerSecret);
            var tokenManager = factory.CreateLearningStandardsProviderAuthApiManager(authConfig);

            //Act
            var result = await tokenManager.GetAuthenticatedRequestAsync(HttpMethod.Get, _uri).ConfigureAwait(false);

            // Act & Assert
            Assert.IsNotNull(result);
            var queryString = HttpUtility.ParseQueryString(result.RequestUri.Query);

            Assert.IsNotNull(queryString["partner.id"]);
            Assert.IsNotNull(queryString["auth.signature"]);
            Assert.IsNotNull(queryString["auth.expires"]);

            Assert.AreEqual(queryString["partner.id"], partnerId);
        }

        [Test]
        public async Task Get_authenticated_request_async_returns_proper_partner_id()
        {
            //Arrange
            var factory = new AcademicBenchmarksAuthApiManagerFactory(_serviceProvider, _logger);
            var partnerId = "userKey";
            var partnerSecret = "userSecret";
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(partnerId, partnerSecret);
            var tokenManager = factory.CreateLearningStandardsProviderAuthApiManager(authConfig);

            //Act
            var result = await tokenManager.GetAuthenticatedRequestAsync(HttpMethod.Get, _uri).ConfigureAwait(false);

            // Act & Assert
            Assert.IsNotNull(result);
            var queryString = HttpUtility.ParseQueryString(result.RequestUri.Query);

            Assert.AreEqual(queryString["partner.id"], partnerId);
        }

        [Test]
        public async Task Get_authenticated_request_async_generates_proper_signature_length()
        {
            //Arrange
            var factory = new AcademicBenchmarksAuthApiManagerFactory(_serviceProvider, _logger);
            var partnerId = "userKey";
            var partnerSecret = "userSecret";
            IAuthenticationConfiguration authConfig = new AuthenticationConfiguration(partnerId, partnerSecret);
            var tokenManager = factory.CreateLearningStandardsProviderAuthApiManager(authConfig);

            //Act
            var result = await tokenManager.GetAuthenticatedRequestAsync(HttpMethod.Get, _uri).ConfigureAwait(false);

            // Act & Assert
            Assert.IsNotNull(result);
            var queryString = HttpUtility.ParseQueryString(result.RequestUri.Query);

            string sig = queryString["auth.signature"];
            var sigBytes = Convert.FromBase64String(sig);

            //Assert
            Assert.AreEqual(256, sigBytes.Length * 8);
        }

        [Test]
        public void Api_manager_factory_throws_on_null_logger()
        {
            //Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                var factory = new AcademicBenchmarksAuthApiManagerFactory(null, null);
            });
        }

        [Test]
        public void Api_manager_throws_on_create_with_null_options()
        {
            //Arrange
            var factory = new AcademicBenchmarksAuthApiManagerFactory(_serviceProvider, _logger);

            //Act -> Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                factory.CreateLearningStandardsProviderAuthApiManager(null);
            });
        }
    }
}
