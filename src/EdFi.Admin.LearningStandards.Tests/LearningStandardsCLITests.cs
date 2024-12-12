// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using EdFi.Admin.LearningStandards.CLI;
using EdFi.Admin.LearningStandards.Core;
using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Models;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Async;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests
{
    [TestFixture]
    public class LearningStandardsCLITests
    {
        private const string _expectedAccessToken = "940934d3a405492c99572db9329fc081";

        private readonly string _syncArgs = "sync -v --ab-connect-id=abc123 --ab-connect-key=123abc --ed-fi-url=https://ed-fi-ods.org --ed-fi-key=abc123 --ed-fi-secret=123abc";

        private readonly string _validateArgs = "validate -v --ab-connect-id=abc123 --ab-connect-key=123abc --ed-fi-url=https://ed-fi-ods.org --ed-fi-key=abc123 --ed-fi-secret=123abc";

        private readonly string _changesArgs = "changes -v --ab-connect-id=abc123 --ab-connect-key=123abc --ed-fi-url=https://ed-fi-ods.org --ed-fi-key=abc123 --ed-fi-secret=123abc";

        [Test]
        public void Can_build_application()
        {
            //Arrange
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("validate/authentication", GetDefaultProxyResponse())
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken));

            //Act
            var actual = new LearningStandardsCLIApplication(services =>
            {
                services.ConfigureAll<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = httpHandler);
                });
            });

            //Assert
            Assert.NotNull(actual);
        }

        [Test]
        public async Task Can_validate_configuration_only()
        {
            //Arrange
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("standards", ABConnectApiFileBasedTestCases.ValidApiResponse_LearningStandards())
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken));
            var app = new LearningStandardsCLIApplication(services =>
            {
                services.ConfigureAll<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = httpHandler);
                });
            });

            //Act
            var actual = await app.Main(_validateArgs.Split(' ')).ConfigureAwait(false);

            //Assert
            Assert.AreEqual(true, actual.IsSuccess);
        }

        [Test]
        public async Task Can_validate_configuration_only_with_failure()
        {
            //Arrange
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("validate/authentication", HttpStatusCode.NotFound);
            var app = new LearningStandardsCLIApplication(services =>
            {
                services.ConfigureAll<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = httpHandler);
                });
            });

            //Act
            var actual = await app.Main(_validateArgs.Split(' ')).ConfigureAwait(false);

            //Assert
            Assert.AreEqual(false, actual.IsSuccess);
        }

        [Test]
        public async Task Will_display_changes_available_summary()
        {
            //Arrange
            var consoleStringListWriter = new ConsoleStringListWriter();
            Console.SetOut(consoleStringListWriter);

            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("standards", JToken.Parse(ABConnectApiFileBasedTestCases.ValidApiResponse_LearningStandards()))
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken))
                .AddRouteResponse("events", JToken.Parse(ABConnectApiFileBasedTestCases.ValidApiResponse_EventsSingleResponse()));
            var app = new LearningStandardsCLIApplication(services =>
            {
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

            string cliArgs = _changesArgs;

            //Act
            var actual = await app.Main(cliArgs.Split(' ')).ConfigureAwait(false);

            //Assert
            Assert.AreEqual(true, actual.IsSuccess);
            Assert.IsNotNull(consoleStringListWriter.OutputLines.SingleOrDefault(sd => sd.StartsWith("Changes available: True")));

            consoleStringListWriter.Dispose();
        }

        [Test]
        public void Will_display_help_on_empty_params()
        {
            //Arrange
            var httpHandler = new MockJsonHttpMessageHandler()
                .AddRouteResponse("validate/authentication", GetDefaultProxyResponse())
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken));

            var app = new LearningStandardsCLIApplication(services =>
            {
                services.ConfigureAll<HttpClientFactoryOptions>(options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b => b.PrimaryHandler = httpHandler);
                });
            });

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
                .AddRouteResponse("validate/authentication", GetDefaultProxyResponse())
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken))
                .AddRouteResponse("*", HttpStatusCode.OK);

            var app = new LearningStandardsCLIApplication(services =>
            {
                services.AddSingleton<ILearningStandardsDataRetriever, ThrowingLearningStandardsDataRetriever>();
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
                var appResponse = await app.Main(_syncArgs.Split(' ')).ConfigureAwait(false);
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
                .AddRouteResponse("validate/authentication", GetDefaultProxyResponse())
                .AddRouteResponse("token", GetDefaultAccessCodeResponse(_expectedAccessToken))
                .AddRouteResponse("*", HttpStatusCode.OK);

            var app = new LearningStandardsCLIApplication(services =>
            {
                services.AddSingleton<ILearningStandardsDataRetriever, ThrowingLearningStandardsDataRetriever>();
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
            var appResponse = await app.Main(_syncArgs.Split(' ')).ConfigureAwait(false);

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

        private JObject GetDefaultProxyResponse()
        {
            return new JObject
            {
                ["status"] = "OK"
            };
        }

        private class ThrowingLearningStandardsDataRetriever : ILearningStandardsDataRetriever
        {
            private readonly ILogger<ThrowingLearningStandardsDataRetriever> _logger;

            public ThrowingLearningStandardsDataRetriever(ILogger<ThrowingLearningStandardsDataRetriever> logger)
            {
                _logger = logger;
            }

            public AsyncEnumerableOperation<EdFiBulkJsonModel> GetLearningStandardsDescriptors(
                EdFiOdsApiCompatibilityVersion version,
                IChangeSequence changeSequence,
                IAuthApiManager learningStandardsProviderAuthTokenManager,
                CancellationToken cancellationToken)
            {
                return GetFakeResult();
            }

            public AsyncEnumerableOperation<EdFiBulkJsonModel> GetLearningStandards(
                EdFiOdsApiCompatibilityVersion version,
                IChangeSequence syncStartSequence,
                IAuthApiManager learningStandardsProviderAuthTokenManager,
                CancellationToken cancellationToken = default)
            {
                return GetFakeResult();
            }

            public Task<IChangesAvailableResponse> GetChangesAsync(
                IChangeSequence changeSequence,
                IAuthApiManager learningStandardsProviderAuthTokenManager,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public event EventHandler<AsyncEnumerableOperationStatus> ProcessCountEvent;

            private AsyncEnumerableOperation<EdFiBulkJsonModel> GetFakeResult()
            {
                var id = Guid.NewGuid();
                ProcessCountEvent?.Invoke(this, new AsyncEnumerableOperationStatus(id, 500));
                ThrowDuringGetEdFiBulkAsyncEnumerable();
                return new AsyncEnumerableOperation<EdFiBulkJsonModel>(id, default(IAsyncEnumerable<EdFiBulkJsonModel>));
            }

            private void ThrowDuringGetEdFiBulkAsyncEnumerable()
            {
                try
                {
                    throw new LearningStandardsHttpRequestException("Bad Request", HttpStatusCode.BadRequest, "", "AB");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                    throw new Exception("An unexpected error occurred while streaming Learning Standards. Please try again. If this problem persists, please contact support.", ex);
                }
            }

            public AsyncEnumerableOperation<LearningStandardsSegmentModel> GetChangedSegments(EdFiOdsApiCompatibilityVersion version, IChangeSequence syncStartSequence, IAuthApiManager learningStandardsProviderAuthTokenManager, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public AsyncEnumerableOperation<EdFiBulkJsonModel> GetSegmentLearningStandards(EdFiOdsApiCompatibilityVersion version, LearningStandardsSegmentModel section, IAuthApiManager learningStandardsProviderAuthTokenManager, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
