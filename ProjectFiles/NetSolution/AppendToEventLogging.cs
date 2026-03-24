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
using FTOptix.OPCUAServer;
using FTOptix.Recipe;
using System.Diagnostics.Metrics;
using System.Collections.Generic;
using UAManagedCore.OpcUa;
#endregion

public class AppendToEventLogging : BaseNetLogic, IUAEventObserver
{
    public override void Start() {
		Debugger.Launch();
		NodeId db = LogicObject.GetVariable("Database").Value;
		historyDB = InformationModel.Get(db) as SQLiteStore;
		dbTable = LogicObject.GetVariable("TableName").Value;
		colRecipe = LogicObject.GetVariable("TableRecipeColumn").Value;

		recipeNumber = LogicObject.GetVariable("RecipeNumber");
		recipeName = LogicObject.GetVariable("RecipeName");
		recipeNumber.VariableChange += VariableUpdate;
		recipeName.VariableChange += VariableUpdate;

		affinityId = LogicObject.Context.AssignAffinityId();
		IUAObject prj = Project.Current.Owner.Children.Get("Server") as IUAObject;
		eventId = new NodeId(0, 2100);
		eventReg = prj.RegisterUAEventObserver(this, eventId, affinityId);

		CompileList(LogicObject.GetVariable("LoggerPath").Value);
	}

	public override void Stop() {
		recipeNumber.VariableChange -= VariableUpdate;
		recipeName.VariableChange -= VariableUpdate;

		try {
			eventReg?.Dispose();
			eventReg = null;
		} catch (Exception ex) {
			Log.Warning("AppendToEventLogging", $"UnregisterUAEventObserver failed: {ex.Message}");
		}

	}

	private void VariableUpdate(object sender, VariableChangeEventArgs e) {
		Log.Verbose1("AppendToEventLogging", "Automatic Variable History Update Task");
		UpdateDatabase();
	}

	public void OnEvent(IUAObject eNotifier, IUAObjectType eType, IReadOnlyList<Object> eArgs, ulong senderId) {
		try {
			if (!eType.NodeId.Equals(eventId))
				return;
		} catch (Exception e) {
			Log.Error("AppendToEventLogging", $"Error in OnEvent Method: {e}");
		}
	}

	private IUAVariable ResolveNodeId(string source) {
		return null;
	}

	private void CompileList(NodeId source) {
		var input = InformationModel.Get(source);
		if (input.GetType().Name.ToLowerInvariant() is not "folder") {
			Log.Error("AppendToEventLogging", "Error resolving LoggerPath. Path was not determined to be a folder. Please only use a folder item or check the CompileList function in NetLogic.");
			return;
		} 
		foreach (var child in input.Children) {
			Debugger.Break();
		}
	}

	[ExportMethod]
	public void UpdateDatabase() {
		string getName = recipeName.Value;
		int getNumber = recipeNumber.Value;

		string query = $"SELECT * FROM {dbTable} WHERE {colRecipe} IS NULL OR {colRecipe}=''";

		string[] header;
		object[,] resultSet;
		historyDB.Query(query, out header, out resultSet);
		Debugger.Break();
	}

	private uint affinityId;
	private NodeId eventId;
	private IEventRegistration eventReg;

	private HashSet<string> varList;
	private NodeId varSource;

	private SQLiteStore historyDB;
	private string dbTable;
	private string colRecipe;
	private IUAVariable recipeName;
	private IUAVariable recipeNumber;
}
