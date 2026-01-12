#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.Recipe;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.InfluxDBStoreLocal;
using FTOptix.SQLiteStore;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.OPCUAServer;
using FTOptix.InfluxDBStore;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.IO;
using FTOptix.TwinCAT;
#endregion

public class ProductionStatisticsChart_NetLogic : BaseNetLogic
{
    public override void Start()
    {
        projectPath = (ResourceUri.FromProjectRelativePath("").Uri);
        folderSeparator = Path.DirectorySeparatorChar.ToString();
        templatePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "Production Info Chart" + folderSeparator + "Template-data.js";
        filePath = projectPath + folderSeparator + "eCharts" + folderSeparator + "Production Info Chart" + folderSeparator + "data.js";

        Log.Info("ProductionStatisticsChart_NetLogic", "Project path: " + projectPath);
        Log.Info("ProductionStatisticsChart_NetLogic", "Template path: " + templatePath);
        Log.Info("ProductionStatisticsChart_NetLogic", "File path: " + filePath);

        PeriodicTask RefreshChart = new(UpdateChart, TimeSpan.FromSeconds(3), LogicObject);

        goodValue = LogicObject.Owner.GetVariable("Good_Value");
        badValue = LogicObject.Owner.GetVariable("Bad_Value");
        percentPrecision = LogicObject.Owner.GetVariable("Percent_Precision");
        fontSize = LogicObject.Owner.GetVariable("Font_Size");

        goodValue.VariableChange += VariableChangeEvent;
        badValue.VariableChange += VariableChangeEvent;

        UpdateChart();
    }
    public override void Stop()
    {
        goodValue.VariableChange -= VariableChangeEvent;
        badValue.VariableChange -= VariableChangeEvent;
    }

    private void VariableChangeEvent(object sender, VariableChangeEventArgs e)
    {
        UpdateChart();
    }

    public void UpdateChart()
    {
        // Read template page content
        string text = File.ReadAllText(templatePath);

        text = text.Replace("$1", goodValue.Value);
        text = text.Replace("$2", badValue.Value);
        text = text.Replace("$3", fontSize.Value);
        text = text.Replace("$4", percentPrecision.Value);

        // Write to file
        File.WriteAllText(filePath, text);

        // Refresh WebBrowser page
        Owner.Get<WebBrowser>("ProductionStatisticsChart_Webpage").Refresh();
        Log.Info("ProductionStatisticsChart_NetLogic", "Chart updated with Good Value: " + goodValue.Value + ", Bad Value: " + badValue.Value);
    }

    private string projectPath;
    private string folderSeparator;
    private string templatePath;
    private string filePath;
    private IUAVariable goodValue;
    private IUAVariable badValue;
    private IUAVariable percentPrecision;
    private IUAVariable fontSize;
}
