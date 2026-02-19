#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.WebUI;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.Recipe;
using FTOptix.RecipeX;
using FTOptix.DataLogger;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.TwinCAT;
using FTOptix.Report;
using FTOptix.Retentivity;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.SerialPort;
#endregion

public class CollapseMenuControl : BaseNetLogic {
	public override void Start() {
		collapseVariable = LogicObject.GetVariable("CollapseMenuExpanded");
		buttonLayout = LogicObject.Owner.Get<ColumnLayout>("ButtonLayout");
		expandedWidth = (UInt32)buttonLayout.Width;
	}

	[ExportMethod]
	public void ToggleCollapseMenu() {
		bool isCollapsed = collapseVariable.Value;
		if (isCollapsed) {
			buttonLayout.Width = 0;
			collapseVariable.Value = false;
		} else {
			// insert collapse menu code
			collapseVariable.Value = true;
		}
	}

	private IUAVariable collapseVariable;
	private ColumnLayout buttonLayout;
	private UInt32 expandedWidth;
}
