using EdFi.Admin.LearningStandards.Core.Auth;
using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Services;
using EdFi.Admin.LearningStandards.Core.Services.FromCsv;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces.FromCsv;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Linq;
using System.Net.Http;

namespace EdFi.Admin.LearningStandards.Core.Installers
{
    public static class LearningStandardsServiceCollectionExtensions
    {
        private static readonly Random _jitterer = new Random();

        public static IServiceCollection AddLearningStandardsServices(this IServiceCollection services, IEdFiOdsApiClientConfiguration odsApiClientConfiguration)
        {
            // Should use IServiceCollection and related interfaces, but may not use anything that would require a dependency beyond NET STANDARD.
            // https://medium.com/volosoft/asp-net-core-dependency-injection-58bc78c5d369

            // Configuration and Options:
            services.AddSingleton(odsApiClientConfiguration);

            // HTTP Client Registration:
            services.InstallLearningStandardsEdFiHttpClients();
            services.InstallLearningStandardHttpClients();

            // Auth Services:
            services.AddSingleton<IEdFiVersionManager, EdFiVersionManager>();
            services.AddSingleton<IEdFiOdsApiAuthTokenManagerFactory, EdFiOdsApiAuthTokenManagerFactory>();
            services
                .AddSingleton<ILearningStandardsProviderAuthApiManagerFactory,
                    AcademicBenchmarksAuthApiManagerFactory>();



            services.AddSingleton<AcademicBenchmarksLearningStandardsDataRetriever>();
            services.AddSingleton<ILearningStandardsDataRetriever>(x => x.GetRequiredService<AcademicBenchmarksLearningStandardsDataRetriever>());
            services.AddSingleton<ILearningStandardsDataValidator>(x => x.GetRequiredService<AcademicBenchmarksLearningStandardsDataRetriever>());

            // Data Mapper Registration:
            services.AddSingleton<ILearningStandardsDataMapper, LearningStandardsDataMapper>();

            services.AddSingleton<IEdFiBulkJsonPersisterFactory, EdFiBulkJsonPersisterFactory>();

            services.AddSingleton<LearningStandardsSynchronizer>();
            services.AddSingleton<ILearningStandardsSynchronizer>(x => x.GetRequiredService<LearningStandardsSynchronizer>());
            services.AddSingleton<ILearningStandardsChangesAvailable>(x => x.GetRequiredService<LearningStandardsSynchronizer>());
            services.AddSingleton<ILearningStandardsConfigurationValidator, LearningStandardsConfigurationValidator>();

            if (services.All(x => x.ServiceType != typeof(IChangeSequencePersister)))
            {
                services.AddSingleton<IChangeSequencePersister, DefaultChangeSequencePersister>();
            }

            services
                .ConfigureDefaultLearningStandardSynchronizationOptions<
                    LearningStandardsSynchronizationOptions>(options => options.ForceFullSync = false);


            return services;
        }

        public static IServiceCollection AddLearningStandardsSyncFromCsvSpecificServices(this IServiceCollection services,
            IEdFiOdsApiClientConfiguration odsApiClientConfiguration)
        {
            // Configuration and Options:
            services.AddSingleton(odsApiClientConfiguration);

            // Auth Services:
            services.AddSingleton<IEdFiVersionManager, EdFiVersionManager>();
            services.AddSingleton<IEdFiOdsApiAuthTokenManagerFactory, EdFiOdsApiAuthTokenManagerFactory>();

            // HTTP Client Registration:
            services.InstallLearningStandardsEdFiHttpClients();


            // Data Mapper Registration:
            services.AddSingleton<ILearningStandardsDataMapper, LearningStandardsDataMapper>();

            // CSV data reading and mapping services:
            services.AddSingleton<ISwaggerDocumentRetriever, SwaggerDocumentRetriever>();
            services.AddSingleton<IMetaDataRetriever, MetaDataRetriever>();
            services.AddSingleton<ICsvFileProcessor, CsvFileProcessor>();
            services.AddSingleton<IDataMappingProcess, DataMappingProcess>();
            services.AddSingleton<CsvLearningStandardsDataRetriever>();
            services.AddSingleton<ILearningStandardsCsvDataRetriever>(x => x.GetRequiredService<CsvLearningStandardsDataRetriever>());

            services.AddSingleton<IEdFiBulkJsonPersisterFactory, EdFiBulkJsonPersisterFactory>();

            services.AddSingleton<LearningStandardsCsvSynchronizer>();
            services.AddSingleton<ILearningStandardsCsvSynchronizer>(x => x.GetRequiredService<LearningStandardsCsvSynchronizer>());
            services.AddSingleton<ILearningStandardsSyncFromCsvConfigurationValidator, LearningStandardsSyncFromCsvConfigurationValidator>();

            return services;
        }

        public static IServiceCollection ConfigureDefaultLearningStandardSynchronizationOptions<TOptions>(
            this IServiceCollection services,
            Action<TOptions> configureOptions)
            where TOptions : class, ILearningStandardsSynchronizationOptions
        {
            return services.Configure(configureOptions);
        }

        public static IServiceCollection ConfigureLearningStandardsProvider<TOptions>(this IServiceCollection services, Action<TOptions> configureOptions)
            where TOptions : class, ILearningStandardsProviderConfiguration
        {
            return services.Configure(configureOptions);
        }

        private static IAsyncPolicy<HttpResponseMessage> GetDataRetryPolicy(IServiceProvider provider, int retryLimit, Type callingType)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => !msg.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    retryLimit,
                    retryAttempt => TimeSpan.FromSeconds(
                                        Math.Pow(
                                            2,
                                            retryAttempt))
                                    + TimeSpan.FromMilliseconds(_jitterer.Next(0, 100)),
                    (
                        outcome,
                        timespan,
                        retryAttempt,
                        context) =>
                    {
                        (provider.GetService(
                                typeof(ILogger<>).MakeGenericType(callingType)) as ILogger)
                            ?.LogWarning(
                                "Delaying for {delay}ms, then making retry {retry}.",
                                timespan.TotalMilliseconds,
                                retryAttempt);
                    });
        }

        private static void InstallLearningStandardHttpClients(this IServiceCollection services)
        {
            services.AddHttpClient(nameof(ILearningStandardsDataRetriever))
                .AddPolicyHandler(
                    (provider, request) => GetDataRetryPolicy(
                        provider,
                        provider
                            .GetRequiredService<
                                IOptionsSnapshot<AcademicBenchmarksOptions>>()
                            .Value.Retries,
                        provider.GetRequiredService<ILearningStandardsDataRetriever>()
                            .GetType()));
        }

        private static void InstallLearningStandardsEdFiHttpClients(this IServiceCollection services)
        {
            services.AddHttpClient(nameof(IAuthTokenManager))
                .AddPolicyHandler(
                    (provider, request) => HttpPolicyExtensions.HandleTransientHttpError()
                        .RetryAsync(
                            provider.GetRequiredService<IEdFiOdsApiClientConfiguration>().Retries));

            services.AddHttpClient(nameof(IEdFiBulkJsonPersister))
                .AddPolicyHandler(
                    (provider, request) => GetDataRetryPolicy(
                        provider,
                        provider.GetRequiredService<IEdFiOdsApiClientConfiguration>().Retries,
                        typeof(EdFiBulkJsonPersister)));
        }
    }
}
