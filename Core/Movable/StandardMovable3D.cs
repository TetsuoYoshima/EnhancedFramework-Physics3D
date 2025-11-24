// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Ready-to-use <see cref="Movable3D"/> for any moving object with simple collisions.
    /// </summary>
    [AddComponentMenu(FrameworkUtility.MenuPath + "Physics [3D]/Movable [3D]"), DisallowMultipleComponent]
    public sealed class StandardMovable3D : Movable3D {
        #region Global Members
        #if UNITY_EDITOR
        [Section("Movable [3D]"), PropertyOrder(0)]

        // Exists only to properly display the section.
        [SerializeField, Enhanced, ShowIf(nameof(sectionName))] private bool sectionName = false;
        #endif
        #endregion
    }
}
