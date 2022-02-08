using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using log4net;
using Newtonsoft.Json;


[assembly: PluginAttribute("GreenshotPlugins.ERPNext.ERPNextPlugin", true)]
namespace GreenshotPlugins.ERPNext
{
    public class ERPNextPlugin : IGreenshotPlugin
    {
        public string Name => "ERPNext";
        public bool IsConfigurable => true;

        private static readonly ILog Log = LogManager.GetLogger(typeof(ERPNextPlugin));
        private static ERPNextPluginConfiguration _config;
        private ToolStripMenuItem _itemPlugInConfig;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (_itemPlugInConfig == null) return;
            _itemPlugInConfig.Dispose();
            _itemPlugInConfig = null;
        }

        public bool Initialize(IGreenshotHost host, PluginAttribute pluginAttribute)
        {
            JsonConvert.SerializeObject("{}");

            _config = IniConfig.GetIniSection<ERPNextPluginConfiguration>();

            _itemPlugInConfig = new ToolStripMenuItem
            {
                Text = "Configure ERPNext"
            };
            _itemPlugInConfig.Click += ConfigMenuClick;

            PluginUtils.AddToContextMenu(host, _itemPlugInConfig);

            return true;
        }

        public void Shutdown()
        {
            Log.Debug("ERPNext plugin shutdown.");
        }

        public void ConfigMenuClick(object sender, EventArgs eventArgs)
        {
            Configure();
        }

        public void Configure()
        {
            _config.ShowConfigDialog();
        }
        public IEnumerable<IProcessor> Processors()
        {
            yield break;
        }

        public IEnumerable<IDestination> Destinations()
        {
            yield return new ERPNextDestination();
        }
    }
}
