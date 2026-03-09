using System;
using UAManagedCore;

//-------------------------------------------
// WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
//-------------------------------------------

[MapType(NamespaceUri = "OptimateDemoProject", Guid = "cad23cc01859ccdb433eb4694398e887")]
public class CustomAlarm : FTOptix.Alarm.DigitalAlarm
{
#region Children properties
    //-------------------------------------------
    // WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
    //-------------------------------------------
    public string Help_Text
    {
        get
        {
            return (string)Refs.GetVariable("Help_Text").Value.Value;
        }
        set
        {
            Refs.GetVariable("Help_Text").SetValue(value);
        }
    }
    public IUAVariable Help_TextVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Help_Text");
        }
    }
    public string Aux_Text
    {
        get
        {
            return (string)Refs.GetVariable("Aux_Text").Value.Value;
        }
        set
        {
            Refs.GetVariable("Aux_Text").SetValue(value);
        }
    }
    public IUAVariable Aux_TextVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Aux_Text");
        }
    }
    public FTOptix.Core.ResourceUri Image
    {
        get
        {
            return new FTOptix.Core.ResourceUri(Refs.GetVariable("Image").Value);
        }
        set
        {
            Refs.GetVariable("Image").SetValue((string)value);
        }
    }
    public IUAVariable ImageVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Image");
        }
    }
    public FTOptix.Core.ResourceUri Overview
    {
        get
        {
            return new FTOptix.Core.ResourceUri(Refs.GetVariable("Overview").Value);
        }
        set
        {
            Refs.GetVariable("Overview").SetValue((string)value);
        }
    }
    public IUAVariable OverviewVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Overview");
        }
    }
    public DateTime Timestamp
    {
        get
        {
            return (DateTime)Refs.GetVariable("Timestamp").Value.Value;
        }
        set
        {
            Refs.GetVariable("Timestamp").SetValue(value);
        }
    }
    public IUAVariable TimestampVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Timestamp");
        }
    }
#endregion
}
