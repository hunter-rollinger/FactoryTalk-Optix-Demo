#region Using directives
using System;
using System.Collections.Generic;
using System.Linq;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using UAManagedCore;
using System.Text;
using System.IO;
using FTOptix.Core;
using System.Net.NetworkInformation;
using FTOptix.SerialPort;
using FTOptix.OPCUAServer;
using FTOptix.System;
using FTOptix.DataLogger;
#endregion

public class UnusedTranslationKeys : BaseNetLogic
{
    /// <summary>
    /// This method executes the SearchLocalizedTextKeys method asynchronously to prevent the UI from freezing.
    /// </summary>
    [ExportMethod]
    public void SearchForUnusedKeys()
    {
        // Execute the code asynchronously to prevent the UI from freezing
        var checkLinks = new LongRunningTask(SearchLocalizedTextKeys, LogicObject);
        checkLinks.Start();
    }

    /// <summary>
    /// This method searches for unused translation keys in the current project.
    /// It retrieves the localization dictionary, extracts the keys and values,
    /// and performs a recursive search in the project.
    /// It generates lists of translations and keys, checks for unused keys,
    /// and saves a report to a file.
    /// </summary>
    private void SearchLocalizedTextKeys()
    {
        Log.Info(LogicObject.BrowseName, "Searching for unused translation keys in the current project, please wait...");

        // Get the project name space index
        projectNameSpaceIndex = Project.Current.NodeId.NamespaceIndex;

        // Get the localization dictionary from the project (based on dynamic link value)
        try
        {
            GetDictionaryFromProject();
        }
        catch (Exception e)
        {
            Log.Error(LogicObject.BrowseName, $"Error while getting the localization dictionary: {e.Message}");
            return;
        }

        // Get the dictionary content (keys and values) and header (languages)
        try
        {
            ExtractValuesFromDictionary();
        }
        catch (Exception e)
        {
            Log.Error(LogicObject.BrowseName, $"Error while extracting values from the localization dictionary: {e.Message}");
            return;
        }

        try
        {
            // Start recursive search in the current project
            foreach (var projectFolder in Project.Current.Children)
            {
                if (projectFolder.Children.Count > 0)
                    RecursiveLocalizedTextSearch(projectFolder);
            }
            if (usedProjectTranslations.Count == 0)
            {
                LogMessage("No translation keys were found in the current project");
                return;
            }
        }
        catch (Exception e)
        {
            Log.Error(LogicObject.BrowseName, $"Error while searching for translation keys in the project: {e.Message}");
            return;
        }

        // Generate lists of translations and keys from dictionary and project
        // and sort them alphabetically. Also, get some statistics about keys
        // such as the number of unused keys in the dictionary and in the project
        try
        {
            GenerateTranslationsLists();
        }
        catch (Exception e)
        {
            Log.Error(LogicObject.BrowseName, $"Error while generating translations lists: {e.Message}");
            return;
        }

        // Print some statistics to the console and append to the report
        PrintStats();

        // Check if some keys are not used in the project and
        // remove them from the dictionary (if enabled)
        try
        {
            ProcessUnusedKeys();
        }
        catch (Exception e)
        {
            Log.Error(LogicObject.BrowseName, $"Error while processing unused keys from localization dictionary: {e.Message}");
            return;
        }

        // Save a report of the keys to a file
        string outPath = LogicObject.GetVariable("ReportPath").Value;
        if (!string.IsNullOrEmpty(outPath))
        {
            try
            {
                SaveReportToFile(outPath);
            }
            catch (Exception e)
            {
                Log.Error(LogicObject.BrowseName, $"Error while saving the report to {outPath}: {e.Message}");
            }
        }
        else
        {
            Log.Warning(LogicObject.BrowseName, $"Report path is not set, no report will be generated");
        }
    }

