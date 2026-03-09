#region Using directives
using System;
using System.Collections.Generic;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.OPCUAServer;
using System.Diagnostics;
using FTOptix.SerialPort;
using FTOptix.System;
using FTOptix.DataLogger;
#endregion

public class BackProvider : BaseNetLogic
{
	public override void Start() {
		oldPanelStack = new Stack<NodeId>();
		newPanelStack = new Stack<NodeId>();

		var panelLoader = Owner as PanelLoader;
		if (panelLoader == null)
			Log.Error("BackProvider", "Panel loader not found");
		panelLoader.PanelVariable.VariableChange += PanelVariable_VariableChange;

		canGoBack = LogicObject.GetVariable("CanGoBack");
		canGoNext = LogicObject.GetVariable("CanGoNext");

		fromBack = false;
		fromNext = false;
	}

	/// <summary>
	/// Handles the change event for a panel variable.
	/// Updates the old panel and its node ID before pushing it onto the stack.
	/// </summary>
	/// <param name="sender">Event source.</param>
	/// <param name="e">Event arguments containing the old value.</param>
	/// <remarks>
	/// Old panel is updated from the old value, and its node ID is extracted.
	/// The old panel's node ID is then pushed onto the stack.
	/// </remarks>
	private void PanelVariable_VariableChange(object sender, VariableChangeEventArgs e) {
		if (fromBack) {
			var oldPanel = InformationModel.Get(e.OldValue);
			NodeId newPanelNodeId = e.OldValue;
			newPanelStack.Push(newPanelNodeId);
			canGoNext.Value = true;
		} else {
			var oldPanel = InformationModel.Get(e.OldValue);
			NodeId oldPanelNodeId = e.OldValue;
			oldPanelStack.Push(oldPanelNodeId);
			canGoBack.Value = true;
		}

		if (!fromNext && !fromBack) {
			newPanelStack.Clear();
			canGoNext.Value = false;
		}
	}

	public override void Stop() {
		var panelLoader = Owner as PanelLoader;
		if (panelLoader == null)
			Log.Error("BackProvider", "Panel loader not found");

		panelLoader.PanelVariable.VariableChange -= PanelVariable_VariableChange;
	}

	/// <summary>
	/// Method to navigate back through panels.
	/// If no panel loader or stack is available, logs an error message.
	/// Otherwise, pops the top panel from the stack, unregisters the variable change event,
	/// changes the panel using the provided node ID, and then reenables the variable change event.
	/// </summary>
	/// <remarks>
	/// If there are no panels left on the stack when navigating back, this method does nothing.
	/// </remarks>
	[ExportMethod]
	public void Back() {
		var panelLoader = Owner as PanelLoader;
		if (panelLoader == null)
			Log.Error("BackProvider", "Panel loader not found");

		if (oldPanelStack.Count <= 1)
			canGoBack.Value = false;

		if (oldPanelStack.Count == 0)
			return;

		var panelNodeId = oldPanelStack.Pop();
		fromBack = true;
		panelLoader.ChangePanel(panelNodeId, NodeId.Empty);
		fromBack = false;
	}
	public void StoreNext(NodeId panel) {
		newPanelStack.Push(panel);
	}

	[ExportMethod]
	public void Next() {
		var panelLoader = Owner as PanelLoader;
		if (panelLoader == null)
			Log.Error("BackProvider", "Panel loader not found");

		if (newPanelStack.Count <= 1)
			canGoNext.Value = false;

		if (newPanelStack.Count == 0)
			return;

			var panelNodeId = newPanelStack.Pop();
		fromNext = true;
		panelLoader.ChangePanel(panelNodeId, NodeId.Empty);
		fromNext = false;
	}

	private Stack<NodeId> oldPanelStack;
	private Stack<NodeId> newPanelStack;
	private bool fromBack;
	private bool fromNext;
	private IUAVariable canGoBack;
	private IUAVariable canGoNext;
}
