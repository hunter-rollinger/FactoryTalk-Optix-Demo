#region Using directives
using FTOptix.Alarm;
using FTOptix.AuditSigning;
using FTOptix.Core;
using FTOptix.DataLogger;
using FTOptix.EdgeAppPlatform;
using FTOptix.EventLogger;
using FTOptix.HMIProject;
using FTOptix.InfluxDBStore;
using FTOptix.InfluxDBStoreLocal;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.ODBCStore;
using FTOptix.OPCUAClient;
using FTOptix.OPCUAServer;
using FTOptix.Recipe;
using FTOptix.RecipeX;
using FTOptix.Report;
using FTOptix.SerialPort;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.System;
using FTOptix.TwinCAT;
using FTOptix.UI;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.PortableExecutable;
using System.Security.Claims;
using System.Threading;
using UAManagedCore;
using UAManagedCore.Logging;
using CoreBase = FTOptix.CoreBase;

#endregion

public class CustomAlarming : BaseNetLogic
{
	#region startup / stop
	public override void Start() {
        Debugger.Launch();
		burgerMenuScreen = (BurgerMenu)InformationModel.Get((NodeId)LogicObject.GetVariable("BurgerMenuScreen").Value);
		debugMenu = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("DebugMenu").Value);
		standardQuickInfo = (PanelLoader)InformationModel.Get((NodeId)LogicObject.GetVariable("StandardQuickInfo").Value);
		emptyPanel = (NodeId)LogicObject.GetVariable("EmptyScreen").Value;

		var context = LogicObject.Context;
        affinityId = context.AssignAffinityId();

