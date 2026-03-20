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
#endregion

public interface IHistoryUpdate {
	void UpdateDatabase();
	// interface to expose the updatedatabase method
}

public static class HistoryUpdate {
	public static IHistoryUpdate Service => AppendToEventLogging.Instance;
	// helper class for the exposed interface
}

public class AppendToEventLogging : BaseNetLogic, IHistoryUpdate
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



		minuteTask = new PeriodicTask(UpdateDatabase, 60000, LogicObject);
		minuteTask.Start();
		Instance = this;	// set instance equal to this whole class
	}

	public static IHistoryUpdate Instance { get; private set; }
	// declaration of the instance referenced by the public helper class

	private void VariableUpdate(object sender, VariableChangeEventArgs e) {
		Log.Verbose1("AppendToEventLogging", "Automatic Variable History Update Task");
		UpdateDatabase();
	}

	public override void Stop() {
		// check if the instance is this then set to null if true
		if (Instance == this)
			Instance = null;
		minuteTask.Dispose();
		minuteTask = null;
		recipeNumber.VariableChange -= VariableUpdate;
		recipeName.VariableChange -= VariableUpdate;
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

	SQLiteStore historyDB;
	string dbTable;
	string colRecipe;
	IUAVariable recipeName;
	IUAVariable recipeNumber;
	private PeriodicTask minuteTask;
	private uint affinityId;
}
