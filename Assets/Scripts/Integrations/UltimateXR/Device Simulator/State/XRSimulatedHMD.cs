﻿using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;

namespace UltimateXR.Devices.Integrations.DeviceSimulator
{
	/// <summary>
	/// An input device representing a simulated XR head mounted display.
	/// </summary>
	[InputControlLayout(stateType = typeof(XRSimulatedHMDState), isGenericTypeOfDevice = false, displayName = "XR Simulated HMD")]
	[Preserve]
	public class XRSimulatedHMD : XRHMD
	{
	}
}