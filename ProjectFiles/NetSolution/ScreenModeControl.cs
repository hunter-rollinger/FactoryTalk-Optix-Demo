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
using FTOptix.OPCUAServer;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.SerialPort;
#endregion

public class ScreenModeControl : BaseNetLogic
{
	[ExportMethod]
	public void ChangeToBurger() {
		var project = LogicObject.GetProject();
		var hmiProject = project as HMIProject;
		if (hmiProject == null)
			return;
		var screenManager = hmiProject.GetScreenManager();
		if (screenManager == null)
			return;
		screenManager.ChangeScreen("BurgerMenuScreen");
	}

}
