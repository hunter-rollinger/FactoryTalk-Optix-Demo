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
#endregion

public class QueryFormatter : BaseNetLogic
{
    public override void Start() {
		query = LogicObject.GetVariable("Query");
		dbTable = LogicObject.GetVariable("DatabaseTable");
		fromTimestampVariable = LogicObject.GetVariable("FromTimestamp");
		toTimestampVariable = LogicObject.GetVariable("ToTimestamp");
		severityVariable = LogicObject.GetVariable("Severity");
		textVariable = LogicObject.GetVariable("Text");

		languageVariable = LogicObject.GetVariable("Language");
		fromTimestampVariableFormatted = LogicObject.GetVariable("FromTimestampFormatted");
		toTimestampVariableFormatted = LogicObject.GetVariable("ToTimestampFormatted");
		severityVariableFormatted = LogicObject.GetVariable("SeverityFormatted");
		textVariableFormatted = LogicObject.GetVariable("TextFormatted");

		activeColumn = LogicObject.GetVariable("ActiveColumn").Value;
		colReceiveTime = LogicObject.GetVariable("TableReceiveTime").Value;

		detailsColumn = InformationModel.Get<GridLayout>(LogicObject.GetVariable("DetailsColumn").Value);
		resetQuery();
	}

	public override void Stop() {
		unsubscribeFromVariableChanges();
	}

	private void subscribeToVariableChanges() {
		languageVariable.VariableChange += VariableUpdate;
		fromTimestampVariableFormatted.VariableChange += VariableUpdate;
		toTimestampVariableFormatted.VariableChange += VariableUpdate;
		severityVariableFormatted.VariableChange += VariableUpdate;
		textVariableFormatted.VariableChange += VariableUpdate;
	}

	private void unsubscribeFromVariableChanges() {
		languageVariable.VariableChange -= VariableUpdate;
		fromTimestampVariableFormatted.VariableChange -= VariableUpdate;
		toTimestampVariableFormatted.VariableChange -= VariableUpdate;
		severityVariableFormatted.VariableChange -= VariableUpdate;
		textVariableFormatted.VariableChange -= VariableUpdate;
	}

	[ExportMethod]
	public void resetQuery() {
		unsubscribeFromVariableChanges();

		DateTime now = DateTime.Now;
		fromTimestampVariable.Value = now.AddDays(-1);
		toTimestampVariable.Value = now;
		severityVariable.Value = 0;
		textVariable.Value = "";

		subscribeToVariableChanges();
		UpdateQuery();
	}

	private void VariableUpdate(object sender, VariableChangeEventArgs e) {
		UpdateQuery();
	}

	private void UpdateQuery() {
		string timestampResult = "";
		language = languageVariable.Value;
		eventLogger = dbTable.Value;
		fromTimestamp = fromTimestampVariableFormatted.Value;
		toTimestamp = toTimestampVariableFormatted.Value;
		severity = severityVariableFormatted.Value;
		text = textVariableFormatted.Value;

		// check variables for null or whitespace and format the query accordingly
		if (int.Parse(severity) > 0)
			severity = $" AND Severity = {severity}";
		else severity = "";

		if (!fromTimestamp.Contains("1601") && !toTimestamp.Contains("1601")) {
			timestampResult = $" AND {colReceiveTime} BETWEEN {fromTimestamp} AND {toTimestamp}";
		} else timestampResult = $" AND {colReceiveTime} BETWEEN '{DateTime.Now.AddDays(-1)}' AND '{DateTime.Now}'";

		if (language == "Message_" && language.Length < 8)
			language = "Message_en-US";

		if (text != "''" && text != "'%%'" && text.Length > 4)
			text = $" AND {language} LIKE {text}";
		else text = "";

		if (string.IsNullOrWhiteSpace(eventLogger)) {
			Log.Error("QueryFormatter", $"Event Logger variable is empty. Please provide a valid event logger name.");
			return;
		} else Log.Verbose1("QueryFormatter", $"Using event logger: {eventLogger}");

		string initSort = $"WHERE {activeColumn}=0";

		// build sql query. for reference on names of columns see alarm database in optix studio
		string output = $"SELECT {language} AS Message, Severity, Time, LocalTime, {colReceiveTime} FROM {eventLogger} {initSort}{timestampResult}{severity}{text} ORDER BY {colReceiveTime} DESC";
		query.Value = output;
		Log.Info("QueryFormatter", output);
	}

	private IUAVariable query;
	private IUAVariable languageVariable;
	private IUAVariable dbTable;
	private IUAVariable fromTimestampVariable;
	private IUAVariable toTimestampVariable;
	private IUAVariable severityVariable;
	private IUAVariable textVariable;
	private IUAVariable fromTimestampVariableFormatted;
	private IUAVariable toTimestampVariableFormatted;
	private IUAVariable severityVariableFormatted;
	private IUAVariable textVariableFormatted;
	private GridLayout detailsColumn;
	private string language;
	private string eventLogger;
	private string fromTimestamp;
	private string toTimestamp;
	private string severity;
	private string text;
	private string activeColumn;
	private string colReceiveTime;
}
