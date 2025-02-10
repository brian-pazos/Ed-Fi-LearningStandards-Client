// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests.IntegrationTests
{
    [TestFixture]
    [Category("Interactive")]
    [Ignore("These tests are for interactive local use only. Comment out this line to use.")]
    public class SynchronizerResponseIntegrationJsonTests
    {
        private const string _documentSavePath = @"C:\Projects\TestOutput\EdFi.Admin.LearningStandards.CLI";

        private const string _defaultOdsUrl = "https://api.testing-ed-fi.org/v3/api";

        private const string _oAuthKey = "1a2b3c4d5e6f7g8h9i0j";

        private const string _oAuthSecret = "j0i9h8g7f6e5d4c3b2a1";

        private const string DescriptorsRouteType = "Descriptors";

        private const string SyncRouteType = "Sync";

        private Mock<IOptionsSnapshot<AcademicBenchmarksOptions>> _academicBenchmarksSnapshotOptionMock;

        private Mock<IAuthTokenManager> _authTokenManager;

        private readonly NUnitConsoleLogger<LearningStandardsSynchronizer> _debugLogger = new NUnitConsoleLogger<LearningStandardsSynchronizer>(LogLevel.Information);

        private ILoggerProvider _loggerFactory;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var lf = new Mock<ILoggerProvider>();
            lf.Setup(m => m.CreateLogger(It.IsAny<string>()))
                .Returns(_debugLogger);
            _loggerFactory = lf.Object;

            _authTokenManager = new Mock<IAuthTokenManager>();
            _authTokenManager.Setup(x => x.GetTokenAsync())
                .Returns(Task.FromResult("ThisIsAToken"));

            _academicBenchmarksSnapshotOptionMock = new Mock<IOptionsSnapshot<AcademicBenchmarksOptions>>();
            _academicBenchmarksSnapshotOptionMock.Setup(x => x.Value)
                .Returns(
                    new AcademicBenchmarksOptions
                    { Url = "https://localhost:7777" });
        }

        public void Cli_application_can_retrieve_change_documents_only()
        {

        }

        private class ABOnlyHttpMessageHandler : DelegatingHandler
        {
            private readonly HttpClient _mockClient;

            public ABOnlyHttpMessageHandler(HttpClient mockClient)
            {
                _mockClient = mockClient;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (request.RequestUri.ToString().Contains("localhost"))
                {
                    return base.SendAsync(request, cancellationToken);
                }

                return _mockClient.SendAsync(request, cancellationToken);
            }
        }

        private class FileEdFiBulkJsonPersisterFactory : IEdFiBulkJsonPersisterFactory
        {
            private readonly string _filePath;

            public FileEdFiBulkJsonPersisterFactory(string filePath)
            {
                _filePath = filePath;
            }

            public IEdFiBulkJsonPersister CreateEdFiBulkJsonPersister(IAuthTokenManager authTokenManager, IEdFiOdsApiConfiguration odsApiConfiguration)
            {
                return new FileEdFiBulkJsonPersister(_filePath);
            }
        }

        private class FileEdFiBulkJsonPersister : IEdFiBulkJsonPersister
        {
            private readonly string _filePath;

            public FileEdFiBulkJsonPersister(string filePath)
            {
                _filePath = filePath;
            }

            public async Task<EdFiVersionModel> GetEdFiVersion()
            {
                return new EdFiVersionModel(EdFiWebApiVersion.Unknown, EdFiDataStandardVersion.Unknown, new EdFiWebApiInfo());
            }

            public async Task<IList<IResponse>> PostEdFiBulkJson(EdFiBulkJsonModel edFiBulkJson, CancellationToken cancellationToken)
            {
                using (var sr = new StreamWriter(_filePath))
                {
                    await sr.WriteAsync(JsonConvert.SerializeObject(edFiBulkJson));
                }

                return new List<IResponse>();
            }
        }
    }
}
