#region Using directives
using FTOptix.Core;
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.OPCUAServer;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.ODBCStore;
using FTOptix.InfluxDBStoreLocal;
using FTOptix.InfluxDBStore;
using FTOptix.SerialPort;
using FTOptix.EventLogger;
using System.Linq;
using System.Collections.Generic;
using FTOptix.Recipe;
using FTOptix.OPCUAClient;
using FTOptix.AuditSigning;
using System.Diagnostics;
using System.Timers;
using FTOptix.System;
using FTOptix.EdgeAppPlatform;
using FTOptix.MQTTClient;
using System.Runtime.CompilerServices;
using FTOptix.TwinCAT;
using FTOptix.Report;
using FTOptix.RecipeX;
using System.Diagnostics.CodeAnalysis;
#endregion

public class ScreenModeControl : BaseNetLogic
{
	public override void Start() {
		burgerMenuScreen = (BurgerMenu)InformationModel.Get((NodeId)LogicObject.GetVariable("BurgerMenuScreen").Value);
		debugMenu = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("DebugMenu").Value);
		standardContent = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardContent").Value);
		standarMenuBar = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardMenuBar").Value);
		standardQuickInfo = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardQuickInfo").Value);
		emptyPanel = (NodeId)LogicObject.GetVariable("EmptyScreen").Value;
		standardContentContainer = (Rectangle)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardContentContainer").Value);
		bottomBarMode = LogicObject.GetVariable("BottomBarMode");
	}

	[ExportMethod]
	public void LoadNewScreen(NodeId panelToLoadNodeId = null, NodeId menuBarToLoad = null, int modeToChangeTo = -1) {
		CloseAllOpen(); // close all open popups, menus, dialogs, etc.
		if (panelToLoadNodeId == null || panelToLoadNodeId == emptyPanel || menuBarToLoad == null || menuBarToLoad == emptyPanel || modeToChangeTo == -1)
			return;

		var panelToLoad = InformationModel.Get(panelToLoadNodeId);
		// easiest method thought of at the time to set the menu bar text since the screen loading is handled in netlogic already anyway
		UAVariable MenuBarText = InformationModel.Get(menuBarToLoad).Get("MenuBarText") as UAVariable;    // check for textbox on menu bar
		if (MenuBarText == null || MenuBarText.Value == null || MenuBarText.Value.Value == null) {
			Log.Verbose1("ScreenModeControl", $"Could not retrieve MenuBarText item. If screen: {panelToLoad.BrowseName} is supposed to have screen text, fix this.");   // error if burgerMenuBarText variable is null
		} else {
			if (panelToLoad.DisplayName.HasTextId && (panelToLoad.DisplayName.TextId != "")) {
				MenuBarText.Value = panelToLoad.DisplayName.TextId;   // set text on burger menu bar
			} else if (panelToLoad.DisplayName.HasTextId && (panelToLoad.DisplayName.TextId != "")) {
				MenuBarText.Value = panelToLoad.DisplayName.Text;     // set text on burger menu bar
			} else {
				Log.Warning("ScreenModeControl", $"Missing DisplayName on screen: {Log.Node(panelToLoad)}");   // warn if no display name is set
			}
		}
		
		standardContent.ChangePanel(panelToLoadNodeId);		// close standard content
		standarMenuBar.ChangePanel(menuBarToLoad);			// close standard menu bar

		bottomBarMode.Value = modeToChangeTo;	// remove highlight from other bottom bar buttons
	}

	[ExportMethod]
	public void CloseAllOpen() {
		var root = Project.Current;
		ClosePopupsRecurse(root);

		burgerMenuScreen.Visible = false;			// close burger menu
		debugMenu.ChangePanel(emptyPanel);			// close debug menu
		standardQuickInfo.ChangePanel(emptyPanel);  // close standard quick info
	}

	private static void ClosePopupsRecurse(IUANode node) {
		if (node is Dialog dialog) {
			try {
				var openDialog = InformationModel.Get<Dialog>(dialog.NodeId);
				openDialog.Close();
			} catch { }
		}

		foreach (var child in node.Children)
			ClosePopupsRecurse(child);

		return;
	}

	private BurgerMenu burgerMenuScreen;
	private PanelLoader debugMenu;
	private PanelLoader standardContent;
	private PanelLoader standarMenuBar;
	private PanelLoader standardQuickInfo;
	private NodeId emptyPanel;
	private Rectangle standardContentContainer;
	private IUAVariable bottomBarMode;
}
