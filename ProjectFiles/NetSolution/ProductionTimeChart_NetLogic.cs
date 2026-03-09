#region Using directives
using System;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.NetLogic;
using FTOptix.Core;
using System.IO;
using System.Diagnostics;
using System.Windows.Markup;
using FTOptix.RecipeX;
using FTOptix.SerialPort;
using FTOptix.OPCUAServer;
using FTOptix.System;
using FTOptix.DataLogger;
#endregion

public class ProductionTimeChart_NetLogic : BaseNetLogic
{
	public override void Start()
	{
		projectPath = ResourceUri.FromProjectRelativePath("").Uri;
		chartFolder = Path.Combine(projectPath, "eCharts", "Production Time Chart");
		sourcePath = Path.Combine(chartFolder, "source-chart.js");
		destPath = Path.Combine(chartFolder, "data.js");

		Log.Verbose2("ProductionTimeChart_NetLogic", "Data file path: " + destPath);

		PeriodicTask RefreshChart = new(UpdateChart, TimeSpan.FromSeconds(10), LogicObject);

		values_optix = LogicObject.Owner.GetVariable("Values");
		values_optix.VariableChange += VariableChangeEvent;

		colors_optix = LogicObject.Owner.GetVariable("Colors");
		colors = (uint[])colors_optix.Value.Value;

		backgroundColor = LogicObject.Owner.GetVariable("Background_Color");

		UpdateChart();
	}
	public override void Stop()
	{
		values_optix.VariableChange -= VariableChangeEvent;
	}

	private void VariableChangeEvent(object sender, VariableChangeEventArgs e)
	{
		UpdateChart();
	}

	public void UpdateChart()
	{
		// Read template page content
		string text = File.ReadAllText(sourcePath);

		values = (int[])values_optix.Value.Value;

		for (int i = 0; i < values.Length; i++)
		{
			int value = values[i];
			string color = DecimalToHex(colors[i], false, true);
			if (value <= 0 && color.Contains("#00000000")) {
				text = text.Replace($"${i}$", "");
			} else {
				text = text.Replace($"${i}$", "value: $value, itemStyle: {color: '$color', borderColor: '$bg$', borderWidth: 3 }");
				text = text.Replace("$value", value.ToString());
				text = text.Replace("$color", color);
				Log.Verbose1("ProductionTimeChart_NetLogic", $"Updating Chart Segment ${i}:   " + values[i].ToString() + "   " + DecimalToHex(colors[i], false, true));
			}
		}

		text = text.Replace("$bg$", DecimalToHex(backgroundColor.Value, false, true));

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
	private IUAVariable backgroundColor;
}
