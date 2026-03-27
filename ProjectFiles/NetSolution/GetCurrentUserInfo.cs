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
using System.Linq;
#endregion

public class GetCurrentUserInfo : BaseNetLogic
{
    public override void Start() {
        //Debugger.Launch();
        topUserRole = LogicObject.GetVariable("TopUserRole");
        userNodeId = LogicObject.GetVariable("CurrentUser");
		userNodeId.VariableChange += UserChanged;
    }

	private void UserChanged(object sender, VariableChangeEventArgs e) {
		var currentUser = InformationModel.Get(userNodeId.Value);
		var userRoles = currentUser.Refs.GetObjects(FTOptix.Core.ReferenceTypes.HasRole, false);
        if (userRoles.Count < 1)
            return;
        
        NodeId[] userRoleIds = new NodeId[userRoles.Count];
        int i = 0;
        foreach (Role role in userRoles) {
            userRoleIds[i] = role.NodeId;
            i++;
        }

        NodeId[] roleVars = LogicObject.GetVariable("AllUserRoles").Value.Value as NodeId[];
        for (i = roleVars.Length - 1; i > -1; i--) {
            if (!userRoleIds.Contains(roleVars[i]))
                continue;
            string roleName = InformationModel.Get<Role>(roleVars[i]).BrowseName;
            topUserRole.Value = roleName;
            return;
        }
	}

    public override void Stop() {
		userNodeId.VariableChange -= UserChanged;
	}

    private IUAVariable userNodeId;
    private IUAVariable topUserRole;
}
