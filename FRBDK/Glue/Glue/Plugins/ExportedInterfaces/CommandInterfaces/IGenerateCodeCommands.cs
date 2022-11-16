﻿using FlatRedBall.Glue.SaveClasses;
using System;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IGenerateCodeCommands
    {
        void GenerateAllCode();

        /// <summary>
        /// Generates the current element's code, using
        /// TaskExecutionPreference.AddOrMoveToEnd
        /// </summary>
        void GenerateCurrentElementCode();

        /// <summary>
        /// Generates the argument element's code. Also generates all elements which inherit from this element.
        /// </summary>
        /// <param name="element"></param>
        [Obsolete("Use GenerateElementCodeAsync")]
        void GenerateElementCode(GlueElement element);

        /// <summary>
        /// Generates the argument element's code and all derived element's code. Creates a task or runs immediately if already in a task.
        /// </summary>
        /// <param name="element">The element to generate</param>
        /// <returns>Awaitable task which completes when the generation finishes.</returns>
        Task GenerateElementCodeAsync(GlueElement element);

        void GenerateGlobalContentCode();
        void GenerateGlobalContentCodeTask();

        Task GenerateElementAndReferencedObjectCode(GlueElement element);


        string GetNamespaceForElement(GlueElement element);

        void GenerateCurrentCsvCode();

        void GenerateCustomClassesCode();

        void GenerateStartupScreenCode();

        void GenerateGame1();
    }
}
