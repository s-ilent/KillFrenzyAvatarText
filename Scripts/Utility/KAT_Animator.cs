/*
	Avatar Text for VRChat
	Copyright (C) 2022 KillFrenzy / Evan Tran

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program. If not, see <https://www.gnu.org/licenses/>.
*/

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

using System.Collections.Generic;

using AnimatorControllerLayer = UnityEditor.Animations.AnimatorControllerLayer;
using AnimatorController = UnityEditor.Animations.AnimatorController;

using Driver = VRC.SDK3.Avatars.Components.VRCAvatarParameterDriver;
using DriverParameter = VRC.SDKBase.VRC_AvatarParameterDriver.Parameter;

using KillFrenzy.AvatarTextTools.Settings;


namespace KillFrenzy.AvatarTextTools.Utility
{
	public static class KatAnimatorInstaller
	{
		public static bool InstallToAnimator(AnimatorController controller, bool installKeyboard)
		{
			KatAnimations animations = new KatAnimations(installKeyboard);

			if (animations.valid == false) {
				Debug.LogError("Failed: Resources/" + KatSettings.CharacterAnimationFolder + " is missing some animations.");
				return false;
			}

			// Add parameters
			// controller.AddParameter("IsLocal", AnimatorControllerParameterType.Bool);
			controller.AddParameter(KatSettings.ParamTextVisible, AnimatorControllerParameterType.Bool);
			controller.AddParameter(KatSettings.ParamTextPointer, AnimatorControllerParameterType.Int);

			if (installKeyboard) {
				controller.AddParameter(KatSettings.ParamKeyboardPrefix, AnimatorControllerParameterType.Bool);
				controller.AddParameter(KatSettings.ParamKeyboardPressedClear, AnimatorControllerParameterType.Float);
				controller.AddParameter(KatSettings.ParamKeyboardPressedBackspace, AnimatorControllerParameterType.Float);
				controller.AddParameter(KatSettings.ParamKeyboardPressedCaps, AnimatorControllerParameterType.Float);
				controller.AddParameter(KatSettings.ParamKeyboardPressedShiftL, AnimatorControllerParameterType.Float);
				controller.AddParameter(KatSettings.ParamKeyboardPressedShiftR, AnimatorControllerParameterType.Float);

				for (int i = 0; i < KatSettings.KeyboardKeysCount; i++) {
					controller.AddParameter(KatSettings.ParamKeyboardPressed + i.ToString(), AnimatorControllerParameterType.Float);
				}
			}

			for (int i = 0; i < KatSettings.SyncParamsSize; i++) {
				controller.AddParameter(KatSettings.ParamTextSyncPrefix + i.ToString(), AnimatorControllerParameterType.Float);
			}

			// Add Layers
			controller.AddLayer(CreateToggleLayer(controller, animations, installKeyboard));

			if (installKeyboard) {
				controller.AddLayer(CreateKeyboardLayer(controller, animations));
			}

			for (int i = 0; i < KatSettings.SyncParamsSize; i++) {
				controller.AddLayer(CreateSyncLayer(controller, i, animations, installKeyboard));
			}

			return true;
		}

