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
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EdFi.Admin.LearningStandards.Tests.IntegrationTests
{
    [TestFixture]
    [Category("Interactive")]
    public class OdsApiInteractiveTests
    {
        [TestFixture]
        [Ignore("These tests are for interactive local use only. Comment out this line to use.")]
        public class V2
        {
            // Steps to test on v2:
            // 0) Checkout development-v2 on ODS,Impl,Common repos, initdev.
            // 1) Run AB Vendor claimset insert sql into Security DB
            // 2) Run Certica Test Vendor key/secret generator sql v2 (in this folder) against Admin Db => Record Key secret generated.
            // 3) Rename minimal template database to be [EdFi_Ods_Sandbox_<generated_key_here>]
            // 4) Set key and secret variables in this class from generated values.
            // 5) Launch WebApi project in debug in VS2015 session.

            private const string Key = "ABABC5C932B84A3A";
            private const string Secret = "29AF3D060A3A";

            // TODO: Move these to work of the v2 files like v3 does below.
            [TestCase(
                @"{""Schema"":""ed-fi"", ""Resource"":""academicSubjectDescriptors"", ""Operation"":""Upsert"", ""Data"":[{""id"":null,""academicSubjectDescriptorId"":null,""academicSubjectType"":""English Language Arts"",""codeValue"":""F1FAC302-3B53-11E0-B042-495E9DFF4B22"",""description"":""Language Arts"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""namespace"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""LANG"",""_etag"":null},{""id"":null,""academicSubjectDescriptorId"":null,""academicSubjectType"":""Mathematics"",""codeValue"":""F1FB2F2C-3B53-11E0-B042-495E9DFF4B22"",""description"":""Mathematics"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""namespace"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""MATH"",""_etag"":null},{""id"":null,""academicSubjectDescriptorId"":null,""academicSubjectType"":""Social Studies"",""codeValue"":""F1FB4B38-3B53-11E0-B042-495E9DFF4B22"",""description"":""Social Studies"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""namespace"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""SOC"",""_etag"":null},{""id"":null,""academicSubjectDescriptorId"":null,""academicSubjectType"":""Science"",""codeValue"":""F1FB3DD2-3B53-11E0-B042-495E9DFF4B22"",""description"":""Science"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""namespace"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""SCI"",""_etag"":null},{""id"":null,""academicSubjectDescriptorId"":null,""academicSubjectType"":""Fine and Performing Arts"",""codeValue"":""F1FB8062-3B53-11E0-B042-495E9DFF4B22"",""description"":""The Arts"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""namespace"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""ARTS"",""_etag"":null},{""id"":null,""academicSubjectDescriptorId"":null,""academicSubjectType"":""Physical, Health, and Safety Education"",""codeValue"":""F1FB589E-3B53-11E0-B042-495E9DFF4B22"",""description"":""Health Education"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""namespace"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""HEAL"",""_etag"":null},{""id"":null,""academicSubjectDescriptorId"":null,""academicSubjectType"":""Foreign Language and Literature"",""codeValue"":""F1FB7338-3B53-11E0-B042-495E9DFF4B22"",""description"":""Foreign Language"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""namespace"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""FORN"",""_etag"":null},{""id"":null,""academicSubjectDescriptorId"":null,""academicSubjectType"":""Other"",""codeValue"":""F1FB8DD2-3B53-11E0-B042-495E9DFF4B22"",""description"":""Technology"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""namespace"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""TECH"",""_etag"":null}]}")]
            [TestCase(
                @"{""Schema"":""ed-fi"",""Resource"":""gradeLevelDescriptors"",""Operation"":""Upsert"",""Data"":[{""id"":null,""codeValue"":""F1FA7154-3B53-11E0-B042-495E9DFF4B22"",""description"":""9th Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Ninth grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""9"",""_etag"":null},{""id"":null,""codeValue"":""F1FA7E92-3B53-11E0-B042-495E9DFF4B22"",""description"":""10th Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Tenth grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""10"",""_etag"":null},{""id"":null,""codeValue"":""F1FA8BD0-3B53-11E0-B042-495E9DFF4B22"",""description"":""11th Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Eleventh grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""11"",""_etag"":null},{""id"":null,""codeValue"":""F1FA9904-3B53-11E0-B042-495E9DFF4B22"",""description"":""12th Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Twelfth grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""12"",""_etag"":null},{""id"":null,""codeValue"":""F1FA49AE-3B53-11E0-B042-495E9DFF4B22"",""description"":""6th Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Sixth grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""6"",""_etag"":null},{""id"":null,""codeValue"":""F1FA56F6-3B53-11E0-B042-495E9DFF4B22"",""description"":""7th Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Seventh grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""7"",""_etag"":null},{""id"":null,""codeValue"":""F1FA642A-3B53-11E0-B042-495E9DFF4B22"",""description"":""8th Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Eighth grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""8"",""_etag"":null},{""id"":null,""codeValue"":""F1FA221C-3B53-11E0-B042-495E9DFF4B22"",""description"":""3rd Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Third grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""3"",""_etag"":null},{""id"":null,""codeValue"":""F1FA3C84-3B53-11E0-B042-495E9DFF4B22"",""description"":""5th Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Fifth grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""5"",""_etag"":null},{""id"":null,""codeValue"":""F1FA2F50-3B53-11E0-B042-495E9DFF4B22"",""description"":""4th Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Fourth grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""4"",""_etag"":null},{""id"":null,""codeValue"":""F1FA078C-3B53-11E0-B042-495E9DFF4B22"",""description"":""1st Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""First grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""1"",""_etag"":null},{""id"":null,""codeValue"":""F1FA14D4-3B53-11E0-B042-495E9DFF4B22"",""description"":""2nd Grade"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Second grade"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""2"",""_etag"":null},{""id"":null,""codeValue"":""F1F9FA12-3B53-11E0-B042-495E9DFF4B22"",""description"":""Kindergarten"",""effectiveBeginDate"":null,""effectiveEndDate"":null,""gradeLevelDescriptorId"":null,""gradeLevelType"":""Kindergarten"",""namespace"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml"",""priorDescriptorId"":null,""shortDescription"":""K"",""_etag"":null}]}")]
            [TestCase(
                @"{""Schema"":""ed-fi"",""Resource"":""learningStandards"",""Operation"":""Upsert"",""Data"":[{""id"":null,""parentLearningStandardReference"":{""learningStandardId"":null,""link"":null},""academicSubjectDescriptor"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml/F1FB4B38-3B53-11E0-B042-495E9DFF4B22"",""courseTitle"":""Grades: K-12"",""description"":""Grades: K-12"",""learningStandardId"":""321B3A6E-297E-11DE-BC16-22199DFF4B22"",""itemCode"":null,""namespace"":""http://academicbenchmarks.com/LearningStandards.xml"",""successCriteria"":null,""uri"":null,""contentStandard"":{""mandatingEducationOrganizationReference"":null,""publicationStatusType"":""Adopted"",""title"":""Social Studies"",""version"":null,""uri"":null,""publicationDate"":null,""publicationYear"":2009,""beginDate"":null,""endDate"":null,""authors"":[]},""gradeLevels"":[{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1F9FA12-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA078C-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA14D4-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA221C-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA2F50-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA3C84-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA49AE-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA56F6-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA642A-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA7154-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA7E92-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA8BD0-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA9904-3B53-11E0-B042-495E9DFF4B22""}],""identificationCodes"":[{""identificationCode"":""3F7B6486-297E-11DE-BC16-22199DFF4B22:00000001"",""contentStandardName"":""AB Sequence""}],""prerequisiteLearningStandards"":null,""_etag"":null},{""id"":null,""parentLearningStandardReference"":{""learningStandardId"":""321B3A6E-297E-11DE-BC16-22199DFF4B22"",""link"":null},""academicSubjectDescriptor"":""http://academicbenchmarks.com/AcademicSubjectDescriptor.xml/F1FB4B38-3B53-11E0-B042-495E9DFF4B22"",""courseTitle"":""Grades: K-12"",""description"":""Grades: K-12"",""learningStandardId"":""321B97C0-297E-11DE-BC16-22199DFF4B22"",""itemCode"":null,""namespace"":""http://academicbenchmarks.com/LearningStandards.xml"",""successCriteria"":null,""uri"":null,""contentStandard"":{""mandatingEducationOrganizationReference"":null,""publicationStatusType"":""Adopted"",""title"":""Social Studies"",""version"":null,""uri"":null,""publicationDate"":null,""publicationYear"":2009,""beginDate"":null,""endDate"":null,""authors"":[]},""gradeLevels"":[{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1F9FA12-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA078C-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA14D4-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA221C-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA2F50-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA3C84-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA49AE-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA56F6-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA642A-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA7154-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA7E92-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA8BD0-3B53-11E0-B042-495E9DFF4B22""},{""gradeLevelDescriptor"":""http://academicbenchmarks.com/GradeLevelDescriptor.xml/F1FA9904-3B53-11E0-B042-495E9DFF4B22""}],""identificationCodes"":[{""identificationCode"":""3F7B6486-297E-11DE-BC16-22199DFF4B22:00000002"",""contentStandardName"":""AB Sequence""}],""prerequisiteLearningStandards"":null,""_etag"":null}]}")]
            public async Task When_posting_edfi_v2_bulkJson(string bulkJson)
            {
                var edfiBulkJson = JsonConvert.DeserializeObject<EdFiBulkJsonModel>(bulkJson);

                var httpClient = new HttpClient();

                var config = new EdFiOdsApiConfiguration(
                    "http://localhost:54746/",
                    EdFiOdsApiCompatibilityVersion.v2,
                    new AuthenticationConfiguration(Key, Secret),
                    2018);

                var authTokenManager = new EdFiOdsApiv2AuthTokenManager(
                    config,
                    httpClient,
                    new NUnitConsoleLogger<EdFiOdsApiv2AuthTokenManager>());

                var versionManager = new Mock<IEdFiVersionManager>();
                versionManager.Setup(x => x.GetEdFiVersion(config, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => new EdFiVersionModel(
                                                EdFiWebApiVersion.v2x,
                                                EdFiDataStandardVersion.DS5_2,
                                                new EdFiWebApiInfo()
                                                )
                    );

                var results = await new EdFiBulkJsonPersister(
                    config,
                    versionManager.Object,
                    authTokenManager,
                    new NUnitConsoleLogger<EdFiBulkJsonPersister>(),
                    httpClient).PostEdFiBulkJson(edfiBulkJson).ConfigureAwait(false);

                foreach (IResponse response in results)
                {
                    Assert.True(response.IsSuccess);
                }
            }
        }

        [TestFixture]
        [Ignore("These tests are for interactive local use only. Comment out this line to use.")]
        public class V3
        {
            // Steps to test on v2:
            // 0) Checkout development-v3 on ODS,Impl,Common repos, initdev.
            // 1) Run build in VS2015
            // 2) Run initdev -noCompile
            // 3) Run AB Vendor claimset insert sql for v3 into Security DB
            // 4) Run Certica Test Vendor key/secret generator sql (in this folder) against Admin Db => Record Key secret generated.
            // 5) Rename minimal template database to be [EdFi_Ods_Sandbox_<generated_key_here>]
            // 6) Set key and secret variables in this class from generated values.
            // 7) Launch WebApi project in debug in VS2015 session.

            private const string Key = "3A88849ECFE94F82";
            private const string Secret = "F855FB294190";


            [TestCaseSource(typeof(V3), nameof(GetTestCases))]
            public async Task When_posting_edfi_v3_bulkJson(string bulkJson)
            {
                var edfiBulkJson = JsonConvert.DeserializeObject<EdFiBulkJsonModel>(bulkJson);

                var httpClient = new HttpClient();

                var config = new EdFiOdsApiConfiguration(
                    "http://localhost:60199/",
                    EdFiOdsApiCompatibilityVersion.v3,
                    new AuthenticationConfiguration(Key, Secret));

                var versionManager = new Mock<IEdFiVersionManager>();
                versionManager.Setup(x => x.GetEdFiVersion(config, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() => new EdFiVersionModel(
                                                EdFiWebApiVersion.v7x,
                                                EdFiDataStandardVersion.DS5_2,
                                                new EdFiWebApiInfo()
                                                )
                    );

                var authTokenManager = new EdFiOdsApiv3AuthTokenManager(
                    config,
                    versionManager.Object,
                    httpClient,
                    new NUnitConsoleLogger<EdFiOdsApiv3AuthTokenManager>());

                var results = await new EdFiBulkJsonPersister(
                    config,
                    versionManager.Object,
                    authTokenManager,
                    new NUnitConsoleLogger<EdFiBulkJsonPersister>(),
                    httpClient).PostEdFiBulkJson(edfiBulkJson).ConfigureAwait(false);

                foreach (IResponse response in results)
                {
                    Assert.True(response.IsSuccess);
                }
            }

            private static object[] GetTestCases()
            {
                return new TestCaseData[]
                       {
                           new TestCaseData(

                               JToken.Parse(
                                         TestCaseHelper.GetTestCaseTextFromFile(
                                             "Valid-Descriptors-v3.txt"))
                                     .First.ToString()),
                           // This is a workaround to get the second set since there are 3.
                           new TestCaseData(
                               JToken.Parse(
                                         TestCaseHelper.GetTestCaseTextFromFile(
                                             "Valid-Descriptors-v3.txt"))
                                     .First.Next
                                     .ToString()
                           ),
                           new TestCaseData(
                               JToken.Parse(
                                         TestCaseHelper.GetTestCaseTextFromFile(
                                             "Valid-Descriptors-v3.txt"))
                                     .Last.ToString()
                           ),
                           new TestCaseData(
                               JToken.Parse(
                                         TestCaseHelper.GetTestCaseTextFromFile(
                                             "Valid-Sync-v3.txt"))
                                     .First.ToString()
                           ),
                       };
            }

        }
    }
}
