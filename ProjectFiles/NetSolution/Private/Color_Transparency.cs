using System;
using UAManagedCore;

//-------------------------------------------
// WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
//-------------------------------------------

[MapType(NamespaceUri = "OptimateDemoProject", Guid = "8ece0394c900e3f341c60c69921c1b1c")]
public class Color_Transparency : FTOptix.CoreBase.ExpressionEvaluator
{
#region Children properties
    //-------------------------------------------
    // WARNING: AUTO-GENERATED CODE, DO NOT EDIT!
    //-------------------------------------------
    public object Sourcecolor
    {
        get
        {
            return (object)Refs.GetVariable("Sourcecolor").Value.Value;
        }
        set
        {
            Refs.GetVariable("Sourcecolor").SetValue(value);
        }
    }
    public IUAVariable SourcecolorVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Sourcecolor");
        }
    }
    public object Sourcetransparency
    {
        get
        {
            return (object)Refs.GetVariable("Sourcetransparency").Value.Value;
        }
        set
        {
            Refs.GetVariable("Sourcetransparency").SetValue(value);
        }
    }
    public IUAVariable SourcetransparencyVariable
    {
        get
        {
            return (IUAVariable)Refs.GetVariable("Sourcetransparency");
        }
    }
#endregion
}
