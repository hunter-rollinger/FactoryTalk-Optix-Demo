using System;
using UAManagedCore;

//-------------------------------------------
// WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
//-------------------------------------------

[MapType(NamespaceUri = "OptimateDemoProject", Guid = "4a84ae9a0006f33b7fb9f23c79b0e367")]
public class DigitalFeedbackCounterDatatype : UAObject
{
#region Children properties
    //-------------------------------------------
    // WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
    //-------------------------------------------
    public int CounterNumber
    {
        get
        {
            return (int)Refs.GetVariable("CounterNumber").Value.Value;
        }
        set
        {
            Refs.GetVariable("CounterNumber").SetValue(value);
        }
    }
    public IUAVariable CounterNumberVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("CounterNumber");
        }
    }
    public float ActualPosition
    {
        get
        {
            return (float)Refs.GetVariable("ActualPosition").Value.Value;
        }
        set
        {
            Refs.GetVariable("ActualPosition").SetValue(value);
        }
    }
    public IUAVariable ActualPositionVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("ActualPosition");
        }
    }
    public float CalibrationPosition
    {
        get
        {
            return (float)Refs.GetVariable("CalibrationPosition").Value.Value;
        }
        set
        {
            Refs.GetVariable("CalibrationPosition").SetValue(value);
        }
    }
    public IUAVariable CalibrationPositionVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("CalibrationPosition");
        }
    }
    public int Setpoint
    {
        get
        {
            return (int)Refs.GetVariable("Setpoint").Value.Value;
        }
        set
        {
            Refs.GetVariable("Setpoint").SetValue(value);
        }
    }
    public IUAVariable SetpointVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Setpoint");
        }
    }
    public bool FaultBypass
    {
        get
        {
            return (bool)Refs.GetVariable("FaultBypass").Value.Value;
        }
        set
        {
            Refs.GetVariable("FaultBypass").SetValue(value);
        }
    }
    public IUAVariable FaultBypassVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("FaultBypass");
        }
    }
    public bool CalibrateCounter
    {
        get
        {
            return (bool)Refs.GetVariable("CalibrateCounter").Value.Value;
        }
        set
        {
            Refs.GetVariable("CalibrateCounter").SetValue(value);
        }
    }
    public IUAVariable CalibrateCounterVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("CalibrateCounter");
        }
    }
    public float CalibrationValue
    {
        get
        {
            return (float)Refs.GetVariable("CalibrationValue").Value.Value;
        }
        set
        {
            Refs.GetVariable("CalibrationValue").SetValue(value);
        }
    }
    public IUAVariable CalibrationValueVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("CalibrationValue");
        }
    }
    public sbyte PositionDecimalPlaces
    {
        get
        {
            return (sbyte)Refs.GetVariable("PositionDecimalPlaces").Value.Value;
        }
        set
        {
            Refs.GetVariable("PositionDecimalPlaces").SetValue(value);
        }
    }
    public IUAVariable PositionDecimalPlacesVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("PositionDecimalPlaces");
        }
    }
    public bool DisplayOrientation
    {
        get
        {
            return (bool)Refs.GetVariable("DisplayOrientation").Value.Value;
        }
        set
        {
            Refs.GetVariable("DisplayOrientation").SetValue(value);
        }
    }
    public IUAVariable DisplayOrientationVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("DisplayOrientation");
        }
    }
    public bool SpindleDirection
    {
        get
        {
            return (bool)Refs.GetVariable("SpindleDirection").Value.Value;
        }
        set
        {
            Refs.GetVariable("SpindleDirection").SetValue(value);
        }
    }
    public IUAVariable SpindleDirectionVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("SpindleDirection");
        }
    }
    public float InPositionWindow
    {
        get
        {
            return (float)Refs.GetVariable("InPositionWindow").Value.Value;
        }
        set
        {
            Refs.GetVariable("InPositionWindow").SetValue(value);
        }
    }
    public IUAVariable InPositionWindowVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("InPositionWindow");
        }
    }
    public float SpindlePitch
    {
        get
        {
            return (float)Refs.GetVariable("SpindlePitch").Value.Value;
        }
        set
        {
            Refs.GetVariable("SpindlePitch").SetValue(value);
        }
    }
    public IUAVariable SpindlePitchVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("SpindlePitch");
        }
    }
    public bool PositionFault
    {
        get
        {
            return (bool)Refs.GetVariable("PositionFault").Value.Value;
        }
        set
        {
            Refs.GetVariable("PositionFault").SetValue(value);
        }
    }
    public IUAVariable PositionFaultVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("PositionFault");
        }
    }
    public string CounterName
    {
        get
        {
            return (string)Refs.GetVariable("CounterName").Value.Value;
        }
        set
        {
            Refs.GetVariable("CounterName").SetValue(value);
        }
    }
    public IUAVariable CounterNameVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("CounterName");
        }
    }
#endregion
}
