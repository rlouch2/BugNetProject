using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Diagnostics;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.BugNetProject
{
    [Serializable]
    /// <summary>
    /// Represents an issue from BugNetProject
    /// </summary>
    internal sealed class BtnetIssue : IssueTrackerIssue
    {

        internal static class DefaultStatusNames
        {
            public static string Open = "Open";
            public static string Reopened = "Verified";
            public static string InProgress = "In Progress";
            public static string Resolved = "Review";
            public static string Closed = "Closed";
        }

        internal string[] AvailableStatusNames { get; private set; }

        //internal string IssueStatusId { get; private set; }

        //public override string IssueStatus
        //{
        //    get { return this.IssueStatus; }
        //}
        //public override string IssueDescription
        //{
        //    get { return this.IssueDescription; }
        //}
        //public override string IssueId
        //{
        //    get { return this.IssueId; }
        //}
        //public override string IssueTitle
        //{
        //    get { return this.IssueTitle; }
        //}
        //public override string ReleaseNumber
        //{
        //    get { return this.ReleaseNumber; }
        //}

        internal bool StatusExists(string status)
        {
            foreach (string availableStatus in AvailableStatusNames)
            {
                if (availableStatus == status) return true;
            }
            return false;
        }

        public override IssueTrackerIssue.RenderMode IssueDescriptionRenderMode
        {
            get
            {
                return IssueTrackerIssue.RenderMode.Html;
            }
        }

        internal BtnetIssue(DataRow dr, string Trackerurl)
            : base(dr["IssueId"].ToString(), dr["StatusName"].ToString(), dr["IssueTitle"].ToString(), dr["IssueDescription"].ToString(), dr["ReleaseNumber"].ToString())
        {
            //Nothing here
        }
    }
}
