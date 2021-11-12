﻿using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin.ViewModels;
using GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin.Views;
using GlueFormsCore.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin
{
    [Export(typeof(PluginBase))]
    public class MainAddScreenPlugin : EmbeddedPlugin
    {
        public override void StartUp()
        {
            AssignEvents();
        }

        private void AssignEvents()
        {
            this.ModifyAddScreenWindow += HandleModifyAddScreenWindow;
            this.NewScreenCreatedWithUi += HandleNewScreenCreatedWithUi;
        }

        private void HandleModifyAddScreenWindow(AddScreenWindow window)
        {
            var optionsView = new AddScreenOptionsView();

            var viewModel = new ViewModels.AddScreenViewModel();

            var gameScreen =
                ObjectFinder.Self.GetScreenSaveUnqualified("GameScreen");
            var hasGameScreen = gameScreen != null;
            viewModel.HasGameScreen = hasGameScreen;

            if(hasGameScreen)
            {
                viewModel.AddScreenType = AddScreenType.LevelScreen;
                viewModel.InheritFromGameScreen = true;

                if(GlueCommands.Self.GluxCommands.StartUpScreenName == gameScreen.Name)
                {
                    // no reason to have the game screen be the startup screen if we are going to have levels
                    viewModel.IsSetAsStartupChecked = true;
                }

                var levelName = "Level1";

                var allScreenNames =
                    GlueState.Self.CurrentGlueProject.Screens
                    .Select(item => item.GetStrippedName())
                    .ToList();

                levelName = StringFunctions.MakeStringUnique(levelName,
                    allScreenNames, 2);

                window.Result = levelName;
            }
            else
            {
                viewModel.AddScreenType = AddScreenType.BaseLevelScreen;
            }

            var allTmxFiles = ObjectFinder.Self.GetAllReferencedFiles()
                .Where(item => FileManager.GetExtension(item.Name) == "tmx")
                .Select(item => item.Name)
                .ToArray();

            viewModel.AvailableTmxFiles.AddRange(allTmxFiles);
            viewModel.SelectedTmxFile = allTmxFiles.FirstOrDefault();

            optionsView.DataContext = viewModel;
            window.AddControl(optionsView);
        }

        private void HandleNewScreenCreatedWithUi(ScreenSave newScreen, AddScreenWindow window)
        {
            var foundControl = window.UserControlChildren.FirstOrDefault(item => item is AddScreenOptionsView);

            var viewModel = foundControl?.DataContext as ViewModels.AddScreenViewModel;

            if(viewModel != null)
            {
                ApplyViewModelToScreen(newScreen, viewModel);
            }
        }

        public static NamedObjectSave AddCollision(ScreenSave screen, string name, bool setFromMapObject = true)
        {
            var addObjectViewModel = new AddObjectViewModel();
            addObjectViewModel.ForcedElementToAddTo = screen;
            addObjectViewModel.SourceType = SourceType.FlatRedBallType;
            addObjectViewModel.SourceClassType = "FlatRedBall.TileCollisions.TileShapeCollection";
            addObjectViewModel.ObjectName = name;

            var nos = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectViewModel, screen, null);
            nos.SetByDerived = true;

            if(setFromMapObject)
            {

                const int FromType = 4;
                nos.Properties.SetValue("CollisionCreationOptions", FromType);
                nos.Properties.SetValue("SourceTmxName", "Map");
                nos.Properties.SetValue("CollisionTileTypeName", name);
            }
            return nos;
        }


        static void AddListsForAllBaseEntities(ScreenSave gameScreen)
        {
            var entitiesForLists = GlueState.Self.CurrentGlueProject.Entities.Where(item => string.IsNullOrWhiteSpace(item.BaseElement))
                .ToArray();

            foreach(var entity in entitiesForLists)
            {
                //var listNos = new NamedObjectSave();

                //listNos.SourceType = SourceType.FlatRedBallType;
                //listNos.SourceClassType = "FlatRedBall.Math.PositionedObjectList<T>";
                //listNos.SourceClassGenericType = entity.Name;

                var addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ForcedElementToAddTo = gameScreen;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                addObjectViewModel.SourceClassType = "FlatRedBall.Math.PositionedObjectList<T>";
                addObjectViewModel.SourceClassGenericType = entity.Name;
                addObjectViewModel.ObjectName = $"{entity.GetStrippedName()}List";

                var nos = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectViewModel, gameScreen, null);
                nos.ExposedInDerived = true;
            }
        }

        private void ApplyViewModelToScreen(ScreenSave newScreen, AddScreenViewModel viewModel)
        {

            var shouldSave = false;
            switch(viewModel.AddScreenType)
            {
                case AddScreenType.BaseLevelScreen:


                    if(viewModel.IsAddMapLayeredTileMapChecked)
                    {
                        AddMapObject(newScreen);

                        shouldSave = true;
                    }
                    if (viewModel.IsAddSolidCollisionShapeCollectionChecked)
                    {
                        AddCollision(newScreen, "SolidCollision");
                        shouldSave = true;
                    }
                    if (viewModel.IsAddCloudCollisionShapeCollectionChecked)
                    {
                        AddCollision(newScreen, "CloudCollision");
                        shouldSave = true;
                    }

                    if(viewModel.IsAddListsForEntitiesChecked)
                    {
                        AddListsForAllBaseEntities(newScreen);
                        shouldSave = true;
                    }

                    break;
                case AddScreenType.LevelScreen:

                    if(viewModel.InheritFromGameScreen)
                    {
                        var gameScreen = ObjectFinder.Self.GetScreenSaveUnqualified("GameScreen");
                        if(gameScreen != null)
                        {
                            newScreen.BaseScreen = gameScreen.Name;
                            GlueCommands.Self.GluxCommands.ElementCommands.UpdateFromBaseType(newScreen);
                            shouldSave = true;

                        }
                    }

                    if(viewModel.IsSetAsStartupChecked)
                    {
                        GlueCommands.Self.GluxCommands.StartUpScreenName = newScreen.Name;
                        //GlueCommands.Self.RefreshCommands.RefreshUi()
                        // meh, eventually we'll fix this:
                    }

                    switch(viewModel.TmxOptions)
                    {
                        case TmxOptions.NewStandardTmx:
                            ShowUiForNewTmx(newScreen);
                            break;
                        case TmxOptions.CopiedTmx:
                            var originalFile = GlueCommands.Self.GetAbsoluteFilePath(viewModel.SelectedTmxFile);

                            var destinationFolder = GlueCommands.Self.FileCommands.GetContentFolder(newScreen);
                            var strippedName = newScreen.GetStrippedName() + "Map";
                            FilePath destinationFile = new FilePath(destinationFolder + strippedName + ".tmx");

                            if(originalFile.Exists())
                            {
                                try
                                {
                                    GlueCommands.Self.TryMultipleTimes(() =>
                                    {
                                        System.IO.Directory.CreateDirectory(destinationFile.GetDirectoryContainingThis().FullPath);
                                        System.IO.File.Copy(originalFile.FullPath, destinationFile.FullPath);
                                    });
                                }
                                catch(Exception e)
                                {
                                    GlueCommands.Self.PrintError($"Error copying TMX:\n{e}");
                                }
                            }

                            if(destinationFile.Exists())
                            {
                                var newRfs = GlueCommands.Self.GluxCommands.CreateReferencedFileSaveForExistingFile(newScreen, destinationFile);

                                if (newRfs != null)
                                {
                                    UpdateMapObjectToNewTmx(newScreen, newRfs);
                                }
                            }


                            break;
                            // don't do anything for the other options
                    }
                    break;
            }

            if (shouldSave)
            {
                GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(newScreen);
                GlueCommands.Self.GluxCommands.SaveGlux();
            }
        }

        public static void AddMapObject(ScreenSave newScreen)
        {
            var addObjectViewModel = new AddObjectViewModel();
            addObjectViewModel.ForcedElementToAddTo = newScreen;
            addObjectViewModel.SourceType = SourceType.FlatRedBallType;
            addObjectViewModel.SourceClassType = "FlatRedBall.TileGraphics.LayeredTileMap";
            addObjectViewModel.ObjectName = "Map";

            var nos = GlueCommands.Self.GluxCommands.AddNewNamedObjectTo(addObjectViewModel, newScreen, null);
            nos.SetByDerived = true;
            nos.SetVariable("CreateEntitiesFromTiles", true);
        }

        private static void ShowUiForNewTmx(ScreenSave newScreen)
        {
            // add standard TMX....how?
            // Currently there's no way to run
            // logic in plugins, so we'll just show the window and select the 
            // TMX option:
            var addNewFileViewModel = new AddNewFileViewModel();
            var tmxAti = 
                AvailableAssetTypes.Self.GetAssetTypeFromExtension("tmx");
            addNewFileViewModel.SelectedAssetTypeInfo = tmxAti;
            addNewFileViewModel.ForcedType = tmxAti;
            addNewFileViewModel.FileName = newScreen.GetStrippedName() + "Map";

            var newRfs = GlueCommands.Self.DialogCommands.ShowAddNewFileDialog(addNewFileViewModel);

            if (newRfs != null)
            {
                UpdateMapObjectToNewTmx(newScreen, newRfs);
            }
        }

        private static void UpdateMapObjectToNewTmx(ScreenSave newScreen, ReferencedFileSave newRfs)
        {
            var mapObject = newScreen.NamedObjects
                .FirstOrDefault(item => item.InstanceName == "Map" && item.GetAssetTypeInfo().FriendlyName.StartsWith("LayeredTileMap"));
            if (mapObject != null)
            {
                mapObject.SourceType = SourceType.File;
                mapObject.SourceFile = newRfs.Name;
                mapObject.SourceName = "Entire File (LayeredTileMap)";
            }
        }
    }
}
