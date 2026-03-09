#region Using directives
using System;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.NetLogic;
using FTOptix.Core;
using System.IO;
using System.Diagnostics;
using FTOptix.RecipeX;
using FTOptix.SerialPort;
using FTOptix.OPCUAServer;
using FTOptix.System;
using FTOptix.HMIProject;
using FTOptix.DataLogger;
#endregion

public class ProductionStatisticsChart_NetLogic : BaseNetLogic
{
	public override void Start()
	{
		projectPath = ResourceUri.FromProjectRelativePath("").Uri;
		chartFolder = Path.Combine(projectPath, "eCharts", "Production Info Chart");
		sourcePath = Path.Combine(chartFolder, "source-chart.js");
		destPath = Path.Combine(chartFolder, "data.js");
	
		Log.Verbose2("ProductionStatisticsChart_NetLogic", "Data file path: " + destPath);

		PeriodicTask RefreshChart = new(UpdateChart, TimeSpan.FromSeconds(10), LogicObject);

		innerValue = LogicObject.Owner.GetVariable("Inner_Value");
		innerColor = LogicObject.Owner.GetVariable("Inner_Color");
		innerBackground = LogicObject.Owner.GetVariable("Inner_Background_Color");
		outerValue = LogicObject.Owner.GetVariable("Outer_Value");
		outerColor = LogicObject.Owner.GetVariable("Outer_Color");
		outerBackground = LogicObject.Owner.GetVariable("Outer_Background_Color");
		screenSizeIsNormal = LogicObject.GetVariable("ScreenSizeIsNormal");

		innerValue.VariableChange += VariableChangeEvent;
		outerValue.VariableChange += VariableChangeEvent;
		screenSizeIsNormal.VariableChange += VariableChangeEvent;

		UpdateChart();
	}
	public override void Stop()
	{
		innerValue.VariableChange -= VariableChangeEvent;
		outerValue.VariableChange -= VariableChangeEvent;
		screenSizeIsNormal.VariableChange -= VariableChangeEvent;
	}

	private void VariableChangeEvent(object sender, VariableChangeEventArgs e)
	{
		UpdateChart();
	}

	public void UpdateChart()
	{
		// Read template page content
		string text = File.ReadAllText(sourcePath);

		text = text.Replace("$1$", innerValue.Value);
		text = text.Replace("$2$", DecimalToHex(innerColor.Value, false, true));
		text = text.Replace("$3$", DecimalToHex(innerBackground.Value, false, true));
		Log.Verbose1("ProductionStatisticsChart_NetLogic", $"Updating Chart Outer Circle:   ${innerValue.Value}   ${DecimalToHex(innerColor.Value, false, true)}   ${DecimalToHex(innerBackground.Value, false, true)}");

		text = text.Replace("$4$", outerValue.Value);
		text = text.Replace("$5$", DecimalToHex(outerColor.Value, false, true));
		text = text.Replace("$6$", DecimalToHex(outerBackground.Value, false, true));
		Log.Verbose1("ProductionStatisticsChart_NetLogic", $"Updating Chart Outer Circle:   ${outerValue.Value}   ${DecimalToHex(outerColor.Value, false, true)}   ${DecimalToHex(outerBackground.Value, false, true)}");

		if (screenSizeIsNormal != null) {
			if ((bool)screenSizeIsNormal.Value.Value == true) {
				text = text.Replace("$21$", "79%");
				text = text.Replace("$22$", "89%");
				text = text.Replace("$23$", "94%");
				text = text.Replace("$24$", "99%");
				text = text.Replace("$25$", "5");
			} else {
				text = text.Replace("$21$", "77%");
				text = text.Replace("$22$", "87%");
				text = text.Replace("$23$", "94%");
				text = text.Replace("$24$", "99%");
				text = text.Replace("$25$", "3");
			}
		}

		// Write to file
		File.WriteAllText(destPath, text);

		// Refresh WebBrowser page
		Owner.Get<WebBrowser>("ProductionStatisticsChart_Webpage").Refresh();
	}

	public static string DecimalToHex(uint input, bool argb = false, bool rgba = false)
	{
		// input from optix is always argb
		byte a = (byte)((input >> 24) & 0xFF);
		byte r = (byte)((input >> 16) & 0xFF);
		byte g = (byte)((input >> 8) & 0xFF);
		byte b = (byte)(input & 0xFF);

		if (argb) { return $"#{a:X2}{r:X2}{g:X2}{b:X2}"; }
		else if (rgba) { return $"#{r:X2}{g:X2}{b:X2}{a:X2}"; }
		else { return $"#{r:X2}{g:X2}{b:X2}"; }
	}

	// windows variables
	private string projectPath;
	private string chartFolder;
	private string sourcePath;
	private string destPath;

	// internal variables


	// optix variables
	private IUAVariable innerValue;
	private IUAVariable innerColor;
	private IUAVariable innerBackground;
	private IUAVariable outerValue;
	private IUAVariable outerColor;
	private IUAVariable outerBackground;
	private IUAVariable screenSizeIsNormal;
}
