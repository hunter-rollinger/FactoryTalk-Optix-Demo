#region Using directives
using System;
using CoreBase = FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using System.Linq;
using UAManagedCore.Logging;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using System.Collections.Generic;
using System.Threading;
using FTOptix.InfluxDBStore;
using FTOptix.InfluxDBStoreLocal;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.ODBCStore;
using FTOptix.SerialPort;
using FTOptix.EventLogger;
using System.Security.Claims;
using FTOptix.UI;
using FTOptix.NativeUI;
using System.Linq.Expressions;
using FTOptix.Core;
using FTOptix.OPCUAServer;
using FTOptix.Recipe;
using FTOptix.OPCUAClient;
using FTOptix.AuditSigning;
using FTOptix.System;
using FTOptix.EdgeAppPlatform;
using FTOptix.TwinCAT;
using FTOptix.Report;
using FTOptix.RecipeX;
using System.Diagnostics;
#endregion

public class CustomAlarming : BaseNetLogic
{
    public override void Start()
    {
        var context = LogicObject.Context;
        affinityId = context.AssignAffinityId();

        RegisterObserverOnLocalizedAlarmsContainer(context);
        RegisterObserverOnSessionActualLanguageChange(context);
        RegisterObserverOnLocalizedAlarmsObject(context);
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
        var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);

        retainedAlarmsObjectObserver = new RetainedAlarmsObjectObserver((ctx) => RegisterObserverOnLocalizedAlarmsContainer(ctx));

