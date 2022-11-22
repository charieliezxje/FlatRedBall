﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using EditorObjects.SaveClasses;
using FlatRedBall.IO;
using System.IO;
using EditorObjects.Collections;
using FlatRedBall.IO.Csv;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.Views;

namespace FlatRedBall.Glue.SaveClasses
{
    #region FileProgramAssociation

    public class FileProgramAssociations
    {
        public string Extension;
        public string DefaultProgram;

 
    }

    #endregion

    #region ProjectFileGlueFilePair Class
    public class ProjectFileGlueFilePair
    {
        public string GlueFileName { get; set; }
        public string GameProjectFileName { get; set; }
    }

    #endregion

    public class RecentFileSave
    {
        public string FileName { get; set; }
        public bool IsFavorite { get; set; }
    }

    /// <summary>
    /// Global glue settings for the current user, not tied to any particular project.
    /// </summary>
    public class GlueSettingsSave
    {
        #region Fields

        // Vic says: I think this is in its own file - I don't think we're using it
        //public List<string> AvailablePrograms;          
    
        // This is migrating to be project-specific
        // so that users can set up their tools and not
        // have to worry about others in the project having
        // to manually set them up.
        [XmlElementAttribute("Association")]
        public ExternalSeparatingList<FileProgramAssociations> Associations = new ExternalSeparatingList<FileProgramAssociations>();

        // November 22, 2022
        // DO NOT mark this as obsolete yet.
        // Doing so will make the XmlSerializer
        // ignore this property which will make old paths get lost.
        // Annoying. Can mark this as obsolete when enough time has
        // passed. Perhaps 1 year? Doing so will at worst wipe some recent
        // files for users, but the 1 year period gives enough time to migrate.
        //[Obsolete("Use RecentFileList")]
        public List<string> RecentFiles = new List<string>();

        public List<RecentFileSave> RecentFileList { get; set; } = new List<RecentFileSave>();

        public List<string> BuildTools = new List<string>();

        #endregion

        #region Properties

        public static string SettingsFileName
        {
            get
            {
                if (StopSavesAndLoads)
                    return "";
                
                return FileManager.UserApplicationDataForThisApplication +
                       "settings.xml";
            }
        }

		public string LastProjectFile
		{
			get;
			set;
		}

        public List<ProjectFileGlueFilePair> GlueLocationSpecificLastProjectFiles
        {
            get;
            set;
        } = new List<ProjectFileGlueFilePair>();

        public int WindowLeft { get; set; }
        public int WindowTop { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        public int MainSplitterDistance { get; set; }
        public int StoredRecentFiles { get; set; }

        public List<string> TopTabs { get; set; } = new List<string>();
        public List<string> LeftTabs { get; set; } = new List<string>();
        public List<string> CenterTabs { get; set; } = new List<string>();
        public List<string> RightTabs { get; set; } = new List<string>();
        public List<string> BottomTabs { get; set; } = new List<string>();


        public ExternalSeparatingList<BuildToolAssociation> BuildToolAssociations = new ExternalSeparatingList<BuildToolAssociation>();

        [XmlIgnore]
        public static bool StopSavesAndLoads { get; set; }

        public List<PropertySave> Properties = new List<PropertySave>();


        #endregion

        public void Save()
        {
            if(StopSavesAndLoads) return;

            string directory = FileManager.GetDirectory(SettingsFileName);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Associations.RemoveExternals();
            BuildToolAssociations.RemoveExternals();

            FileManager.XmlSerialize(this, SettingsFileName);

            Associations.ReAddExternals();
            BuildToolAssociations.ReAddExternals();
        }

        public void FixAllTypes()
        {
            foreach (var property in Properties)
            {
                FixAllTypes(property);
            }

            foreach(var recentFileName in RecentFiles)
            {
                RecentFileList.Add(new RecentFileSave
                {
                    FileName = recentFileName,
                });
            }

            RecentFiles.Clear();
        }

        private static void FixAllTypes(PropertySave property)
        {
            if (!string.IsNullOrEmpty(property.Type) && property.Value != null)
            {
                object variableValue = property.Value;
                var type = property.Type;

                variableValue = CustomVariableExtensionMethods.FixValue(variableValue, type);

                property.Value = variableValue;
            }
        }

        public void LoadExternalBuildToolsFromCsv(string csvFileName)
        {
            List<BuildToolAssociation> externals = new List<BuildToolAssociation>();

            CsvFileManager.CsvDeserializeList(typeof(BuildToolAssociation), csvFileName, externals);

            BuildToolAssociations.AddExternalRange(externals);
        }
	}
}
