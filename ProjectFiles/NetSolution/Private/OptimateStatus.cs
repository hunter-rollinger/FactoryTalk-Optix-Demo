using System;
using UAManagedCore;

//-------------------------------------------
// WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
//-------------------------------------------

[MapType(NamespaceUri = "OptimateDemoProject", Guid = "05e862619b891247738e0618931b3d0e")]
public class OptimateStatus : FTOptix.Alarm.DigitalAlarm
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
#endregion
}