		public static bool RemoveFromAnimator(AnimatorController controller) {
			// Remove Blend Trees
			// AssetDatabase.RemoveObjectFromAsset();
			// BlendTree[] blendTrees = controller.GetStateEffectiveMotion();

			// Remove Layers
			for (int i = controller.layers.Length - 1; i >= 0; i--) {
				AnimatorControllerLayer layer = controller.layers[i];
				if (
					layer.name.StartsWith(KatSettings.ParamTextVisible) ||
					layer.name.StartsWith(KatSettings.ParamKeyboardPrefix) ||
					layer.name.StartsWith(KatSettings.ParamKeyboardPressed) ||
					layer.name.StartsWith(KatSettings.ParamTextSyncPrefix)
				) {
					// Remove Blend Trees
					foreach (ChildAnimatorState childState in layer.stateMachine.states) {
						if (childState.state.motion != null && childState.state.motion is BlendTree) {
							AssetDatabase.RemoveObjectFromAsset(childState.state.motion);
						}
					}
					controller.RemoveLayer(i);
				}
			}

			// Remove Parameters
			for (int i = controller.parameters.Length - 1; i >= 0; i--) {
				AnimatorControllerParameter parameter = controller.parameters[i];
				if (
					parameter.name.StartsWith(KatSettings.ParamTextVisible) ||
					parameter.name.StartsWith(KatSettings.ParamKeyboardPrefix) ||
					parameter.name.StartsWith(KatSettings.ParamKeyboardPressed) ||
					parameter.name.StartsWith(KatSettings.ParamTextPointer) ||
					parameter.name.StartsWith(KatSettings.ParamTextSyncPrefix)
				) {
					controller.RemoveParameter(i);
				}
			}

			return true;
		}

		private static AnimatorControllerLayer CreateToggleLayer(AnimatorController controller, KatAnimations animations, bool installKeyboard)
		{
			AnimatorControllerLayer layer = new AnimatorControllerLayer();
			AnimatorStateMachine stateMachine;

			layer.name = KatSettings.ParamTextVisible;
			layer.defaultWeight = 1f;
			layer.stateMachine = stateMachine = new AnimatorStateMachine();
			layer.stateMachine.name = layer.name;
			AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(controller));

			// Hide KAT state
			AnimatorState stateDisabled = CreateState(stateMachine, "Disabled", new Vector3(300f, 200f, 0f), animations.disable);

			// Show KAT state
			AnimatorState stateEnabled = CreateState(stateMachine, "Enabled", new Vector3(300f, 300f, 0f), animations.enable);
			AnimatorStateTransition transitionEnable = CreateTransition(stateDisabled, stateEnabled);
			transitionEnable.AddCondition(AnimatorConditionMode.If, 0, KatSettings.ParamTextVisible);
			AnimatorStateTransition transitionDisable = CreateTransition(stateEnabled, stateDisabled);
			transitionDisable.AddCondition(AnimatorConditionMode.IfNot, 0, KatSettings.ParamTextVisible);

			if (installKeyboard) {
				// Add extra transitions to keep text visible if keyboard is visible
				transitionDisable.AddCondition(AnimatorConditionMode.IfNot, 0, KatSettings.ParamKeyboardPrefix);

				AnimatorStateTransition transitionEnable2 = CreateTransition(stateDisabled, stateEnabled);
				transitionEnable2.AddCondition(AnimatorConditionMode.If, 0, KatSettings.ParamKeyboardPrefix);
			}

			return layer;
		}

		private static AnimatorControllerLayer CreateSyncLayer(AnimatorController controller, int logicId, KatAnimations animations, bool installKeyboard)
		{
			AnimatorControllerLayer layer = new AnimatorControllerLayer();
			AnimatorStateMachine stateMachine;

			layer.name = KatSettings.ParamTextSyncPrefix + logicId.ToString();
			layer.defaultWeight = 1f;
			layer.stateMachine = stateMachine = new AnimatorStateMachine();
			layer.stateMachine.name = layer.name;
			AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(controller));

			// Default state
			AnimatorState stateDisabled = CreateState(stateMachine, "Disabled", new Vector3(300f, 200f, 0f), animations.nothing);
			AnimatorStateTransition transitionDisabled = CreateTransition(stateMachine, stateDisabled);
			transitionDisabled.AddCondition(AnimatorConditionMode.Equals, 0, KatSettings.ParamTextPointer);

			// Standby state - waits for updates to the pointer
			AnimatorState stateStandby = CreateState(stateMachine, "StandBy", new Vector3(300f, 500f, 0f), animations.nothing);
			AnimatorStateTransition transitionStandby = CreateTransition(stateDisabled, stateStandby);
			transitionStandby.AddCondition(AnimatorConditionMode.NotEqual, 0, KatSettings.ParamTextPointer);

