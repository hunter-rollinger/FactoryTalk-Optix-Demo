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
#endregion

public class ScreenModeControl : BaseNetLogic
{
	public override void Start() {
		// Debugger.Launch();
		burgerMenuScreen = (BurgerMenu)InformationModel.Get((NodeId)LogicObject.GetVariable("BurgerMenuScreen").Value);
		debugMenu = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("DebugMenu").Value);
		standardContent = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardContent").Value);
		standarMenuBar = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardMenuBar").Value);
		standardQuickInfo = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardQuickInfo").Value);
		burgerContent = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("BurgerContent").Value);
		burgerMenuBar = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("BurgerMenuBar").Value);
		burgerQuickInfo = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("BurgerQuickInfo").Value);
		emptyPanel = (NodeId)LogicObject.GetVariable("EmptyScreen").Value;
		burgerMenuBarScreen = (NodeId)LogicObject.GetVariable("BurgerMenuBarScreen").Value;
		standardMenuBarScreen = (NodeId)LogicObject.GetVariable("StandardMenuBarScreen").Value;
		quickInfoScreen = (NodeId)LogicObject.GetVariable("QuickInfoScreen").Value;
		standardContentContainer = (Rectangle)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardContentContainer").Value);
		burgerContentContainer = (Rectangle)InformationModel.Get((NodeId)LogicObject.GetVariable("BurgerContentContainer").Value);
		bottomBarMode = LogicObject.GetVariable("BottomBarMode");
	}

	[ExportMethod]
	public void LoadBurgerScreen(NodeId panelToLoad = null, NodeId aliasNode = null) {
		Debugger.Launch();

		burgerMenuScreen.Visible = false;				    // close burger menu
		if (panelToLoad == null || panelToLoad == emptyPanel)
			return;
		
		burgerContent.ChangePanel(panelToLoad, aliasNode);  // load burger panel
		burgerMenuBar.ChangePanel(burgerMenuBarScreen);     // load burger menu bar
		burgerQuickInfo.ChangePanel(quickInfoScreen);       // load burger quick info
		burgerContentContainer.Visible = true;              // make burger content container visible

		/*
		// easiest method thought of at the time to set the menu bar text since the screen loading is handled in netlogic already anyway
		UAVariable burgerMenuBarText = InformationModel.Get(burgerMenuBarScreen).Get("BurgerMenuBarText") as UAVariable;	// get text from burger menu screen
		if (burgerMenuBarText.Value.Value == null) {
			burgerMenuBarText.Value = "PLACEHOLDER TEXT. TO FIX ADD DISPLAYNAME TO LOADED SCREEN/PANEL ITEM."; // set placeholder text on burger menu bar
		} else {
			burgerMenuBarText.Value = InformationModel.Get<IUAVariable>(panelToLoad).DisplayName.Text;         // set text on burger menu bar
		}
		*/
		
		standardContentContainer.Visible = false;           // make standard content container invisible
		standardContent.ChangePanel(emptyPanel);            // close standard content
		standarMenuBar.ChangePanel(emptyPanel);             // close standard menu bar
		standardQuickInfo.ChangePanel(emptyPanel);          // close standard quick info
		
		bottomBarMode.Value = 0;                            // remove highlight from other bottom bar buttons
	}

	[ExportMethod]
	public void LoadStandardScreen(NodeId panelToLoad = null, NodeId aliasNode = null) {
		Debugger.Launch();

		burgerMenuScreen.Visible = false;                       // close burger menu
		if (panelToLoad == null || panelToLoad == emptyPanel)
			return;

		standardContent.ChangePanel(panelToLoad, aliasNode);    // close standard content
		standarMenuBar.ChangePanel(standardMenuBarScreen);      // close standard menu bar
		standardQuickInfo.ChangePanel(quickInfoScreen);         // close standard quick info
		standardContentContainer.Visible = true;				// make standard content container visible

		burgerContentContainer.Visible = false;					// make burger content container invisible
		burgerContent.ChangePanel(emptyPanel);					// load burger panel
		burgerMenuBar.ChangePanel(emptyPanel);					// load burger menu bar
		burgerQuickInfo.ChangePanel(emptyPanel);				// load burger quick info
	}

	[ExportMethod]
	public void CloseAllOpen() {
		Debugger.Launch();
		var root = Project.Current;
		ClosePopupsRecurse(root);

		burgerMenuScreen.Visible = false;		// close burger menu
		debugMenu.ChangePanel(emptyPanel);		// close debug menu
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
	private PanelLoader burgerContent;
	private PanelLoader burgerMenuBar;
	private PanelLoader burgerQuickInfo;
	private NodeId emptyPanel;
	private NodeId burgerMenuBarScreen;
	private NodeId standardMenuBarScreen;
	private NodeId quickInfoScreen;
	private Rectangle standardContentContainer;
	private Rectangle burgerContentContainer;
	private IUAVariable bottomBarMode;
}
