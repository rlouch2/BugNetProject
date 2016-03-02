using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Inedo.Web.Controls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.BuildMaster.Diagnostics;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.BugNetProject
{
    [Serializable]
    internal sealed class BugNetProjectCategory : IssueTrackerCategory
    {
        public enum CategoryTypes
        {
            Project
        }

        public CategoryTypes CategoryType { get; private set; }

        private BugNetProjectCategory(string categoryId, string categoryName, CategoryTypes categoryType)
            : base(categoryId, categoryName, null)
        {
            this.CategoryType = categoryType;
        }

        internal static BugNetProjectCategory CreateProject(DataRow projectInfo)
        {
            return new BugNetProjectCategory(
                projectInfo["ProjectId"].ToString(),
                projectInfo["ProjectName"].ToString(),
                CategoryTypes.Project
                );
        }
    }
}
