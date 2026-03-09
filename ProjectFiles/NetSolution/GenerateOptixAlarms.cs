using FTOptix.Alarm;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using System;
using UAManagedCore;
using FTOptix.DataLogger;

public class GenerateOptixAlarms : BaseNetLogic {
	[ExportMethod]
	public void GenerateAlarmsFromPLC() {
		bool AllContentFromPLC = (bool)LogicObject.GetVariable("AllContentFromPLC").Value;

		Folder destinationFolder = (Folder)InformationModel.Get((NodeId)LogicObject.GetVariable("DestinationFolder").Value);
		if (destinationFolder == null) {
			Log.Error("GenerateOptixAlarms", "Destination folder is null, cannot generate alarms.");
			return;
		}

		IUANode getAlarmSource = InformationModel.Get((NodeId)LogicObject.GetVariable("AlarmSource").Value);
		if (getAlarmSource == null) {
			Log.Error("GenerateOptixAlarms", "Alarm source is null, cannot generate alarms.");
			return;
		}

		NodeId customAlarmId = Project.Current.Get("Alarms/CustomAlarm").NodeId;
		if (customAlarmId == null) {
			Log.Error("GenerateOptixAlarms", "CustomAlarm type not found in project, cannot generate alarms.");
			return;
		}

		int x = 0;
		foreach (IUAVariable node in getAlarmSource.Children) {
			if (node.Value.Value is not Struct) {
				Log.Warning("GenerateOptixAlarms", $"Child {node.BrowseName} of {getAlarmSource.BrowseName} is not a struct, skipping.");
				continue;
			}

			DigitalAlarm output = (DigitalAlarm)InformationModel.MakeObject($"AlarmFromPLC.{x}", customAlarmId);
			Log.Info("GenerateOptixAlarms", $"Creating alarm {output.BrowseName} from PLC struct {node.BrowseName}.");

			if (node.Children.Count == 0) {
				Log.Warning("GenerateOptixAlarms", $"Struct {node.BrowseName} has no children, cannot link alarm variables. Skipping.");
				continue;
			}

			int y = 0;
			IUAVariable[] alarmStruct = new IUAVariable[node.Children.Count];
			foreach (IUAVariable child in node.Children) {
				alarmStruct[y] = child;
				y++;
			}

			// items 1 and 2 are not relevant for this
			TrySetDynamicLink(output.InputValueVariable, alarmStruct[2]);
			TrySetDynamicLink(output.MessageVariable, alarmStruct[5]);

			if (AllContentFromPLC) {
				TrySetDynamicLink(output.SeverityVariable, alarmStruct[3]);
				TrySetDynamicLink(output.EnabledVariable, alarmStruct[4]);

				if (output.ObjectType.BrowseName == "CustomAlarm") {
					// custom alarm is not recognized as a referencable variable during code execution. this is the only way i found to link these items easily. -hsr
					//															4 = help_text						3 = aux_text					6 = image						5 = overview
					IUAVariable[] convert = new IUAVariable[] { (IUAVariable)output.Children[4], (IUAVariable)output.Children[3], (IUAVariable)output.Children[6], (IUAVariable)output.Children[5] };

					for (int z = 0; z < 4; z++)
						TrySetDynamicLink(convert[z], alarmStruct[z + 6]);
				} else {
					Log.Warning("GenerateOptixAlarms", $"Alarm {output.BrowseName} is not of type CustomAlarm, skipping linking of HelpText, AuxText, Image, and Overview.");
				}
			} else {
				Log.Info("GenerateOptixAlarms", $"AllContentFromPLC is false, only linking Message and InputValue for alarm {output.BrowseName}.");
			}

				// we always want these true otherwise users have to manually acknowledge and confirm alarms
				output.AutoAcknowledge = true;
			output.AutoConfirm = true;

			destinationFolder.Add(output);
			Log.Info("GenerateOptixAlarms", $"Alarm {output.BrowseName} added to {destinationFolder.BrowseName}.");
			x++;
		}
	}

	private void TrySetDynamicLink(IUAVariable target, IUAVariable source) {
		try {
			target.SetDynamicLink(source);
		} catch (Exception ex) {
			Log.Error("GenerateOptixAlarms", $"Failed to set dynamic link from {source.BrowseName} to {target.BrowseName}: {ex.Message}");
		}
	}
}