        RegisterObserverOnLocalizedAlarmsContainer(context);
        RegisterObserverOnSessionActualLanguageChange(context);
        RegisterObserverOnLocalizedAlarmsObject(context);
        startup = true;
    }
    public override void Stop()
    {
        alarmEventRegistration?.Dispose();
        alarmEventRegistration2?.Dispose();
        sessionActualLanguageRegistration?.Dispose();
        alarmHandler?.Dispose();

        alarmEventRegistration = null;
        alarmEventRegistration2 = null;
        sessionActualLanguageRegistration = null;
        alarmHandler = null;
        retainedAlarmsObjectObserver = null;
    }
    public void RegisterObserverOnLocalizedAlarmsObject(IContext context)
    {
        var localRetainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);

        retainedAlarmsObjectObserver = new RetainedAlarmsObjectObserver((ctx) => RegisterObserverOnLocalizedAlarmsContainer(ctx));

        // observe ReferenceAdded of localized alarm containers
        alarmEventRegistration2 = localRetainedAlarms.RegisterEventObserver(
            retainedAlarmsObjectObserver, EventType.ForwardReferenceAdded, affinityId);
    }
    public void RegisterObserverOnLocalizedAlarmsContainer(IContext context)
    {
        var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);
        var localizedAlarmsVariable = retainedAlarms.GetVariable("LocalizedAlarms");
        var localizedAlarmsNodeId = (NodeId)localizedAlarmsVariable.Value;
        IUANode localizedAlarmsContainer = null;
        if (localizedAlarmsNodeId != null && !localizedAlarmsNodeId.IsEmpty)
            localizedAlarmsContainer = context.GetNode(localizedAlarmsNodeId);

        if (alarmEventRegistration != null)
        {
            alarmEventRegistration.Dispose();
            alarmEventRegistration = null;
        }

        if (alarmHandler != null)
            alarmHandler.Dispose();
        alarmHandler = new AlarmHandler(this, LogicObject, localizedAlarmsContainer);

        if (localizedAlarmsContainer?.Children.Count > 0)
            alarmHandler.Initialize();

        alarmEventRegistration = localizedAlarmsContainer?.RegisterEventObserver(
            alarmHandler,
            EventType.ForwardReferenceAdded | EventType.ForwardReferenceRemoved, affinityId);
    }
    public void RegisterObserverOnSessionActualLanguageChange(IContext context)
    {
        var currentSessionActualLanguage = context.Sessions.CurrentSessionInfo.SessionObject.Children["ActualLanguage"];

#pragma warning disable CS0618 // Type or member is obsolete
		sessionActualLanguageChangeObserver = new CallbackVariableChangeObserver(
            (IUAVariable variable, UAValue newValue, UAValue oldValue, uint[] indexes, ulong senderId) =>
            {
                RegisterObserverOnLocalizedAlarmsContainer(context);
            });
#pragma warning restore CS0618 // Type or member is obsolete

		sessionActualLanguageRegistration = currentSessionActualLanguage.RegisterEventObserver(
            sessionActualLanguageChangeObserver, EventType.VariableValueChanged, affinityId);
    }
	#endregion startup / stop
	private class RetainedAlarmsObjectObserver : IReferenceObserver
    {
        public RetainedAlarmsObjectObserver(Action<IContext> action)
        {
            registrationCallback = action;
        }
        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            string localeId = targetNode.Context.Sessions.CurrentSessionHandler.ActualLocaleId;
            if (String.IsNullOrEmpty(localeId))
                localeId = "en-US";

            if (targetNode.BrowseName == localeId)
                registrationCallback(targetNode.Context);
        }
        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
        }
        private Action<IContext> registrationCallback;
    }
    public class AlarmHandler : IDisposable, IReferenceObserver
    {
		#region initialize
		private readonly CustomAlarming parent;
        public AlarmHandler(CustomAlarming parent, IUANode logicNode, IUANode localizedAlarmsContainer) {
            this.parent = parent;
            this.retainedAlarms = new List<NodeId>();
            this.retainedAlarmsLock = new Object();
            this.logicNode = logicNode;
            InitializeRetainedAlarmList(localizedAlarmsContainer);

            currentDisplayedAlarm = logicNode.GetVariable("CurrentDisplayedAlarm");
            currentDisplayedAlarmIndex = logicNode.GetVariable("CurrentDisplayedAlarmIndex");
            retainedAlarmsCount = logicNode.GetVariable("AlarmCount");
            rotationTime = logicNode.GetVariable("RotationTime");
            rotationTime.VariableChange += RotationTime_VariableChange;
            bannerRotation = logicNode.GetVariable("BannerRotation");
            bannerRotation.VariableChange += BannerRotationToggle;
            alarmsDB = InformationModel.Get<SQLiteStore>(logicNode.GetVariable("AlarmsDatabase").Value);
            dbTable = InformationModel.Get(logicNode.GetVariable("DatabaseTable").Value).BrowseName;
            colReceiveTime = InformationModel.Get(logicNode.GetVariable("TableReceiveTime").Value).BrowseName;
			colNodeId = InformationModel.Get(logicNode.GetVariable("TableAlarmNodeId").Value).BrowseName;
			colUtcTime = InformationModel.Get(logicNode.GetVariable("TableUtcTime").Value).BrowseName;
			colLocalTime = InformationModel.Get(logicNode.GetVariable("TableLocalTime").Value).BrowseName;
			colCondName = InformationModel.Get(logicNode.GetVariable("TableConditionName").Value).BrowseName;

			retainedAlarmsCount.Value = retainedAlarms.Count;

            rotationTask = new PeriodicTask(() => { lock (retainedAlarmsLock) { DisplayNextAlarm(); } }, rotationTime.Value, logicNode);
        }
        public void Initialize()
        {
            //Debugger.Launch();
            currentIndex = 0;
            UpdateCurrentDisplayedAlarm();
            if (RotationRunning)
                StopRotation();
            if (bannerRotation.Value)
                StartRotation();

            retainedAlarmsCount = logicNode.GetVariable("AlarmCount");
            stopAlarm = logicNode.GetVariable("StopAlarm");
            latestAlarm = logicNode.GetVariable("LatestAlarm");
            bannerRotation = logicNode.GetVariable("BannerRotation");
            alarmSeverityLevel = logicNode.GetVariable("AlarmSeverityLevel").Value;

            IContext context = logicNode.Context;
            var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);
            var localizedAlarmsVariable = retainedAlarms.GetVariable("LocalizedAlarms");
            var localizedAlarmsNodeId = (NodeId)localizedAlarmsVariable.Value;
            IUANode localizedAlarmsContainer = null;
            if (!localizedAlarmsNodeId.IsEmpty)
                localizedAlarmsContainer = context.GetNode(localizedAlarmsNodeId);

            if (localizedAlarmsContainer == null || !localizedAlarmsContainer.Children.Any())
            {
                retainedAlarmsCount.Value = 0;
                stopAlarm.Value = NodeId.Empty;
                latestAlarm.Value = NodeId.Empty;
                return;
            }

            stopAlarm.Value = localizedAlarmsContainer.Children.First()?.NodeId ?? NodeId.Empty;
            latestAlarm.Value = localizedAlarmsContainer.Children.Last()?.NodeId ?? NodeId.Empty;
            retainedAlarmsCount.Value = localizedAlarmsContainer.Children.Count;
		}
		private void InitializeRetainedAlarmList(IUANode localizedAlarmsContainer) {
			foreach (var localizedAlarm in localizedAlarmsContainer.Children)
				retainedAlarms.Add(localizedAlarm.NodeId);
		}
		#endregion
		#region alarm added / removed
		public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            lock (retainedAlarmsLock)
            {
                retainedAlarms.Add(targetNode.NodeId);
                retainedAlarmsCount.Value = retainedAlarms.Count;

                if (!RotationRunning)
                    Initialize();

                ++retainedAlarmsCount.Value;

                if(stopAlarm.Value == null)
                {
                    stopAlarm.Value = targetNode.NodeId;
                }                
                
                latestAlarm.Value = targetNode.NodeId;

                UpdateCurrentDisplayedAlarm();

				object[,] result;
				string[] header;
                try {
                    string query = $"UPDATE {dbTable} SET {colReceiveTime}=(SELECT {colUtcTime} FROM {dbTable} WHERE {colCondName}='{targetNode.BrowseName}' ORDER BY {colUtcTime} DESC LIMIT 1), {colNodeId}='{targetNode.NodeId}' WHERE rowid=(SELECT rowid FROM {dbTable} ORDER BY {colUtcTime} DESC LIMIT 1)";
					Log.Info("CustomAlarming", $"Executing query to set NodeId for latest alarm: {query}");
					alarmsDB.Query(query, out header, out result);
                } catch { Log.Error("CustomAlarming", "Error when setting NodeId for the latest alarm. Start checking at end of OnReferenceAdded method in NetLogic."); }
			}
        }
        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            lock (retainedAlarmsLock) {
				object[,] result;
				string[] header;
                try {
                    // get row info
                    string query = $"SELECT {colReceiveTime} FROM {dbTable} WHERE {colNodeId}='{targetNode.NodeId}'";
                    Log.Info("CustomAlarming", $"Executing query to set NodeId for latest alarm: {query}");
                    alarmsDB.Query(query, out header, out result);
                    DateTime dtObject = (DateTime)result[0, 0];
                    string dateTime = dtObject.ToString("yyyy-MM-ddTHH:mm:ss:fffffff");

                    // set row info
                    query = $"UPDATE {dbTable} SET {colReceiveTime}='{dateTime}', {colNodeId}='{targetNode.NodeId}' WHERE rowid=(SELECT rowid FROM {dbTable} ORDER BY {colUtcTime} DESC LIMIT 1)";
					Log.Info("CustomAlarming", $"Executing query to set NodeId for latest alarm: {query}");
					alarmsDB.Query(query, out header, out result);
				} catch { Log.Error("CustomAlarming", "DB Query Error. See OnReferenceRemoved in NetLogic."); }

				var alarmIndex = retainedAlarms.IndexOf(targetNode.NodeId);
                if (alarmIndex == -1) {
                    Log.Error($"The alarm {targetNode.NodeId} was not fond in the alarm banner list");
                    return;
                }

                if (alarmIndex < currentIndex)
                    currentIndex--;

                retainedAlarms.RemoveAt(alarmIndex);

                if (currentIndex == retainedAlarms.Count)
                    currentIndex = 0;

                retainedAlarmsCount.Value = retainedAlarms.Count;

                if (retainedAlarmsCount.Value == 0)
                {
                    StopRotation();
                    stopAlarm.Value = targetNode.NodeId;
                }
                else if (CurrentDisplayedAlarmNodeId == targetNode.NodeId)
                {
                    UpdateCurrentDisplayedAlarm();
                    StopRotation();
                    if (bannerRotation.Value)
                    {
                        StartRotation();
                    }
				}
;
				if (GetActiveAlarmIDs().Count == 0)
					popupTriggered = false;

				IContext context = logicNode.Context;
                var localRetainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);
                var localizedAlarmsVariable = localRetainedAlarms.GetVariable("LocalizedAlarms");
                var localizedAlarmsNodeId = (NodeId)localizedAlarmsVariable.Value;
                IUANode localizedAlarmsContainer = null;
                if (localizedAlarmsNodeId != null && !localizedAlarmsNodeId.IsEmpty)
                    localizedAlarmsContainer = context.GetNode(localizedAlarmsNodeId);

                if (localizedAlarmsContainer == null || !localizedAlarmsContainer.Children.Any())
                {
                    retainedAlarmsCount.Value = 0;
                    stopAlarm.Value = NodeId.Empty;
                    latestAlarm.Value = NodeId.Empty;
                    return;
                }

                stopAlarm.Value = localizedAlarmsContainer.Children.First()?.NodeId ?? NodeId.Empty;
                latestAlarm.Value = localizedAlarmsContainer.Children.Last()?.NodeId ?? NodeId.Empty;
                retainedAlarmsCount.Value = localizedAlarmsContainer.Children.Count;
            }
        }
        private List<int> GetActiveAlarmIDs()
        {
            List<int> alarmIDs = [];

            for (int i = 0; i < retainedAlarms.Count; ++i)
            {
                var alarmID = retainedAlarms[i];
                if (alarmID != null)
                {
                    IUANode alarmController = InformationModel.Get(alarmID);
                    if (InformationModel.Get<AlarmController>(alarmController.GetVariable("ConditionId").Value) != null)
                    {
                        DigitalAlarm alarm = InformationModel.Get<DigitalAlarm>(alarmController.GetVariable("ConditionId").Value);
                        if (alarm.Severity == alarmSeverityLevel)
                        {
                            alarmIDs.Add(i);
                        }
                    }
                }
            }

            return alarmIDs;
        }
        #endregion
        #region alarm popup
        private void UpdateCurrentDisplayedAlarm()
        {
            List<int> alarmIDs = GetActiveAlarmIDs();

            if (retainedAlarms.Count == 0)
            {
                currentDisplayedAlarmIndex.Value = 0;
                currentDisplayedAlarm.Value = NodeId.Empty;
            }
            else if (alarmIDs.Count != 0 && !bannerRotation.Value)
            {
                currentDisplayedAlarmIndex.Value = alarmIDs.Min();
                currentDisplayedAlarm.Value = retainedAlarms[alarmIDs.Min()];
            }
            else if (retainedAlarms.Count > 0 && !bannerRotation.Value)
            {
                currentDisplayedAlarmIndex.Value = 0;
                currentDisplayedAlarm.Value = retainedAlarms[0];
            }
            else
            {
                currentDisplayedAlarmIndex.Value = currentIndex;
                currentDisplayedAlarm.Value = retainedAlarms[currentIndex];
            }

            if (alarmIDs.Count == 1 && !popupTriggered && parent.startup)
            {
                popupTriggered = true;
                PopupTrigger(retainedAlarms[alarmIDs.Min()]);
            }
        }
        private void PopupTrigger(NodeId alarm)
        {
            var test = InformationModel.Get(alarm);
            if (test.BrowseName == "PLC_Comm_Lost")
                return;

            DialogType alarmPopup = (DialogType)Project.Current.Get("UI/Screens/Popups/AlarmPopup");

            try
            {
                IUANode native = Project.Current.Get("UI/NativePresentationEngine");    // get native presentation engine
                IUANode native_sessions = native.Get("Sessions");                       // get all native sessions
                IUANode native_session = native_sessions.Children[1];                   // get first (and only) native session
                IUANode native_window = native_session.Get("UIRoot");                   // get main window of active native session
                if (native_window != null)                                              // check if native window exists
                {                                                                       //
                    CloseAllPopups((Session)native_session);                       // close any currently open alarm popups
                    UICommands.OpenDialog(native_window, alarmPopup, alarm);            // trigger popup on native session
                }                                                                       //   
            }
            catch { } // if try fails native session does not exist (this is fine if true)
            
            try
            {
                IUANode web = Project.Current.Get("UI/WebPresentationEngine");  // get web presentation engine
                IUANode web_sessions = web.Get("Sessions");                     // get all web sessions
                foreach (IUANode web_session in web_sessions.Children)          //
                {                                                               //
                    IUANode web_window = web_session.Get("UIRoot");             //
                    if (web_window != null)                                     // multi-client alarm
                    {                                                           // popup handling
                        CloseAllPopups((Session)web_session);              // close any currently open alarm popups
                        UICommands.OpenDialog(web_window, alarmPopup, alarm);   //
                    }                                                           //
                }                                                               //
            }
            catch { } // if try fails web sessions do not exist (this is fine if true)
        }
        public void CloseAllPopups(Session session) {
			// operate on the session's UIRoot instances, not project-level nodes
			var uiRoot = session.Get("UIRoot");
			if (uiRoot != null) {
				var sessionDebug = uiRoot.Children["Debug_Menu"] as PanelLoader;
				if (sessionDebug != null)
					sessionDebug.ChangePanel(parent.emptyPanel);

				var sessionQuickInfo = uiRoot.Children["StandardQuickInfo"] as PanelLoader;
				if (sessionQuickInfo != null)
					sessionQuickInfo.ChangePanel(parent.emptyPanel);

				var sessionBurger = uiRoot.Children["BurgerMenu"] as BurgerMenu;
				if (sessionBurger != null)
					sessionBurger.Visible = false;
			}

			foreach (Dialog item in uiRoot?.Children.OfType<Dialog>().ToList() ?? new List<Dialog>())
				item.Close();
		}
		#endregion
		#region banner rotation
		public bool RotationRunning { get; private set; }
		public NodeId CurrentDisplayedAlarmNodeId {
			get { return currentDisplayedAlarm.Value; }
		}
		private void RotationTime_VariableChange(object sender, VariableChangeEventArgs e) {
			var wasRunning = RotationRunning;
			StopRotation();
			if (bannerRotation.Value) {
				rotationTask = new PeriodicTask(DisplayNextAlarm, e.NewValue, logicNode);
			}
			if (wasRunning)
				StartRotation();
		}
		private void BannerRotationToggle(object sender, VariableChangeEventArgs e) {
			if (bannerRotation.Value) {
				StartRotation();
			} else {
				StopRotation();
			}
		}
		private void StopRotation() {
			if (!RotationRunning)
				return;

			rotationTask.Cancel();
			RotationRunning = false;
			skipFirstCallBack = false;

			UpdateCurrentDisplayedAlarm();
		}
		private void StartRotation() {
			if (RotationRunning)
				return;

			rotationTask.Start();
			RotationRunning = true;
			skipFirstCallBack = true;
		}
		private void DisplayNextAlarm() {
			if (skipFirstCallBack) {
				skipFirstCallBack = false;
				return;
			}

			var nextIndex = currentIndex + 1 >= retainedAlarms.Count ? 0 : currentIndex + 1;
			currentIndex = nextIndex;
			UpdateCurrentDisplayedAlarm();
		}
		#endregion

		private PeriodicTask rotationTask;              // rotation
        private IUAVariable currentDisplayedAlarm;      // rotation
        private IUAVariable currentDisplayedAlarmIndex; // rotation
        private IUAVariable rotationTime;               // rotation
        private IUAVariable retainedAlarmsCount;        // alarm container
        private IUANode logicNode;                      // node
        private IUAVariable stopAlarm;                  // automatic alarmPopup
        private IUAVariable latestAlarm;                // rotation
        private IUAVariable bannerRotation;             // rotation
        private bool skipFirstCallBack = false;         // rotation
        private int currentIndex = 0;                   // rotation
        private List<NodeId> retainedAlarms;            // alarm container
        private Object retainedAlarmsLock;              // alarm container
		public bool popupTriggered;                     // automatic alarmPopup
		private int alarmSeverityLevel;                 // alarm severity levl
        SQLiteStore alarmsDB;                           // get alarm database to add time to
		string dbTable;                                 // get table in database of which to modify
        string colReceiveTime;                          // column name in table to set receive time
		string colNodeId;                               // column in table to set nodeid for a uid to search
		string colUtcTime;                              // column in table to sort by
		string colLocalTime;                            // column in table to get timestamp from
		string colCondName;                             // column in table to get the condition name (variable) from

		#region IDisposable Support
		private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (disposedValue)
                return;

            if (disposing)
            {
                currentIndex = 0;
                UpdateCurrentDisplayedAlarm();
                StopRotation();
                rotationTask.Dispose();
            }

            disposedValue = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    private bool startup = false;

	public BurgerMenu burgerMenuScreen;
	public PanelLoader debugMenu;
	public PanelLoader standardQuickInfo;
	public NodeId emptyPanel;

	uint affinityId = 0;
    AlarmHandler alarmHandler;
    RetainedAlarmsObjectObserver retainedAlarmsObjectObserver;
    IEventRegistration alarmEventRegistration;
    IEventRegistration alarmEventRegistration2;
    IEventRegistration sessionActualLanguageRegistration;
    IEventObserver sessionActualLanguageChangeObserver;
}
