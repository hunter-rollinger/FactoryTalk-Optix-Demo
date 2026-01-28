using System;
using UAManagedCore;

//-------------------------------------------
// WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
//-------------------------------------------

[MapType(NamespaceUri = "OptimateDemoProject", Guid = "6ccddd5c00c29a601137e97985d5d4b3")]
public class FiltersConfiguration : UAObject
{
#region Children properties
    //-------------------------------------------
    // WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
    //-------------------------------------------
    public bool AlarmState
    {
        get
        {
            return (bool)Refs.GetVariable("AlarmState").Value.Value;
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
    public bool Name
    {
        get
        {
            return (bool)Refs.GetVariable("Name").Value.Value;
        }
        set
        {
            Refs.GetVariable("Name").SetValue(value);
        }
    }
    public IUAVariable NameVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Name");
        }
    }
    public bool Class
    {
        get
        {
            return (bool)Refs.GetVariable("Class").Value.Value;
        }
        set
        {
            Refs.GetVariable("Class").SetValue(value);
        }
    }
    public IUAVariable ClassVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Class");
        }
    }
    public bool EventTime
    {
        get
        {
            return (bool)Refs.GetVariable("EventTime").Value.Value;
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
    public bool Group
    {
        get
        {
            return (bool)Refs.GetVariable("Group").Value.Value;
        }
        set
        {
            Refs.GetVariable("Group").SetValue(value);
        }
    }
    public IUAVariable GroupVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Group");
        }
    }
    public bool Inhibit
    {
        get
        {
            return (bool)Refs.GetVariable("Inhibit").Value.Value;
        }
        set
        {
            Refs.GetVariable("Inhibit").SetValue(value);
        }
    }
    public IUAVariable InhibitVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Inhibit");
        }
    }
    public bool Message
    {
        get
        {
            return (bool)Refs.GetVariable("Message").Value.Value;
        }
        set
        {
            Refs.GetVariable("Message").SetValue(value);
        }
    }
    public IUAVariable MessageVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Message");
        }
    }
    public bool Priority
    {
        get
        {
            return (bool)Refs.GetVariable("Priority").Value.Value;
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
    public bool Severity
    {
        get
        {
            return (bool)Refs.GetVariable("Severity").Value.Value;
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
    public bool AlarmStatus
    {
        get
        {
            return (bool)Refs.GetVariable("AlarmStatus").Value.Value;
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
