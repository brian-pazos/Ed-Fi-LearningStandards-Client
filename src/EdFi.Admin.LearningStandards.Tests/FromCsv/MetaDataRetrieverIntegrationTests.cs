using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services.FromCsv;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EdFi.Admin.LearningStandards.Tests.FromCsv
{
    [Category("Interactive")]
    [Ignore("These tests are for interactive local use only. Comment out this line to use. This test focusing on" +
            " making sure the swagger metadata from ODS API is compatible with pre-defined learning standards metadata (Expected-LS-Metadata.json). ")]
    [TestFixture]
    public class MetaDataRetrieverIntegrationTests
    {
        private ILogger<MetaDataRetriever> _logger;

        [Test]
        public async Task Will_return_expected_learning_standards_metadata()
        {
            // Steps to test on v3:
            // 0) Checkout main on ODS,Impl initdev.
            // 1) Run build in VS2019
            // 2) Run initdev -noCompile
            // 3) Launch WebApi project in debug in VS2019 session.

            //Arrange
            string odsApiBasePath = "http://localhost:54746/";
            var edfiConfiguration = new EdFiOdsApiConfiguration(odsApiBasePath,
                EdFiOdsApiCompatibilityVersion.v3, new AuthenticationConfiguration(null, null));
            string swaggerResourceEndPoint = SwaggerMetaDataUriHelper.GetUri(edfiConfiguration);
         
            string folderPath = "TestFiles\\Sample-metadata";
            string fileName = Path.Combine(folderPath, "metadata.json");
            IfFileExistsDelete(fileName);

            _logger = new NUnitConsoleLogger<MetaDataRetriever>();
            string expectedLsContent = TestHelpers.ReadTestFile($"{folderPath}\\Expected-LS-Metadata.json");
            var swaggerDocumentRetriever = new SwaggerDocumentRetriever(new NUnitConsoleLogger<SwaggerDocumentRetriever>(), new MockHttpClientFactory() );
            var metaDataRetriever = new MetaDataRetriever(_logger, swaggerDocumentRetriever);
            string cleanedExpectedContent = Regex.Replace(expectedLsContent, @"\s+", "");

            //Act
            var metaData =  await metaDataRetriever.GetMetadata(swaggerResourceEndPoint, false, folderPath);
            string jsonString = JsonConvert.SerializeObject(metaData);

            //Assert
            FileAssert.Exists(fileName);
            Assert.NotNull(metaData);
            Assert.AreEqual(cleanedExpectedContent, jsonString);
        }

        private static void IfFileExistsDelete(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }


    [Category("Interactive")]
    [Ignore("These tests are for interactive local use only. Comment out this line to use. This test is focusing on" +
            " making sure the swagger metadata from ODS API and pre-defined mappings are compatible.")]
    [TestFixture]
    public class MetaDataRetrieverAndDataMappingProcessIntegrationTests
    {
        private ILogger<MetaDataRetriever> _logger;

        [Test]
        public async Task Will_return_expected_json_payload()
        {
            //Arrange
            string GetCleanContent(string content)
            {
                return Regex.Replace(content, @"\s+", "");
            }
            string odsApiBasePath = "http://localhost:54746/";
            var edfiConfiguration = new EdFiOdsApiConfiguration(odsApiBasePath,
                EdFiOdsApiCompatibilityVersion.v3, new AuthenticationConfiguration(null, null));
            string swaggerResourceEndPoint = SwaggerMetaDataUriHelper.GetUri(edfiConfiguration);

            string folderPath = "TestFiles\\Sample-metadata";
            string fileName = Path.Combine(folderPath, "metadata.json");
            IfFileExistsDelete(fileName);

            _logger = new NUnitConsoleLogger<MetaDataRetriever>();
            var swaggerDocumentRetriever = new SwaggerDocumentRetriever(new NUnitConsoleLogger<SwaggerDocumentRetriever>(), new MockHttpClientFactory());
            var metaDataRetriever = new MetaDataRetriever(_logger, swaggerDocumentRetriever);
            var dataMappingProcessor = new DataMappingProcess();
            dataMappingProcessor.DataMappingsFilePath = Path.Combine(folderPath, "Mappings.json");
            var mappings = dataMappingProcessor.GetDataMappings();
            var learningStandardMetadata =
               await metaDataRetriever.GetMetadata(swaggerResourceEndPoint, false, folderPath);
            var csvFileProcessor = new CsvFileProcessor();
            var csvRows = csvFileProcessor.GetRows(Path.Combine(GetAssemblyPath(), "TestFiles/Sample-metadata/test-data.csv")).ToList();
            var learningStandardMetaDatas = learningStandardMetadata.ToList();
            var dataMappers = mappings.ToList();

            string expectedPayLoad1 = GetCleanContent(TestHelpers.ReadTestFile(Path.Combine(folderPath, "expected-mapped-row1.json")));
            string expectedPayLoad2 = GetCleanContent(TestHelpers.ReadTestFile(Path.Combine(folderPath, "expected-mapped-row2.json")));

            //Act
            var mappedRow1 = dataMappingProcessor.ApplyMap(learningStandardMetaDatas, dataMappers, csvRows[0]);
            string mappedRow1Content = GetCleanContent(JsonConvert.SerializeObject(mappedRow1));

            var mappedRow2 = dataMappingProcessor.ApplyMap(learningStandardMetaDatas, dataMappers, csvRows[1]);
            string mappedRow2Content = GetCleanContent(JsonConvert.SerializeObject(mappedRow2));

            //Assert
            Assert.AreEqual(expectedPayLoad1, mappedRow1Content);
            Assert.AreEqual(expectedPayLoad2, mappedRow2Content);
        }

        public static string GetAssemblyPath()
            => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static void IfFileExistsDelete(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
