using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EdFi.Admin.LearningStandards.Core.Models.ABConnectApiModels
{

    public interface ILearningStandardsApiResponseModel
    {
        public LinksModel Links { get; set; }

    }

    public class ABConnectFacetsResponse : ILearningStandardsApiResponseModel
    {
        public LinksModel Links { get; set; }

        public FacetMetadataModel Meta { get; set; }

    }


    public class ABConnectStandardsResponse : ILearningStandardsApiResponseModel
    {
        public LinksModel Links { get; set; }

        public IList<StandardModel> Data { get; set; }

        public ABConnectMetadataModelBase Meta { get; set; }

    }

    public class ABConnectEventsResponse : ILearningStandardsApiResponseModel
    {
        public LinksModel Links { get; set; }

        public IList<ABConnectEventsModel> Data { get; set; }

        public ABConnectMetadataModelBase Meta { get; set; }

    }


    #region Common Models


    public class LinksModel
    {
        public string First { get; set; }

        public string Prev { get; set; }

        public string Self { get; set; }

        public string Next { get; set; }

        public string Last { get; set; }
    }

    public class ABConnectDataModelBase
    {
        public Guid Id { get; set; }

        public string Type { get; set; }
    }

    public class ABConnectMetadataModelBase
    {
        public int Count { get; set; }

        public int Offset { get; set; }

        public int Limit { get; set; }

        public int Took { get; set; }
    }

    #endregion


    #region Facets Models


    public class FacetMetadataModel : ABConnectMetadataModelBase
    {
        public IList<FacetModel> Facets { get; set; }
    }

    public class FacetModel
    {
        public int Count { get; set; }
        public string Facet { get; set; }
        public IList<FacetElement> Details { get; set; }

    }

    public class FacetElement
    {
        public FacetData Data { get; set; }
        public int Count { get; set; }
    }

    public class FacetData
    {
        public Guid Guid { get; set; }

        public string Descr { get; set; }

        public string? Code { get; set; }

        [JsonProperty("adopt_year")]
        public int? AdoptYear { get; set; }
    }

    #endregion


    #region Events Models

    public class ABConnectEventsModel : ABConnectDataModelBase
    {
        public ABConnectEventsAttributesModel Relationships { get; set; }

        public ABConnectEventsAttributesModel Attributes { get; set; }

    }

    public class ABConnectEventsRelationshipsModel
    {
        [JsonProperty("deleted_standard")]
        public ABConnectEventsRelationshipsStandardModel DeletedStandard { get; set; }

        [JsonProperty("nondeliverable_standard")]
        public ABConnectEventsRelationshipsStandardModel NondeliverableStandard { get; set; }

        public ABConnectEventsRelationshipsStandardModel Standard { get; set; }
    }

    public class ABConnectEventsRelationshipsStandardModel
    {
        public ABConnectDataModelBase Data { get; set; }
    }

    public class ABConnectEventsAttributesModel
    {
        public int Seq { get; set; }

        [JsonProperty("document_guid")]
        public Guid? DocumentGuid { get; set; }

        public string Target { get; set; }

        [JsonProperty("date_utc")]
        public DateTime? DateUtc { get; set; }

        [JsonProperty("affected_properties")]
        public IList<ABConnectEventsAffectedPropertiesModel> AffectedProperties { get; set; }

        [JsonProperty("section_guid")]
        public Guid? SectionGuid { get; set; }

        [JsonProperty("change_type")]
        public string ChangeType { get; set; }

    }

    public class ABConnectEventsAffectedPropertiesModel
    {
        public string Name { get; set; }

        [JsonProperty("new_value")]
        public string NewValue { get; set; }

        [JsonProperty("previous_value")]
        public string PreviousValue { get; set; }
    }


    #endregion


    #region Standard Model
    public class StandardModel : ABConnectDataModelBase
    {
        [JsonProperty("attributes")]
        public StandardAttributesModel Attributes { get; set; }
    }


    public class StandardAttributesModel
    {
        [JsonProperty("section")]
        public Section Section { get; set; }

        [JsonProperty("statement")]
        public Statement Statement { get; set; }

        [JsonProperty("disciplines")]
        public Disciplines Disciplines { get; set; }

        [JsonProperty("education_levels")]
        public EducationLevels EducationLevels { get; set; }

        [JsonProperty("document")]
        public Document Document { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public class Section
    {
        [JsonProperty("descr")]
        public string Descr { get; set; }
    }

    public class Statement
    {
        [JsonProperty("combined_descr")]
        public string CombinedDescr { get; set; }
    }

    public class Disciplines
    {
        [JsonProperty("subjects")]
        public List<Subject> Subjects { get; set; }
    }

    public class Subject
    {
        [JsonProperty("guid")]
        public string Guid { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("descr")]
        public string Descr { get; set; }
    }

    public class EducationLevels
    {
        [JsonProperty("grades")]
        public List<Grade> Grades { get; set; }
    }

    public class Grade
    {
        [JsonProperty("descr")]
        public string Descr { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("seq")]
        public int Seq { get; set; }

        [JsonProperty("guid")]
        public string Guid { get; set; }
    }

    public class Document
    {
        [JsonProperty("descr")]
        public string Descr { get; set; }

        [JsonProperty("adopt_year")]
        public string AdoptYear { get; set; }

        [JsonProperty("publication")]
        public Publication Publication { get; set; }
    }

    public class Publication
    {
        [JsonProperty("authorities")]
        public List<Authority> Authorities { get; set; }
    }

    public class Authority
    {
        [JsonProperty("descr")]
        public string Descr { get; set; }
    }


    #endregion


}
