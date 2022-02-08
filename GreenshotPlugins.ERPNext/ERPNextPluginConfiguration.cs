using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Greenshot.IniFile;
using GreenshotPlugins.ERPNext.Forms;

namespace GreenshotPlugins.ERPNext
{
    /// <summary>
    /// ERPNext plugin configuration
    /// </summary>
    [IniSection("ERPNext", Description = "Greenshot ERPNext Plugin configuration")]
    public class ERPNextPluginConfiguration : IniSection
    {
        [IniProperty("RefreshToken", Description = "ERPNext refresh Token", Encrypted = true, ExcludeIfNull = true)]
        public string RefreshToken { get; set; }

        /// <summary>
        /// AccessToken, not stored
        /// </summary>
        public string AccessToken { get; set; }

        [IniProperty("AvailableTags", Description = "Tag options for upload", DefaultValue = "invoice_queue")]
        public string AvailableTags { get; set; }

        [IniProperty("InstanceURL", Description = "URL of ERPNext instance")]
        public string InstanceURL { get; set; }

        [IniProperty("ClientID", Description = "OAuth Client ID")]
        public string ClientID { get; set; }

        [IniProperty("CopyLinkToClipboard", Description = "Copy new File's link to clipboard after upload", DefaultValue = "true")]
        public bool CopyLinkToClipboard { get; set; }

        [IniProperty("UploadAsGuest", Description = "Upload as guest rather than logged in account", DefaultValue = "true")]
        public bool UploadAsGuest { get; set; }

        /// <summary>
        /// A form for token
        /// </summary>
        /// <returns>bool true if OK was pressed, false if cancel</returns>
        public bool ShowConfigDialog()
        {
            DialogResult result = new SettingsForm().ShowDialog();

            return result == DialogResult.OK;
        }
    }
}