#region Using directives
using FTOptix.Core;
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.OPCUAServer;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.DataLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.ODBCStore;
using FTOptix.InfluxDBStoreLocal;
using FTOptix.InfluxDBStore;
using FTOptix.SerialPort;
using FTOptix.EventLogger;
using System.Linq;
using System.Collections.Generic;
using FTOptix.Recipe;
using FTOptix.OPCUAClient;
using FTOptix.AuditSigning;
using System.Diagnostics;
using System.Timers;
using FTOptix.System;
using FTOptix.EdgeAppPlatform;
using FTOptix.MQTTClient;
using System.Runtime.CompilerServices;
using FTOptix.TwinCAT;
using FTOptix.Report;
using FTOptix.RecipeX;
#endregion

public class IdleTimeoutLogic : BaseNetLogic
{
    public override void Start()
    {
        fadeDuration = LogicObject.GetVariable("FadeDuration");
        duration = LogicObject.GetVariable("Duration");
        enabled = LogicObject.GetVariable("Enabled");
        logout = LogicObject.GetVariable("Logout");
        fade = LogicObject.GetVariable("Fade");
        uiSession = Session as UISession;

        enabled.VariableChange += Enabled_VariableChange;
        duration.VariableChange += Duration_VariableChange;

        uiSession.OnIdleTimeout += UiSession_OnIdleTimeout;
        uiSession.IdleTimeoutEnabled = enabled.Value;
        uiSession.IdleTimeoutDuration = TimeSpan.FromMilliseconds(duration.Value);

        screensaver = (DialogType)Project.Current.Get("UI/Screens/Popups/Screensaver");

        fadeDelay = new Timer(fadeDuration.Value / 100);
        fadeDelay.AutoReset = true;
        fadeDelay.Elapsed += FadeDelay;
    }
    private void Enabled_VariableChange(object sender, VariableChangeEventArgs e)
    {
        uiSession.IdleTimeoutEnabled = e.NewValue;
    }
    private void Duration_VariableChange(object sender, VariableChangeEventArgs e)
    {
        uiSession.IdleTimeoutDuration = TimeSpan.FromMilliseconds(e.NewValue);
    }
    private void UiSession_OnIdleTimeout(object sender, IdleTimeoutEvent e)
    {
        IUANode uiRoot = uiSession.Get("UIRoot");

        // check if session is null and if any dialog windows are open
        if (uiRoot != null && uiRoot.Children.OfType<Dialog>().ToList().Count == 0)
        {
            UICommands.OpenDialog(uiRoot, screensaver);

            if (fade.Value)
                fadeDelay.Start();
            else
                uiSession.Get("UIRoot").Children.OfType<Dialog>().ToList().Last().Opacity = 100;

            if (logout.Value)
                uiSession.Logout();
        }
    }
    private void FadeDelay(object source, ElapsedEventArgs e)
    {
        Dialog ss = uiSession.Get("UIRoot").Children.OfType<Dialog>().ToList().Last();
        ss.Opacity++;
        if (ss.Opacity > 100)
            fadeDelay.Stop();
    }
    public override void Stop()
    {
        if (uiSession != null)
            uiSession.OnIdleTimeout -= UiSession_OnIdleTimeout;
        if (enabled != null)
            enabled.VariableChange -= Enabled_VariableChange;
        if (duration != null)
            duration.VariableChange -= Duration_VariableChange;
        fadeDelay.Dispose();
    }

    private IUAVariable fade;
    private IUAVariable logout;
    private DialogType screensaver;
    private Timer fadeDelay;
    private UISession uiSession;
    private IUAVariable duration;
    private IUAVariable enabled;
    private IUAVariable fadeDuration;
}
