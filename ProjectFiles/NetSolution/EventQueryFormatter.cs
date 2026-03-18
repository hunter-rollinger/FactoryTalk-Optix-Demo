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
using FTOptix.OPCUAServer;
using FTOptix.CommunicationDriver;
using FTOptix.SerialPort;
using FTOptix.Core;
using System.Security.Cryptography;
using System.Diagnostics;
using FTOptix.DataLogger;
using FTOptix.Recipe;
using FTOptix.AuditSigning;
using FTOptix.RecipeX;
#endregion

public class EventQueryFormatter : BaseNetLogic {
	public override void Start() {
		//Debugger.Launch();
		query = LogicObject.GetVariable("Query");
		dbTable = LogicObject.GetVariable("DatabaseTable");
		fromTimestampVariable = LogicObject.GetVariable("FromTimestamp");
		toTimestampVariable = LogicObject.GetVariable("ToTimestamp");
		textVariable = LogicObject.GetVariable("Text");
		userVariable = LogicObject.GetVariable("UserFilter");
		NodeId dbNodeId = LogicObject.GetVariable("VariableDatabase").Value;
		varDB = InformationModel.Get<SQLiteStore>(dbNodeId);

		comment = LogicObject.GetVariable("Comment");
		comment.Value = "";
		comment.VariableChange += CommentUpdate;

		languageVariable = LogicObject.GetVariable("Language");
		colReceiveTime = LogicObject.GetVariable("TableReceiveTime").Value;
		colComment = LogicObject.GetVariable("TableComment").Value;

		resetQuery();
	}

	public override void Stop() {
		unsubscribeFromVariableChanges();
		comment.VariableChange -= CommentUpdate;
	}

	private void subscribeToVariableChanges() {
		languageVariable.VariableChange += VariableUpdate;
		fromTimestampVariable.VariableChange += VariableUpdate;
		toTimestampVariable.VariableChange += VariableUpdate;
		userVariable.VariableChange += VariableUpdate;
		textVariable.VariableChange += VariableUpdate;
	}

	private void unsubscribeFromVariableChanges() {
		languageVariable.VariableChange -= VariableUpdate;
		fromTimestampVariable.VariableChange -= VariableUpdate;
		toTimestampVariable.VariableChange -= VariableUpdate;
		userVariable.VariableChange -= VariableUpdate;
		textVariable.VariableChange -= VariableUpdate;
	}

	[ExportMethod]
	public void resetQuery() {
		unsubscribeFromVariableChanges();

		DateTime now = DateTime.Now;
		fromTimestampVariable.Value = now.AddDays(-1);
		toTimestampVariable.Value = now;
		userVariable.Value = "";
		textVariable.Value = "";

		subscribeToVariableChanges();
		UpdateQuery();
	}

	private void VariableUpdate(object sender, VariableChangeEventArgs e) {
		UpdateQuery();
	}

	private void CommentUpdate(object sender, VariableChangeEventArgs e) {
		if (e.SenderId != 0) {
			var selectedItem = InformationModel.Get(LogicObject.GetVariable("SelectedItem").Value);
			string updateValue = comment.Value;
			DateTime receivedTime = selectedItem.Children.GetVariable(colReceiveTime).Value;
			string formattedReceivedTime = receivedTime.ToString("yyyy-MM-ddTHH:mm:ss");
			string alarmMesssage = selectedItem.Children.GetVariable(colAlarmMesssage).Value;
			string query = $"UPDATE {eventLogger} SET {colComment}='{updateValue}' WHERE {colReceiveTime} LIKE '{formattedReceivedTime}%' AND {language}='{alarmMesssage}'";
			Log.Info("QueryFormatter", $"Executing comment update query: {query}");
			object[,] result;
			string[] header;
			varDB.Query(query, out header, out result);
		} else {
			UpdateCommentText();
		}
	}

	[ExportMethod]
	public void UpdateCommentText() {
		var selectedItem = InformationModel.Get(LogicObject.GetVariable("SelectedItem").Value);
		string currentComment = selectedItem.Children.GetVariable(colComment.Split('_')[0].Replace("\"", "")).Value;
		comment.Value = currentComment;
	}

	private void UpdateQuery() {
		string timestampResult = "";
		language = languageVariable.Value;
		eventLogger = dbTable.Value;
		fromTimestamp = fromTimestampVariable.Value;
		toTimestamp = toTimestampVariable.Value;
		user = userVariable.Value;
		text = textVariable.Value;

		// check variables for null or whitespace and format the query accordingly
		if (!string.IsNullOrWhiteSpace(user))
			user = $" AND User = {user}";
		else user = "";

		if (fromTimestamp.Year > 1601 && toTimestamp.Year > 1601) {
			timestampResult = $" WHERE {colReceiveTime} BETWEEN '{ConvertLocalToUtcString(fromTimestamp)}' AND '{ConvertLocalToUtcString(toTimestamp)}'";
		} else timestampResult = $" WHERE {colReceiveTime} BETWEEN '{ConvertLocalToUtcString(DateTime.Now.AddDays(-1))}' AND '{ConvertLocalToUtcString(DateTime.Now):sql_literal}'";

		if (language == "Message_" && language.Length < 8)
			language = " AND Message_en-US";

		if (text != "''" && text != "'%%'" && text.Length > 4)
			text = $" AND {language} LIKE '{text}'";
		else text = "";

		if (string.IsNullOrWhiteSpace(eventLogger)) {
			Log.Error("QueryFormatter", $"Event Logger variable is empty. Please provide a valid event logger name.");
			return;
		} else Log.Verbose1("QueryFormatter", $"Using event logger: {eventLogger}");

		// build sql query. for reference on names of columns see variable database in optix studio
		string output = $"SELECT {language} AS {language.Split('_')[0].Replace("\"", "")}, User, LocalTime, {colComment} as {colComment.Split('_')[0].Replace("\"", "")}, Recipe, {colReceiveTime} FROM {eventLogger}{timestampResult}{user}{text} ORDER BY {colReceiveTime} DESC";
		query.Value = output;
		Log.Info("QueryFormatter", output);
	}

	private string ConvertLocalToUtcString(DateTime toConvert) {
		return TimeZoneInfo.ConvertTimeToUtc(toConvert).ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
	}

	private IUAVariable query;
	private IUAVariable languageVariable;
	private IUAVariable dbTable;
	private IUAVariable fromTimestampVariable;
	private IUAVariable toTimestampVariable;
	private IUAVariable userVariable;
	private IUAVariable textVariable;
	private IUAVariable comment;
	private SQLiteStore varDB;
	private string language;
	private string eventLogger;
	private DateTime fromTimestamp;
	private DateTime toTimestamp;
	private string user;
	private string text;
	private string colReceiveTime;
	private string colAlarmMesssage;
	private string colComment;
}
