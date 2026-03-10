// ===== Enhanced Framework - https://github.com/TetsuoYoshima/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================= //

using EnhancedEditor.Editor;
using UnityEditor;
using UnityEngine;

namespace EnhancedFramework.Physics3D.Editor {
    /// <summary>
    /// Custom <see cref="PhysicsCollider3D"/> drawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(PhysicsCollider3D), true)]
    public sealed class PhysicsCollider3DPropertyDrawer : EnhancedPropertyEditor {
        #region Drawer Content
        protected override float OnEnhancedGUI(Rect _position, SerializedProperty _property, GUIContent _label) {
            return EnhancedEditorGUI.EnhancedPropertyField(_position, _property.FindPropertyRelative("collider"), _label);
        }
        #endregion
    }
}
