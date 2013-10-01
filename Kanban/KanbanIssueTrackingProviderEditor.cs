using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.LeanKit.Kanban
{
    internal sealed class KanbanIssueTrackingProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtReleaseFormat;
        private ValidatingTextBox txtAccount;
        private ValidatingTextBox txtUserName;
        private PasswordTextBox txtPassword;

        public KanbanIssueTrackingProviderEditor()
        {
        }

        public override void BindToForm(ProviderBase extension)
        {
            this.EnsureChildControls();

            var provider = (KanbanIssueTrackingProvider)extension;
            this.txtReleaseFormat.Text = provider.ReleaseNumberTagFormat;
            this.txtAccount.Text = provider.AccountName;
            this.txtUserName.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
        }
        public override ProviderBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new KanbanIssueTrackingProvider
            {
                ReleaseNumberTagFormat = this.txtReleaseFormat.Text,
                AccountName = this.txtAccount.Text,
                UserName = this.txtUserName.Text,
                Password = this.txtPassword.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtReleaseFormat = new ValidatingTextBox
            {
                Width = 300,
                Required = true,
                Text = "rel-%RELNO%"
            };

            this.txtAccount = new ValidatingTextBox
            {
                Width = 300,
                Required = true
            };

            this.txtUserName = new ValidatingTextBox
            {
                Width = 300,
                Required = true
            };

            this.txtPassword = new PasswordTextBox
            {
                Width = 270,
                Required = true
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Release Number",
                    "BuildMaster Releases are tied to Kanban cards using tags. For example, if this field is <i>rel-%RELNO%</i>, any cards with the tag <i>rel-3.2</i> will be associated with Release 3.2 of the application in BuildMaster.",
                    false,
                    new StandardFormField("Release Number Tag Format:", this.txtReleaseFormat)
                ),
                new FormFieldGroup(
                    "LeanKit Account",
                    "Your account name is the first subdomain of your Kanban URL. For example, <i>mycompany</i> in http://mycompany.kanban.com.",
                    false,
                    new StandardFormField("Account Name:", this.txtAccount)
                ),
                new FormFieldGroup(
                    "Authentication",
                    "Provide the user name (normally an email address) and password BuildMaster will use to access your boards.",
                    false,
                    new StandardFormField("User Name:", this.txtUserName),
                    new StandardFormField("Password:", this.txtPassword)
                )
            );
        }
    }
}
