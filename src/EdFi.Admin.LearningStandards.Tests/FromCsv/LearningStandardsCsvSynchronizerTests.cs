using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.FromCsv;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces.FromCsv;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests.FromCsv
{
    [TestFixture]
    public class LearningStandardsCsvSynchronizerTests
    {
        private CsvLearningStandardsDataRetriever _csvLearningStandardsDataRetriever;
        private readonly string _metadataUri = "https://ed-fi-ods.org/metadata/data/v3/resources/swagger.json";
        private readonly string _folderPath = "TestFiles\\Sample-metadata";
        private DataMappingProcess _dataMappingProcess;
        private MetaDataRetriever _metaDataRetriever;
        private readonly NUnitConsoleLogger<CsvLearningStandardsDataRetriever> _logger = new NUnitConsoleLogger<CsvLearningStandardsDataRetriever>();
        private readonly NUnitConsoleLogger<LearningStandardsCsvSynchronizer> _debugLogger = new
            NUnitConsoleLogger<LearningStandardsCsvSynchronizer>(LogLevel.Information);
        private ILoggerProvider _loggerFactory;

        [SetUp]
        public void Init()
        {
            var mockSwaggerDocRetriever = new Mock<ISwaggerDocumentRetriever>();
            string swaggerDocument = TestHelpers.ReadTestFile($"{_folderPath}\\Swagger-Document-3.x.json");
            mockSwaggerDocRetriever.Setup(x => x.LoadJsonString(_metadataUri))
                .ReturnsAsync(swaggerDocument);
            _metaDataRetriever = new MetaDataRetriever(new NUnitConsoleLogger<MetaDataRetriever>(), mockSwaggerDocRetriever.Object);
            var csvFileProcessor = new CsvFileProcessor();
            _dataMappingProcess = new DataMappingProcess
            {
                DataMappingsFilePath = Path.Combine(_folderPath, "Mappings.json")
            };
            _csvLearningStandardsDataRetriever = new CsvLearningStandardsDataRetriever(_logger, _metaDataRetriever, csvFileProcessor, _dataMappingProcess);

            var lf = new Mock<ILoggerProvider>();
            lf.Setup(m => m.CreateLogger(It.IsAny<string>()))
                .Returns(_debugLogger);
            _loggerFactory = lf.Object;
        }

        [Test]
        public async Task Will_return_expected_progress_percentages()
        {
            //Arrange
            var options =
                new LearningStandardsSynchronizationFromCsvOptions
                {
                    ForceMetaDataReload = false,
                    InputCsvFullPath = Path.Combine(_folderPath, "test-data.csv"),
                    ResourcesMetaDataUri = _metadataUri
                };
            var mockCsvFileProcessor = new Mock<ICsvFileProcessor>();
            const int expectedRecordsCount = 527;
            mockCsvFileProcessor.Setup(x => x.GetRows(It.IsAny<string>()))
                .Returns(TestHelpers.GenerateDataRows(expectedRecordsCount));
            _csvLearningStandardsDataRetriever = new CsvLearningStandardsDataRetriever(_logger,
                _metaDataRetriever, mockCsvFileProcessor.Object, _dataMappingProcess);

            var odsApiConfiguration = new EdFiOdsApiConfiguration(
                "http://localhost:7000",
                EdFiOdsApiCompatibilityVersion.v3,
                new AuthenticationConfiguration("key", "secret"));
            var odsApiClientConfiguration = new EdFiOdsApiClientConfiguration();

            var odsLoggerFactory = new Mock<ILoggerFactory>();
            odsLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_debugLogger);

            var clientFactoryMock = new Mock<IHttpClientFactory>();
            var fakeHttpMessageHandler = new MockJsonHttpMessageHandler();
            fakeHttpMessageHandler.AddRouteResponse("token", GetDefaultAccessCodeResponse(expiresIn: 3600));
            var httpClient = new HttpClient(fakeHttpMessageHandler);
            clientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
            var edfiTokenManager = clientFactoryMock.Object;
            IEdFiOdsApiAuthTokenManagerFactory edfiOdsTokenManagerFactory =
                new EdFiOdsApiAuthTokenManagerFactory(edfiTokenManager, odsLoggerFactory.Object);

            IEdFiBulkJsonPersisterFactory edFiBulkJsonPersister = new FakeEdFiBulkJsonPersisterFactory();

            var versionManager = new Mock<IEdFiVersionManager>();
            versionManager.Setup(x => x.GetEdFiVersion(odsApiConfiguration, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new EdFiVersionModel(
                                            EdFiWebApiVersion.v7x,
                                            EdFiDataStandardVersion.DS5_2,
                                            new EdFiWebApiInfo()
                                            )
                );


            var learningStandardsCsvSynchronizer =
                new LearningStandardsCsvSynchronizer(odsApiClientConfiguration, versionManager.Object, edfiOdsTokenManagerFactory, edFiBulkJsonPersister,
                    _debugLogger, _csvLearningStandardsDataRetriever);
            var prog = new TestProgress(_loggerFactory);

            //Act
            var res = await learningStandardsCsvSynchronizer.SynchronizeAsync(odsApiConfiguration, options,
                CancellationToken.None, prog);

            //Assert
            Assert.IsNotNull(res);
            Assert.AreEqual(0, prog.PercentageUpdates.First());
            Assert.AreEqual(100, prog.PercentageUpdates.Last());
            Assert.IsTrue(prog.PercentageUpdates.All(al => al <= 100));
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

        private class FakeEdFiBulkJsonPersisterFactory : IEdFiBulkJsonPersisterFactory
        {
            public IEdFiBulkJsonPersister CreateEdFiBulkJsonPersister(IAuthTokenManager authTokenManager, IEdFiOdsApiConfiguration odsApiConfiguration)
            {
                return new FakeEdFiBulkJsonPersister();
            }
        }

        private class FakeEdFiBulkJsonPersister : IEdFiBulkJsonPersister
        {
            public Task<EdFiVersionModel> GetEdFiVersion()
            {
                throw new NotImplementedException();
            }

            public async Task<IList<IResponse>> PostEdFiBulkJson(EdFiBulkJsonModel edFiBulkJson,
                CancellationToken cancellationToken)
            {
                var responses = new List<IResponse>();
                await Task.Run(() =>
                {
                    responses.AddRange(edFiBulkJson.Data
                        .Select(data => new ResponseModel(true, null, null, HttpStatusCode.OK)));
                }, cancellationToken);
                return responses;
            }
        }
    }
}
