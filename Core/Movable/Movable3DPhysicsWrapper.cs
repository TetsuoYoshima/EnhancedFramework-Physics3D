// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

#if UNITY_2022_2_OR_NEWER
#define OVERLAP_COMMANDS
#endif

using EnhancedEditor;
using EnhancedFramework.Core;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    // -------------------------------------------
    // Base
    // -------------------------------------------

    /// <summary>
    /// Base non-generic wrapper for a <see cref="Movable3D"/> cast operations.
    /// </summary>
    internal abstract class Movable3DPhysicsWrapper {
        #region Content
        public const QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;
        public const float GroundCastDistance = 3f;

        // -------------------------------------------
        // Manual
        // -------------------------------------------

        /// <summary>
        /// Performs a cast for a given <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance instigator of this cast.</param>
        /// <param name="_operation">Data wrapper of this cast operation.</param>
        /// <param name="_velocity">Velocity to use to perform this cast.</param>
        /// <param name="_distance">Max distance of this cast operation.</param>
        /// <param name="_hit">Main <see cref="RaycastHit"/> result.</param>
        /// <param name="_registerHits">Whether to register or not hit results.</param>
        /// <returns>Total amount of hit results.</returns>
        public abstract int Cast(Movable3D _movable, CollisionOperationData3D _operation, Vector3 _velocity, float _distance, out RaycastHit _hit, bool _registerHits);

        /// <summary>
        /// Performs an overlap for a given <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance instigator of this overlap.</param>
        /// <param name="_buffer">Buffer used to store overlap results.</param>
        /// <param name="_ignoredColliders">All <see cref="Collider"/> to ignore.</param>
        /// <returns>Total count of overlapping colliders.</returns>
        public abstract int Overlap(Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders);

        /// <summary>
        /// Performs an extract operation for a given <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance to extract.</param>
        /// <param name="_ignoredColliders">All <see cref="Collider"/> to ignore.</param>
        public abstract void Extract(Movable3D _movable, IList<Collider> _ignoredColliders);

        // -------------------------------------------
        // Commands
        // -------------------------------------------

        /// <summary>
        /// Registers a cast command for a given <see cref="Movable3D"/>
        /// </summary>
        public abstract void RegisterCastCommand(Movable3D _movable, CollisionOperationData3D _operation, CastOperationCommands3D _commands, Vector3 _velocity);

        /// <summary>
        /// Registers an overlap command for a given <see cref="Movable3D"/>
        /// </summary>
        public abstract void RegisterOverlapCommand(Movable3D _movable, OverlapOperationCommands3D _commands);

        // -------------------------------------------
        // Ground
        // -------------------------------------------

        /// <summary>
        /// Performs final computations for detecting ground and "sticking" to it.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance to perform ground computations for.</param>
        /// <param name="_operation">Data wrapper of this operation.</param>
        /// <param name="_isGrounded">Was a ground surface successfully found for this object?</param>
        /// <param name="_groundHit">If grounded, the associated hit data.</param>
        public abstract CollisionGroundData3D ComputeGround(Movable3D _movable, CollisionOperationData3D _operation, ref bool _isGrounded, ref RaycastHit _groundHit);

        /// <summary>
        /// Get if a specific hit surface can be considered as a ground surface.
        /// </summary>
        /// <param name="_hit">Hit to check.</param>
        /// <returns>True if the surface can be considered as ground, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsGroundSurface(Movable3D _movable, RaycastHit _hit) {
            return Physics3DUtility.IsGroundSurface(_hit, _movable.UpDirection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetGroundCastDistance(Collider _collider) {
            return _collider.contactOffset * GroundCastDistance;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        internal static void RegisterCollisionHits(CollisionOperationData3D _operation, Collider _collider, RaycastHit[] _hits, int _startIndex, int _count) {

            EnhancedCollection<CollisionHit3D> _hitBuffer = _operation.Data.InternalTempBuffer;
            _operation.Data.TempBufferOperationCount++;

            // Early return - only get first hit.
            if (_operation.PhysicsSettings.CollisionOneHitIfNoEffect && !_operation.MovableInstance.CanApplyCollisionEffect(_operation)) {

                if (_count != 0) {
                    CollisionHit3D _collisionHit = new CollisionHit3D(_hits[_startIndex], _collider, false);
                    _hitBuffer.Add(_collisionHit);
                }

                return;
            }

            // Store informations about all Movable-related hits - used later to apply special effects and for callbacks.
            for (int i = 0; i < _count; i++) {

                RaycastHit _hit = _hits[_startIndex + i];

                // In case the same object is hit multiple times, only keep the closest hit.
                if (FindIndex(_hit.collider, out int _index) && (_hit.distance > _hitBuffer[_index].Distance))
                    continue;

                // Register hit.
                CollisionHit3D _collisionHit = new CollisionHit3D(_hit, _collider, true);

                if (_index == -1) {
                    _hitBuffer.Add(_collisionHit);
                } else {
                    _hitBuffer[_index] = _collisionHit;
                }

                // ----- Local Method ----- \\

                bool FindIndex(Collider _collider, out int _index) {

                    for (_index = _hitBuffer.Count; _index-- > 0;) {
                        if (_hitBuffer[_index].HitCollider == _collider) {
                            return true;
                        }
                    }

                    _index = -1;
                    return false;
                }
            }
        }

        internal static void RegisterCollisionHit(CollisionOperationData3D _operation, Collider _collider, RaycastHit _hit) {

            // Simply register hit.
            CollisionHit3D _collisionHit = new CollisionHit3D(_hit, _collider, false);
            _operation.Data.InternalTempBuffer.Add(_collisionHit);
        }

        internal abstract void Setup(Movable3D _movable, CollisionOperationData3D _operation);
        #endregion
    }

    /// <summary>
    /// Base generic wrapper for a <see cref="Movable3D"/> cast operations with an automatic static instance.
    /// </summary>
    internal abstract class Movable3DPhysicsWrapper<T> : Movable3DPhysicsWrapper where T : Movable3DPhysicsWrapper<T>, new() {
        #region Global Members
        /// <summary>
        /// The one and only instance of this class.
        /// </summary>
        public static readonly T Instance = new T();

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <summary>
        /// Prevents from creating new instances of this class.
        /// </summary>
        protected Movable3DPhysicsWrapper() { }
        #endregion
    }

    // -------------------------------------------
    // Wrappers
    // -------------------------------------------

    /// <summary>
    /// <see cref="Movable3DPhysicsWrapper{T}"/> used for classic collisions with a single collider.
    /// </summary>
    internal sealed class SingleColliderMovable3DPhysicsWrapper : Movable3DPhysicsWrapper<SingleColliderMovable3DPhysicsWrapper> {
        #region Content
        // -------------------------------------------
        // Manual
        // -------------------------------------------

        public override int Cast    (Movable3D _movable, CollisionOperationData3D _operation, Vector3 _velocity, float _distance, out RaycastHit _hit, bool _registerHits) {
            // Setup.
            PhysicsCollider3D _physicsCollider = _movable.PhysicsCollider;

            // Perform cast.
            int _amount = _physicsCollider.CastAll(_velocity, out _hit, _distance, TriggerInteraction, !_registerHits, _operation.SelfColliders);

            // Register hits.
            if (_registerHits) {
                RegisterCollisionHits(_operation, _physicsCollider.Collider, PhysicsCollider3D.castBuffer, 0, _amount);
            }

            return _amount;
        }

        public override int Overlap (Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders) {
            // Setup.
            PhysicsCollider3D _physicsCollider = _movable.PhysicsCollider;

            // Perform overlap.
            int _amount = _physicsCollider.Overlap(_ignoredColliders, TriggerInteraction);

            // Register overlap.
            if (_buffer != null) {
                _buffer.Clear();

                for (int i = 0; i < _amount; i++) {
                    _buffer.Add(PhysicsCollider3D.GetOverlapCollider(i));
                }
            }

            return _amount;
        }

        public override void Extract(Movable3D _movable, IList<Collider> _ignoredColliders) {
            // Setup.
            PhysicsCollider3D _physicsCollider = _movable.PhysicsCollider;

            // Perform overlap.
            int _amount = _physicsCollider.Overlap(_ignoredColliders, TriggerInteraction);

            // Extract while authorized.
            for (int i = 0; i < _amount; i++) {
                if (!_movable.ExtractFromCollider(_physicsCollider.Collider, PhysicsCollider3D.GetOverlapCollider(i), false))
                    break;
            }
        }

        // -------------------------------------------
        // Commands
        // -------------------------------------------

        public override void RegisterCastCommand   (Movable3D _movable, CollisionOperationData3D _operation, CastOperationCommands3D _commands, Vector3 _velocity) {
            _commands.RegisterCommand(_movable, _movable.Collider, _velocity, _velocity.magnitude, _movable.GetColliderMask());
        }

        public override void RegisterOverlapCommand(Movable3D _movable, OverlapOperationCommands3D _commands) {
            #if OVERLAP_COMMANDS
            _commands.RegisterCommand(_movable, _movable.Collider, _movable.GetColliderMask());
            #else
            _movable.LogErrorMessage("Overlap commands are only available in Unity version 2022.2 and above");
            #endif
        }

        // -------------------------------------------
        // Ground
        // -------------------------------------------

        public override CollisionGroundData3D ComputeGround(Movable3D _movable, CollisionOperationData3D _operation, ref bool _isGrounded, ref RaycastHit _groundHit) {

            // If didn't hit ground during movement, try to get it using two casts:
            //  • A raycast from the collider bottom,
            //  • Or a shapecast, if the previous raycast failed.
            //
            // Necessary when movement magnitude is inferior to default contact offset.
            //
            // If using a sphere or a capsule collider, the cast can retrieve an obstacle
            // different than the ground when against a slope.
            // That's why a raycast from the bottom center is appreciated.

            if (!_isGrounded && _movable.UseGravity) {

                PhysicsCollider3D _collider = _movable.PhysicsCollider;
                Collider _colliderComponent = _collider.Collider;
                float _distance             = GetGroundCastDistance(_colliderComponent);
                int _mask                   = _movable.GetColliderMask();
                RaycastHit _hit;

                if (((_collider.RaycastAll(-_movable.GroundNormal, out _hit, _distance, _mask, TriggerInteraction, true, _operation.SelfColliders) != 0) && IsGroundSurface(_movable, _hit)) ||
                    ((_collider.CastAll   ( _movable.GravitySense, out _hit, _distance, _mask, TriggerInteraction, true, _operation.SelfColliders) != 0) && IsGroundSurface(_movable, _hit))) {

                    // If found, set ground.
                    _isGrounded = true;
                    _groundHit  = _hit;

                    RegisterCollisionHit(_operation, _colliderComponent, _hit);
                    return new CollisionGroundData3D(true);
                }
            }

            return new CollisionGroundData3D(false);
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        internal override void Setup(Movable3D _movable, CollisionOperationData3D _operation) {
            PhysicsCollider3D _physicsCollider = _movable.PhysicsCollider;
            _physicsCollider.UpdateTransformPosition();
        }
        #endregion
    }

    /// <summary>
    /// <see cref="Movable3DPhysicsWrapper{T}"/> performing collisions with multiple colliders on the same object.
    /// </summary>
    internal sealed class MultiColliderMovable3DPhysicsWrapper : Movable3DPhysicsWrapper<MultiColliderMovable3DPhysicsWrapper> {
        #region Content
        // -------------------------------------------
        // Manual
        // -------------------------------------------
        
        public override int Cast    (Movable3D _movable, CollisionOperationData3D _operation, Vector3 _velocity, float _distance, out RaycastHit _hit, bool _registerHits) {
            // Setup.
            List<Collider> _selfColliders = _operation.SelfColliders;
            int _count = _selfColliders.Count;
            int _total = 0;

            int _collisionMask = _movable.GetColliderMask();
            _hit = new RaycastHit() { distance = int.MaxValue };

            // Perform cast for each colliders.
            for (int i = 0; i < _count; i++) {

                Collider _collider = _selfColliders[i];
                if (!IsValid(_collider))
                    continue;

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _collisionMask);
                int _amount = _physicsCollider.CastAll(_velocity, out RaycastHit _castHit, _distance, _collisionMask, TriggerInteraction, !_registerHits, _selfColliders);

                // Zero hit - ignore.
                if (_amount == 0)
                    continue;

                _total += _amount;

                // Get closest hit.
                if (_castHit.distance < _hit.distance) {
                    _hit = _castHit;
                }

                // Register hits.
                if (_registerHits) {
                    RegisterCollisionHits(_operation, _collider, PhysicsCollider3D.castBuffer, 0, _amount);
                }
            }

            return _total;
        }

        public override int Overlap (Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders) {

            // Setup.
            List<Collider> _selfColliders = _movable.SelfColliders;
            int _count = _selfColliders.Count;
            int _total = 0;

            int _collisionMask = _movable.GetColliderMask();
            if (_buffer != null) {
                _buffer.Clear();
            }

            // Perform overlap for each colliders.
            for (int i = 0; i < _count; i++) {

                Collider _collider = _selfColliders[i];
                if (!IsValid(_collider))
                    continue;

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _collisionMask);
                int _amount = _physicsCollider.Overlap(_ignoredColliders, _collisionMask, TriggerInteraction);

                // Zero overlap - ignore.
                if (_amount == 0)
                    continue;

                _total += _amount;

                // Register overlap.
                if (_buffer != null) {

                    for (int j = 0; j < _amount; j++) {
                        _buffer.Add(PhysicsCollider3D.GetOverlapCollider(j));
                    }
                }
            }

            return _total;
        }

        public override void Extract(Movable3D _movable, IList<Collider> _ignoredColliders) {

            List<Collider> _selfColliders = _movable.SelfColliders;
            int _count = _selfColliders.Count;

            int _collisionMask = _movable.GetColliderMask();

            // Perform extract for each colliders.
            for (int i = 0; i < _count; i++) {

                Collider _collider = _selfColliders[i];
                if (!IsValid(_collider))
                    continue;

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _collisionMask);

                // Manually synchronize both this collider transform position and rotation to its rigidbody.
                // Rotation, especially, may only be updated during the physics engine or Unity-other-whetever update.
                _physicsCollider.UpdateTransformPosition();

                int _amount = _physicsCollider.Overlap(_ignoredColliders, _collisionMask, TriggerInteraction);

                // Extract while authorized.
                for (int j = 0; j < _amount; j++) {
                    if (!_movable.ExtractFromCollider(_physicsCollider.Collider, PhysicsCollider3D.GetOverlapCollider(j), false))
                        return;
                }
            }
        }

        // -------------------------------------------
        // Commands
        // -------------------------------------------

        public override void RegisterCastCommand   (Movable3D _movable, CollisionOperationData3D _operation, CastOperationCommands3D _commands, Vector3 _velocity) {
            List<Collider> _selfColliders = _operation.SelfColliders;

            int _collisionMask = _movable.GetColliderMask();
            float _distance    = _velocity.magnitude;

            int _count = _selfColliders.Count;
            for (int i = 0; i < _count; i++) {

                Collider _collider = _selfColliders[i];
                if (!IsValid(_collider))
                    continue;

                _commands.RegisterCommand(_movable, _collider, _velocity, _distance, _collisionMask);
            }
        }

        public override void RegisterOverlapCommand(Movable3D _movable, OverlapOperationCommands3D _commands) {
            #if OVERLAP_COMMANDS
            List<Collider> _selfColliders = _movable.SelfColliders;
            int _collisionMask = _movable.GetColliderMask();

            int _count = _selfColliders.Count;
            for (int i = 0; i < _count; i++) {

                Collider _collider = _selfColliders[i];
                if (!IsValid(_collider))
                    continue;

                _commands.RegisterCommand(_movable, _collider, _collisionMask);
            }
            #else
            _movable.LogErrorMessage("Overlap commands are only available in Unity version 2022.2 and above");
            #endif
        }

        // -------------------------------------------
        // Ground
        // -------------------------------------------

        public override CollisionGroundData3D ComputeGround(Movable3D _movable, CollisionOperationData3D _operation, ref bool _isGrounded, ref RaycastHit _groundHit) {

            // Early return.
            if (!_movable.UseGravity) {
                return new CollisionGroundData3D(false);
            }

            List<Movable3D.Foot> _feet = _movable.feet.collection;
            int _footCount = _feet.Count;

            // No foot - regular behaviour.
            if (_footCount == 0) {
                return SingleColliderMovable3DPhysicsWrapper.Instance.ComputeGround(_movable, _operation, ref _isGrounded, ref _groundHit);
            }

            List<Collider> _selfColliders = _operation.SelfColliders;
            Vector3 _castDirection        = _isGrounded ? -_groundHit.normal : -_movable.GroundNormal;
            Vector3 _up                   = _movable.UpDirection;
            int _mask                     = _movable.GetColliderMask();

            // Reset ground state, and recalculate it from the start.
            // Keep information on ground hit.
            _isGrounded = false;

            // ------------------------------------
            // Part I,
            //
            // For each foot, detect if colliding with another collider and if grounded.
            // ------------------------------------

            RaycastHit _hit;

            Vector3 _collidingFeetAverageCenter = Vector3.zero;
            Vector3 _feetAverageCenter          = Vector3.zero;
            int _collidingFootCount             = 0;

            for (int i = 0; i < _footCount; i++) {

                // Setup.
                Movable3D.Foot _foot = _feet[i];
                Collider _collider   = _foot.Collider;

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _mask);
                float _distance                    = GetGroundCastDistance(_collider);

                Vector3 _bottomPosition = _physicsCollider.GetBottomPosition();
                _feetAverageCenter     += _bottomPosition;

                // Cast.
                bool _hitCollider    = _physicsCollider.CastAll(_castDirection, out _hit, _distance, _mask, TriggerInteraction, true, _selfColliders) != 0;
                bool _isFootGrounded = _hitCollider && Physics3DUtility.IsGroundSurface(_hit, _up);

                // Register hit data.
                if (_hitCollider) {

                    float _contactOffset = _hit.collider.contactOffset;
                    _hit.distance = Mathf.Max(0f, _hit.distance - _contactOffset);

                    _contactOffset += _collider.contactOffset;
                    _hit.point     -= _castDirection * _contactOffset;

                    if (_collidingFootCount++ != 0) {

                        _groundHit.distance = Mathf.Min(_groundHit.distance, _hit.distance);
                        _groundHit.normal  += _hit.normal;
                        _groundHit.point   += _hit.point;

                    } else {
                        _groundHit = _hit;
                    }

                    _collidingFeetAverageCenter += _bottomPosition;
                    _isGrounded                 |= _isFootGrounded;
                }

                // Foot update.
                _foot.UpdateData(_bottomPosition, _hitCollider); // Could store more data - hit result, etc.
            }

            // ------------------------------------
            // Part II,
            //
            // If no foot is colliding, check if any other collider is colliding with an object.
            // ------------------------------------

            if (_collidingFootCount == 0) {

                bool _hitCollider = _groundHit.colliderInstanceID != 0;
                if (!_hitCollider) {

                    // Try to find a colliding object from any other non-foot collider.
                    for (int i = _selfColliders.Count; i-- > 0;) {

                        Collider _collider   = _selfColliders[i];
                        if (!IsValid(_collider, _feet, _footCount))
                            continue;

                        PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _mask);
                        float _distance                    = GetGroundCastDistance(_collider);

                        if (_physicsCollider.CastAll(_castDirection, out _groundHit, _distance, _mask, TriggerInteraction, true, _selfColliders) != 0) {

                            _hitCollider = true;

                            // Compute data.
                            float _contactOffset = _groundHit.collider.contactOffset;
                            _groundHit.distance  = Mathf.Max(0f, _groundHit.distance - _contactOffset);

                            _contactOffset   += _collider.contactOffset;
                            _groundHit.point -= _castDirection * _contactOffset;

                            break;
                        }
                    }

                    // No hit - object is the air.
                    if (!_hitCollider) {
                        SetGroundFeetInfos(_movable, -9, _feetAverageCenter / _footCount, _castDirection);
                        return new CollisionGroundData3D(false);
                    }
                }

                // Perform a different operation depending on the object current orientation.
                Vector3 _currentUp = _movable.Rigidbody.rotation * Vector3.up;
                float _dot = Vector3.Dot(_currentUp, _up);

                if (_dot > .1f) {
                    // Rotate according to non-ground hit result.
                    return AdjustToGroundFeet(_feetAverageCenter, _footCount, _groundHit.point, _movable.GroundSettings.FootNoHitAdjustRotationSpeed, -2);
                }

                // Calculate the average position of the nearest feet.
                Vector3 _point  = Vector3.zero;
                int _pointCount = 0;

                for (int i = 0; i < _footCount; i++) {
                    Movable3D.Foot _foot = _feet[i];
                    Vector3 _position    = _foot.BottomPosition;

                    if (_pointCount == 0) {

                        _point = _position;
                        _pointCount++;

                    } else {

                        float _difference = (_position - _groundHit.point).sqrMagnitude - (_point - _groundHit.point).sqrMagnitude;

                        if (_difference < -.1f) {

                            _point = _position;
                            _pointCount = 1;

                        } else if (_difference < .1f) {

                            _point += _position;
                            _pointCount++;
                        }
                    }
                }

                if (_pointCount != 1) {
                    _point /= _pointCount;
                }

                Vector3 _direction = _movable.Rigidbody.rotation * Vector3.up;

                // Rotate according to ideal rotation.
                return AdjustToGround(_point, _direction, _point, _movable.GroundSettings.FootNoHitResetRotationSpeed, -3);
            }

            // ------------------------------------
            // Part III,
            //
            // Compute data, and complete operation if enough feet are colliding with an object.
            // ------------------------------------

            // Compute average hit data.
            if (_collidingFootCount > 1) {

                _groundHit.normal = (_groundHit.normal / _collidingFootCount).normalized;
                _groundHit.point /= _collidingFootCount;

                _collidingFeetAverageCenter /= _collidingFootCount;
            }

            // All feet grounded - complete.
            if (_collidingFootCount /*== _footCount*/ > (_footCount / 2f)) {

                SetGroundFeetInfos(_movable, 1, _groundHit.point, _groundHit.normal);
                return new CollisionGroundData3D(false);
            }

            // ------------------------------------
            // Part IV,
            //
            // Orientate the object according to the vector between grounded and non-grounded feet, to stick to the ground.
            // ------------------------------------

            Vector3 _missingCenter = Vector3.zero;
            int     _missingCount  = 0;

            for (int i = 0; i < _footCount; i++) {
                Movable3D.Foot _foot = _feet[i];

                if (!_foot.IsGrounded) {
                    _missingCenter += _foot.BottomPosition;
                    _missingCount++;
                }
            }

            return AdjustToGroundFeet(_missingCenter, _missingCount, _collidingFeetAverageCenter, _movable.GroundSettings.FootHitAdjustRotationSpeed, 2);

            // ----- Local Methods ----- \\

            static bool IsValid(Collider _collider, List<Movable3D.Foot> _feet, int _footCount) {
                if (!MultiColliderMovable3DPhysicsWrapper.IsValid(_collider))
                    return false;

                // Ignore feet.
                for (int i = _footCount; i-- > 0;) {
                    if (_feet[i].Collider == _collider)
                        return false;
                }

                return true;
            }

            CollisionGroundData3D AdjustToGroundFeet(Vector3 _feetCenter, int _footCount, Vector3 _pivot, float _speed, int _feetMode) {
                Vector3 _center    = _feetCenter / _footCount;
                Vector3 _direction = _pivot - _center;

                return AdjustToGround(_center, _direction, _pivot, _speed, _feetMode);
            }

            CollisionGroundData3D AdjustToGround(Vector3 _center, Vector3 _direction, Vector3 _pivot, float _speed, int _feetMode) {

                // Calculs.
                Vector3 _axis = Vector3.Cross      (_direction, _up);
                float  _angle = Vector3.SignedAngle(_direction, _up, _axis);

                _angle = Mathf.Sign(_angle) * _speed * _operation.Velocity.DeltaTime * 90f;

                // Feet infos.
                SetGroundFeetInfos(_movable, _feetMode, _center, _direction);

                // Rotate.
                CollisionGroundData3D _groundData = RotateAround(_movable, _pivot, _axis, _angle);
                return InterpolateGroundData(_groundData);
            }

            CollisionGroundData3D InterpolateGroundData(CollisionGroundData3D _groundData) {

                Rigidbody _rigidbody = _movable.Rigidbody;

                Quaternion _originRotation = _rigidbody.rotation;
                Vector3    _originPosition = _rigidbody.position;

                RaycastHit _closestHit    = new RaycastHit() { distance = float.MaxValue };
                Collider _closestCollider = null;

                int _colliderCount = _selfColliders.Count;
                float _percent     = 1f;

                // -----------
                List<Vector3> _positionBuffer = BufferUtility.Vector3List;
                _positionBuffer.SoftResize(_colliderCount);

                for (int i = 0; i < _colliderCount; i++) {
                    Collider _collider = _selfColliders[i];

                    if (MultiColliderMovable3DPhysicsWrapper.IsValid(_collider)) {
                        _positionBuffer[i] = _collider.bounds.center;
                    }
                }

                //_rigidbody.position = _groundData.Position;
                //_rigidbody.rotation = _groundData.Rotation;
                // -----------

                // Calculate the closest hit point for each collider.
                for (int i = 0; i < _colliderCount; i++) {

                    // Setup.
                    Collider _collider = _selfColliders[i];
                    if (!MultiColliderMovable3DPhysicsWrapper.IsValid(_collider))
                        continue;

                    PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _mask);
                    Vector3 _colliderPosition          = _collider.bounds.center;

                    // Temporary - update the rigidbody position and rotation.
                    // Then, get the displacement between its original position and after updating the rigidbody.
                    // Preserving only the new rotation, use the displacement vector to perform a cast and get the closest distance to snap.

                    _rigidbody.position = _groundData.Position;
                    _rigidbody.rotation = _groundData.Rotation;
     
                    Vector3 _displacement = _collider.bounds.center - /*_positionBuffer[i]*/ _colliderPosition;
                    float _magnitude      = _displacement.magnitude;

                    // Reset the position only - preserve rotation, and refresh the collider transform to correctly update its rotation.
                    _rigidbody.position -= _displacement;
                    _physicsCollider.UpdateTransformPosition();

                    // Cast.
                    bool _hitCollider = _physicsCollider.CastAll(_displacement, out _hit, _magnitude + _collider.contactOffset * 2f, _mask, TriggerInteraction, true, _selfColliders) != 0;
                    if (_hitCollider) {

                        _hit.distance = Mathf.Max(0f, _hit.distance - _hit.collider.contactOffset);
                        if (_hit.distance >= _closestHit.distance)
                            continue;

                        // Compute data.
                        _closestHit = _hit;
                        _percent    = (_hit.distance == 0f) ? 0f : (_hit.distance / _magnitude);

                        _closestCollider = _collider;
                    }

                    // Reset the rigidbody position and rotation, and refresh the collider transform.
                    _rigidbody.position = _originPosition;
                    _rigidbody.rotation = _originRotation;

                    _physicsCollider.UpdateTransformPosition();
                }

                // If no foot is grounded, set the value for the closest hit only.
                if (_collidingFootCount == 0) {
                    for (int j = 0; j < _footCount; j++) {

                        Movable3D.Foot _foot = _feet[j];
                        if (_foot.Collider == _closestCollider) {

                            _foot.IsGrounded = true;
                            break;
                        }
                    }
                }

                // Lerp towards the ideal position and rotation based on the closest hit result.
                Quaternion _rotation = Quaternion.Lerp(_originRotation, _groundData.Rotation, _percent);
                Vector3    _position = Vector3   .Lerp(_originPosition, _groundData.Position, _percent);

                return new CollisionGroundData3D(_position, _rotation, false);
            }

            static CollisionGroundData3D RotateAround(Movable3D _movable, Vector3 _point, Vector3 _axis, float _angle) {
                Rigidbody _rigidbody = _movable.Rigidbody;

                Quaternion _rotation = _rigidbody.rotation;
                Vector3 _position    = _rigidbody.position;

                Mathm.RotateAround(_point, _axis, _angle, ref _position, ref _rotation);
                return new CollisionGroundData3D(_position, _rotation, false);
            }

            static void SetGroundFeetInfos(Movable3D _movable, int _mode, Vector3 _center, Vector3 _direction) {
                _movable.SetGroundFeetInfos(_mode, _center, _direction);
            }
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValid(Collider _collider) {
            return !_collider.isTrigger && _collider.enabled;
        }

        internal override void Setup(Movable3D _movable, CollisionOperationData3D _operation) {

            List<Collider> _selfColliders = _operation.SelfColliders;
            int _collisionMask = _movable.GetColliderMask();

            for (int i = _selfColliders.Count; i-- > 0;) {

                Collider _collider = _selfColliders[i];
                if (!IsValid(_collider))
                    continue;

                PhysicsCollider3D _physicsCollider = PhysicsCollider3D.GetTemp(_collider, _collisionMask);
                _physicsCollider.UpdateTransformPosition();
            }
        }
        #endregion
    }
}
