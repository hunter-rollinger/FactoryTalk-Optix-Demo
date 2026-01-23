#region Using directives
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UAManagedCore;
using UAManagedCore.OpcUa;
#endregion

public class SetColors : BaseNetLogic
{
	[ExportMethod]
	public void FindAndSetColorVariables() {
		// Insert code to be executed by the method
		myTask?.Dispose();
		myTask = new LongRunningTask(SearchInProject, LogicObject);
		myTask.Start();
	}
	private void SearchInProject() {
		// Debugger.Launch();
		Log.Info("FindAndSetColorVariables", "Searching for dynamic colors in project...");
		var projectNodes = Project.Current.FindNodesByType<IUANode>().ToList();
		projectNodes.Add(Project.Current);

		var projectNodesWithDynamicLinks = projectNodes.Where(node => node.Refs.GetVariable(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink) != null);
		var linksWithColorVariablesCount = 0;

		try {
			colorFolderNodeId = (NodeId)LogicObject.GetVariable("ColorReferenceFolder").Value.Value;
			colorFolder = InformationModel.Get(colorFolderNodeId);
			string projectName = Project.Current.BrowseName;
			colorFolderPath = Log.Node(colorFolder);
			colorFolderPath = colorFolderPath.Substring(colorFolderPath.IndexOf(projectName)+projectName.Length+1);

			foreach (var node in EnumerateDescendants(colorFolder)) {
				if (node is IUAVariable variable) {
					if (variable.DataType == FTOptix.Core.DataTypes.Color) {
						colors.Add((uint)variable.Value.Value);
					}
				}
			}
		} catch {
			Log.Error("FindAndSetColorVariables", "Error encountered when obtaining colors from the designated color folder. Check the path and try again.");
			return;
		}

		// Iterate over all nodes with dynamic links and check the link status
		foreach (IUANode nodeWithDynamicLink in projectNodesWithDynamicLinks) {
			// Check if the current node has a dynamic link
			IUAVariable dynamicLinkVariable = nodeWithDynamicLink.Refs.GetVariable(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink);
			if (dynamicLinkVariable == null) { continue; };

			// Retrieve and Resolve the path of the dynamic link
			string dynamicLinkPath = (string)dynamicLinkVariable.Value;
			var targetVariable = LogicObject.Context.ResolvePath(nodeWithDynamicLink, dynamicLinkPath);	// unsure if used

			// skip if dynamic link is null
			if (targetVariable.ResolvedNode == null) { continue; }

			// get the nodeid, item, and current color.
			NodeId propertyNodeId = nodeWithDynamicLink.NodeId;
			IUANode nodeToModify = InformationModel.Get(propertyNodeId);

			if ((nodeToModify as IUAVariable).DataType != FTOptix.Core.DataTypes.Color) { continue; }; 

			string propertyPath = Log.Node(nodeToModify);
			IUAVariable variableToModify = null;
			uint currentColor = 0;
			
			try {
				currentColor = (uint)(nodeToModify as IUAVariable).Value.Value;
				variableToModify = (IUAVariable)nodeToModify;
			} catch {
				Log.Error("FindAndSetColorVariables", $"Error encountered when obtaining the property information for: {propertyPath}");
			}

			// skip if current color is not in the list or if variable to modify is null
			if (!colors.Contains(currentColor)) { continue; }
			if (null == variableToModify) { continue; }
			if (variableToModify.DataType != FTOptix.Core.DataTypes.Color) { continue; }

			// log the dynamic link and associated color. the color is always the last in the dynamic link path item.
			string colorFromDynamicLink = dynamicLinkPath.Split('/').Last();
			string colorPath = $"{colorFolderPath}/{colorFromDynamicLink}";

			IUAVariable colorNode = (IUAVariable)Project.Current.Get(colorPath);
			uint newColor = (uint)colorNode.Value.Value;
			variableToModify.SetValue(newColor);

			// colorNode.Value.Value = newColor;	// not doable because value.value is readonly
			Log.Verbose2("FindAndSetColorVariables", $"Changing color from {currentColor} to {newColor} at dynamic link {propertyPath}");
			linksWithColorVariablesCount++;
		}
		Log.Info("FindAndSetColorVariables", $"Search completed. Found and updated {linksWithColorVariablesCount} color variables based on dynamic links.");
	}

	private IEnumerable<IUANode> EnumerateDescendants(IUANode root) {
		foreach (var child in root.Children) {
			yield return child;
			foreach (var grand in EnumerateDescendants(child)) {
				yield return grand;
			}
		}
	}

	private string colorFolderPath;
	private NodeId colorFolderNodeId;
	private HashSet<uint> colors = [];
	private IUANode colorFolder;
	private LongRunningTask myTask;
}
