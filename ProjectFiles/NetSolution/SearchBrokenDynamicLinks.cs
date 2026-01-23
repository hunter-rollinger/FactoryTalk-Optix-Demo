#region Using directives
using System;
using UAManagedCore;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System.Linq;
using System.Diagnostics;
#endregion

public class SearchBrokenDynamicLinks : BaseNetLogic
{
    [ExportMethod]
    public void FindBrokenDynamicLink()
    {
        // Insert code to be executed by the method
        myTask?.Dispose();
        myTask = new LongRunningTask(SearchInProject, LogicObject);
        myTask.Start();
    }

    private void SearchInProject()
    {
		Log.Info("BrokenDynamicLinks", "Searching for broken dynamic links in the project");
        // Get all nodes in the project (variables, UI objects, PLC tags, etc.)
        var projectNodes = Project.Current.FindNodesByType<IUANode>().ToList();
        // Add the root node of the project
        projectNodes.Add(Project.Current);
		// Filter nodes that have a dynamic link
		var projectNodesWithDynamicLinks = projectNodes.Where(node => node.Refs.GetVariable(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink) != null).OrderBy(node => Log.Node(node), StringComparer.OrdinalIgnoreCase).ToList();
		// Counters for broken and uncertain links
		var brokenLinksCount = 0;
		var emptyLinksCount = 0;
		var uncertainLinksCount = 0;

        // Iterate over all nodes with dynamic links and check the link status
        foreach (var nodeWithDynamicLink in projectNodesWithDynamicLinks)
        {
            // Check if the current node has a dynamic link
            var dynamicLinkVariable = nodeWithDynamicLink.Refs.GetVariable(FTOptix.CoreBase.ReferenceTypes.HasDynamicLink);

            // Check if node has a DynamicLink
            if (dynamicLinkVariable == null)
                continue;

            // Retrieve the path of the dynamic link
            var dynamicLinkPath = (string)dynamicLinkVariable.Value;
            // Resolve the dynamic link and get to the target node
            var targetVariable = LogicObject.Context.ResolvePath(nodeWithDynamicLink, dynamicLinkPath);
            if (targetVariable.ResolvedNode == null)
            {
                // Check if the link can be reolved at DesignTime
                var nodePathKind = targetVariable.NodePathKind;

                switch (nodePathKind) {
                    case NodePathKind.Invalid:
                        if (dynamicLinkVariable.Children.Any(node => node is FTOptix.CoreBase.Converter)) {
                            Log.Warning("BrokenDynamicLinks", $"Node at: \"{Log.Node(nodeWithDynamicLink)}\", has a converter under the dynamic link and cannot be resolved at design time. It will be reported as \"uncertain\"");
                            uncertainLinksCount++;
                        } else if (dynamicLinkPath == string.Empty) {
                            Log.Verbose1("BrokenDynamicLinks", $"EMPTY | Dynamic link at: \"{Log.Node(nodeWithDynamicLink)}\"");
                            emptyLinksCount++;
						} else {
                            Log.Error("BrokenDynamicLinks", $"INVALID | Dynamic link at: \"{Log.Node(nodeWithDynamicLink)}\", points to unresolved path: \"{dynamicLinkPath}\"");
                            brokenLinksCount++;
                        }
						break;
                    case NodePathKind.Pointer:
						Log.Verbose1("BrokenDynamicLinks", $"POINTER | Dynamic link at: \"{Log.Node(nodeWithDynamicLink)}\" points to path: \"{dynamicLinkPath}\"");
						break;
                    case NodePathKind.Absolute:
                        Log.Verbose1("BrokenDynamicLinks", $"ABSOLUTE | Dynamic link at: \"{Log.Node(nodeWithDynamicLink)}\" points to path: \"{dynamicLinkPath}\"");
						break;
                    case NodePathKind.Relative:
						Log.Verbose1("BrokenDynamicLinks", $"RELATIVE | Dynamic link at: \"{Log.Node(nodeWithDynamicLink)}\" points to path: \"{dynamicLinkPath}\"");
						break;
                    case NodePathKind.Alias:
						Log.Warning("BrokenDynamicLinks", $"ALIAS | Dynamic link at: \"{Log.Node(nodeWithDynamicLink)}\" cannot be resolved at design time ({dynamicLinkPath}) and will be reported as \"uncertain\"");
						uncertainLinksCount++;
						break;
                    case NodePathKind.Session:
						Log.Warning("BrokenDynamicLinks", $"SESSION | Dynamic link at: \"{Log.Node(nodeWithDynamicLink)}\" cannot be resolved at design time ({dynamicLinkPath}) and will be reported as \"uncertain\"");
						uncertainLinksCount++;
						break;
                    default:
                        Log.Error("BrokenDynamicLinks", $"UNKNOWN | Dynamic link at: \"{Log.Node(nodeWithDynamicLink)}\" has an unhandled NodePathKind: {nodePathKind}");
						break;
                }
            }
        }

        Log.Info("BrokenDynamicLinks", $"Found {projectNodesWithDynamicLinks.Count()} dynamic links in the project, {uncertainLinksCount} cannot be resolved at design time, {brokenLinksCount} were broken, and {emptyLinksCount} were empty.");
    }

    private LongRunningTask myTask;
}
