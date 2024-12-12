using EdFi.Admin.LearningStandards.Core;

namespace EdFi.Admin.LearningStandards.Tests.Utilities
{
    internal class ABConnectApiFileBasedTestCases
    {
        public static string ValidApiResponse_LearningStandards()
        {
            return TestCaseHelper.GetTestCaseTextFromFile("ABConnectApiResponses/learning-standards-response.json");
        }

        public static string ValidApiResponse_Facets()
        {
            return TestCaseHelper.GetTestCaseTextFromFile("ABConnectApiResponses/subjects-gradeLevels-response.json");
        }

        public static string ValidApiResponse_EventsSingleResponse()
        {
            return TestCaseHelper.GetTestCaseTextFromFile("ABConnectApiResponses/events-single-response.json");
        }

        public static object[] ValidApiResponse_DescriptorsTestCases()
        {
            return new object[]
                   {
                           new object[]
                           {
                               "standards",
                               EdFiOdsApiCompatibilityVersion.v3,
                               3,
                               TestCaseHelper.GetTestCaseTextFromFile("ABConnectApiResponses/subjects-gradeLevels-response.json")
                           },
                   };
        }

        public static object[] ValidApiResponse_SegmentsChangesTestCases()
        {
            return new object[]
                   {
                           new object[]
                           {
                               "events",
                               EdFiOdsApiCompatibilityVersion.v3,
                               3,
                               TestCaseHelper.GetTestCaseTextFromFile("ABConnectApiResponses/events-response.json")
                           },
                   };
        }

        public static object[] ValidApiResponse_LearningStandardsTestCases()
        {
            return new object[]
                   {
                           new object[]
                           {
                               "standards",
                               EdFiOdsApiCompatibilityVersion.v3,
                               3,
                               TestCaseHelper.GetTestCaseTextFromFile("ABConnectApiResponses/learning-standards-response.json")
                           },
                   };
        }
    }
}
