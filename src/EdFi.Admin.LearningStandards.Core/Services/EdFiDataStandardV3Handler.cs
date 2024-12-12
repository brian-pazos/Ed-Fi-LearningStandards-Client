using EdFi.Admin.LearningStandards.Core.Configuration;
using EdFi.Admin.LearningStandards.Core.Models.ABConnectApiModels;
using EdFi.Admin.LearningStandards.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EdFi.Admin.LearningStandards.Core.Services
{
    internal class EdFiDataStandardV3Handler : IEdFiDataStandardVersionHandler
    {
        private readonly ILearningStandardsProviderConfiguration _learningStandardsProviderConfiguration;
        private readonly ILogger _logger;

        public EdFiDataStandardV3Handler(ILearningStandardsProviderConfiguration config, ILogger logger)
        {
            _learningStandardsProviderConfiguration = config;
            _logger = logger;
        }


        public IEnumerable<EdFiBulkJsonModel> MapResponse(ILearningStandardsApiResponseModel response)
        {
            return response switch
            {
                ABConnectFacetsResponse facetsResponse => HandleFacetsResponse(facetsResponse),
                ABConnectStandardsResponse standardsResponse => HandleStandardsResponse(standardsResponse),
                _ => throw LogAndThrowNotImplemented(response)
            };
        }


        private IEnumerable<EdFiBulkJsonModel> HandleFacetsResponse(ABConnectFacetsResponse response)
        {
            foreach (var facet in response?.Meta?.Facets ?? Enumerable.Empty<FacetModel>())
            {
                if (FacetMappings.TryGetValue(facet.Facet, out var mapping))
                {
                    yield return CreateEdFiDescriptorModel(mapping.resource, mapping.ns, facet.Details);
                }
            }
            yield return CreatePublicationStatusDescriptors();
        }

        private IEnumerable<EdFiBulkJsonModel> HandleStandardsResponse(ABConnectStandardsResponse response)
        {
            yield return CreateLearningStandardsEdFiModel(response);
        }

        private Exception LogAndThrowNotImplemented(ILearningStandardsApiResponseModel response)
        {
            var exception = new NotImplementedException($"The type '{response.GetType()}' is not implemented for mapping.");
            _logger.LogError(exception);
            return exception;
        }



        #region Descriptor Mapping

        protected EdFiBulkJsonModel CreateEdFiDescriptorModel(string resource, string ns, IEnumerable<FacetElement> details)
        {
            return new EdFiBulkJsonModel
            {
                Schema = "ed-fi",
                Resource = resource,
                Operation = "Upsert",
                Data = details.Select(fd =>
                    JObject.FromObject(new
                    {
                        codeValue = fd.Data.Guid,
                        description = fd.Data.Descr,
                        @namespace = ns,
                        shortDescription = fd.Data.Code
                    })).ToList()
            };
        }


        protected EdFiBulkJsonModel CreatePublicationStatusDescriptors()
        {
            // hardcoded values
            var statuses = new List<string> { "Active", "Deleted", "Draft", "Obsolete" };
            return new EdFiBulkJsonModel
            {
                Schema = "ed-fi",
                Resource = "publicationStatusDescriptors",
                Operation = "Upsert",
                Data = PublicationStatusLookup.Select(kvp =>
                    JObject.FromObject(new
                    {
                        codeValue = kvp.Key,
                        description = kvp.Key,
                        @namespace = $"{AcademinBenchmarksNamespace}/PublicationStatusDescriptor",
                        shortDescription = kvp.Key
                    })).ToList()
            };
        }


        #endregion

        #region Standard Mapping


        protected EdFiBulkJsonModel CreateLearningStandardsEdFiModel(ABConnectStandardsResponse response)
        {
            return new EdFiBulkJsonModel
            {
                Schema = "ed-fi",
                Resource = "learningStandards",
                Operation = "Upsert",
                Data = response.Data.Select(s =>
                    JObject.FromObject(new
                    {
                        learningStandardId = s.Id,
                        academicSubjects = s.Attributes.Disciplines.Subjects.Select(subj =>
                            new
                            {
                                academicSubjectDescriptor = $"{AcademinBenchmarksNamespace}/AcademicSubjectDescriptor#{subj.Guid}"
                            }),
                        gradeLevels = s.Attributes.EducationLevels.Grades.Select(g =>
                            new
                            {
                                gradeLevelDescriptor = $"{AcademinBenchmarksNamespace}/GradeLevelDescriptor#{g.Guid}"
                            }),
                        contentStandard = new
                        {
                            title = s.Attributes.Document.Descr,
                            publicationStatusDescriptor = PublicationStatusLookup.ContainsKey(s.Attributes.Status)
                                            ? PublicationStatusLookup[s.Attributes.Status]
                                            : PublicationStatusLookup[PublicationStatusDefaultValue],
                            publicationYear = s.Attributes.Document.AdoptYear,
                            authors = s.Attributes.Document.Publication.Authorities.Select(a => new { author = a.Descr }),
                        },
                        description = s.Attributes.Statement.CombinedDescr.Length > 1024
                                            ? s.Attributes.Statement.CombinedDescr.Substring(0, 1024)
                                            : s.Attributes.Statement.CombinedDescr,
                        courseTitle = s.Attributes.Section.Descr.Length > 60
                                            ? s.Attributes.Section.Descr.Substring(0, 60)
                                            : s.Attributes.Section.Descr,
                        @namespace = $"{AcademinBenchmarksNamespace}/LearningStandard",
                        uri = $"{_learningStandardsProviderConfiguration.Url.TrimEnd('/')}/standards/{s.Id}"

                    })).ToList()
            };
        }

        #endregion

        #region Data

        private readonly static string AcademinBenchmarksNamespace = "uri://academicbenchmarks.com";


        public readonly static string PublicationStatusDefaultValue = "Unknown";
        private Dictionary<string, string> PublicationStatusLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Active",$"{AcademinBenchmarksNamespace}/PublicationStatusDescriptor#Active" },
            { "Deleted", $"{AcademinBenchmarksNamespace}/PublicationStatusDescriptor#Deleted" },
            { "Draft", $"{AcademinBenchmarksNamespace}/PublicationStatusDescriptor#Draft" },
            { "Obsolete", $"{AcademinBenchmarksNamespace}/PublicationStatusDescriptor#Obsolete"},
            { "Unknown", $"{AcademinBenchmarksNamespace}/PublicationStatusDescriptor#Unknown"}
        };


        // Facet to resource/namespace mapping
        private Dictionary<string, (string resource, string ns)> FacetMappings = new Dictionary<string, (string resource, string ns)>
        {
            { "disciplines.subjects", ("academicSubjectDescriptors", $"{AcademinBenchmarksNamespace}/AcademicSubjectDescriptor") },
            { "education_levels.grades", ("gradeLevelDescriptors", $"{AcademinBenchmarksNamespace}/GradeLevelDescriptor") }
        };

        #endregion

    }
}
