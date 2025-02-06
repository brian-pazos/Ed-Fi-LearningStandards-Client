// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.CLI;
using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces.FromCsv;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests.FromCsv
{
    [TestFixture]
    public class LearningStandardsCLICSVSyncTests
    {
        private const string ExpectedAccessToken = "940934d3a405492c99572db9329fc081";

        private const string SyncArgs = "sync-from-csv -v --ed-fi-url=https://ed-fi-ods.org --ed-fi-key=abc123 --ed-fi-secret=123abc --csv-file-path=C:\\test\\input.csv --resources-meta-data-uri=https://ed-fi-ods.org";

        [Test]
        public void Can_build_application()
        {
            //Act
            var actual = new LearningStandardsCLICSVSyncApplication(services => { });

            //Assert
            Assert.NotNull(actual);
        }

        [Test]
        public async Task Will_display_error_on_invalid_configuration()
        {
            //Arrange
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("*", HttpStatusCode.NotFound);
            var app = new LearningStandardsCLICSVSyncApplication(services =>
            {
                services.ConfigureAll<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = httpHandler);
                });
            });

            //Act
            var actual = await app.Main(SyncArgs.Split(' ')).ConfigureAwait(false);

            //Assert
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.AreEqual(HttpStatusCode.NotFound, actual.StatusCode);
        }

        [Test]
        public async Task Will_display_error_on_invalid_odsapi_credentials()
        {
            //Arrange
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("*", HttpStatusCode.Unauthorized);
            var app = new LearningStandardsCLICSVSyncApplication(services =>
            {
                services.ConfigureAll<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = httpHandler);
                });
            });

            //Act
            var actual = await app.Main(SyncArgs.Split(' ')).ConfigureAwait(false);

            //Assert
            Assert.AreEqual(false, actual.IsSuccess);
            Assert.AreEqual(HttpStatusCode.Unauthorized, actual.StatusCode);
        }

        [Test]
        public void Will_display_help_on_empty_params()
        {
            //Arrange
            var app = new LearningStandardsCLICSVSyncApplication(services => { });

            //Assert -> Act
            Assert.ThrowsAsync<LearningStandardsCLIParserException>(async () =>
            {
                await app.Main(new string[] { }).ConfigureAwait(false);
            });
        }

        [Test]
        public async Task Will_display_single_message_per_error()
        {
            //Arrange
            var consoleStringListWriter = new ConsoleStringListWriter();
            Console.SetOut(consoleStringListWriter);


            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(ExpectedAccessToken))
                .AddRouteResponse($"/", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")))
                .AddRouteResponse("*", HttpStatusCode.OK);

            var app = new LearningStandardsCLICSVSyncApplication(services =>
            {
                services.AddSingleton<ILearningStandardsCsvDataRetriever, ThrowingLearningStandardsCsvDataRetriever>();
                services.ConfigureAll<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = httpHandler);
                });
                services.ConfigureAll<ILoggingBuilder>(options =>
                {
                    options.ClearProviders();
                    options.AddProvider(new NUnitLoggerProvider());
                });
            });

            //Act
            int appResult = 1;
            try
            {
                var appResponse = await app.Main(SyncArgs.Split(' ')).ConfigureAwait(false);
                appResult = appResponse.IsSuccess ? 0 : 1;
            }
            catch (OperationCanceledException)
            {
                app.CliWriter.Info("Operation cancelled");
            }
            catch (LearningStandardsCLIParserException pex)
            {
                app.CliWriter.Error(pex.Message);
            }
            catch (Exception ex)
            {
                app.CliWriter.Error(ex.Message);
            }

            //Assert
            Assert.AreEqual(appResult, 1);
            Assert.IsNotNull(consoleStringListWriter.OutputLines.SingleOrDefault(sd => sd.StartsWith("Error: [500]")));

            consoleStringListWriter.Dispose();
        }

        [Test]
        public async Task Will_display_single_message_per_error_with_possible_exceptions()
        {
            //Arrange
            var consoleStringListWriter = new ConsoleStringListWriter();
            Console.SetOut(consoleStringListWriter);

            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(ExpectedAccessToken))
                .AddRouteResponse($"/", JToken.Parse(TestCaseHelper.GetTestCaseTextFromFile("EdFiODSResponse/ODSv7x-Info-Response.json")))
                .AddRouteResponse("*", HttpStatusCode.OK);

            var app = new LearningStandardsCLICSVSyncApplication(services =>
            {
                services.AddSingleton<ILearningStandardsCsvDataRetriever, ThrowingLearningStandardsCsvDataRetriever>();
                services.ConfigureAll<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = httpHandler);
                });
                services.ConfigureAll<ILoggingBuilder>(options =>
                {
                    options.ClearProviders();
                    options.AddProvider(new NUnitLoggerProvider());
                });
            });

            //Act
            var appResponse = await app.Main(SyncArgs.Split(' ')).ConfigureAwait(false);

            //Assert
            Assert.AreEqual(false, appResponse.IsSuccess);
            Assert.IsNotNull(consoleStringListWriter.OutputLines.SingleOrDefault(sd => sd.StartsWith("Error: [500]")));

            consoleStringListWriter.Dispose();
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

        private class ThrowingLearningStandardsCsvDataRetriever : ILearningStandardsCsvDataRetriever
        {
            private readonly ILogger<ThrowingLearningStandardsCsvDataRetriever> _logger;

            public ThrowingLearningStandardsCsvDataRetriever(
                ILogger<ThrowingLearningStandardsCsvDataRetriever> logger)
            {
                _logger = logger;
            }

            public async Task<AsyncEnumerableOperation<EdFiBulkJsonModel>> GetLearningStandards(
                ILearningStandardsSynchronizationFromCsvOptions options,
                CancellationToken cancellationToken = default)
            {
                return await GetFakeResult();
            }

            public event EventHandler<AsyncEnumerableOperationStatus> ProcessCountEvent;

            private async Task<AsyncEnumerableOperation<EdFiBulkJsonModel>> GetFakeResult()
            {
                var id = Guid.NewGuid();
                ProcessCountEvent?.Invoke(this, new AsyncEnumerableOperationStatus(id, 500));
                await ThrowDuringGetEdFiBulkAsyncEnumerable();
                return new AsyncEnumerableOperation<EdFiBulkJsonModel>(id,
                    default);
            }

            private async Task ThrowDuringGetEdFiBulkAsyncEnumerable()
            {
                try
                {
                    await Task.Run(() => throw new LearningStandardsHttpRequestException("Bad Request", HttpStatusCode.BadRequest,
                         "", "SyncFromCsv"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                    throw new Exception(
                        "An unexpected error occurred while streaming Learning Standards. Please try again. If this problem persists, please contact support.",
                        ex);
                }
            }
        }
    }
}
