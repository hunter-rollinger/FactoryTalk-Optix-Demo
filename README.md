This readme will explain what the intent for all of the NetLogic items.

1. SetColors design time netlogic will apply dynamic link colors to on screen elements.
2. UnusedTranslationKeys will generate a txt file with all unused translation items.
3. GenerateOptixAlarms will generate Optimate alarms in FactoryTalkOptix using the given parameters.
    - input: an array of alarm items
    - output: an optix alarm linked back to the plc item
4. CustomAlarming is the netlogic routine which handles the alarm popups and alarm banner.
    - many exposed parameters are only exposed to reduce interaction with the base netlogic code.
    - this routine also filters out the duplicate alarms which are caused by how optix tracks alarm history.
    - it also where the stop alarm is held. this can be used to track for the top 10 stop reasons later on.
5. ConstrainedVariablesVerifier will verify that the variable constraints are met.
    - this routine will not force variables to stay within their constraints but it will log an error that they went out of bounds.
    - in order to actually constrain variables limits must be set in the plc OR variable change events can be configured in optix.
6. AppendToEventLogging is a routine which appends the recipe name to each variable audit event as they come in.
7. IdleTimeoutLogic controls the screensaver.
8. ScreenControl controls the screen mode change between run/manual/maintenenance/burger. Use this for changing modes DO NOT do any other method.
9. GetCurrentUserInfo is how we obtain the current role which populates on the logout screen. It can be used for more past this but this is all it does at this time.
10. BackProvider is the routine which controls the forward and backward buttons logic. It stores the NodeID of the screens in an infinite list.
11. AlarmQueryFormatter is what controls the alarm query on the alarm history screen.
12. EventQueryFormatter is what controls the event query on the event history screen.
13. CalendarPickerLogic was imported from the Optix Library along with the calendar picker. I did not create this.
14. NodeCounterDialogLogic was imported from the Optix Library for the Node Counter screen element.
15. ProductionStatisticsChart_NetLogic controls the loading of the production statistics chart widget.
16. ProductionTimeChart_NetLogic controls the loading of the production time chart widget.
17. CollapseMenuControl was never fully developed. Presumeably it would work if it had a configured widget but I never bothered.
18. CustomAlarmBehaviour is not used. It can be deleted.
19. SystemInformationCollection is to be used to collect all necessary HMI and PLC system information.