    /// <summary>
    /// This method performs a recursive search for LocalizedText keys in the project.
    /// It traverses the project structure, checking each node for LocalizedText values
    /// and adding them to the list of used project translations.
    /// </summary>
    /// <param name="startingNode"> The root element from which to recursively search for localized text variables.</param>
    private void RecursiveLocalizedTextSearch(IUANode startingNode)
    {
        foreach (var child in startingNode.Children)
        {
            if (child is IUADataType)
            {
                // Try to get the EnumValues variable to extract the enumeration values
                try
                {
                    foreach (var item in (Struct[])child.GetVariable("EnumValues").Value.Value)
                    {
                        foreach (var value in from value in item.Values
                                              where value is LocalizedText
                                              select value)
                        {
                            AddNewProjectTranslationKey((LocalizedText)value, child);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Verbose2(LogicObject.BrowseName, $"Element {child.BrowseName} cannot be cast to extract enumeration: {e.Message}");
                }
            }

            // Get Description of element
            try
            {
                AddNewProjectTranslationKey(child.Description, child);
            }
            catch (Exception e)
            {
                Log.Verbose2(LogicObject.BrowseName, $"Element {child.BrowseName} has no description: {e.Message}");
            }

            // Get DisplayName of element
            try
            {
                AddNewProjectTranslationKey(child.DisplayName, child);
            }
            catch (Exception e)
            {
                Log.Verbose2(LogicObject.BrowseName, $"Element {child.BrowseName} has no display name: {e.Message}");
            }

            if (child.Children.Count > 0)
            {
                // Recursive call to search for children
                RecursiveLocalizedTextSearch(child);
            }
            else
            {
                // Get variables of type LocalizedText
                try
                {
                    // Try to cast the child to a variable to get the LocalizedText value
                    var localizedTextVariableValue = (LocalizedText)((IUAVariable)child).Value.Value;
                    if (localizedTextVariableValue.HasTextId)
                    {
                        // Add the key to the used keys list
                        AddNewProjectTranslationKey(localizedTextVariableValue, child);
                    }
                }
                catch (Exception e)
                {
                    Log.Verbose2(LogicObject.BrowseName, $"Variable {child.BrowseName} cannot be cast to LocalizedText: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// This method returns a list of keys from a 2D array representing a dictionary.
    /// The keys are extracted from the first column of the array, starting from the second row.
    /// </summary>
    /// <returns>
    /// A list of strings containing the keys from the dictionary.
    /// </returns>
    /// <param name="dictionary">A 2D array representing a dictionary.</param>
    /// <remarks>
    /// The method assumes that the first column of the array contains the keys and
    /// the rows represent entries in the dictionary.
    /// </remarks>
    private static List<string> GetDictionaryKeysList(string[,] dictionary)
    {
        List<string> keys = new List<string>();
        for (int i = 1; i < dictionary.GetLength(0); i++)
        {
            keys.Add(dictionary[i, 0]);
        }
        return keys;
    }

    /// <summary>
    /// This method cleans the dictionary content by removing unused keys.
    /// It creates a new 2D array with the cleaned content and returns it.
    /// </summary>
    /// <param name="dictUsedKeysList">A list of keys that are used in the project.</param>
    /// <param name="dictionaryHeader">A list of dictionary header values (languages).</param>
    /// <param name="dictKeysList">A list of keys from the dictionary.</param>
    /// <param name="dictionaryContent">A 2D array representing the dictionary content.</param>
    /// <returns>
    /// A new 2D array with the cleaned dictionary content.
    /// </returns>
    private static string[,] CleanDictionaryContent(List<string> dictUsedKeysList, List<string> dictionaryHeader, List<string> dictKeysList, string[,] dictionaryContent)
    {
        var newTranslations = new string[dictUsedKeysList.Count + 1, dictionaryHeader.Count];
        // Set dictionary header
        for (int i = 0; i < dictionaryHeader.Count; i++)
        {
            newTranslations[0, i] = dictionaryHeader[i];
        }
        // Set dictionary content
        var rowIndex = 1;
        for (int i = 1; i <= dictKeysList.Count; i++)
        {
            if (dictUsedKeysList.Contains(dictionaryContent[i, 0]))
            {
                for (int k = 0; k < dictionaryHeader.Count; k++)
                {
                    newTranslations[rowIndex, k] = dictionaryContent[i, k];
                }
                ++rowIndex;
            }
        }
        return newTranslations;
    }

    /// <summary>
    /// This method logs a message with a specified severity level (info or warning).
    /// The message is appended to the report content.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="warning">Indicates whether the message is a warning (default is false).</param>
    /// <returns>
    /// The method does not return a value.
    /// </returns>
    private void LogMessage(string message, bool warning = false)
    {
        if (warning)
            Log.Warning(LogicObject.BrowseName, message);
        else
            Log.Info(LogicObject.BrowseName, message);

        reportContent += message + "\n";
    }

    private void AddNewProjectTranslationKey(LocalizedText inputText, IUANode inputNode)
    {
        ProjectTranslation newElement = new(inputText.TextId, LogNodeNoPrefix(inputNode));

        // This is required as some translations comes from the CoreBase namespace
        // or other elements that are not part of the project (like Companion Specification)
        if (inputText.NamespaceIndex == projectNameSpaceIndex)
            usedProjectTranslations.Add(newElement);
        else
            Log.Verbose2(LogicObject.BrowseName, $"Element {LogNodeNoPrefix(inputNode)} has a translation key from another namespace: {inputText.NamespaceIndex}");
    }

    /// <summary>
    /// This method removes the prefix "Root/Objects/{Project.Current.BrowseName}/" from the node path.
    /// </summary>
    /// <param name="node">The node object to process.</param>
    /// <returns>
    /// A string representing the node path without the prefix.
    /// </returns>
    private static string LogNodeNoPrefix(IUANode node)
    {
        return Log.Node(node).Replace($"Root/Objects/{Project.Current.BrowseName}/", "");
    }

    /// <summary>
    /// This method logs various statistics about the project's translation configuration and language usage.
    /// It checks the number of used, unused, and unused dictionary languages, as well as unused project keys,
    /// and logs the results to the log message.
    /// </summary>
    /// <remarks>
    /// The method logs:
    /// - The number of elements in the project with translation keys, along with the number of configured languages
    /// - Unused project languages (if any)
    /// - Unused dictionary languages (if any)
    /// - Unused project keys (if any)
    /// </remarks>
    private void PrintStats()
    {
        LogMessage($"Project contains {usedProjectTranslations.Count} elements with a translation key for {projectLanguages.Length} configured languages, unique translation keys are {distinctUsedProjectKeys.Count}");

        if (unusedProjectLanguages.Count > 0)
            LogMessage($"Project contains {unusedProjectLanguages.Count} languages that are not declared in \"{localizationDictionary.BrowseName}\", list: \"{string.Join("\", \"", unusedProjectLanguages)}\"", true);
        else
            LogMessage($"All project languages are properly declared in \"{localizationDictionary.BrowseName}\"");

        if (unusedDictionaryLanguages.Count > 0)
            LogMessage($"\"{localizationDictionary.BrowseName}\" contains {unusedDictionaryLanguages.Count} languages that are not used in the current project, list: \"{string.Join("\", \"", unusedDictionaryLanguages)}\"", true);
        else
            LogMessage($"All languages in \"{localizationDictionary.BrowseName}\" are properly declared in the current project");

        if (unusedProjectKeysCount > 0)
            LogMessage($"Project contains {unusedProjectKeysCount} keys that are not declared in \"{localizationDictionary.BrowseName}\"", true);
        else
            LogMessage($"All project keys are properly declared in \"{localizationDictionary.BrowseName}\"");
    }

    /// <summary>
    /// This method retrieves the localization dictionary from the project.
    /// It checks if the dictionary is properly configured and exists in the information model.
    /// </summary>
    private void GetDictionaryFromProject()
    {
        // Get the localization dictionary from the NodePointer
        var localizationDictionaryNodeId = LogicObject.GetVariable("LocalizationDictionary").Value;
        if (localizationDictionaryNodeId == null)
        {
            throw new ArgumentException("Localization dictionary variable was not properly configured");
        }
        localizationDictionary = InformationModel.GetVariable(localizationDictionaryNodeId);
        if (localizationDictionary == null || !localizationDictionary.IsInstanceOf(FTOptix.Core.VariableTypes.LocalizationDictionary))
        {
            throw new ArgumentException("Localization dictionary was not found in the information model");
        }
    }

    /// <summary>
    /// This method extracts the values from the localization dictionary.
    /// It retrieves the dictionary content and header (languages) from the dictionary.
    /// </summary>
    private void ExtractValuesFromDictionary()
    {
        // Get the localization dictionary content (bidimensional string array)
        dictionaryContent = (string[,])localizationDictionary.Value.Value;
        if (dictionaryContent.GetLength(0) <= 1)
        {
            throw new ArgumentException("Localization dictionary is empty");
        }

        // Get the list of locales from the dictionary (first row)
        for (int i = 0; i < dictionaryContent.GetLength(1); i++)
        {
            dictionaryHeader.Add(dictionaryContent[0, i]);
        }
    }

    /// <summary>
    /// This method processes unused keys in the localization dictionary.
    /// It checks if there are any unused keys and logs the results.
    /// If the option to remove unused keys is enabled,
    /// it removes them from the dictionary.
    /// </summary>
    private void ProcessUnusedKeys()
    {
        if (unusedDictionaryKeysCount == 0)
        {
            LogMessage($"All keys from \"{localizationDictionary.BrowseName}\" are used in the project");
        }
        else
        {
            LogMessage($"\"{localizationDictionary.BrowseName}\" contains {dictionaryKeysList.Count} keys for {dictionaryHeader.Count - 1} languages, {unusedDictionaryKeysCount} of these keys are unused", true);

            // Remove unused keys from the dictionary if the option is enabled
            if (LogicObject.GetVariable("RemoveUnusedKeys").Value)
            {
                RemoveUnusedKeysFromDictionary();
                LogMessage($"{dictionaryKeysList.Distinct().ToList().Count - distinctUsedProjectKeys.Count} unused keys were successfully removed, \"{localizationDictionary.BrowseName}\" now contains {((string[,])localizationDictionary.Value.Value).GetLength(0) - 1} keys");
            }
            else
            {
                LogMessage("Unused keys were not removed, \"RemoveUnusedKeys\" is set to False");
            }
        }
    }

    /// <summary>
    /// This method saves a report to a specified file path.
    /// It logs the report details and writes the content to the specified file.
    /// </summary>
    /// <param name="outPath">The path where the report will be saved.</param>
    /// <returns>
    /// A reference to the StreamWriter object used to write the report content.
    /// </returns>
    /// <remarks>
    /// The method writes the current date and time, the project name, and various
    /// translation-related details to the file. It also includes information about
    /// languages declared in the project, the dictionary, and unused languages.
    /// </remarks>
    private void SaveReportToFile(string outPath)
    {
        var outUri = new ResourceUri(outPath).Uri;
        Log.Info(LogicObject.BrowseName, $"Saving report to {outUri}");

        using StreamWriter sw = new StreamWriter(outUri);
        sw.WriteLine($"{DateTime.Now} - Translation keys report for {Project.Current.BrowseName}\n");
        sw.Write(reportContent);
        sw.WriteLine("\n\nLanguages declared in the current project:");
        foreach (var lang in projectLanguages)
        {
            sw.WriteLine("- " + lang);
        }
        sw.WriteLine($"\n\nLanguages declared in {localizationDictionary.BrowseName}:");
        foreach (var lang in dictionaryHeader)
        {
            if (!string.IsNullOrEmpty(lang))
                sw.WriteLine("- " + lang);
        }
        sw.WriteLine("\n\nLanguages that are declared in the project locales list but not in the dictionary:");
        foreach (var lang in unusedProjectLanguages)
        {
            sw.WriteLine("- " + lang);
        }
        sw.WriteLine("\n\nLanguages that are declared in the dictionary but not in the project locales list:");
        foreach (var lang in unusedDictionaryLanguages)
        {
            sw.WriteLine("- " + lang);
        }
        sw.WriteLine("\n\nDictionary keys that are used in the current project:");
        foreach (var key in distinctUsedProjectKeys)
        {
            sw.WriteLine("- " + key);
        }
        sw.Write("\n\nDictionary keys that are not used in the current project");
        if (LogicObject.GetVariable("RemoveUnusedKeys").Value)
            sw.Write(" (removed):\n");
        else
            sw.Write(" (not removed):\n");
        foreach (var key in dictionaryKeysList.Except(dictionaryUsedKeysList))
        {
            sw.WriteLine("- " + key);
        }
        sw.WriteLine($"\n\nProject elements with a translation key which does not exist in \"{LogNodeNoPrefix(localizationDictionary)}\":");
        foreach (var key in usedProjectKeysList.Except(dictionaryKeysList))
        {
            var translationElement = usedProjectTranslations.First(x => x.Key == key);
            sw.WriteLine($"- {translationElement.Key} ({translationElement.Path})");
        }
        sw.Close();
    }

    /// <summary>
    /// This method removes unused keys from the localization dictionary.
    /// It updates the localization dictionary with the cleaned content.
    /// </summary>
    private void RemoveUnusedKeysFromDictionary()
    {
        Log.Debug(LogicObject.BrowseName, "Removing unused keys from dictionary");
        // Update the localization dictionary
        var newTranslations = CleanDictionaryContent(dictionaryUsedKeysList, dictionaryHeader, dictionaryKeysList, dictionaryContent);
        localizationDictionary.Value = new UAValue(newTranslations);
    }

    /// <summary>
    /// This method generates lists of translations and keys from the project and dictionary.
    /// It sorts the keys and checks for unused keys in both the project and dictionary.
    /// </summary>
    private void GenerateTranslationsLists()
    {
        // Get used keys list and sort it
        usedProjectTranslations = usedProjectTranslations.Where(x => !string.IsNullOrEmpty(x.Key)).ToList();
        usedProjectKeysList = usedProjectTranslations.Select(x => x.Key).ToList();
        usedProjectKeysList.Sort();
        distinctUsedProjectKeys = usedProjectKeysList.Distinct().ToList();
        // Get dictionary keys list and sort it
        dictionaryKeysList = GetDictionaryKeysList(dictionaryContent);
        dictionaryKeysList = dictionaryKeysList.Where(x => !string.IsNullOrEmpty(x)).ToList();
        dictionaryKeysList.Sort();
        // Get used keys that are also in the dictionary
        dictionaryUsedKeysList = usedProjectKeysList.Intersect(dictionaryKeysList).ToList();
        // Get list of languages from the current project
        projectLanguages = (string[])Project.Current.GetVariable("Localization/Locales").Value.Value;
        // Get list of unused keys from dictionary
        unusedDictionaryKeysCount = dictionaryKeysList.Except(distinctUsedProjectKeys).ToList().Count;
        // Get list of unused keys from the project
        unusedProjectKeysCount = usedProjectKeysList.Except(dictionaryKeysList).ToList().Count;
        // Get list of unused languages
        var cleanedDictionaryHeader = dictionaryHeader.Where(x => !string.IsNullOrEmpty(x));
        unusedProjectLanguages = projectLanguages.Except(cleanedDictionaryHeader).ToList();
        unusedDictionaryLanguages = cleanedDictionaryHeader.Except(projectLanguages).ToList();
    }

    private sealed record class ProjectTranslation(string Key, string Path);

    private string reportContent = "";
    private int projectNameSpaceIndex;
    private List<ProjectTranslation> usedProjectTranslations = new List<ProjectTranslation>();
    private List<string> usedProjectKeysList = new List<string>();
    private List<string> distinctUsedProjectKeys = new List<string>();
    private List<string> dictionaryKeysList = new List<string>();
    private List<string> dictionaryUsedKeysList = new List<string>();
    private string[] projectLanguages;
    private List<string> unusedProjectLanguages = new List<string>();
    private List<string> unusedDictionaryLanguages = new List<string>();
    private int unusedProjectKeysCount;
    private int unusedDictionaryKeysCount;
    private IUAVariable localizationDictionary;
    private readonly List<string> dictionaryHeader = new List<string>();
    private string[,] dictionaryContent;
}
