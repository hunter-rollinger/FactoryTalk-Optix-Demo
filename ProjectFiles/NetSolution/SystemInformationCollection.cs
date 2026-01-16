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
#endregion

public class SystemInformationCollection : BaseNetLogic
{
    public override void Start()
    {
        // machine information
        LogicObject.GetVariable("Hostname").Value = Environment.MachineName;
        LogicObject.GetVariable("OS Version").Value = Environment.OSVersion.VersionString;
        LogicObject.GetVariable("OS User").Value = Environment.UserName;

        periodicTask = new PeriodicTask(UpdateSystemVariables, 1000, LogicObject);
        periodicTask.Start();
    }

    public override void Stop()
    {
        periodicTask.Dispose();
        periodicTask = null;
    }

    private void UpdateSystemVariables()
    {
        // local date time
        DateTime localTime = DateTime.Now;
        LogicObject.GetVariable("DateTime").Value = localTime;
        LogicObject.GetVariable("DateTime/Year").Value = localTime.Year;
        LogicObject.GetVariable("DateTime/Month").Value = localTime.Month;
        LogicObject.GetVariable("DateTime/Day").Value = localTime.Day;
        LogicObject.GetVariable("DateTime/Hour").Value = localTime.Hour;
        LogicObject.GetVariable("DateTime/Minute").Value = localTime.Minute;
        LogicObject.GetVariable("DateTime/Second").Value = localTime.Second;
        LogicObject.GetVariable("DateTime/Day Of Week").Value = localTime.DayOfWeek.ToString();
        LogicObject.GetVariable("DateTime/Day Of Year").Value = localTime.DayOfYear;
        LogicObject.GetVariable("DateTime/Daylight Savings").Value = localTime.IsDaylightSavingTime();
    }

    private PeriodicTask periodicTask;
}
