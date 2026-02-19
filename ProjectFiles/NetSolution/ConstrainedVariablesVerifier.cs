#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.CommunicationDriver;
using FTOptix.Modbus;
using FTOptix.CoreBase;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.OPCUAServer;
using FTOptix.NativeUI;
using FTOptix.NetLogic;
using FTOptix.Core;
using FTOptix.SerialPort;
#endregion

public class ConstrainedVariablesVerifier : BaseNetLogic
{
    /// <summary>
    /// Verifies that constrained variables are within their specified ranges.
    /// <example>
    /// For example:
    /// <code>
    /// VerifyConstrainedVariablesAreInRange();
    /// </code>
    /// will check each variable's value against its range constraints.
    /// If a variable violates its constraint, it logs an error message indicating the affected node.
    /// </example>
    /// </summary>
    /// <remarks>
    /// The method iterates through all constrained variables defined by `InformationModel`.
    /// It checks if each variable has been assigned a parent variable (`rangeParentVariable`),
    /// then verifies whether the variable's value falls within its specified low/high range.
    /// If a violation is detected, it logs a warning message with the relevant node information.
    /// </remarks>
    [ExportMethod]
    public void VerifyConstrainedVariablesAreInRange()
    {
        var rangeType = InformationModel.GetVariableType<RangeType>();
        var rangeInstances = rangeType.Instances;
        IUAVariable rangeParentVariable;

        foreach (var rangeInstance in rangeInstances)
        {
            if (rangeInstance.Constrain)
            {
                rangeParentVariable = rangeInstance.Owner as IUAVariable;
                if (rangeParentVariable == null)
                    continue;

                var value = rangeParentVariable.Value;
                if (value < rangeInstance.Low || value > rangeInstance.High)
                    Log.Warning("ConstrainedVariablesVerifier", Log.Node(rangeParentVariable));
            }
        }
    }
}
