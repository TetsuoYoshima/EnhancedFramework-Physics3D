// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Contains multiple 3D Physics related utility methods.
    /// </summary>
    public static class Physics3DUtility {
        #region Collision Mask
        /// <summary>
        /// Get the collision layer mask that indicates which layer(s) the specified <see cref="GameObject"/> can collide with.
        /// </summary>
        /// <param name="_gameObject">The <see cref="GameObject"/> to retrieve the collision layer mask for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLayerCollisionMask(GameObject _gameObject) {
            return GetLayerCollisionMask(_gameObject.layer);
        }

        /// <summary>
        /// Get the collision layer mask that indicates which layer(s) the specified layer can collide with.
        /// </summary>
        /// <param name="_layer">The layer to retrieve the collision layer mask for.</param>
        public static int GetLayerCollisionMask(int _layer) {
            int _layerMask = 0;
            for (int i = 0; i < 32; i++) {
                if (!Physics.GetIgnoreLayerCollision(_layer, i))
                    _layerMask |= 1 << i;
            }

            return _layerMask;
        }
        #endregion

        #region Ground
        /// <inheritdoc cref="IsGroundSurface(Collider, Vector3, Vector3)"/>
        /// <param name="_hit">Hit result of the surface to stand on.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGroundSurface(RaycastHit _hit, Vector3 _up) {
            return IsGroundSurface(_hit.collider, _hit.normal, _up);
        }

        /// <inheritdoc cref="IsGroundAngle(Collider, Vector3, Vector3, out bool)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGroundSurface(Collider _collider, Vector3 _normal, Vector3 _up) {
            bool _isGroundAngle = IsGroundAngle(_collider, _normal, _up, out bool _isGroundSurface);
            return _isGroundAngle && _isGroundSurface;
        }

        /// <inheritdoc cref="IsGroundAngle(Collider, Vector3, Vector3, out bool)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGroundAngle(RaycastHit _hit, Vector3 _up, out bool _isGroundSurface) {
            return IsGroundAngle(_hit.collider, _hit.normal, _up, out _isGroundSurface);
        }

        /// <summary>
        /// Get if a specific surface can be considered as a ground (surface to stand on) or not.
        /// </summary>
        /// <param name="_collider">Collider attached to the testing surface.</param>
        /// <param name="_normal">The normal surface to check.</param>
        /// <param name="_up">Referential up vector of the object to stand on the surface.</param>
        /// <param name="_isGroundSurface">True if this collider has not the <see cref="NonGroundSurface3D"/> component, false otherwise.</param>
        /// <returns>True if this surface angle can be considered as ground, false otherwise.</returns>
        public static bool IsGroundAngle(Collider _collider, Vector3 _normal, Vector3 _up, out bool _isGroundSurface) {
            float _angle = Vector3.Angle(_normal, _up);
            _isGroundSurface = IsGroundSurface(_collider);

            return _angle <= Physics3DSettings.I.GroundAngle;
        }

        /// <summary>
        /// Gat if a specific <see cref="Collider"/> can be considered as a ground surface.
        /// </summary>
        /// <param name="_collider">The collider to check.</param>
        /// <returns>True if this collider can be considered as a ground surface, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGroundSurface(Collider _collider) {
            return !_collider.TryGetComponent<NonGroundSurface3D>(out _);
        }
        #endregion

        // --- Physics Operation --- \\

        #region Raycast Hit
        /// <summary>
        /// Maximum distance when compared to the first hit of a cast, to be considered as valid.
        /// </summary>
        public const float MaxCastDifferenceDetection = .001f;
        private static readonly Comparison<RaycastHit> raycastHitComparison = CompareRaycastHits;

        // -------------------------------------------
        // General
        // -------------------------------------------

        /// <summary>
        /// Filters all collision hit results and get actual valid hit count.
        /// </summary>
        public static int FilterCastHits(Collider _collider, RaycastHit[] _hits, int _startIndex, int _count, out RaycastHit _mainHit, bool _ignoreFurtherHits = true,
                                         IList<Collider> _ignoredColliders = null) {

            // No hit, so get full distance.
            if (_count == 0) {
                _mainHit = default;
                return 0;
            }

            // Detect results end index.
            for (int i = 0; i < _count; i++) {
                RaycastHit hit = _hits[_startIndex + i];

                if (hit.colliderInstanceID == 0) {
                    _count = i;
                    break;
                }
            }

            // Ignored colliders.
            if ((_ignoredColliders != null) && (_ignoredColliders.Count != 0)) {

                _count = FilterHits(_hits, _ignoredColliders, _startIndex, _count);

            } else {
                // Remove this object collider if detected.
                int _colliderId = _collider.GetInstanceID();

                if (_hits[_startIndex + _count - 1].colliderInstanceID == _colliderId) {
                    _count--;
                }

                #if DEVELOPMENT
                // Debug utility. Should be remove at some point.
                for (int i = 0; i < _count; i++) {
                    if (_hits[i].colliderInstanceID == _colliderId) {
                        _collider.LogError($"This object collider found => {i}/{_count}");
                    }
                }
                #endif
            }

            // No hit.
            if (_count == 0) {
                _mainHit = default;
                return 0;
            }

            SortRaycastHitByDistance(_hits, _count);

            _mainHit = _hits[_startIndex];
            float _maxDistance = Mathf.Max(0f, _mainHit.distance - _collider.contactOffset);

            _mainHit.distance  = _maxDistance;
            _hits[_startIndex] = _mainHit;

            // Ignore hits that are too distants from the closest one.
            if (_ignoreFurtherHits) {

                for (int i = 1; i < _count; i++) {
                    if (_hits[_startIndex + i].distance > (_maxDistance + MaxCastDifferenceDetection))
                        return i;
                }
            }

            return _count;
        }

        /// <summary>
        /// Filters a given <see cref="RaycastHit"/> collection to ignore some given elements.
        /// </summary>
        /// <param name="_colliders">Collection to filter.</param>
        /// <param name="_toIgnore">All elements to ignore.</param>
        /// <param name="_startIndex">Collection filter start index.</param>
        /// <param name="_count">Collection filter total element count.</param>
        /// <returns>New filtered collection element count.</returns>
        public static int FilterHits(RaycastHit[] _colliders, IList<Collider> _toIgnore, int _startIndex, int _count) {

            // Ignore.
            if ((_toIgnore == null) || (_toIgnore.Count == 0)) {
                return _count;
            }

            // Filter.
            int _ignoredCount = _toIgnore.Count;

            for (int i = 0; i < _count; i++) {
                for (int j = 0; j < _ignoredCount; j++) {

                    if (_colliders[_startIndex + i].colliderInstanceID == _toIgnore[j].GetInstanceID()) {
                        _colliders[_startIndex + i] = _colliders[_startIndex + --_count];
                        i--;

                        break;
                    }
                }
            }

            return _count;
        }

        // -------------------------------------------
        // Sort
        // -------------------------------------------

        /// <summary>
        /// Sort an array of <see cref="RaycastHit"/> by their distance.
        /// </summary>
        /// <param name="_hits">Hits to sort.</param>
        /// <param name="_count">Total count of hits to sort.</param>
        public static void SortRaycastHitByDistance(RaycastHit[] _hits, int _count, int _startIndex = 0) {
            if (_count == 0)
                return;

            // Security.
            if (_hits.Length < _count) {
                throw new ArgumentOutOfRangeException(nameof(_count));
            }

            // We sort an index array to avoid copying the struct all over.
            ref int[] _indexArray = ref BufferUtility.IntArray;
            ArrayUtility.Realloc(ref _indexArray, ToPoT(_count));

            for (var i = 0; i < _count; i++) {
                _indexArray[i] = _startIndex + i;
            }

            SortRaycastHitByDistance_Internal(_hits, _indexArray, 0, _count - 1);

            // Reorder the original array based on the sorted indexes.
            ref RaycastHit[] _sortedArray = ref BufferUtility.RaycastArray;
            ArrayUtility.Realloc(ref _sortedArray, ToPoT(_count));

            for (var i = 0; i < _count; i++) {
                _sortedArray[i] = _hits[_indexArray[i]];
            }

            // Update array content.
            Array.Copy(_sortedArray, 0, _hits, _startIndex, _count);
        }

        /// <inheritdoc cref="SortRaycastHitByDistance(RaycastHit[], int, int)"/>
        public static void SortRaycastHitByDistance(List<RaycastHit> _hits) {
            _hits.Sort(raycastHitComparison);
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        private static int CompareRaycastHits(RaycastHit a, RaycastHit b) {
            return a.distance.CompareTo(b.distance);
        }

        private static void SortRaycastHitByDistance_Internal(RaycastHit[] _array, int[] _indexArray, int _left, int _right) {
            if (_left < _right) {
                int _pivotIndex = PartitionByDistance(_array, _indexArray, _left, _right);

                SortRaycastHitByDistance_Internal(_array, _indexArray, _left, _pivotIndex - 1);
                SortRaycastHitByDistance_Internal(_array, _indexArray, _pivotIndex + 1, _right);
            }

            // ----- Local Method ----- \\

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int PartitionByDistance(RaycastHit[] _array, int[] _indexArray, int _left, int _right) {

                int _pivotIndex = _indexArray[_right];
                float _pivotDistance = _array[_pivotIndex].distance;
                int i = _left - 1;

                for (var j = _left; j < _right; j++) {
                    if (_array[_indexArray[j]].distance <= _pivotDistance) {

                        i++;
                        (_indexArray[i], _indexArray[j]) = (_indexArray[j], _indexArray[i]);
                    }
                }

                (_indexArray[i + 1], _indexArray[_right]) = (_indexArray[_right], _indexArray[i + 1]);
                return i + 1;
            }
        }
        #endregion

        #region Collider
        private static readonly Comparison<Collider> colliderComparison = CompareColliders;
        private static Vector3 reference = Vector3.zero;

        // -------------------------------------------
        // Filter
        // -------------------------------------------

        /// <summary>
        /// Filters a given <see cref="Collider"/> collection to ignore some given elements.
        /// </summary>
        /// <param name="_colliders">Collection to filter.</param>
        /// <param name="_toIgnore">All elements to ignore.</param>
        /// <param name="_startIndex">Collection filter start index.</param>
        /// <param name="_count">Collection filter total element count.</param>
        /// <returns>New filtered collection element count.</returns>
        public static int FilterColliders(Collider[] _colliders, IList<Collider> _toIgnore, int _startIndex, int _count) {

            // Ignore.
            if ((_toIgnore == null) || (_toIgnore.Count == 0)) {
                return _count;
            }

            // Filter.
            int _ignoredCount = _toIgnore.Count;

            for (int i = 0; i < _count; i++) {
                for (int j = 0; j < _ignoredCount; j++) {

                    if (_colliders[_startIndex + i].GetInstanceID() == _toIgnore[j].GetInstanceID()) {
                        _colliders[_startIndex + i] = _colliders[_startIndex + --_count];
                        i--;

                        break;
                    }
                }
            }

            return _count;
        }

        // -------------------------------------------
        // Sort
        // -------------------------------------------

        /// <summary>
        /// Sort an array of <see cref="Collider"/> by their distance from a reference <see cref="Vector3"/>.
        /// </summary>
        /// <param name="_colliders">Colliders to sort.</param>
        /// <param name="_count">Total count of colliders to sort.</param>
        /// <param name="_reference">Reference position used for sorting.</param>
        public static void SortCollidersByDistance(Collider[] _colliders, int _count, Vector3 _reference, int _startIndex = 0) {
            if (_count == 0)
                return;

            // Security.
            if (_colliders.Length < _count) {
                throw new ArgumentOutOfRangeException(nameof(_count));
            }

            // We sort an index array to avoid copying the struct all over.
            ref int[] _indexArray = ref BufferUtility.IntArray;
            ArrayUtility.Realloc(ref _indexArray, ToPoT(_count));

            for (var i = 0; i < _count; i++) {
                _indexArray[i] = _startIndex + i;
            }

            SortCollidersByDistance_Internal(_colliders, _indexArray, 0, _count - 1, _reference);

            // Reorder the original array based on the sorted indexes.
            ref Collider[] _sortedArray = ref BufferUtility.ColliderArray;
            ArrayUtility.Realloc(ref _sortedArray, ToPoT(_count));

            for (var i = 0; i < _count; i++) {
                _sortedArray[i] = _colliders[_indexArray[i]];
            }

            // Update array content.
            Array.Copy(_sortedArray, 0, _colliders, _startIndex, _count);
        }

        /// <inheritdoc cref="SortCollidersByDistance(Collider[], int, Vector3, int)"/>
        public static void SortCollidersByDistance(List<Collider> _colliders, Vector3 _reference) {
            reference = _reference;
            _colliders.Sort(colliderComparison);
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        private static int CompareColliders(Collider a, Collider b) {
            return (a.transform.position - reference).sqrMagnitude.CompareTo((b.transform.position - reference).sqrMagnitude);
        }

        private static void SortCollidersByDistance_Internal(Collider[] _array, int[] _indexArray, int _left, int _right, Vector3 _reference) {
            if (_left < _right) {
                int _pivotIndex = PartitionByDistance(_array, _indexArray, _left, _right, _reference);

                SortCollidersByDistance_Internal(_array, _indexArray, _left, _pivotIndex - 1, _reference);
                SortCollidersByDistance_Internal(_array, _indexArray, _pivotIndex + 1, _right, _reference);
            }

            // ----- Local Method ----- \\

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int PartitionByDistance(Collider[] _array, int[] _indexArray, int _left, int _right, Vector3 _reference) {

                int _pivotIndex = _indexArray[_right];
                float _pivotDistance = (_array[_pivotIndex].transform.position - _reference).sqrMagnitude;
                int i = _left - 1;

                for (var j = _left; j < _right; j++) {

                    float _distance = (_array[_indexArray[j]].transform.position - _reference).sqrMagnitude;
                    if (_distance <= _pivotDistance) {

                        i++;
                        (_indexArray[i], _indexArray[j]) = (_indexArray[j], _indexArray[i]);
                    }
                }

                (_indexArray[i + 1], _indexArray[_right]) = (_indexArray[_right], _indexArray[i + 1]);
                return i + 1;
            }
        }
        #endregion

        // --- Utility --- \\

        #region Utility
        /// <summary>
        /// Physics related default contact offset.
        /// </summary>
        public static float ContactOffset {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Physics.defaultContactOffset; }
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        private static int ToPoT(int n) {
            if (n <= 0) {
                return 1;
            }

            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            return n + 1;
        }
        #endregion
    }
}
