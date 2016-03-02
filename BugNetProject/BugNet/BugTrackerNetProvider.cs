using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Diagnostics;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.BugNetProject
{

    [ProviderProperties(
        "BugNetProject",
        "Supports BugNetProject 1.0 and later; requires that a custom field be added to bugs so that they can be associated with releases.")]
    [CustomEditor(typeof(BugNetProviderEditor))]
    public sealed class BugNetProvider : IssueTrackingProviderBase, ICategoryFilterable, IReleaseNumberCreator, IReleaseNumberCloser
    {
        private string _ConnectionString;
        [Persistent]
        public string ConnectionString
        {
            get { return _ConnectionString; }
            set { _ConnectionString = value; }
        }

        private string _ClosedStatusName = "closed";

        /// <summary>
        /// Gets or sets the name of the status that indicates an issue is closed
        /// </summary>
        [Persistent]
        public string ClosedStatusName
        {
            get { return _ClosedStatusName; }
            set { _ClosedStatusName = value; }
        }

        private string _ReleaseNumberCustomField;
        /// <summary>
        /// Gets or sets the name of the custom field on the bug used to indicate
        /// whether an issue is tied to a particular release. If null or empty,
        /// issues are not tied to a release
        /// </summary>
        [Persistent]
        public string ReleaseNumberCustomField
        {
            get { return _ReleaseNumberCustomField; }
            set { _ReleaseNumberCustomField = value; }
        }

        private string _DefectTrackerURL;
        /// <summary>
        /// Gets or sets the name of the custom field on the bug used to indicate
        /// whether an issue is tied to a particular release. If null or empty,
        /// issues are not tied to a release
        /// </summary>
        [Persistent]
        public string DefectTrackerURL
        {
            get { return _DefectTrackerURL; }
            set { _DefectTrackerURL = value; }
        }

        public string[] CategoryIdFilter { get; set; }

        public string[] CategoryTypeNames
        {
            get { return new[] { "Project" }; }
        }

        #region DataHelper Stuff
        /// <summary>
        /// Creates a <see cref="SqlConnection"/> with a connection string set
        /// </summary>
        /// <param name="storedProcName"></param>
        /// <returns></returns>
        private SqlConnection CreateConnection()
        {
            SqlConnectionStringBuilder conStr = new SqlConnectionStringBuilder(ConnectionString);
            conStr.Pooling = false;

            SqlConnection con = new SqlConnection(conStr.ToString());
            con.InfoMessage += (s, e) => this.LogDebug(e.Message);

            return con;
        }

        /// <summary>
        /// Creates a <see cref="SqlCommand"/> with a connection string set
        /// </summary>
        /// <param name="storedProcName"></param>
        /// <returns></returns>
        private SqlCommand CreateCommand(string cmdText)
        {
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = cmdText;
            cmd.Connection = CreateConnection();
            return cmd;
        }

        /// <summary>
        /// Executes the specified command text 
        /// </summary>
        /// <param name="storedProcName"></param>
        /// <param name="parameters"></param>
        private void ExecuteNonQuery(string cmdText)
        {
            using (SqlCommand cmd = CreateCommand(cmdText))
            {
                try
                {
                    cmd.Connection.Open();
                    cmd.ExecuteNonQuery();
                }
                finally
                {
                    cmd.Connection.Close();
                }
            }
        }

        /// <summary>
        /// Executes the specified command text and returns a datatable as a result
        /// </summary>
        /// <param name="storedProcName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private DataTable ExecuteDataTable(string cmdText)
        {
            return ExecuteDataTable(cmdText, new SqlParameter[0]);
        }

        /// <summary>
        /// Executes the specified command text and returns a datatable as a result
        /// </summary>
        /// <param name="cmdText"></param>
        /// <returns></returns>
        private DataTable ExecuteDataTable(string cmdText, params SqlParameter[] sqlParams)
        {
            DataTable dt = new DataTable();

            using (SqlCommand cmd = CreateCommand(cmdText))
            {
                cmd.Parameters.AddRange(sqlParams);
                try
                {
                    cmd.Connection.Open();
                    dt.Load(cmd.ExecuteReader());
                }
                finally
                {
                    cmd.Connection.Close();
                }
            }
            return dt;
        }
        #endregion

        /// <summary>
        /// Builds a SQL command that, returns either all issues 
        /// (if <see cref="ReleaseNumberCustomField"/> is defined) or issues tagged with
        /// a specific release
        /// </summary>
        /// <param name="releaseNumber"></param>
        /// <returns></returns>
        string BuildGetIssuesSql(string releaseNumber, string ProjectFilter)
        {
            string strSQL = "SELECT iv.IssueId, iv.IssueTitle, iv.IssueDescription, iv.StatusName" +
                    (string.IsNullOrEmpty(ReleaseNumberCustomField)
                        ? string.Empty
                        : string.Format("      ,[{0}] AS [ReleaseNumber]", ReleaseNumberCustomField)
                        ) +
                    "  FROM [BugNet_IssuesView] iv" +
                    " WHERE Disabled = 0" + ProjectFilter +
                    (string.IsNullOrEmpty(ReleaseNumberCustomField) || releaseNumber == null
                        ? string.Empty
                        : string.Format(" AND [{0}] = '{1}'",
                            ReleaseNumberCustomField,
                            releaseNumber.Replace("'", "''"))
                        );
            this.LogDebug(strSQL);

            return strSQL;
        }

        /// <summary>
        /// Builds SQL command to get the list of project from BugNet
        /// </summary>
        /// <returns></returns>
        string BuildGetCategoriesSql()
        {
            return "SELECT ProjectId, ProjectName FROM [BugNet_ProjectsView] WHERE ProjectDisabled = 0 ORDER BY ProjectName";
        }

        /// <summary>
        /// Get Custom Fields by project id
        /// </summary>
        /// <param name="ProjectId"></param>
        /// <returns></returns>
        string GetCustomFieldsByProjectId(string ProjectId)
        {
            string CheckReleaseNumSQL = string.Format("EXEC BugNet_ProjectCustomField_GetCustomFieldsByProjectId {0}", ProjectId);

            return CheckReleaseNumSQL;
        }

        /// <summary>
        /// Get DropDown list values by custom field id
        /// </summary>
        /// <param name="CustomFieldId"></param>
        /// <returns></returns>
        string GetCustomFieldSelectionsByCustomFieldId(string CustomFieldId)
        {
            string CheckReleaseNumSQL = string.Format("EXEC BugNet_ProjectCustomFieldSelection_GetCustomFieldSelectionsByCustomFieldId {0}", CustomFieldId);
            return CheckReleaseNumSQL;
        }

        /// <summary>
        /// Get the releases (Milestones) for the current project.
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        string GetMilestones(string projectId)
        {
            string GetMilestonesSQL = string.Format("EXEC BugNet_ProjectMilestones_GetMilestonesByProjectId {0}, 1", projectId);
            return GetMilestonesSQL;

        }

        /// <summary>
        /// Update Custom Field DropDown list order
        /// </summary>
        /// <param name="CustomFieldSelectionId"></param>
        /// <param name="CustomFieldId"></param>
        /// <param name="releaseNumber"></param>
        /// <returns></returns>
        string UpdateCustomFieldSelection(string CustomFieldSelectionId, string CustomFieldId, string CustomFieldValue)
        {
            string CreateNewCustomFieldSelectionSQL = string.Format("EXEC [BugNet_ProjectCustomFieldSelection_Update] {0}, {1}, '{2}', '{2}', 1", CustomFieldId, CustomFieldValue);
            return CreateNewCustomFieldSelectionSQL;
        }

        string InsertCustomFieldSelections(string CustomFieldId, string CustomFieldValue)
        {
            string CreateNewCustomFieldSelectionSQL = string.Format("EXEC [BugNet_ProjectCustomFieldSelection_CreateNewCustomFieldSelection]  {0}, '{1}', '{1}'", CustomFieldId, CustomFieldValue);
            return CreateNewCustomFieldSelectionSQL;
        }

        /// <summary>
        /// Create new release (Milestone)
        /// </summary>
        /// <param name="ProjectId"></param>
        /// <param name="releaseNumber"></param>
        /// <param name="releaseName"></param>
        /// <returns></returns>
        string CreateMilestoneSQL(string ProjectId, string releaseNumber, string releaseName)
        {
            string CreateMilestoneSQL = string.Format("EXEC [BugNet_ProjectMilestones_CreateNewMilestone] {0}, '{1}', '', '', '', '{2}', 0", ProjectId, releaseNumber, releaseName);
            return CreateMilestoneSQL;
        }

        /// <summary>
        /// Update the release (Milestone)
        /// </summary>
        /// <param name="ProjectId"></param>
        /// <param name="MilestoneId"></param>
        /// <param name="releaseNumber"></param>
        /// <param name="releaseName"></param>
        /// <param name="SortOrder"></param>
        /// <param name="DueDate"></param>
        /// <param name="ReleaseDate"></param>
        /// <param name="MilestoneNotes"></param>
        /// <param name="releaseClosed"></param>
        /// <returns></returns>
        string UpdateMilestoneSQL(string ProjectId, string MilestoneId, string releaseNumber, string releaseName, string SortOrder, string DueDate, string ReleaseDate, string MilestoneNotes, string releaseClosed)
        {
            DueDate = (DueDate.ToUpper() == "NULL") ? DueDate.ToUpper() : "'" + DueDate + "'";
            ReleaseDate = (ReleaseDate.ToUpper() == "NULL") ? ReleaseDate.ToUpper() : "'" + ReleaseDate + "'";

            string UpdateMilestoneSQL = string.Format("EXEC [BugNet_ProjectMilestones_UpdateMilestone] {0}, {1}, '{2}', '', {3}, {4}, {5}, '{6}', {7}", ProjectId, MilestoneId, releaseNumber, SortOrder, DueDate, ReleaseDate, MilestoneNotes, releaseClosed);
            return UpdateMilestoneSQL;
        }

        public IssueTrackerCategory[] GetCategories()
        {
            var categories = new List<BugNetProjectCategory>();

            foreach (DataRow dr in ExecuteDataTable(BuildGetCategoriesSql()).Rows)
            {
                categories.Add(BugNetProjectCategory.CreateProject(dr));
            }

            return categories.ToArray();
        }

        public override IssueTrackerIssue[] GetIssues(string releaseNumber)
        {
            string ProjectFilter = (this.CategoryIdFilter != null && this.CategoryIdFilter.Length > 0)
                ? string.Format(" AND iv.ProjectId = '{0}'", this.CategoryIdFilter[0]) : "";

            List<IssueTrackerIssue> issues = new List<IssueTrackerIssue>();
            foreach (DataRow dr in ExecuteDataTable(BuildGetIssuesSql(releaseNumber, ProjectFilter)).Rows)
            {
                issues.Add(new BtnetIssue(dr, DefectTrackerURL));
            }
            return issues.ToArray();
        }

        public override string GetIssueUrl(IssueTrackerIssue issue)
        {
            //return base.GetIssueUrl(issue);           
            //Check if has trailing "/" for url
            string BaseUrl = (DefectTrackerURL.EndsWith("/")) ? DefectTrackerURL : DefectTrackerURL + "/";

            return BaseUrl +"Issues/IssueDetail.aspx?id=" + issue.IssueId.ToString();
        }

        public void CreateReleaseNumber(string releaseNumber)
        {
            if (string.IsNullOrEmpty(releaseNumber))
                throw new ArgumentNullException("releaseNumber");

            if (this.CategoryIdFilter == null || this.CategoryIdFilter.Length == 0)
                throw new InvalidOperationException("Application must be specified in category ID filter to create a release.");

            string ProjectId = this.CategoryIdFilter[0];
            //This checks if the version number is already created
            DataRow[] MilestonesDR = ExecuteDataTable(GetMilestones(ProjectId)).Select("MilestoneName='" + releaseNumber + "'");

            if (MilestonesDR.Length > 0) //Milestone already exists exit here
            {
                return;
            }

            //Now we can add the version number
            ExecuteNonQuery(CreateMilestoneSQL(ProjectId, releaseNumber, ""));
            //CustomFieldId = CustomFieldIdDR[0]["CustomFieldId"].ToString();
            MilestonesDR = ExecuteDataTable(GetMilestones(ProjectId)).Select("MilestoneName='" + releaseNumber + "'");

            //If version number exists exit
            if (MilestonesDR.Length > 0)
            {
                //Now we need to set the order to the top of the drop down list in BugNet
                string MilestoneId = MilestonesDR[0]["MilestoneId"].ToString();
                string MilestoneName = MilestonesDR[0]["MilestoneName"].ToString();
                string Notes = MilestonesDR[0]["MilestoneNotes"].ToString();
                ExecuteNonQuery(UpdateMilestoneSQL(ProjectId, MilestoneId, releaseNumber, MilestoneName, "1", "NULL", "NULL", Notes, "false"));
            }
        }

        public void CloseReleaseNumber(string releaseNumber)
        {
            if (string.IsNullOrEmpty(releaseNumber))
                throw new ArgumentNullException("releaseNumber");

            if (this.CategoryIdFilter == null || this.CategoryIdFilter.Length == 0)
                throw new InvalidOperationException("Application must be specified in category ID filter to create a release.");

            string ProjectId = this.CategoryIdFilter[0];

            //This checks if the version number is already created
            DataRow[] MilestonesDR = ExecuteDataTable(GetMilestones(ProjectId)).Select("MilestoneName='" + releaseNumber + "'");

            //If version number exists exit we can close it here.
            if (MilestonesDR.Length > 0)
            {
                //Now we need to set the order to the top of the drop down list in BugNet
                string MilestoneId = MilestonesDR[0]["MilestoneId"].ToString();
                string MilestoneName = MilestonesDR[0]["MilestoneName"].ToString();
                string Notes = MilestonesDR[0]["MilestoneNotes"].ToString();
                //string ReleaseDate = MilestonesDR[0]["MilestoneReleaseDate"].ToString();
                string ReleaseDate = DateTime.Now.ToString();
                string DueDate = MilestonesDR[0]["MilestoneDueDate"].ToString();
                string SortOrder = MilestonesDR[0]["SortOrder"].ToString();

                ExecuteNonQuery(UpdateMilestoneSQL(ProjectId, MilestoneId, releaseNumber, MilestoneName, SortOrder, DueDate, ReleaseDate, Notes, "true"));
            }
        }

        public override bool IsIssueClosed(IssueTrackerIssue issue)
        {
            return string.Equals(
                issue.IssueStatus,
                ClosedStatusName,
                StringComparison.InvariantCultureIgnoreCase);
        }

        public override string ToString()
        {
            return "Provides access to a BugNetProject.";
        }

        public override bool IsAvailable()
        {
            return true;
        }

        public override void ValidateConnection()
        {
            try
            {
                GetIssues("0");
            }
            catch (Exception ex)
            {
                throw new NotAvailableException(ex.Message, ex);
            }
        }

    }
}
