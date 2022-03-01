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
using UnityEngine.Animations;

using VRCAvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

using KillFrenzy.AvatarTextTools.Settings;


namespace KillFrenzy.AvatarTextTools.Utility
{
	public static class KatObjectsInstaller
	{
		public static bool InstallObjectsToAvatar(VRCAvatarDescriptor avatarDescriptor, int attachmentPoint)
		{
			Material textMaterial = Resources.Load<Material>("KAT_Misc/KAT_Text");
			Transform avatarRootTransform = avatarDescriptor.gameObject.transform;
			Transform avatarAttachmentTransform = null;
			Vector3 avatarAttachmentOffset = new Vector3(0.0f, 1.0f, 0.4f);


			switch (attachmentPoint) {
				case KatAttachmentPoint.Head: {
					avatarAttachmentTransform = FindAvatarHead(avatarRootTransform);
					if (avatarAttachmentTransform == null) {
						Debug.LogWarning("Warning: Avatar head not found.");
					} else {
						avatarAttachmentOffset = new Vector3(-0.4f, 0.15f, 0.0f);
					}
					break;
				}
				case KatAttachmentPoint.Chest: {
					avatarAttachmentTransform = FindAvatarChest(avatarRootTransform);
					if (avatarAttachmentTransform == null) {
						Debug.LogWarning("Warning: Avatar chest not found.");
					} else {
						avatarAttachmentOffset = new Vector3(-0.0f, 0.0f, 0.4f);
					}
					break;
				}
			}

			if (textMaterial == null) {
				Debug.LogWarning("Error: Material for Text not found.");
				return false;
			}

			GameObject katObject = new GameObject("KillFrenzyAvatarText");
			katObject.transform.SetParent(avatarRootTransform);
			katObject.SetActive(false);

			GameObject constraintObject = new GameObject("Constraint");
			constraintObject.transform.SetParent(katObject.transform);

			ParentConstraint constraint = constraintObject.AddComponent<ParentConstraint>();
			constraint.locked = true;
			if (avatarAttachmentTransform != null) {
				ConstraintSource constraintSource = new ConstraintSource();
				constraintSource.sourceTransform = avatarAttachmentTransform;
				constraintSource.weight = 1f;
				constraint.AddSource(constraintSource);
				constraint.constraintActive = true;

				if (attachmentPoint == KatAttachmentPoint.Chest) {
					constraint.rotationAxis = Axis.Y | Axis.Z;
				}
			}

			GameObject textObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
			textObject.name = "Text";
			textObject.transform.SetParent(constraintObject.transform);
			textObject.transform.localPosition = avatarAttachmentOffset;
			textObject.transform.localScale = new Vector3(0.5f, 1f / 6f, 1.0f);

			GameObject.DestroyImmediate(textObject.GetComponent<MeshCollider>());

			MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
			textRenderer.material = textMaterial;

			return true;
		}

		public static bool RemoveObjectsFromAvatar(VRCAvatarDescriptor avatar)
		{
			Transform avatarRootTransform = avatar.gameObject.transform;
			Transform katObjectTransform = avatarRootTransform.transform.Find("KillFrenzyAvatarText");
			if (katObjectTransform) {
				try {
					GameObject.DestroyImmediate(katObjectTransform.gameObject);
				} catch {
					Debug.LogWarning("Warning: Unable to destroy the KillFrenzyAvatarText object.");
					return false;
				}
			}
			return true;
		}

		private static Transform FindAvatarChest(Transform transform)
		{
			Transform chest = FindTransformRecursive(transform, "chest");

			if (chest != null) {
				Transform neck = FindTransformRecursive(transform, "neck");
				return chest;
			}

			return null;
		}

		private static Transform FindAvatarHead(Transform transform)
		{
			Transform neck = FindTransformRecursive(transform, "neck");

			if (neck != null) {
				Transform head = FindTransformRecursive(transform, "head");
				return head;
			}

			return null;
		}

		private static Transform FindTransformRecursive(Transform transform, string name)
		{
			foreach (Transform child in transform) {
				if (child.name.ToLower().Contains(name)) {
					return child;
				} else {
					Transform found = FindTransformRecursive(child, name);
					if (found != null) {
						return found;
					}
				}
			}
			return null;
		}
	}
}

#endif
