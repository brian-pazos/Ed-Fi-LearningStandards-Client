using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace EdFi.Admin.LearningStandards.Tests.FromCsv
{
    public static class TestHelpers
    {
        public static string Json(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }

        public static string ReadTestFile(string fileName)
        {
            string filePath = Path.Combine(GetAssemblyPath(), fileName);
            return File.ReadAllText(filePath);
        }

        public static string GetAssemblyPath()
        {
            string codeBase = Assembly.GetExecutingAssembly().Location;
            var uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            return Path.GetDirectoryName(path);
        }

        public static IEnumerable<Dictionary<string, string>> GenerateDataRows(int recordsCount)
        {
            var rows = new List<Dictionary<string, string>>();

            for (int i = 0; i < recordsCount; i++)
            {
                var row = new Dictionary<string, string>
                {
                    { "AcademicSubject", "subject" },
                    { "GradeLevel", "grade" },
                    { "LearningStandardId", $"id{i}" },
                    { "ParentLearningStandardId", null },
                    { "ContentStandard_Title", string.Empty },
                    { "ContentStandard_PublicationStatus", string.Empty },
                    { "ContentStandard_PublicationYear", string.Empty },
                    { "ContentStandard_Author", string.Empty },
                    { "CourseTitle", string.Empty },
                    { "IdCodeItem_ContentStandardName", string.Empty },
                    { "IdCodeItem_IdCode", string.Empty },
                    { "LearningStandardItemCode", string.Empty },
                    { "Description", "description" },
                    { "Namespace", "namespace" },
                    { "URI", string.Empty }
                };
                rows.Add(row);
            }
            return rows;
        }
    }
}
