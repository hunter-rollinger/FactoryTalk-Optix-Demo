#region Using directives
using System;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.NetLogic;
using FTOptix.Core;
using System.IO;
using System.Diagnostics;
using System.Windows.Markup;
#endregion

public class ProductionTimeChart_NetLogic : BaseNetLogic
{
	public override void Start()
	{
		// Debugger.Launch();

		projectPath = ResourceUri.FromProjectRelativePath("").Uri;
		chartFolder = Path.Combine(projectPath, "eCharts", "Production Info Chart");
		sourcePath = Path.Combine(chartFolder, "source-chart.js");
		destPath = Path.Combine(chartFolder, "data.js");

		Log.Verbose2("ProductionTimeChart_NetLogic", "Data file path: " + destPath);

		PeriodicTask RefreshChart = new(UpdateChart, TimeSpan.FromSeconds(10), LogicObject);

		values_optix = LogicObject.Owner.GetVariable("Values");
		values = (int[])values_optix.Value.Value;
		values_optix.VariableChange += VariableChangeEvent;

		colors_optix = LogicObject.Owner.GetVariable("Values");
		colors = (uint[])colors_optix.Value.Value;
		colors_optix.VariableChange += VariableChangeEvent;

		UpdateChart();
	}
	public override void Stop()
	{
		values_optix.VariableChange -= VariableChangeEvent;
		colors_optix.VariableChange -= VariableChangeEvent;
	}

	private void VariableChangeEvent(object sender, VariableChangeEventArgs e)
	{
		UpdateChart();
	}

	public void UpdateChart()
	{
		// Read template page content
		string text = File.ReadAllText(sourcePath);

		for (int i = 0; i < values.Length; i++)
		{
			text = text.Replace($"${i + 1}", values[i].ToString());
			text = text.Replace($"${i + 10}", DecimalToHex(colors[i], false, true));
			Log.Verbose1("ProductionTimeChart_NetLogic", $"Updating Chart Segment ${i + 1}:   " + values[i].ToString() + "   " + DecimalToHex(colors[i], false, true));
		}

		// Write to file
		File.WriteAllText(destPath, text);

		// Refresh WebBrowser page
		Owner.Get<WebBrowser>("ProductionTimeChart_Webpage").Refresh();
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
	private int[] values;
	private uint[] colors;

	// optix variables
	private IUAVariable values_optix;
	private IUAVariable colors_optix;
}
