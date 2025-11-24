// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// <see cref="PhysicsSurface3D"/> related <see cref="AssetMaterialDatabase{T}"/>.
    /// </summary>
    [CreateAssetMenu(fileName = MenuPrefix + "PhysicsSurface3DMaterialDatabase", menuName = MenuPath + "Physics Surface [3D]", order = MenuOrder)]
    public sealed class PhysicsSurface3DMaterialDatabase : AssetMaterialDatabase<PhysicsSurface3D> {
        #region Global Members
        [PropertyOrder(3)]

        [Tooltip("Affects any object having at least one of the specified tags - affects everything if empty")]
        [SerializeField] private TagGroup affectedTags = new TagGroup();
        #endregion

        #region Utility
        /// <summary>
        /// Get the <see cref="PhysicsSurface3D.Settings"/> associated with a specific material for a given <see cref="TagGroup"/> from this database.
        /// </summary>
        /// <param name="_material"><see cref="Material"/> to get the associated settings.</param>
        /// <param name="_tags"><see cref="TagGroup"/> used to get the right settings.</param>
        /// <param name="_settings">Matching surface settings for the given parameters (null if none).</param>
        /// <returns>true if a valid <see cref="PhysicsSurface3D.Settings"/> could be found for the specified parameters, false otherwise.</returns>
        public bool GetSettings(Material _material, TagGroup _tags, out PhysicsSurface3D.Settings _settings) {

            if (!IsValid(_tags)) {
                _settings = null;
                return false;
            }

            ref AssetMaterialGroup<PhysicsSurface3D>[] _span = ref groups;
            int _count = _span.Length;

            for (int i = 0; i < _count; i++) {
                if (_span[i].GetAsset(_material, out PhysicsSurface3D _surface)) {
                    _settings = _surface.SurfaceSettings;
                    return true;
                }
            }

            _settings = DefaultAsset.SurfaceSettings;
            return true;
        }

        /// <summary>
        /// Get if this database is valid for a specific <see cref="TagGroup"/>.
        /// </summary>
        /// <param name="_tags">Tags to check for eligiblity.</param>
        /// <returns>True if the specified tags are valid, false otherwise.</returns>
        public bool IsValid(TagGroup _tags) {
            return affectedTags.ContainsAny(_tags, true);
        }
        #endregion
    }
}
