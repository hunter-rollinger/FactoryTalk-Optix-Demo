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
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.OPCUAServer;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.DataLogger;
using FTOptix.Recipe;
using FTOptix.AuditSigning;
using FTOptix.RecipeX;
using System.Diagnostics;
#endregion

public class AddRecipeToLog : BaseNetLogic
{
    public override void Start() {
        Debugger.Launch();
        eventHistory = Owner as EventLogger;
        eventHistory.UAEvent += HistoryUpdate;
        try {
            tmp = Owner as EventHistory;
            tmp.UAEvent += HistoryUpdate;
        } catch (Exception ex) { Debug.WriteLine(ex); }
	}

    public override void Stop() {
		eventHistory.UAEvent -= HistoryUpdate;
	}

    private void HistoryUpdate(object sender, UAManagedCore.UAEventArgs e) {
        var tmp = sender;
    }

    EventLogger eventHistory;
    EventHistory tmp;
}
