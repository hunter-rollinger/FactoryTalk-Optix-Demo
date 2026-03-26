#region Using directives
using FTOptix.AuditSigning;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.DataLogger;
using FTOptix.EdgeAppPlatform;
using FTOptix.EventLogger;
using FTOptix.HMIProject;
using FTOptix.InfluxDBStore;
using FTOptix.InfluxDBStoreLocal;
using FTOptix.MQTTClient;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.ODBCStore;
using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;
using FTOptix.Recipe;
using FTOptix.RecipeX;
using FTOptix.Report;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.System;
using FTOptix.TwinCAT;
using FTOptix.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Timers;
using UAManagedCore;
using static System.Collections.Specialized.BitVector32;
using OpcUa = UAManagedCore.OpcUa;

#endregion

public class ScreenControl : BaseNetLogic
{
	public override void Start() {
		burgerMenuScreen = (BurgerMenu)InformationModel.Get((NodeId)LogicObject.GetVariable("BurgerMenuScreen").Value);
		debugMenu = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("DebugMenu").Value);
		standardContent = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardContent").Value);
		standarMenuBar = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardMenuBar").Value);
		standardQuickInfo = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardQuickInfo").Value);
		emptyPanel = (NodeId)LogicObject.GetVariable("EmptyScreen").Value;
		bottomBarMode = LogicObject.GetVariable("BottomBarMode");
		screenSizeIsNormal = LogicObject.GetVariable("ScreenSizeIsNormal");

		Window applicationWindow = (Window)LogicObject.Owner;

		windowHeightOut = LogicObject.GetVariable("WindowHeight");
		windowHeightIn = applicationWindow.HeightVariable;
		windowHeightOut.Value = windowHeightIn.Value;

		windowWidthOut = LogicObject.GetVariable("WindowWidth");
		windowWidthIn = applicationWindow.WidthVariable;
		windowWidthOut.Value = windowWidthIn.Value;

		windowHeightIn.VariableChange += WindowSizeChanged;
		windowWidthIn.VariableChange += WindowSizeChanged;
	}

	[ExportMethod]
	public void LoadNewScreen(NodeId panelToLoadNodeId = null, NodeId menuBarToLoad = null, int modeToChangeTo = -1) {
		CloseAllOpen(); // close all open popups, menus, dialogs, etc.
		if (panelToLoadNodeId == null || panelToLoadNodeId == emptyPanel || menuBarToLoad == null || menuBarToLoad == emptyPanel || modeToChangeTo == -1)
			return;
		
		standardContent.ChangePanel(panelToLoadNodeId);		// close standard content
		standarMenuBar.ChangePanel(menuBarToLoad);			// close standard menu bar

		bottomBarMode.Value = modeToChangeTo;	// remove highlight from other bottom bar buttons
	}

	[ExportMethod]
	public void CloseAllOpen(bool CloseAllWindowsBool = true) {
		var root = Project.Current;

		if (CloseAllWindowsBool == false)
			return;

		Session current =  GetSession(root);
		if (current == null) {
			Log.Error("ScreenControl - CloseAllOpen", "No session found for current node.");
			return;
		}

		ClosePopupsRecurse(current);				// close all popups and dialogs in current session
		burgerMenuScreen.Visible = false;           // close burger menu
		debugMenu.ChangePanel(emptyPanel);          // close debug menu
		standardQuickInfo.ChangePanel(emptyPanel);  // close standard quick info
	}

	private Session GetSession(IUANode node) {
		Session nativeSession = null;
		string nativeSessionId = null;
		Session[] webSessions = null;
		string[] webSessionIds = null;
		string callingSessionId = null;

		try {
			var native = Project.Current.Get("UI/NativePresentationEngine");    // get native presentation engine
			var nativeSessions = native.Get("Sessions");                        // get all native sessions
			nativeSession = (Session)nativeSessions.Children[1];                // get first (and only) native session
			nativeSessionId = nativeSession.BrowseName;							// get native session id
		} catch { Log.Warning("ScreenControl", "No Native Session Found."); }	// if try fails native session does not exist (this is fine if true)

		try {
			var web = Project.Current.Get("UI/WebPresentationEngine");			// get web presentation engine
			var getWebSessions = web.Get("Sessions");							// get all web sessions
			webSessions = new Session[getWebSessions.Children.Count];			// set length of web session array to number of web sessions
			webSessionIds = new string[getWebSessions.Children.Count];			// set length of web session id array to number of web sessions
			for (int i = 1; i < getWebSessions.Children.Count; i++) {			// iterate over web session children to get id
				webSessionIds[i - 1] = getWebSessions.Children[i].BrowseName;	// get each web session id and put into web session id array
			}
		} catch { Log.Warning("ScreenControl", "No Web Sessions Found."); }		// if try fails web sessions do not exist (this is fine if true)

		try {
			callingSessionId = node.Context.Sessions.CurrentSessionInfo.SessionObject.BrowseName;

			if (nativeSessionId != null && nativeSessionId == callingSessionId)
				return nativeSession;

			if (webSessionIds != null && webSessionIds.Contains(callingSessionId))
				return webSessions[Array.IndexOf(webSessionIds, callingSessionId)];

		} catch { Log.Error("ScreenControl - ClosePopupsRecurse", "Error while obtaining calling session id."); }

		return null;
	}

	private static void ClosePopupsRecurse(Session session) {
		foreach (Dialog item in session.Get("UIRoot").Children.OfType<Dialog>().ToList()) {
			item.Close(); // close any currently open alarm popups
		}
	}

	private void WindowSizeChanged(object sender, VariableChangeEventArgs e) {
		Window applicationWindow = (Window)LogicObject.Owner;
		windowHeightIn = applicationWindow.HeightVariable;
		windowHeightOut.Value = windowHeightIn.Value;
		windowWidthIn = applicationWindow.WidthVariable;
		windowWidthOut.Value = windowWidthIn.Value;

		if (windowWidthIn.Value >= 1920 && windowHeightIn.Value >= 1080) {
			screenSizeIsNormal.Value = true;
		} else {
			screenSizeIsNormal.Value = false;
		}
	}

	private BurgerMenu burgerMenuScreen;
	private PanelLoader debugMenu;
	private PanelLoader standardContent;
	private PanelLoader standarMenuBar;
	private PanelLoader standardQuickInfo;
	private NodeId emptyPanel;
	private IUAVariable bottomBarMode;
	private IUAVariable windowWidthIn;
	private IUAVariable windowHeightIn;
	private IUAVariable windowWidthOut;
	private IUAVariable windowHeightOut;
	private IUAVariable screenSizeIsNormal;
}
