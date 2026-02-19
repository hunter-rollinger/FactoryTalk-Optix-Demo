using System;
using UAManagedCore;

//-------------------------------------------
// WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
//-------------------------------------------

[MapType(NamespaceUri = "OptimateDemoProject", Guid = "c9885f166221defeec7e44d97feaed9a")]
public class AlarmSummaryConfiguration : UAObject
{
#region Children properties
    //-------------------------------------------
    // WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
    //-------------------------------------------
    public object AlarmState
    {
        get
        {
            return (object)Refs.GetVariable("AlarmState").Value.Value;
        }
        set
        {
            Refs.GetVariable("AlarmState").SetValue(value);
        }
    }
    public IUAVariable AlarmStateVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("AlarmState");
        }
    }
    public object EventTime
    {
        get
        {
            return (object)Refs.GetVariable("EventTime").Value.Value;
        }
        set
        {
            Refs.GetVariable("EventTime").SetValue(value);
        }
    }
    public IUAVariable EventTimeVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("EventTime");
        }
    }
    public object Priority
    {
        get
        {
            return (object)Refs.GetVariable("Priority").Value.Value;
        }
        set
        {
            Refs.GetVariable("Priority").SetValue(value);
        }
    }
    public IUAVariable PriorityVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Priority");
        }
    }
    public object Severity
    {
        get
        {
            return (object)Refs.GetVariable("Severity").Value.Value;
        }
        set
        {
            Refs.GetVariable("Severity").SetValue(value);
        }
    }
    public IUAVariable SeverityVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Severity");
        }
    }
    public object AlarmStatus
    {
        get
        {
            return (object)Refs.GetVariable("AlarmStatus").Value.Value;
        }
        set
        {
            Refs.GetVariable("AlarmStatus").SetValue(value);
        }
    }
    public IUAVariable AlarmStatusVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("AlarmStatus");
        }
    }
#endregion
}
