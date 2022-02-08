using System;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.SelfHost;
using GreenshotPlugin.Controls;
using Greenshot.IniFile;

namespace GreenshotPlugins.ERPNext.Forms
{
    public partial class SettingsForm : GreenshotForm
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ERPNextPlugin));
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox availableTags;
        private System.Windows.Forms.TextBox instanceURL;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox clientID;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox copyLinkOnUpload;
        private System.Windows.Forms.CheckBox uploadAsGuest;
        private System.Windows.Forms.Label loginLabel;
        private System.Windows.Forms.Button loginButton;
        private static readonly ERPNextPluginConfiguration Config = IniConfig.GetIniSection<ERPNextPluginConfiguration>();

        public SettingsForm()
        {
            InitializeComponent();

            AcceptButton = buttonOK;
            CancelButton = buttonCancel;

            if (Config.RefreshToken != String.Empty)
            {
                loginLabel.Text = "Login (Currently Logged In)";
                loginButton.Text = "Login Again to ERPNext";
            }

            availableTags.Text = Config.AvailableTags;
            instanceURL.Text = Config.InstanceURL;
            clientID.Text = Config.ClientID;
            copyLinkOnUpload.Checked = Config.CopyLinkToClipboard;
            uploadAsGuest.Checked = Config.UploadAsGuest;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {

        }

        private async Task<bool> HandleOAuthToken(string code, string state)
        {
            var generatedState = Globals.LastGeneratedOAuthState;

            if(generatedState != state)
            {
                Log.Error($"OAuth state comparison failed. Expected `{generatedState}` got `{state}`");
                return false;
            }

            if(code is null || code == string.Empty)
            {
                Log.Error("OAuth code is missing");
                return false;
            }

            var response = await ERPNextUtils.GetFrappeOAuthTokenResponse(
                instanceURL: instanceURL.Text, 
                clientID: clientID.Text, 
                code: code, 
                redirectURI: Globals.OAuthRedirectUri
            );

            if(response is null)
            {
                Log.Error("Frappe did not return a token response");
                return false;
            }

            if(response.ContainsKey("error")) {
                Log.Error($"Response contained a Frappe error: {response["error"]}");

                return false;
            }

            Log.Info("Successfully received Frappe token response");

            Config.RefreshToken = response["refresh_token"];
            Config.AccessToken = response["access_token"];
            Config.IsDirty = true;
            IniConfig.Save();

            return true;
        }

        private async void CloseOnOAuthRequest(HttpSelfHostServer server)
        {
            Log.Info("Waiting for OAuth code");

            while (Globals.LastOAuthAuthCode == String.Empty || Globals.LastOAuthAuthCode is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            var oauthCode = Globals.LastOAuthAuthCode;
            var oauthState = Globals.LastOAuthState;

            Log.InfoFormat("Received OAuth code with state `{0}`", oauthState);

            await server.CloseAsync();
            await HandleOAuthToken(code: oauthCode, state: oauthState);

            Globals.LastOAuthAuthCode = String.Empty;
            Globals.LastGeneratedOAuthState = String.Empty;

            Close();
        }

        private async void loginButton_Click(object sender, EventArgs e)
        {
            var redirectUri = Globals.OAuthRedirectUri;

            var serverConfig = new HttpSelfHostConfiguration(redirectUri);

            var instanceURL = this.instanceURL.Text;
            var clientID = this.clientID.Text;
            var state = ERPNextUtils.RandomString(13);

            var authUrl = ERPNextUtils.GetAuthorizationUrl(instanceURL: instanceURL, clientID: clientID, state: state, redirectURI: redirectUri);

            Globals.LastGeneratedOAuthState = state;

            System.Diagnostics.Process.Start(authUrl);

            serverConfig.Routes.MapHttpRoute(
                name: "Wildcard", 
                routeTemplate: "{*.}",
                defaults: new { controller = "ERPNextAPI", action = "Index", code = RouteParameter.Optional, state = RouteParameter.Optional }
            );

            try
            {
                Log.Info("Starting ERPNextAPI controller");

                var server = new HttpSelfHostServer(serverConfig);
                await server.OpenAsync();

                CloseOnOAuthRequest(server);
            } catch(Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.availableTags = new System.Windows.Forms.TextBox();
            this.instanceURL = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.clientID = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.copyLinkOnUpload = new System.Windows.Forms.CheckBox();
            this.uploadAsGuest = new System.Windows.Forms.CheckBox();
            this.loginLabel = new System.Windows.Forms.Label();
            this.loginButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(183, 286);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(102, 286);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(170, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Available Tags (comma separated)";
            // 
            // availableTags
            // 
            this.availableTags.Location = new System.Drawing.Point(15, 37);
            this.availableTags.Name = "availableTags";
            this.availableTags.Size = new System.Drawing.Size(233, 20);
            this.availableTags.TabIndex = 3;
            // 
            // instanceURL
            // 
            this.instanceURL.Location = new System.Drawing.Point(15, 92);
            this.instanceURL.Name = "instanceURL";
            this.instanceURL.Size = new System.Drawing.Size(233, 20);
            this.instanceURL.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Instance URL";
            // 
            // clientID
            // 
            this.clientID.Location = new System.Drawing.Point(12, 151);
            this.clientID.Name = "clientID";
            this.clientID.Size = new System.Drawing.Size(236, 20);
            this.clientID.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 135);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "OAuth Client ID";
            // 
            // copyLinkOnUpload
            // 
            this.copyLinkOnUpload.AutoSize = true;
            this.copyLinkOnUpload.Location = new System.Drawing.Point(12, 188);
            this.copyLinkOnUpload.Name = "copyLinkOnUpload";
            this.copyLinkOnUpload.Size = new System.Drawing.Size(125, 17);
            this.copyLinkOnUpload.TabIndex = 8;
            this.copyLinkOnUpload.Text = "Copy Link on Upload";
            this.copyLinkOnUpload.UseVisualStyleBackColor = true;
            // 
            // uploadAsGuest
            // 
            this.uploadAsGuest.AutoSize = true;
            this.uploadAsGuest.Location = new System.Drawing.Point(143, 188);
            this.uploadAsGuest.Name = "uploadAsGuest";
            this.uploadAsGuest.Size = new System.Drawing.Size(105, 17);
            this.uploadAsGuest.TabIndex = 9;
            this.uploadAsGuest.Text = "Upload as Guest";
            this.uploadAsGuest.UseVisualStyleBackColor = true;
            // 
            // loginLabel
            // 
            this.loginLabel.AutoSize = true;
            this.loginLabel.Location = new System.Drawing.Point(9, 222);
            this.loginLabel.Name = "loginLabel";
            this.loginLabel.Size = new System.Drawing.Size(128, 13);
            this.loginLabel.TabIndex = 10;
            this.loginLabel.Text = "Login (currently logged in)";
            // 
            // loginButton
            // 
            this.loginButton.Location = new System.Drawing.Point(12, 238);
            this.loginButton.Name = "loginButton";
            this.loginButton.Size = new System.Drawing.Size(75, 23);
            this.loginButton.TabIndex = 11;
            this.loginButton.Text = "Login";
            this.loginButton.UseVisualStyleBackColor = true;
            this.loginButton.Click += new EventHandler(loginButton_Click);
            // 
            // SettingsForm
            // 
            this.ClientSize = new System.Drawing.Size(270, 321);
            this.Controls.Add(this.loginButton);
            this.Controls.Add(this.loginLabel);
            this.Controls.Add(this.uploadAsGuest);
            this.Controls.Add(this.copyLinkOnUpload);
            this.Controls.Add(this.clientID);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.instanceURL);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.availableTags);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SettingsForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Config.AvailableTags = availableTags.Text;
            Config.InstanceURL = instanceURL.Text;
            Config.ClientID = clientID.Text;
            Config.CopyLinkToClipboard = copyLinkOnUpload.Checked;
            Config.UploadAsGuest = uploadAsGuest.Checked;

            Config.IsDirty = true;
            IniConfig.Save();
        }
    }
}
