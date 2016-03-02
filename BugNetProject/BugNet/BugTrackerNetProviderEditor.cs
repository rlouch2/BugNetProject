using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;


namespace Inedo.BuildMasterExtensions.BugNetProject
{
    public sealed class BugNetProviderEditor : ProviderEditorBase
    {
        ValidatingTextBox txtConnectionString;
        ValidatingTextBox txtStatus;
        TextBox txtReleaseNumberCustomField;
        TextBox txtDefectTrackerURL;

        protected override void CreateChildControls()
        {
            //txtConnectionString
            txtConnectionString = new ValidatingTextBox();
            txtConnectionString.Required = true;

            //txtStatus
            txtStatus = new ValidatingTextBox();
            txtStatus.Required = true;

            //txtReleaseNumberCustomField
            txtReleaseNumberCustomField = new TextBox();

            //txtProjectId
            txtDefectTrackerURL = new TextBox();

            CUtil.Add(this,
                new FormFieldGroup("Connection",
                    "A SQL Connection string used to connect to BugNetProject's SQL Database.",
                    false,
                    new StandardFormField("Connection String:", txtConnectionString)
                    )
                , new FormFieldGroup("Configuration",
                    "When an issue's status is equal to the 'Closed Status', that issue will be considered closed."
                    + "<br /><br />The Release Number Field is a custom BugNet field that ties to the BuildMaster release number.",
                    false
                    , new StandardFormField("Release Number Field:", txtReleaseNumberCustomField)
                    , new StandardFormField("Closed Status:", txtStatus)
                    , new StandardFormField("Defect Tracker url", txtDefectTrackerURL)
                    )
            );
        }

        public override void BindToForm(ProviderBase provider)
        {
            EnsureChildControls();
            BugNetProvider btnProvider = (BugNetProvider)provider;
            txtConnectionString.Text = btnProvider.ConnectionString;
            txtStatus.Text = btnProvider.ClosedStatusName;
            txtReleaseNumberCustomField.Text = btnProvider.ReleaseNumberCustomField;
            txtDefectTrackerURL.Text = btnProvider.DefectTrackerURL;
        }

        public override ProviderBase CreateFromForm()
        {
            EnsureChildControls();
            BugNetProvider btnProvider = new BugNetProvider();
            btnProvider.ConnectionString = txtConnectionString.Text;
            btnProvider.ClosedStatusName = txtStatus.Text;
            btnProvider.ReleaseNumberCustomField = txtReleaseNumberCustomField.Text;
            btnProvider.DefectTrackerURL = txtDefectTrackerURL.Text;
            return btnProvider;
        }
    }
}
