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
using FTOptix.RecipeX;
using FTOptix.SerialPort;
using FTOptix.OPCUAServer;
using FTOptix.System;
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
		Log.Info("FindAndSetColorVariables", "Searching for dynamic colors in project...");
		var projectNodes = Project.Current.FindNodesByType<IUANode>().ToList();
		projectNodes.Add(Project.Current);

		var projectNodesWithDynamicLinks = projectNodes.Where(node => node.Refs.GetVariable(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink) != null);
		var linksWithColorVariablesCount = 0;
		var reject_badResolvedLink = 0;
		var reject_noDynamicLink = 0;
		var reject_notAColorVariable = 0;
		var reject_arraysNotSupported = 0;
		var reject_colorNotInFolder = 0;
		var reject_noVariableToModify = 0;

		try {
			colorFolderNodeId = (NodeId)LogicObject.GetVariable("ColorReferenceFolder").Value.Value;
			colorFolder = InformationModel.Get(colorFolderNodeId);
			string projectName = Project.Current.BrowseName;
			colorFolderPath = Log.Node(colorFolder);
			colorFolderPath = colorFolderPath.Substring(colorFolderPath.IndexOf(projectName)+projectName.Length+1);

			foreach (var node in EnumerateDescendants(colorFolder)) {
				if (node is IUAVariable variable) {
					if (variable.DataType == FTOptix.Core.DataTypes.Color) {
						colors.Add(variable.NodeId);
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
			if (dynamicLinkVariable == null) {
				Log.Verbose2("FindAndSetColorVariables", $"The node {Log.Node(nodeWithDynamicLink)} does not have a dynamic link. Skipping...");
				reject_noDynamicLink++;
				continue;
			};

			// Retrieve and Resolve the path of the dynamic link
			string dynamicLinkPath = (string)dynamicLinkVariable.Value;
			var targetVariable = LogicObject.Context.ResolvePath(nodeWithDynamicLink, dynamicLinkPath);	// unsure if used

			// skip if resolved link is null
			if (targetVariable.ResolvedNode == null) {
				Log.Verbose1("FindAndSetColorVariables", $"The dynamic link at node {Log.Node(nodeWithDynamicLink)} could not be resolved. Skipping...");
				reject_badResolvedLink++;
				continue;
			}

			if ((nodeWithDynamicLink as IUAVariable).DataType != FTOptix.Core.DataTypes.Color) {
				Log.Verbose1("FindAndSetColorVariables", $"The node at dynamic link {Log.Node(nodeWithDynamicLink)} is not a color variable. Skipping...");
				reject_notAColorVariable++;
				continue;
			}; 

			if ((nodeWithDynamicLink as IUAVariable).ValueRank != ValueRank.Scalar) {
				Log.Verbose1("FindAndSetColorVariables", $"The node at dynamic link {Log.Node(nodeWithDynamicLink)} is not a scalar variable. Skipping...");
				reject_arraysNotSupported++;
				continue;
			}

			string propertyPath = Log.Node(nodeWithDynamicLink);
			IUAVariable variableToModify = null;
			NodeId currentColor = null;
			
			try {
				currentColor = targetVariable.ResolvedNode.NodeId;
				variableToModify = (IUAVariable)nodeWithDynamicLink;
			} catch (Exception e) {
				Log.Error("FindAndSetColorVariables", $"Error encountered when obtaining the property information for: {propertyPath}: {e}");
			}

			// skip if current color is not in the list or if variable to modify is null
			if (!colors.Contains(currentColor)) {
				Log.Warning("FindAndSetColorVariables", $"The color at dynamic link {propertyPath} is not in the designated color folder: {colorFolderPath}. Skipping...");
				reject_colorNotInFolder++;
				continue;
			}
			if (null == variableToModify) {
				Log.Warning("FindAndSetColorVariables", $"No variable to modify at dynamic link {propertyPath}. Skipping...");
				reject_noVariableToModify++;
				continue;
			}

			try {
				var newColor = (uint)((IUAVariable)targetVariable.ResolvedNode).Value.Value;
				variableToModify.SetValue(newColor);

				Log.Verbose2("FindAndSetColorVariables", $"Changing color from {(nodeWithDynamicLink as IUAVariable).Value.Value} to {newColor} at dynamic link {propertyPath}");
				linksWithColorVariablesCount++;
			} catch (Exception e) { Log.Error("FindAndSetColorVariables", $"Unexpected exception caught: ${e}"); }
		}
		Log.Info("FindAndSetColorVariables", $"Search completed. Found and updated {linksWithColorVariablesCount} color variables based on dynamic links.");
		Log.Info("FindAndSetColorVariables", $"Rejected {reject_noDynamicLink} nodes without dynamic links.");
		Log.Info("FindAndSetColorVariables", $"Rejected {reject_badResolvedLink} nodes with bad resolved links.");
		Log.Info("FindAndSetColorVariables", $"Rejected {reject_notAColorVariable} nodes that are not color variables.");
		Log.Info("FindAndSetColorVariables", $"Rejected {reject_arraysNotSupported} nodes that are arrays. These are not currently supported.");
		Log.Info("FindAndSetColorVariables", $"Rejected {reject_colorNotInFolder} nodes with colors not in the designated color folder: {colorFolderPath}.");
		Log.Info("FindAndSetColorVariables", $"Rejected {reject_noVariableToModify} nodes with no variable to modify.");
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
	private HashSet<NodeId> colors = [];
	private IUANode colorFolder;
	private LongRunningTask myTask;
}
