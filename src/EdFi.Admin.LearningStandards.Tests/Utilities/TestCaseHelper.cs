// SPDX-License-Identifier: Apache-2.0
// Licensed to the Ed-Fi Alliance under one or more agreements.
// The Ed-Fi Alliance licenses this file to you under the Apache License, Version 2.0.
// See the LICENSE and NOTICES files in the project root for more information.

using NUnit.Framework;
using System.IO;

namespace EdFi.Admin.LearningStandards.Tests.Utilities
{
    internal static class TestCaseHelper
    {
        public static string GetTestCaseTextFromFile(string testCaseFileName)
        {
            return File.ReadAllText(
                Path.Combine(
                    TestContext.CurrentContext.WorkDirectory,
                    "TestFiles",
                    testCaseFileName));
        }
    }
}