			if (installKeyboard) {
				AnimatorStateTransition transitionDisabled2 = CreateTransition(stateMachine, stateDisabled);
				transitionDisabled2.AddCondition(AnimatorConditionMode.Greater, KatSettings.KeyboardBackspaceMode - 0.005f, KatSettings.ParamTextSyncPrefix + "0");
				transitionDisabled2.AddCondition(AnimatorConditionMode.Less, KatSettings.KeyboardBackspaceMode + 0.005f, KatSettings.ParamTextSyncPrefix + "0");

				transitionStandby.AddCondition(AnimatorConditionMode.Less, KatSettings.KeyboardBackspaceMode - 0.005f, KatSettings.ParamTextSyncPrefix + "0");

				AnimatorStateTransition transitionStandby2 = CreateTransition(stateDisabled, stateStandby);
				transitionStandby2.AddCondition(AnimatorConditionMode.NotEqual, 0, KatSettings.ParamTextPointer);
				transitionStandby2.AddCondition(AnimatorConditionMode.Greater, KatSettings.KeyboardBackspaceMode + 0.005f, KatSettings.ParamTextSyncPrefix + "0");
			}

			// Create pointer animations states
			for (int i = 0; i < KatSettings.PointerCount; i++) {
				int pointerIndex = i + 1;
				int charIndex = KatSettings.SyncParamsSize * i + logicId;

				float pointerPosOffsetY = 500f + 50f * i;

				BlendTree blendTree = new BlendTree();
				// controller.CreateBlendTreeInController("Char" + charIndex + " BlendTree", out blendTree);
				blendTree.name = "Char" + charIndex + " BlendTree";
				blendTree.blendParameter = KatSettings.ParamTextSyncPrefix + logicId.ToString();
				blendTree.blendType = BlendTreeType.Simple1D;
				blendTree.useAutomaticThresholds = false;
				blendTree.AddChild(animations.charsStart[charIndex], -1.0f);
				blendTree.AddChild(animations.charsEnd[charIndex], 1.0f);
				AssetDatabase.AddObjectToAsset(blendTree, AssetDatabase.GetAssetPath(controller));

				AnimatorState statePointerChar = CreateState(stateMachine, "Pointer" + pointerIndex.ToString() + " Char" + charIndex, new Vector3(800f, pointerPosOffsetY, 0f), blendTree);
				statePointerChar.timeParameter = KatSettings.ParamTextSyncPrefix + logicId.ToString();
				statePointerChar.timeParameterActive = true;

				// Create transitions to activate pointer
				AnimatorStateTransition transitionPointerStart = CreateTransition(stateStandby, statePointerChar);
				transitionPointerStart.AddCondition(AnimatorConditionMode.Equals, pointerIndex, KatSettings.ParamTextPointer);
				AnimatorStateTransition transitionPointerExit = CreateTransition(statePointerChar, stateStandby);
				transitionPointerExit.AddCondition(AnimatorConditionMode.NotEqual, pointerIndex, KatSettings.ParamTextPointer);

				// Create alternate transitions to point to a single character at a time (for in-game keyboard)
				if (installKeyboard) {
					AnimatorStateTransition transitionAltStart = CreateTransition(stateStandby, statePointerChar);
					transitionAltStart.AddCondition(AnimatorConditionMode.Equals, KatSettings.PointerAltSyncOffset + charIndex + 1, KatSettings.ParamTextPointer);

					transitionPointerExit.AddCondition(AnimatorConditionMode.NotEqual, KatSettings.PointerAltSyncOffset + charIndex + 1, KatSettings.ParamTextPointer);
				}
			}

