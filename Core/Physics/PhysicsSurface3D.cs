// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using System;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// <see cref="ScriptableObject"/> used to store data about alterating displacement on a surface.
    /// </summary>
    [CreateAssetMenu(fileName = "PHS_PhysicsSurface", menuName = FrameworkUtility.MenuPath + "Physics Surface [3D]", order = FrameworkUtility.MenuOrder)]
    public sealed class PhysicsSurface3D : ScriptableObject {
        /// <summary>
        /// <see cref="PhysicsSurface3D"/>-related settings data.
        /// </summary>
        [Serializable]
        public sealed class Settings {
            #region Content
            /// <summary>
            /// Static default instance of this class.
            /// </summary>
            public static readonly Settings Default = new Settings() {
                IncreaseSpeedCoef   = 1f,
                DecreaseSpeedCoef   = 1f,
                MaxSpeedCoef        = 1f,

                ChangeDirectionCoef = 1f,
                DecelerationCoef    = 1f,
            };

            // -----------------------

            [SerializeField] public float IncreaseSpeedCoef     = 1f;
            [SerializeField] public float DecreaseSpeedCoef     = 1f;
            [SerializeField] public float MaxSpeedCoef          = 1f;

            [Space(10f)]

            [SerializeField] public float ChangeDirectionCoef   = 1f;
            [SerializeField] public float DecelerationCoef      = 1f;
            #endregion
        }

        #region Global Members
        [Section("Physics Surface [3D]")]

        [SerializeField, Enhanced, Block] private Settings settings = new Settings();

        // -----------------------

        /// <summary>
        /// <see cref="Settings"/> wrapped in this object.
        /// </summary>
        public Settings SurfaceSettings {
            get { return settings; }
        }
        #endregion
    }
}
