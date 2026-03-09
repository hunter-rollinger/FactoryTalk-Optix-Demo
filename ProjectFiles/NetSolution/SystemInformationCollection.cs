#region Using directives
using System;
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.OPCUAServer;
using FTOptix.Recipe;
using FTOptix.OPCUAClient;
using FTOptix.AuditSigning;
using FTOptix.System;
using FTOptix.EdgeAppPlatform;
using FTOptix.InfluxDBStoreLocal;
using FTOptix.InfluxDBStore;
using FTOptix.EventLogger;
using FTOptix.TwinCAT;
using FTOptix.Report;
using FTOptix.RecipeX;
using FTOptix.SerialPort;
using FTOptix.UI;
using System.Diagnostics;
using FTOptix.DataLogger;
#endregion

public class SystemInformationCollection : BaseNetLogic
{
    public override void Start() {
        // machine information
        LogicObject.GetVariable("Hostname").Value = Environment.MachineName;
        LogicObject.GetVariable("OS Version").Value = Environment.OSVersion.VersionString;
        LogicObject.GetVariable("OS User").Value = Environment.UserName;

		secondTask = new PeriodicTask(UpdateEverySecond, 1000, LogicObject);
		secondTask.Start();

		minuteTask = new PeriodicTask(UpdateEveryMinute, 60000, LogicObject);
		minuteTask.Start();
	}

    public override void Stop() {
		secondTask.Dispose();
		secondTask = null;

		minuteTask.Dispose();
		minuteTask = null;
	}

	private void UpdateEverySecond() {
		localTime = DateTime.Now;
		LogicObject.GetVariable("CurrentDT").Value = localTime;
		LogicObject.GetVariable("Yesterday").Value = localTime.AddDays(-1);
	}

	private void UpdateEveryMinute() {
	}

	private DateTime localTime;
	private PeriodicTask secondTask;
    private PeriodicTask minuteTask;
}
