﻿using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace OfficialPlugins.AnimationChainPlugin.ViewModels
{
    internal class AnimationFrameViewModel : ViewModel
    {
        public AnimationFrameSave BackingModel { get; set; }
        public AnimationChainViewModel Parent { get; private set; }
        public float LengthInSeconds
        {
            get => Get<float>();
            set => Set(value);
        }

        public string StrippedTextureName
        {
            get => Get<string>();
            set => Set(value);
        }

        public float LeftCoordinate
        {
            get => Get<float>();
            set => Set(value);
        }


        public float TopCoordinate
        {
            get => Get<float>();
            set => Set(value);
        }


        public float RightCoordinate
        {
            get => Get<float>();
            set => Set(value);
        }


        public float BottomCoordinate
        {
            get => Get<float>();
            set => Set(value);
        }

        int ResolutionWidth;
        int ResolutionHeight;

        [DependsOn(nameof(LengthInSeconds))]
        public string Text => $"{LengthInSeconds.ToString("0.00")} ({StrippedTextureName})";

        public void SetFrom(AnimationChainViewModel parent, AnimationFrameSave animationFrame, int resolutionWidth, int resolutionHeight)
        {
            BackingModel = animationFrame;
            Parent = parent;
            LengthInSeconds = animationFrame.FrameLength;
            StrippedTextureName = FileManager.RemovePath(FileManager.RemoveExtension(animationFrame.TextureName));

            LeftCoordinate = animationFrame.LeftCoordinate;
            TopCoordinate = animationFrame.TopCoordinate;
            RightCoordinate = animationFrame.RightCoordinate;
            BottomCoordinate = animationFrame.BottomCoordinate;

            ResolutionWidth = resolutionWidth;
            ResolutionHeight = resolutionHeight;


        }
    }
}