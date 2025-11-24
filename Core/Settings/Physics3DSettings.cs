// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core.Settings;
using UnityEngine;

using Min   = EnhancedEditor.MinAttribute;
using Range = EnhancedEditor.RangeAttribute;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// 3D Physics related global settings.
    /// </summary>
    [CreateAssetMenu(fileName = MenuPrefix + "Physics3DSettings", menuName = MenuPath + "Physics [3D]", order = MenuOrder)]
	public sealed class Physics3DSettings : BaseSettings<Physics3DSettings> {
        #region Global Members
        [Section("Physics 3D Settings")]

        [Tooltip("If true, checks for NaN values during collision calculs")]
        public bool CheckForNAN = true;

        [Tooltip("Only register and computes the first hit result for collisions if no collision effect can be applied")]
        public bool CollisionOneHitIfNoEffect = true;

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f)]

        [Enhanced, Max(0f)] public float Gravity    = -9.81f;

        [Tooltip("Maximum gravity velocity for an object")]
        [Enhanced, Max(0f)] public float MaxGravity = -25f;

        [Space(10f)]

        // -----------------------

        [Tooltip("Maximum angle force a surface to be considered as ground")]
        [Enhanced, Range(.1f, 90f)] public float GroundAngle    = 30f;

        [Tooltip("Maximum default height used to climb steps and surfaces")]
        [Enhanced, Min(0f)] public float ClimbHeight            = .2f;

        [Tooltip("Maximum default height used for snapping to the nearest surface")]
        [Enhanced, Min(0f)] public float SnapHeight             = .2f;

        // -----------------------

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f)]

        [Tooltip("Coefficient applied on an Movable force when hitting ground")]
        [Enhanced, Range(0f, 1f)] public float OnGroundedForceMultiplier = .55f;

        [Space(5f)]

        [Tooltip("Deceleration applied on an Movable force while on ground")]
        [Enhanced, Min(0f)] public float GroundForceDeceleration    = 17f;

        [Tooltip("Deceleration applied on an object force while in the air")]
        [Enhanced, Min(0f)] public float AirForceDeceleration       = 5f;

        // -----------------------

        [Space(10f), HorizontalLine(SuperColor.Green, 1f), Space(10f)]

        [SerializeField] public PhysicsSurface3DMaterialDatabase[] PhysicsSurfaces = new PhysicsSurface3DMaterialDatabase[0];
        #endregion

        #region Physics Surface
        /// <param name="_component"><see cref="Component"/> to get the associated settings.</param>
        /// <inheritdoc cref="GetPhysicsSurface(Material, TagGroup)"/>
        public PhysicsSurface3D.Settings GetPhysicsSurface(Material _material, Component _component) {
            if (!_component.GetTags(out TagGroup _tags)) {
                _tags = TagGroup.Empty;
            }

            return GetPhysicsSurface(_material, _tags);
        }

        /// <summary>
        /// Get the <see cref="PhysicsSurface3D.Settings"/> associated with a specific material.
        /// </summary>
        /// <param name="_material"><see cref="Material"/> to get the associated settings.</param>
        /// <param name="_tags"><see cref="TagGroup"/> used to get the right settings.</param>
        /// <returns><see cref="PhysicsSurface3D.Settings"/> associated with the given values.</returns>
        public PhysicsSurface3D.Settings GetPhysicsSurface(Material _material, TagGroup _tags) {

            ref PhysicsSurface3DMaterialDatabase[] _span = ref PhysicsSurfaces;
            for (int i = _span.Length; i-- > 0;) {
                if (_span[i].GetSettings(_material, _tags, out PhysicsSurface3D.Settings _settings)) {
                    return _settings;
                }
            }

            return PhysicsSurface3D.Settings.Default;
        }
        #endregion
    }
}
