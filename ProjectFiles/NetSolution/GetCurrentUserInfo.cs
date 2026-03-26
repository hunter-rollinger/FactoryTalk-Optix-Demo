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
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Diagnostics;
using System.Collections.Generic;
#endregion

public class GetCurrentUserInfo : BaseNetLogic
{
    public override void Start() {
        //Debugger.Launch();
        topUserRole = LogicObject.GetVariable("topUserRole");
        userNodeId = LogicObject.GetVariable("CurrentUser");
		userNodeId.VariableChange += UserChanged;
    }

	private void UserChanged(object sender, VariableChangeEventArgs e) {
		var currentUser = InformationModel.Get(userNodeId.Value);
		var userRoles = currentUser.Refs.GetObjects(FTOptix.Core.ReferenceTypes.HasRole, false);
        NodeId[] roleVar = LogicObject.GetVariable("AllUserRoles").Value.Value as NodeId[];
        HashSet<Role> roles = [];
        foreach (NodeId child in roleVar) {
            if (child == null)
                continue;
            Role role = InformationModel.Get(child) as Role;
            roles.Add(role);
        }
	}

    public override void Stop() {
		userNodeId.VariableChange -= UserChanged;
	}

    private IUAVariable userNodeId;
    private IUAVariable topUserRole;
}
