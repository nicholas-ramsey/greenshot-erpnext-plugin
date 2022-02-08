using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using Greenshot.Plugin;

namespace GreenshotPlugins.ERPNext
{
    public class ERPNextTagDestination : ERPNextDestination
    {
        string Tag { set; get; }

        public override bool IsDynamic => false;

        public ERPNextTagDestination(string tag)
        {
            Tag = tag;
        }

        public override string Description
        {
            get
            {
                return Tag;
            }
        }

        public override string Designation
        {
            get
            {
                return "ERPNext";
            }
        }

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            Log.InfoFormat("Starting export of image to ERPNext with tag `{0}`", Tag);

            var exportInformation = new ExportInformation(Designation, Description);
            
            if (string.IsNullOrEmpty(Config.InstanceURL) || string.IsNullOrEmpty(Config.ClientID))
            {
                Log.Info("InstanceURL or ClientID is not configured. Rejecting export.");

                MessageBox.Show($"Please configure your ERPNext OAuth client ID and instance URL.");

                exportInformation.ExportMade = false;
                exportInformation.ErrorMessage = "Instance URL or client ID is nullish";

                return exportInformation;
            }

            var fileName = FilenameHelper.GetFilenameFromPattern(CoreConfig.OutputFileFilenamePattern, CoreConfig.OutputFileFormat, captureDetails);
            var outputSettings = new SurfaceOutputSettings(CoreConfig.OutputFileFormat, CoreConfig.OutputFileJpegQuality, CoreConfig.OutputFileReduceColors);

            // Run upload in background
            try
            {
                new PleaseWaitForm().ShowAndWait(
                    "ERPNext Upload",
                    "Communicating with ERPNext. Please wait...",
                    delegate
                    {
                        // Always refreshing the access token, so as to save myself
                        // a few minutes of dev time (e.g. laziness)
                        var refreshAccessTokenTask = ERPNextUtils.RefreshOAuthToken(
                            instanceURL: Config.InstanceURL,
                            refreshToken: Config.RefreshToken,
                            clientID: Config.ClientID,
                            redirectURI: Globals.OAuthRedirectUri
                        );
                        refreshAccessTokenTask.Wait();

                        if (!refreshAccessTokenTask.Result.ContainsKey("access_token"))
                        {
                            exportInformation.ExportMade = false;
                            exportInformation.ErrorMessage = "Could not refresh access token. Please re-login under ERPNext plugin's config";

                            return;
                        }

                        var accessToken = refreshAccessTokenTask.Result["access_token"];

                        Config.AccessToken = accessToken;
                        Config.IsDirty = true;
                        IniConfig.Save();

                        Log.InfoFormat("Saved access token: {0}", accessToken);
                        Log.Info("Refreshed ERPNext tokens");

                        var uploadTask = ERPNextUtils.UploadImage(
                            instanceURL: Config.InstanceURL,
                            accessToken: Config.UploadAsGuest ? string.Empty : accessToken,
                            fileName: fileName,
                            image: surface,
                            outputSettings: outputSettings
                        );
                        uploadTask.Wait();

                        Dictionary<string, Dictionary<string, string>> uploadResponse = uploadTask.Result;

                        var message = uploadResponse["message"];
                        var docname = message["name"];
                        var doctype = message["doctype"];

                        var docURL = ERPNextUtils.BuildDocURL(Config.InstanceURL, doctype, docname);

                        Log.InfoFormat("Uploaded to {0}", docURL);

                        var addTagTask = ERPNextUtils.AddTagToFile(
                            instanceURL: Config.InstanceURL,
                            accessToken: accessToken,
                            tag: Tag,
                            docname: docname,
                            doctype: doctype
                        );
                        addTagTask.Wait();

                        Log.InfoFormat("Tagged {0} with {1}", docURL, Tag);

                        if (!string.IsNullOrEmpty(docURL) && Config.CopyLinkToClipboard)
                        {
                            try
                            {
                                ClipboardHelper.SetClipboardData(docURL);
                            }
                            catch (Exception ex)
                            {
                                Log.Error("Can't write to clipboard: ", ex);
                                docURL = null;
                            }
                        }

                        exportInformation.ExportMade = true;
                        exportInformation.Uri = docURL;
                    }
                );
            }
            catch (Exception ex)
            {
                Log.Error("Error uploading.", ex);
                MessageBox.Show($"Upload failed with error: {ex.Message}");

                exportInformation.ExportMade = false;
            }

            ProcessExport(exportInformation, surface);
            return exportInformation;
        }
    }

    public class ERPNextDestination : AbstractDestination
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ERPNextPluginConfiguration));
        protected static readonly ERPNextPluginConfiguration Config = IniConfig.GetIniSection<ERPNextPluginConfiguration>();
        protected static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

        public override bool IsDynamic => true;
        public override bool UseDynamicsOnly => true;

        public override IEnumerable<IDestination> DynamicDestinations()
        {
            foreach (var tag in Config.AvailableTags.Split(','))
            {
                var tagLabel = tag.Trim();

                var dest = new ERPNextTagDestination(tagLabel);

                yield return dest;
            }
        }

        public override string Description
        {
            get
            {
                return "Upload to ERPNext";
            }
        }

        public override string Designation
        {
            get
            {
                return "ERPNext";
            }
        }

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            return null;
        }
    }
}