        // observe ReferenceAdded of localized alarm containers
        alarmEventRegistration2 = retainedAlarms.RegisterEventObserver(
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
        alarmHandler = new AlarmHandler(LogicObject, localizedAlarmsContainer);

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
        public AlarmHandler(IUANode logicNode, IUANode localizedAlarmsContainer)
        {
            this.retaiendAlarms = new List<NodeId>();
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

            retainedAlarmsCount.Value = retaiendAlarms.Count;

            rotationTask = new PeriodicTask(() => { lock (retainedAlarmsLock) { DisplayNextAlarm(); } }, rotationTime.Value, logicNode);
        }
        public void Initialize()
        {
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
        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            lock (retainedAlarmsLock)
            {
                retaiendAlarms.Add(targetNode.NodeId);
                retainedAlarmsCount.Value = retaiendAlarms.Count;

                if (!RotationRunning)
                    Initialize();

                ++retainedAlarmsCount.Value;

                if(stopAlarm.Value == null)
                {
                    stopAlarm.Value = targetNode.NodeId;
                }                
                
                latestAlarm.Value = targetNode.NodeId;

                UpdateCurrentDisplayedAlarm();
            }
        }
        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            lock (retainedAlarmsLock)
            {
                var alarmIndex = retaiendAlarms.IndexOf(targetNode.NodeId);
                if (alarmIndex == -1)
                {
                    Log.Error($"The alarm {targetNode.NodeId} was not fond in the alarm banner list");
                    return;
                }

                if (alarmIndex < currentIndex)
                    currentIndex--;

                retaiendAlarms.RemoveAt(alarmIndex);

                if (currentIndex == retaiendAlarms.Count)
                    currentIndex = 0;

                retainedAlarmsCount.Value = retaiendAlarms.Count;

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

                IContext context = logicNode.Context;
                var retainedAlarms = context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);
                var localizedAlarmsVariable = retainedAlarms.GetVariable("LocalizedAlarms");
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
;
                if (GetActiveAlarmIDs().Count == 0)
                    popupTriggered = false;
            }
        }
        private List<int> GetActiveAlarmIDs()
        {
            List<int> alarmIDs = [];

            for (int i = 0; i < retaiendAlarms.Count; ++i)
            {
                var alarmID = retaiendAlarms[i];
                if (alarmID != null)
                {
                    IUANode alarmController = InformationModel.Get(alarmID);
                    if (InformationModel.Get<AlarmController>(alarmController.GetVariable("ConditionId").Value) != null)
                    {
                        DigitalAlarm alarm = InformationModel.Get<DigitalAlarm>(alarmController.GetVariable("ConditionId").Value);
                        if (alarm.Severity == 1000)
                        {
                            alarmIDs.Add(i);
                        }
                    }
                }
            }

            return alarmIDs;
        }
        private void InitializeRetainedAlarmList(IUANode localizedAlarmsContainer)
        {
            foreach (var localizedAlarm in localizedAlarmsContainer.Children)
                retaiendAlarms.Add(localizedAlarm.NodeId);
        }
        private void UpdateCurrentDisplayedAlarm()
        {
            //Debugger.Launch();
            List<int> alarmIDs = GetActiveAlarmIDs();

            if (retaiendAlarms.Count == 0)
            {
                currentDisplayedAlarmIndex.Value = 0;
                currentDisplayedAlarm.Value = NodeId.Empty;
            }
            else if (alarmIDs.Count != 0 && !bannerRotation.Value)
            {
                currentDisplayedAlarmIndex.Value = alarmIDs.Min();
                currentDisplayedAlarm.Value = retaiendAlarms[alarmIDs.Min()];
            }
            else if (retaiendAlarms.Count > 0 && !bannerRotation.Value)
            {
                currentDisplayedAlarmIndex.Value = 0;
                currentDisplayedAlarm.Value = retaiendAlarms[0];
            }
            else
            {
                currentDisplayedAlarmIndex.Value = currentIndex;
                currentDisplayedAlarm.Value = retaiendAlarms[currentIndex];
            }

            if (alarmIDs.Count == 1 && !popupTriggered)
            {
                popupTriggered = true;
                PopupTrigger(retaiendAlarms[alarmIDs.Min()]);
            }
        }

        #region banner rotation
        public bool RotationRunning { get; private set; }
        public NodeId CurrentDisplayedAlarmNodeId
        {
            get { return currentDisplayedAlarm.Value; }
        }
        private void RotationTime_VariableChange(object sender, VariableChangeEventArgs e)
        {
            var wasRunning = RotationRunning;
            StopRotation();
            if (bannerRotation.Value)
            {
                rotationTask = new PeriodicTask(DisplayNextAlarm, e.NewValue, logicNode);
            }
            if (wasRunning)
                StartRotation();
        }
        private void BannerRotationToggle(object sender, VariableChangeEventArgs e)
        {
            if (bannerRotation.Value)
            {
                StartRotation();
            }
            else
            {
                StopRotation();
            }
        }
        private void StopRotation()
        {
            if (!RotationRunning)
                return;

            rotationTask.Cancel();
            RotationRunning = false;
            skipFirstCallBack = false;

            UpdateCurrentDisplayedAlarm();
        }
        private void StartRotation()
        {
            if (RotationRunning)
                return;

            rotationTask.Start();
            RotationRunning = true;
            skipFirstCallBack = true;
        }
        private void DisplayNextAlarm()
        {
            if (skipFirstCallBack)
            {
                skipFirstCallBack = false;
                return;
            }

            var nextIndex = currentIndex + 1 >= retaiendAlarms.Count ? 0 : currentIndex + 1;
            currentIndex = nextIndex;
            UpdateCurrentDisplayedAlarm();
        }
        #endregion

        #region alarm popup
        private void PopupTrigger(NodeId alarm)
        {
            DialogType alarmPopup = (DialogType)Project.Current.Get("UI/Screens/Popups/AlarmPopup");

            try
            {
                IUANode native = Project.Current.Get("UI/NativePresentationEngine");    // get native presentation engine
                IUANode native_sessions = native.Get("Sessions");                       // get all native sessions
                IUANode native_session = native_sessions.Children[1];                   // get first (and only) native session
                IUANode native_window = native_session.Get("UIRoot");                   // get main window of active native session
                if (native_window != null)                                              // check if native window exists
                {                                                                       //
                    CloseAllAlarmPopups((Session)native_session);                       // close any currently open alarm popups
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
                        CloseAllAlarmPopups((Session)web_session);              // close any currently open alarm popups
                        UICommands.OpenDialog(web_window, alarmPopup, alarm);   //
                    }                                                           //
                }                                                               //
            }
            catch { } // if try fails web sessions do not exist (this is fine if true)
        }
        private void CloseAllAlarmPopups(Session session)
        {
            foreach (Dialog item in session.Get("UIRoot").Children.OfType<Dialog>().ToList())
            {
                if (item.BrowseName.ToLowerInvariant() == "alarmpopup" || item.BrowseName.ToLowerInvariant() == "screensaver")
                {
                    item.Close(); // close any currently open alarm popups
                }
            }
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
        private List<NodeId> retaiendAlarms;            // alarm container
        private Object retainedAlarmsLock;              // alarm container
        public bool popupTriggered;                     // automatic alarmPopup

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

    uint affinityId = 0;
    AlarmHandler alarmHandler;
    RetainedAlarmsObjectObserver retainedAlarmsObjectObserver;
    IEventRegistration alarmEventRegistration;
    IEventRegistration alarmEventRegistration2;
    IEventRegistration sessionActualLanguageRegistration;
    IEventObserver sessionActualLanguageChangeObserver;
}