			// Create clear state
			AnimatorState stateClear = CreateState(stateMachine, "Clear Text", new Vector3(800f, 500f - 50f, 0f), animations.clear);
			stateClear.timeParameter = KatSettings.ParamTextSyncPrefix + logicId.ToString();
			stateClear.timeParameterActive = true;

			AnimatorStateTransition clearStartTransition = CreateTransition(stateStandby, stateClear);
			clearStartTransition.AddCondition(AnimatorConditionMode.Equals, 255, KatSettings.ParamTextPointer);
			AnimatorStateTransition clearReturnTransition = CreateTransition(stateClear, stateStandby);
			clearReturnTransition.AddCondition(AnimatorConditionMode.NotEqual, 255, KatSettings.ParamTextPointer);

			return layer;
		}

		private static AnimatorControllerLayer CreateKeyboardLayer(AnimatorController controller, KatAnimations animations)
		{
			AnimatorControllerLayer layer = new AnimatorControllerLayer();
			AnimatorStateMachine stateMachine;

			layer.name = KatSettings.ParamKeyboardPrefix;
			layer.defaultWeight = 1f;
			layer.stateMachine = stateMachine = new AnimatorStateMachine();
			layer.stateMachine.name = layer.name;
			AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(controller));

			// Default state
			AnimatorState stateDisabled = CreateState(stateMachine, "Disabled", new Vector3(300f, 200f, 0f), animations.keyboardDisable);
			AnimatorStateTransition transitionDisabled = CreateTransition(stateMachine, stateDisabled);
			transitionDisabled.AddCondition(AnimatorConditionMode.IfNot, 0, KatSettings.ParamKeyboardPrefix);

			Driver driverDisabled = stateDisabled.AddStateMachineBehaviour<Driver>();
			driverDisabled.localOnly = true;
			driverDisabled.parameters = new List<DriverParameter>();

			// Init state - initializes keyboard
			AnimatorState stateInit = CreateState(stateMachine, "Init", new Vector3(300f, 400f, 0f), animations.keyboardInit);
			stateInit.speed = 1.0f;
			AnimatorStateTransition transitionInit = CreateTransition(stateDisabled, stateInit);
			transitionInit.AddCondition(AnimatorConditionMode.If, 0, KatSettings.ParamKeyboardPrefix);

			Driver driverInit = stateInit.AddStateMachineBehaviour<Driver>();
			driverInit.localOnly = true;
			driverInit.parameters = new List<DriverParameter>();

			DriverParameter driverStateInit1 = new DriverParameter();
			driverStateInit1.name = KatSettings.ParamTextPointer;
			driverStateInit1.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
			driverStateInit1.value = KatSettings.PointerAltSyncOffset;
			driverInit.parameters.Add(driverStateInit1);

			for (int i = 0; i < KatSettings.SyncParamsSize; i++) {
				DriverParameter driverParameterChar = new DriverParameter();
				driverParameterChar.name = KatSettings.ParamTextSyncPrefix + i.ToString();
				driverParameterChar.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
				driverParameterChar.value = 0.0f;
				driverInit.parameters.Add(driverParameterChar);
			}

			// Standby state - waits for updates to the pointer
			AnimatorState stateStandby = CreateState(stateMachine, "StandBy", new Vector3(300f, 500f, 0f), animations.keyboardEnable);
			AnimatorStateTransition transitionEnabled = CreateTransition(stateInit, stateStandby);
			transitionEnabled.AddCondition(AnimatorConditionMode.If, 0, KatSettings.ParamKeyboardPrefix);
			transitionEnabled.hasExitTime = true;
			transitionEnabled.exitTime = 0.1f;

			// Fix Pointer - places pointer within offset if out of range
			AnimatorState stateFix = CreateState(stateMachine, "Fix Pointer", new Vector3(0f, 500f, 0f), animations.nothing);
			AnimatorStateTransition transitionFixEnter1 = CreateTransition(stateStandby, stateFix);
			transitionFixEnter1.AddCondition(AnimatorConditionMode.Greater, KatSettings.PointerAltSyncOffset + KatSettings.TextLength + 1, KatSettings.ParamTextPointer);
			AnimatorStateTransition transitionFixEnter2 = CreateTransition(stateStandby, stateFix);
			transitionFixEnter2.AddCondition(AnimatorConditionMode.Less, KatSettings.PointerAltSyncOffset, KatSettings.ParamTextPointer);
			AnimatorStateTransition transitionFixExit = CreateTransition(stateFix, stateStandby);
			transitionFixExit.AddCondition(AnimatorConditionMode.If, 0, KatSettings.ParamKeyboardPrefix);

			Driver driverFix = stateFix.AddStateMachineBehaviour<Driver>();
			driverFix.localOnly = true;
			driverFix.parameters = new List<DriverParameter>();

			DriverParameter driverParameterFix = new DriverParameter();
			driverParameterFix.name = KatSettings.ParamTextPointer;
			driverParameterFix.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
			driverParameterFix.value = KatSettings.PointerAltSyncOffset;
			driverFix.parameters.Add(driverParameterFix);

			// Create keyboard presses
			for (int i = 0; i < KatSettings.KeyboardKeysCount; i++) {
				int key = i;
				string keyString = key.ToString();
				float positionY = 500f + 50f * i;

				AnimatorState stateKeyPress = CreateState(stateMachine, "Key" + keyString, new Vector3(600f, positionY, 0f), animations.nothing);
				AnimatorStateTransition transitionKeyDown = CreateTransition(stateStandby, stateKeyPress);
				transitionKeyDown.AddCondition(AnimatorConditionMode.Greater, 0.5f, KatSettings.ParamKeyboardPressed + keyString);
				AnimatorStateTransition transitionKeyUp = CreateTransition(stateKeyPress, stateStandby);
				transitionKeyUp.AddCondition(AnimatorConditionMode.Less, 0.5f, KatSettings.ParamKeyboardPressed + keyString);

				Driver driver = stateKeyPress.AddStateMachineBehaviour<Driver>();
				driver.localOnly = true;
				driver.parameters = new List<DriverParameter>();

				DriverParameter driverParameterPointer = new DriverParameter();
				driverParameterPointer.name = KatSettings.ParamTextPointer;
				driverParameterPointer.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Add;
				driverParameterPointer.value = 1;
				driver.parameters.Add(driverParameterPointer);

				for (int j = 0; j < KatSettings.SyncParamsSize; j++) {
					DriverParameter driverParameterChar = new DriverParameter();
					driverParameterChar.name = KatSettings.ParamTextSyncPrefix + j.ToString();
					driverParameterChar.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
					driverParameterChar.value = ConvertKeyToFloat(key);
					driver.parameters.Add(driverParameterChar);
				}
			}

			// Create clear key
			AnimatorState stateKeyClear = CreateState(stateMachine, "KeyClear", new Vector3(0f, 550f, 0f), animations.nothing);
			AnimatorStateTransition transitionKeyClearDown = CreateTransition(stateStandby, stateKeyClear);
			transitionKeyClearDown.AddCondition(AnimatorConditionMode.Greater, 0.5f, KatSettings.ParamKeyboardPressedClear);
			AnimatorStateTransition transitionKeyClearUp = CreateTransition(stateKeyClear, stateStandby);
			transitionKeyClearUp.AddCondition(AnimatorConditionMode.Less, 0.5f, KatSettings.ParamKeyboardPressedClear);

			Driver driveKeyClear = stateKeyClear.AddStateMachineBehaviour<Driver>();
			driveKeyClear.localOnly = true;
			driveKeyClear.parameters = new List<DriverParameter>();

			DriverParameter driverKeyClearParameter = new DriverParameter();
			driverKeyClearParameter.name = KatSettings.ParamTextPointer;
			driverKeyClearParameter.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
			driverKeyClearParameter.value = 255;
			driveKeyClear.parameters.Add(driverKeyClearParameter);

			for (int i = 0; i < KatSettings.SyncParamsSize; i++) {
				DriverParameter driverParameterChar = new DriverParameter();
				driverParameterChar.name = KatSettings.ParamTextSyncPrefix + i.ToString();
				driverParameterChar.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
				driverParameterChar.value = 0.0f;
				driveKeyClear.parameters.Add(driverParameterChar);
			}

			// Create backspace start key
			AnimatorState stateKeyBkspStart = CreateState(stateMachine, "KeyBackspaceStart", new Vector3(0f, 600f, 0f), animations.nothing);
			stateKeyBkspStart.speed = 1f;
			AnimatorStateTransition transitionKeyBkspDownStart = CreateTransition(stateStandby, stateKeyBkspStart);
			transitionKeyBkspDownStart.AddCondition(AnimatorConditionMode.Greater, 0.5f, KatSettings.ParamKeyboardPressedBackspace);

			Driver driverBkspStart = stateKeyBkspStart.AddStateMachineBehaviour<Driver>();
			driverBkspStart.localOnly = true;
			driverBkspStart.parameters = new List<DriverParameter>();

			for (int i = 0; i < KatSettings.SyncParamsSize; i++) {
				DriverParameter driverParameterChar = new DriverParameter();
				driverParameterChar.name = KatSettings.ParamTextSyncPrefix + i.ToString();
				driverParameterChar.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
				driverParameterChar.value = 0.0f;
				driverBkspStart.parameters.Add(driverParameterChar);
			}

			// Create backspace end key
			AnimatorState stateKeyBkspEnd = CreateState(stateMachine, "KeyBackspaceEnd", new Vector3(0f, 650f, 0f), animations.nothing);
			AnimatorStateTransition transitionKeyBkspEnd = CreateTransition(stateKeyBkspEnd, stateStandby);
			transitionKeyBkspEnd.AddCondition(AnimatorConditionMode.Less, 0.5f, KatSettings.ParamKeyboardPressedBackspace);

			AnimatorStateTransition transitionKeyBkspUpFirst = CreateTransition(stateKeyBkspStart, stateKeyBkspEnd);
			transitionKeyBkspUpFirst.hasExitTime = true;
			transitionKeyBkspUpFirst.exitTime = 0.2f;

			Driver driverBksp = stateKeyBkspEnd.AddStateMachineBehaviour<Driver>();
			driverBksp.localOnly = true;
			driverBksp.parameters = new List<DriverParameter>();

			DriverParameter driverParameterBksp = new DriverParameter();
			driverParameterBksp.name = KatSettings.ParamTextPointer;
			driverParameterBksp.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Add;
			driverParameterBksp.value = -1;
			driverBksp.parameters.Add(driverParameterBksp);

			for (int j = 0; j < KatSettings.SyncParamsSize; j++) {
				DriverParameter driverParameterChar = new DriverParameter();
				driverParameterChar.name = KatSettings.ParamTextSyncPrefix + j.ToString();
				driverParameterChar.type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Set;
				driverParameterChar.value = KatSettings.KeyboardBackspaceMode;
				driverBksp.parameters.Add(driverParameterChar);
			}

			return layer;
		}

		private static AnimatorControllerLayer CreateKeyboardShiftLayer(AnimatorController controller, KatAnimations animations)
		{
			AnimatorControllerLayer layer = new AnimatorControllerLayer();
			AnimatorStateMachine stateMachine;

			layer.name = KatSettings.ParamKeyboardPrefix;
			layer.defaultWeight = 1f;
			layer.stateMachine = stateMachine = new AnimatorStateMachine();
			layer.stateMachine.name = layer.name;
			AssetDatabase.AddObjectToAsset(layer.stateMachine, AssetDatabase.GetAssetPath(controller));

			return layer;
		}

		public static AnimatorState CreateState(AnimatorStateMachine stateMachine, string name, Vector3 position, Motion animation)
		{
			AnimatorState state = stateMachine.AddState(name, position);
			state.motion = animation;
			state.writeDefaultValues = false;
			state.speed = 0f;
			return state;
		}

		public static AnimatorStateTransition CreateTransition(AnimatorState source, AnimatorState destination)
		{
			AnimatorStateTransition transition = source.AddExitTransition(false);
			transition.destinationState = destination;
			transition.duration = 0;
			return transition;
		}

		public static AnimatorStateTransition CreateTransition(AnimatorStateMachine source, AnimatorState destination)
		{
			AnimatorStateTransition transition = source.AddAnyStateTransition(destination);
			transition.duration = 0;
			transition.canTransitionToSelf = false;
			return transition;
		}

		private static float ConvertKeyToFloat(int key)
		{
			float value = (float)key;
			if (value > 127.5f) {
				value = value - 256.0f;
			}
			value = value / 127.0f;
			return value;
		}
	}

	public class KatAnimations
	{
		public readonly AnimationClip[] charsStart;
		public readonly AnimationClip[] charsEnd;
		public readonly AnimationClip clear;
		public readonly AnimationClip disable;
		public readonly AnimationClip enable;
		public readonly AnimationClip nothing;

		public readonly AnimationClip keyboardDisable;
		public readonly AnimationClip keyboardEnable;
		public readonly AnimationClip keyboardInit;


		public readonly bool valid;

		public KatAnimations(bool installKeyboard)
		{
			valid = true;

			charsStart = GetCharAnimations(false);
			charsEnd = GetCharAnimations(true);
			disable = Resources.Load<AnimationClip>(KatSettings.CharacterAnimationFolder + "KAT_Disable");
			enable = Resources.Load<AnimationClip>(KatSettings.CharacterAnimationFolder + "KAT_Enable");
			nothing = Resources.Load<AnimationClip>(KatSettings.CharacterAnimationFolder + "KAT_Nothing");
			clear = Resources.Load<AnimationClip>(KatSettings.CharacterAnimationFolder + KatSettings.CharacterAnimationClipNamePrefix + "Clear");

			if (
				charsStart == null &&
				charsEnd == null &&
				disable == null &&
				enable == null &&
				nothing == null
			) {
				valid = false;
			}

			if (installKeyboard) {
				keyboardDisable = Resources.Load<AnimationClip>(KatSettings.CharacterAnimationFolder + "KAT_Keyboard_Disable");
				keyboardEnable = Resources.Load<AnimationClip>(KatSettings.CharacterAnimationFolder + "KAT_Keyboard_Enable");
				keyboardInit = Resources.Load<AnimationClip>(KatSettings.CharacterAnimationFolder + "KAT_Keyboard_Init");

				if (
					keyboardDisable == null &&
					keyboardEnable == null &&
					keyboardInit == null
				) {
					valid = false;
				}
			}
		}

		private static AnimationClip[] GetCharAnimations(bool end = false)
		{
			AnimationClip[] animationClips = Resources.LoadAll<AnimationClip>(KatSettings.CharacterAnimationFolder);
			AnimationClip[] orderedAnimationClips = new AnimationClip[KatSettings.TextLength];

			// Order the animation clips so they match their index
			for (int i = 0; i < KatSettings.TextLength; i++) {
				orderedAnimationClips[i] = null;
				foreach (var animationClip in animationClips) {
					string name;
					if (end) {
						name = KatSettings.CharacterAnimationClipNamePrefix + "End" + i.ToString();
					} else {
						name = KatSettings.CharacterAnimationClipNamePrefix + i.ToString();
					}
					if (animationClip.name == name) {
						orderedAnimationClips[i] = animationClip;
						break;
					}
				}
				if (orderedAnimationClips[i] == null) {
					return null;
				}
			}

			return orderedAnimationClips;
		}
	}
}

#endif
