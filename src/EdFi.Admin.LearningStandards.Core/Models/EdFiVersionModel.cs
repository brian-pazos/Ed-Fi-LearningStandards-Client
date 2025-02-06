using System.Collections.Generic;

namespace EdFi.Admin.LearningStandards.Core.Models
{
    public class EdFiVersionModel
    {
        public EdFiWebApiVersion WebApiVersion { get; }

        public EdFiDataStandardVersion DataStandardVersion { get; }

        public EdFiWebApiInfo WebApiInfo { get; }

        public EdFiVersionModel(EdFiWebApiVersion webApiVersion, EdFiDataStandardVersion dataStandardVersion, EdFiWebApiInfo webApiInfo)
        {
            WebApiVersion = webApiVersion;
            DataStandardVersion = dataStandardVersion;
            WebApiInfo = webApiInfo;
        }

    }

    #region WebApi info models

    public class EdFiWebApiInfo
    {
        public string version { get; set; }

        public string informationalVersion { get; set; }

        public string suite { get; set; }

        public string build { get; set; }

        public IList<EdFiWebApiDataModel> dataModels { get; set; }

        public EdFiWebApiInfoURLs urls { get; set; }

    }

    public class EdFiWebApiDataModel
    {
        public string name { get; set; }

        public string version { get; set; }

        public string informationalVersion { get; set; }
    }

    public class EdFiWebApiInfoURLs
    {
        public string dependencies { get; set; }
        public string openApiMetadata { get; set; }
        public string oauth { get; set; }
        public string dataManagementApi { get; set; }
        public string xsdMetadata { get; set; }
        public string changeQueries { get; set; }
        public string composites { get; set; }
    }


    #endregion


}
