﻿using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace OfficialPlugins.ErrorReportingPlugin
{
    [Export(typeof(PluginBase))]
    public class MainErrorReportingPlugin : PluginBase
    {
        public override string FriendlyName => "Error reporting plugin";

        public override Version Version => new Version(1,0);

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AddErrorReporter(new MainErrorReporter());

            this.ReactToFileChangeHandler += HandleFileChanged;
        }

        private void HandleFileChanged(string fileName)
        {
            this.RefreshErrors();
        }
    }
}
