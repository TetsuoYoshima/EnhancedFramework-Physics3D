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
    // -------------------------------------------
    // Data & Utility
    // -------------------------------------------

    /// <summary>
    /// <see cref="PhysicsSystem3D{T}"/>-related enum used to determine collision calculs.
    /// </summary>
    public enum PhysicsSystem3DType {
        [Tooltip("Simple collisions for afordable performances\n\nIterations: 1")]
        Simple          = 0,

        [Tooltip("Intermediate collisions complexity\n\nIterations: 2")]
        Intermediate    = 2,

        [Tooltip("Complex collisions for a more accurate behaviour\n\nIterations: 3")]
        Complex         = 3,

        [Tooltip("Creature-like collisions with additional operations according to the surface\n\nIterations: 3")]
        Creature        = 10,

        [Separator(SeparatorPosition.Top)]

        [Tooltip("Complex collisions with multiple colliders\n\nIterations: 3")]
        MultiComplex    = 21,

        [Tooltip("Creature-like collisions with multiple colliders\n\nIterations: 3")]
        MultiCreature   = 22,
    }

    /// <summary>
    /// Data wrapper for a single <see cref="Movable3D"/>-related <see cref="UnityEngine.RaycastHit"/>.
    /// </summary>
    public struct CollisionHit3D {
        #region Global Members
        public readonly RaycastHit RaycastHit;

        public readonly Collider SourceCollider;
        public readonly Movable3D HitMovable;

        public readonly bool HasHitMovable;
        public float Distance;
        public bool IsValid;

        public readonly Collider HitCollider {
            get { return RaycastHit.collider; }
        }

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <inheritdoc cref="CollisionHit3D"/>
        public CollisionHit3D(float _distance) : this(new RaycastHit() { distance = _distance }) { }

        /// <inheritdoc cref="CollisionHit3D"/>
        public CollisionHit3D(RaycastHit _hit) : this(_hit, null, false) {
            IsValid = false;
        }

        /// <inheritdoc cref="CollisionHit3D"/>
        public CollisionHit3D(RaycastHit _hit, Collider _collider, bool _getMovable = true) {
            RaycastHit     = _hit;
            SourceCollider = _collider;

            Distance = _hit.distance;
            IsValid  = true;

            if (_getMovable) {
                HasHitMovable = _hit.collider.TryGetComponentInParent(out HitMovable);
            } else {
                HasHitMovable = false;
                HitMovable    = null;
            }
        }
        #endregion

        #region Utility
        public static readonly Comparison<CollisionHit3D> DistanceComparer = SortByDistance;

        // -----------------------

        /// <summary>
        /// Get this hit associated <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> of the hit object.</param>
        /// <returns>True if a <see cref="Movable3D"/> could be found on the hit object, false otherwise.</returns>
        public readonly bool GetMovable(out Movable3D _movable) {
            _movable = HitMovable;
            return HasHitMovable;
        }

        /// <summary>
        /// Sorts two <see cref="CollisionHit3D"/> by their hit distance.
        /// </summary>
        public static int SortByDistance(CollisionHit3D a, CollisionHit3D b) {
            return a.Distance.CompareTo(b.Distance);
        }
        #endregion
    }

    /// <summary>
    /// Data wrapper for a collision-related ground detection operation(s).
    /// </summary>
    public readonly struct CollisionGroundData3D {
        #region Content
        public readonly Quaternion Rotation;
        public readonly Vector3    Position;

        public readonly bool ComputeImpacts;
        public readonly bool ApplyData;

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <inheritdoc cref="CollisionGroundData3D"/>
        public CollisionGroundData3D(Vector3 _position, Quaternion _rotation, bool _computeHits) : this(_computeHits, true, _position, _rotation) { }

        /// <inheritdoc cref="CollisionGroundData3D"/>
        public CollisionGroundData3D(bool _computeHits) : this(_computeHits, false, Vector3.zero, Quaternion.identity) { }

        /// <inheritdoc cref="CollisionGroundData3D"/>
        public CollisionGroundData3D(bool _computeHits, bool _applyData, Vector3 _position, Quaternion _rotation) {
            ComputeImpacts = _computeHits;
            ApplyData   = _applyData;

            Position    = _position;
            Rotation    = _rotation;
        }
        #endregion
    }

    /// <summary>
    /// <see cref="PhysicsSystem3D{T}"/> result data-wrapper
    /// <para/>
    /// Configured as a class with a static instance to avoid creating a new instance
    /// <br/> each time it is passed as a parameter, or its value is changed (which happens a lot).
    /// </summary>
    public sealed class CollisionData3D {
        #region Global Members
        /// <summary>
        /// Static instance of this class.
        /// </summary>
        public static readonly CollisionData3D Data = new CollisionData3D();

        /// <summary>
        /// Internal temporary buffer used to store a single cast related hits result.
        /// </summary>
        public readonly EnhancedCollection<CollisionHit3D> InternalTempBuffer = new EnhancedCollection<CollisionHit3D>(3);

        /// <summary>
        /// All hits of this collision operations.
        /// </summary>
        public readonly EnhancedCollection<CollisionHit3D> HitBuffer          = new EnhancedCollection<CollisionHit3D>(3);

        public Vector3 OriginalVelocity = Vector3.zero;
        public Vector3 InstantVelocity  = Vector3.zero;
        public Vector3 DynamicVelocity  = Vector3.zero;

        public Quaternion AppliedRotation = Quaternion.identity;
        public Vector3    AppliedVelocity = Vector3.zero;

        public bool UpdatePosition = false;
        public bool UpdateRotation = false;

        /// <summary>
        /// Is the object considered as grounded after collisions?
        /// </summary>
        public bool IsGrounded = false;

        /// <summary>
        /// Temp buffer related total amount of distinct operation result registered.
        /// </summary>
        internal int TempBufferOperationCount = 0;

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <summary>
        /// Prevents from creating new instances of this class in other assemblies.
        /// </summary>
        internal CollisionData3D() { }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes this collision infos, reseting all its results to their default values.
        /// </summary>
        /// <param name="_velocity">Initial velocity used to perform collisions.</param>
        /// <returns>This <see cref="CollisionData3D"/>.</returns>
        internal CollisionData3D Init(Movable3D _movable, FrameVelocity _velocity) {
            Vector3 _dynamicVelocity = _movable.GetAxisVelocity(_velocity.Movement + _velocity.Force);

            OriginalVelocity = _dynamicVelocity;
            DynamicVelocity  = _dynamicVelocity;

            InstantVelocity = _velocity.Instant;
            AppliedVelocity = Vector3.zero;
            AppliedRotation = Quaternion.identity;

            UpdatePosition = false;
            UpdateRotation = false;

            IsGrounded = false;
            HitBuffer.Clear();

            return this;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Computes a collision impact.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance instigator of this collision.</param>
        /// <param name="_hit">Hit to compute.</param>
        internal void ComputeImpact(Movable3D _movable, CollisionOperationData3D _operation, CollisionHit3D _hit) {
            HitBuffer.Add(_hit);
            DynamicVelocity = _movable.ComputeImpact(_operation, DynamicVelocity, _hit);
        }

        /// <summary>
        /// Set the value of both <see cref="AppliedVelocity"/> and <see cref="AppliedRotation"/> at once.
        /// </summary>
        /// <param name="_appliedVelocity">Applied velocity value for this collision calcul.</param>
        /// <param name="_appliedRotation">Applied rotation value for this collision calcul.</param>
        public void SetAppliedData(Vector3 _appliedVelocity, Quaternion _appliedRotation) {
            AppliedVelocity = _appliedVelocity;
            AppliedRotation = _appliedRotation;

            UpdatePosition = AppliedVelocity != Vector3.zero;
            UpdateRotation = AppliedRotation != Quaternion.identity;
        }

        /// <summary>
        /// Copies all data content from another <see cref="CollisionData3D"/>.
        /// </summary>
        public void Copy(CollisionData3D _other) {
            HitBuffer.ReplaceBy(_other.HitBuffer);

            OriginalVelocity = _other.OriginalVelocity;
            InstantVelocity  = _other.InstantVelocity;
            DynamicVelocity  = _other.DynamicVelocity;

            AppliedVelocity  = _other.AppliedVelocity;
            AppliedRotation  = _other.AppliedRotation;

            UpdatePosition = _other.UpdatePosition;
            UpdateRotation = _other.UpdateRotation;

            IsGrounded = _other.IsGrounded;
        }

        /// <summary>
        /// Clears this object temporary hits related data.
        /// </summary>
        public void ClearTempHits() {
            InternalTempBuffer.Clear();
            TempBufferOperationCount = 0;
        }
        #endregion
    }

    /// <summary>
    /// Utility methods related to the <see cref="PhysicsSystem3DType"/> enum.
    /// </summary>
    internal static class PhysicsSystem3DTypeExtensions {
        #region Collision
        // -------------------------------------------
        // Manual
        // -------------------------------------------

        /// <summary>
        /// Performs collision calculs for a specific <see cref="Movable3D"/> according to this <see cref="PhysicsSystem3DType"/>,
        /// <br/> moving the object rigidbody accordingly in space.
        /// </summary>
        /// <param name="_type">Physics system to use.</param>
        /// <inheritdoc cref="PhysicsSystem3D{T}.CollisionPerformManual"/>
        public static void CollisionPerformManual(this PhysicsSystem3DType _type, Movable3D _movable, CollisionOperationData3D _operation) {
            switch (_type) {
                // Standard collisions - only one iteration.
                case PhysicsSystem3DType.Simple:
                    SimplePhysicsSystem3D.Instance  .CollisionPerformManual(_movable, _operation, 1, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Standard collisions - two iterations maximum.
                case PhysicsSystem3DType.Intermediate:
                    SimplePhysicsSystem3D.Instance  .CollisionPerformManual(_movable, _operation, 2, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Standard collisions - three iterations maximum.
                case PhysicsSystem3DType.Complex:
                    SimplePhysicsSystem3D.Instance  .CollisionPerformManual(_movable, _operation, 3, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex collisions with additional operations - three iterations maximum.
                case PhysicsSystem3DType.Creature:
                    CreaturePhysicsSystem3D.Instance.CollisionPerformManual(_movable, _operation, 3, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex collisions using multiple colliders - two iterations maximum.
                case PhysicsSystem3DType.MultiComplex:
                    SimplePhysicsSystem3D.Instance  .CollisionPerformManual(_movable, _operation, 2, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Creature collisions using multiple colliders - three iterations maximum.
                case PhysicsSystem3DType.MultiCreature:
                    CreaturePhysicsSystem3D.Instance.CollisionPerformManual(_movable, _operation, 3, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }

        // -------------------------------------------
        // Async
        // -------------------------------------------

        /// <inheritdoc cref="PhysicsSystem3D{T}.CollisionInitOperation"/>
        public static void CollisionInitOperation       (this PhysicsSystem3DType _type, Movable3D _movable, CollisionOperationData3D _operation) {
            switch (_type) {
                // Standard collisions - only one iteration.
                case PhysicsSystem3DType.Simple:
                    SimplePhysicsSystem3D.Instance  .CollisionInitOperation(_movable, _operation, 1, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Standard collisions - two iterations maximum.
                case PhysicsSystem3DType.Intermediate:
                    SimplePhysicsSystem3D.Instance  .CollisionInitOperation(_movable, _operation, 2, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Standard collisions - three iterations maximum.
                case PhysicsSystem3DType.Complex:
                    SimplePhysicsSystem3D.Instance  .CollisionInitOperation(_movable, _operation, 3, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex collisions with additional operations - three iterations maximum.
                case PhysicsSystem3DType.Creature:
                    CreaturePhysicsSystem3D.Instance.CollisionInitOperation(_movable, _operation, 3, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex collisions using multiple colliders - two iterations maximum.
                case PhysicsSystem3DType.MultiComplex:
                    SimplePhysicsSystem3D.Instance  .CollisionInitOperation(_movable, _operation, 3, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Creature collisions using multiple colliders - three iterations maximum.
                case PhysicsSystem3DType.MultiCreature:
                    CreaturePhysicsSystem3D.Instance.CollisionInitOperation(_movable, _operation, 2, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }

        /// <inheritdoc cref="PhysicsSystem3D{T}.CollisionPerformOperation"/>
        public static bool CollisionPerformOperation    (this PhysicsSystem3DType _type, Movable3D _movable, CollisionOperationData3D _operation, CastOperationCommands3D _commands) {
            switch (_type) {
                // Simple system.
                case PhysicsSystem3DType.Simple:
                case PhysicsSystem3DType.Intermediate:
                case PhysicsSystem3DType.Complex:
                case PhysicsSystem3DType.MultiComplex:
                    return SimplePhysicsSystem3D.Instance   .CollisionPerformOperation(_movable, _operation, _commands);

                // Complex system.
                case PhysicsSystem3DType.Creature:
                case PhysicsSystem3DType.MultiCreature:
                    return CreaturePhysicsSystem3D.Instance .CollisionPerformOperation(_movable, _operation, _commands);

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }

        /// <inheritdoc cref="PhysicsSystem3D{T}.CollisionComputeOperation"/>
        public static bool CollisionComputeOperation    (this PhysicsSystem3DType _type, Movable3D _movable, CollisionOperationData3D _operation) {
            switch (_type) {
                // Simple system.
                case PhysicsSystem3DType.Simple:
                case PhysicsSystem3DType.Intermediate:
                case PhysicsSystem3DType.Complex:
                case PhysicsSystem3DType.MultiComplex:
                    return SimplePhysicsSystem3D.Instance   .CollisionComputeOperation(_movable, _operation);

                // Complex system.
                case PhysicsSystem3DType.Creature:
                case PhysicsSystem3DType.MultiCreature:
                    return CreaturePhysicsSystem3D.Instance .CollisionComputeOperation(_movable, _operation);

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }

        /// <inheritdoc cref="PhysicsSystem3D{T}.CollisionFinalizeOperation"/>
        public static void CollisionFinalizeOperation   (this PhysicsSystem3DType _type, Movable3D _movable, CollisionOperationData3D _operation) {
            switch (_type) {
                // Simple system.
                case PhysicsSystem3DType.Simple:
                case PhysicsSystem3DType.Intermediate:
                case PhysicsSystem3DType.Complex:
                case PhysicsSystem3DType.MultiComplex:
                    SimplePhysicsSystem3D.Instance  .CollisionFinalizeOperation(_movable, _operation);
                    break;

                // Complex system.
                case PhysicsSystem3DType.Creature:
                case PhysicsSystem3DType.MultiCreature:
                    CreaturePhysicsSystem3D.Instance.CollisionFinalizeOperation(_movable, _operation);
                    break;

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }

        /// <inheritdoc cref="PhysicsSystem3D{T}.CollisionOnOperationResults"/>
        public static void CollisionOnOperationResults  (this PhysicsSystem3DType _type, Movable3D _movable, CollisionOperationData3D _operation, Collider _collider, RaycastHit[] _results, int _startIndex, int _count) {
            switch (_type) {
                // Simple system.
                case PhysicsSystem3DType.Simple:
                case PhysicsSystem3DType.Intermediate:
                case PhysicsSystem3DType.Complex:
                case PhysicsSystem3DType.MultiComplex:
                    SimplePhysicsSystem3D.Instance  .CollisionOnOperationResults(_movable, _operation, _collider, _results, _startIndex, _count);
                    break;

                // Complex system.
                case PhysicsSystem3DType.Creature:
                case PhysicsSystem3DType.MultiCreature:
                    CreaturePhysicsSystem3D.Instance.CollisionOnOperationResults(_movable, _operation, _collider, _results, _startIndex, _count);
                    break;

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }
        #endregion

        #region Overlap
        // -------------------------------------------
        // Manual
        // -------------------------------------------

        /// <summary>
        /// Performs an overlap for a specific <see cref="Movable3D"/> according to this <see cref="PhysicsSystem3DType"/>,
        /// <br/> and get informations about all overlapping <see cref="Collider"/>.
        /// </summary>
        /// <param name="_type">Physics system to use.</param>
        /// <inheritdoc cref="PhysicsSystem3D{T}.OverlapPerformManual"/>
        public static int OverlapPerformManual  (this PhysicsSystem3DType _type, Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders = null) {
            switch (_type) {
                // Simple system with a single collider.
                case PhysicsSystem3DType.Simple:
                case PhysicsSystem3DType.Intermediate:
                case PhysicsSystem3DType.Complex:
                    return SimplePhysicsSystem3D.Instance   .OverlapPerformManual(_movable, _buffer, _ignoredColliders, SingleColliderMovable3DPhysicsWrapper.Instance);

                // Complex system with a single collider.
                case PhysicsSystem3DType.Creature:
                    return CreaturePhysicsSystem3D.Instance .OverlapPerformManual(_movable, _buffer, _ignoredColliders, SingleColliderMovable3DPhysicsWrapper.Instance);

                // Simple system with multiple colliders.
                case PhysicsSystem3DType.MultiComplex:
                    return SimplePhysicsSystem3D.Instance   .OverlapPerformManual(_movable, _buffer, _ignoredColliders, MultiColliderMovable3DPhysicsWrapper.Instance);

                // Complex system with multiple colliders.
                case PhysicsSystem3DType.MultiCreature:
                    return CreaturePhysicsSystem3D.Instance .OverlapPerformManual(_movable, _buffer, _ignoredColliders, MultiColliderMovable3DPhysicsWrapper.Instance);

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }

        /// <summary>
        /// Performs an overlap and extract a specific <see cref="Movable3D"/> from all colliders according to this <see cref="PhysicsSystem3DType"/>
        /// </summary>
        /// <param name="_type">Physics system to use.</param>
        /// <inheritdoc cref="PhysicsSystem3D{T}.ExtractPerformManual"/>
        public static void ExtractPerformManual (this PhysicsSystem3DType _type, Movable3D _movable, IList<Collider> _ignoredColliders = null) {
            switch (_type) {
                // Simple system with a single collider.
                case PhysicsSystem3DType.Simple:
                case PhysicsSystem3DType.Intermediate:
                case PhysicsSystem3DType.Complex:
                    SimplePhysicsSystem3D.Instance  .ExtractPerformManual(_movable, _ignoredColliders, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex system with a single collider.
                case PhysicsSystem3DType.Creature:
                    CreaturePhysicsSystem3D.Instance.ExtractPerformManual(_movable, _ignoredColliders, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Simple system with multiple colliders.
                case PhysicsSystem3DType.MultiComplex:
                    SimplePhysicsSystem3D.Instance  .ExtractPerformManual(_movable, _ignoredColliders, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex system with multiple colliders.
                case PhysicsSystem3DType.MultiCreature:
                    CreaturePhysicsSystem3D.Instance.ExtractPerformManual(_movable, _ignoredColliders, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }

        // -------------------------------------------
        // Async
        // -------------------------------------------

        /// <inheritdoc cref="PhysicsSystem3D{T}.OverlapInitOperation"/>
        public static void OverlapInitOperation (this PhysicsSystem3DType _type, Movable3D _movable, OverlapOperationCommands3D _commands) {
            switch (_type) {
                // Simple system with a single collider.
                case PhysicsSystem3DType.Simple:
                case PhysicsSystem3DType.Intermediate:
                case PhysicsSystem3DType.Complex:
                    SimplePhysicsSystem3D.Instance  .OverlapInitOperation(_movable, _commands, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex system with a single collider.
                case PhysicsSystem3DType.Creature:
                    CreaturePhysicsSystem3D.Instance.OverlapInitOperation(_movable, _commands, SingleColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Simple system with multiple colliders.
                case PhysicsSystem3DType.MultiComplex:
                    SimplePhysicsSystem3D.Instance  .OverlapInitOperation(_movable, _commands, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                // Complex system with multiple colliders.
                case PhysicsSystem3DType.MultiCreature:
                    CreaturePhysicsSystem3D.Instance.OverlapInitOperation(_movable, _commands, MultiColliderMovable3DPhysicsWrapper.Instance);
                    break;

                default:
                    throw new InvalidPhysicsSystem3DTypeException();
            }
        }
        #endregion
    }

    // -------------------------------------------
    // Collision Systems
    // -------------------------------------------

    /// <summary>
    /// Physics system used to move an object in a 3D space and other physics operations (see <see cref="Movable3D"/>).
    /// <br/> Configured as a non-static class to allow using inheritance for creating new systems.
    /// </summary>
    internal abstract class PhysicsSystem3D<T> where T : PhysicsSystem3D<T>, new() {
        #region Global Members
        /// <summary>
        /// The one and only instance of this system.
        /// </summary>
        public static readonly T Instance = new T();

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <summary>
        /// Prevents from creating new instances of this class.
        /// </summary>
        protected PhysicsSystem3D() { }
        #endregion

        // ===== Collisions ===== \\

        #region Collision Calculs
        // -------------------------------------------
        // Manual
        // -------------------------------------------

        /// <summary>
        /// Performs collisions and move the object in space.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> to perform collisions for.</param>
        /// <param name="_operation">Data wrapper of this collision operation.</param>
        /// <param name="_recursivity">Maximum allowed recursivity loop.</param>
        /// <param name="_physicsWrapper">Wrapper used to perform physics operations.</param>
        public void CollisionPerformManual(Movable3D _movable, CollisionOperationData3D _operation, int _recursivity, Movable3DPhysicsWrapper _physicsWrapper) {

            // Init.
            CollisionInitOperation(_movable, _operation, _recursivity, _physicsWrapper);

            // Collision calculs.
            CollisionPerformManualRecursively(_movable, _movable.Rigidbody, _operation);

            // Finalize.
            CollisionFinalizeOperation(_movable, _operation);
        }

        /// <inheritdoc cref="CollisionPerformManual"/>
        private void CollisionPerformManualRecursively(Movable3D _movable, Rigidbody _rigidbody, CollisionOperationData3D _operation) {

            _operation.ClearTempHits();

            // Cast.
            ref Vector3 _velocity = ref GetCollisionVelocity(_operation, out bool i);
            PerformCastAll(_movable, _operation, _velocity, out _, true, false);

            // Compute.
            if (CollisionComputeOperation(_movable, _operation))
                return;

            // Continue.
            CollisionPerformManualRecursively(_movable, _rigidbody, _operation);
        }

        // -------------------------------------------
        // Async
        // -------------------------------------------

        /// <summary>
        /// Initializes this collision async operation.
        /// </summary>
        /// <inheritdoc cref="CollisionPerformManual"/>
        public void CollisionInitOperation      (Movable3D _movable, CollisionOperationData3D _operation, int _recursivity, Movable3DPhysicsWrapper _physicsWrapper) {
            FrameVelocity _velocity = OnComputeVelocity(_movable, _operation.Velocity);
            _operation.Setup(_physicsWrapper, _velocity, _recursivity);
        }

        /// <summary>
        /// Registers a command to perform a single collision cast according to its next velocity.
        /// </summary>
        /// <inheritdoc cref="CollisionPerformManual"/>
        public bool CollisionPerformOperation   (Movable3D _movable, CollisionOperationData3D _operation, CastOperationCommands3D _commands) {
            Vector3 _velocity = GetCollisionVelocity(_operation, out bool i);

            // Collisions are over.
            if (_velocity.IsNull() || (_operation.Recursivity <= 0))
                return true;

            _operation.ClearTempHits();

            // Register cast.
            _operation.PhysicsWrapper.RegisterCastCommand(_movable, _operation, _commands, _velocity);
            return false;
        }

        /// <summary>
        /// Computes the results from a previously registered cast command.
        /// </summary>
        /// <inheritdoc cref="CollisionPerformManual"/>
        public bool CollisionComputeOperation   (Movable3D _movable, CollisionOperationData3D _operation) {

            ref Vector3 _velocity = ref GetCollisionVelocity(_operation, out bool _isInstantVelocity);
            int _amount = _operation.Data.InternalTempBuffer.Count;

            Rigidbody _rigidbody  = _movable.Rigidbody;

            // Nothing hit along the way, so simply move the object and complete operation.
            if (_amount == 0) {
                MoveObject(_rigidbody, _velocity);

                // Instant velocity management - continue with dynamic velocity.
                if (_isInstantVelocity) {
                    _velocity = Vector3.zero;
                    return false;
                }

                return Break();
            }

            CollisionHit3D _hit = ComputeCollisionHits(_movable, _operation, _velocity);

            // Move this object and compute impacts - return true (complete) if the object is stuck into something and cannot move.
            if (!MoveObjectAndComputeImpacts(_movable, _rigidbody, _operation, ref _velocity, _hit, _amount))
                return true;

            // Instant velocity management.
            if (_isInstantVelocity) {
                _velocity = Vector3.zero;
                return false;
            }

            // Recursivity limit.
            if (--_operation.Recursivity == 0) {
                return Break();
            }

             // Compute main collision.
            OnComputeCollision(_movable, _rigidbody, _operation, _hit);

            if (_velocity.IsNull()) {
                return Break();
            }

            return false;

            // ----- Local Method ----- \\

            bool Break() {
                OnCollisionBreak(_movable, _rigidbody, _operation);
                return true;
            }
        }

        /// <summary>
        /// Called after all collisions calculs.
        /// </summary>
        /// <inheritdoc cref="CollisionPerformManual"/>
        public void CollisionFinalizeOperation  (Movable3D _movable, CollisionOperationData3D _operation) {
            // Ground.
            OnComputeGround(_movable, _operation);

            // Compute final data.
            _operation.UpdateAppliedData(_movable.Position, _movable.Rotation);
        }

        /// <summary>
        /// Called after collision cast to compute its results.
        /// </summary>
        /// <inheritdoc cref="CollisionPerformManual"/>
        public void CollisionOnOperationResults (Movable3D _movable, CollisionOperationData3D _operation, Collider _collider, RaycastHit[] _results, int _startIndex, int _count) {

            // Register hit(s).
            _count = Physics3DUtility.FilterCastHits(_collider, _results, _startIndex, _count, out _, true, _operation.SelfColliders);
            Movable3DPhysicsWrapper.RegisterCollisionHits(_operation, _collider, _results, _startIndex, _count);
        }

        // -------------------------------------------
        // Cast Operations
        // -------------------------------------------

        /// <inheritdoc cref="PerformCastAll"/>
        protected bool PerformCast  (Movable3D _movable, CollisionOperationData3D _operation, Vector3 _velocity, out CollisionHit3D _hit, bool _registerHits = true, bool _computeHits = true) {
            return PerformCastAll(_movable, _operation, _velocity, out _hit, _registerHits, _computeHits) != 0;
        }

        /// <summary>
        /// Performs a cast and precisely compute hit datas of the object.
        /// </summary>
        /// <param name="_velocity">Velocity used to perform this cast.</param>
        /// <param name="_hit">First hit of this cast.</param>
        /// <param name="_computeHits">If true automatically registers and computes hits.
        /// <br/> Otherwise, only  returns the closest one without any computation or registration.</param>
        /// <returns>Total amount of hit object.</returns>
        /// <inheritdoc cref="CollisionPerformManualRecursively"/>
        protected int PerformCastAll(Movable3D _movable, CollisionOperationData3D _operation, Vector3 _velocity, out CollisionHit3D _hit, bool _registerHits = true, bool _computeHits = true) {

            float _distance = _velocity.magnitude;
            int _amount     = _operation.PhysicsWrapper.Cast(_movable, _operation, _velocity, _distance, out RaycastHit _raycastHit, _registerHits);

            // Nothing on the way.
            if (_amount == 0) {
                _hit = new CollisionHit3D(_distance);
                return 0;
            }

            // Compute hits.
            if (_computeHits) {
                _hit = ComputeCollisionHits(_movable, _operation, _velocity);
            } else {
                _hit = new CollisionHit3D(_raycastHit);
            }

            return _amount;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Computes and apply all given <see cref="CollisionHit3D"/>.
        /// </summary>
        /// <param name="_movable">This computing object movable.</param>
        /// <param name="_velocity">Frame velocity of the object.</param>
        /// <returns>Main closest <see cref="CollisionHit3D"/> hit after computations.</returns>
        private CollisionHit3D ComputeCollisionHits(Movable3D _movable, CollisionOperationData3D _operation, Vector3 _velocity) {

            EnhancedCollection<CollisionHit3D> _hits = _operation.Data.InternalTempBuffer;
            float _distance = _velocity.magnitude;
            int _count      = _hits.Count;

            // No hit.
            if (_count == 0) {
                return new CollisionHit3D(_distance);
            }

            // If multiple operation results were registered, sort them.
            if (_operation.Data.TempBufferOperationCount > 1) {
                _hits.Sort(CollisionHit3D.DistanceComparer);
            }

            // Early return - only get first hit.
            if (_operation.PhysicsSettings.CollisionOneHitIfNoEffect && !_movable.CanApplyCollisionEffect(_operation)) {
                return _hits[0];
            }

            float _contactOffset    = Physics3DUtility.ContactOffset;
            float _moveDistance     = _distance;
            bool _isStopped         = false;
            int _maxIndex           = _count - 1;

            // Try to push encountered objects - first, calculate how far we can go.
            for (int i = 0; i < _count; i++) {
                CollisionHit3D _collisionHit = _hits[i];

                if (_collisionHit.GetMovable(out Movable3D _other)) {

                    _other.OnHitByMovable(_movable, _collisionHit);
                    _moveDistance *= _movable.GetPushVelocityCoef(_other);

                } else {

                    _moveDistance = 0f;
                }

                if (Mathm.ApproximatelyZero(_moveDistance)) {

                    _isStopped = true;
                    _maxIndex  = i;

                    _contactOffset = _collisionHit.SourceCollider.contactOffset + _collisionHit.HitCollider.contactOffset;

                    break;
                }
            }

            // Get closest hit.
            CollisionHit3D _hit = _hits[_maxIndex];

            if (_isStopped) {
                _velocity = _velocity.normalized * Mathf.Max(0f, _hit.Distance - _contactOffset);
            } else {
                _hit.Distance = _distance;
            }

            // Push objects on the way.
            for (int i = 0; i <= _maxIndex; i++) {
                CollisionHit3D _collisionHit = _hits[i];

                if (_collisionHit.GetMovable(out Movable3D _other) && _other.enabled) {
                    _velocity = _movable.PushObject(_other, _velocity);
                }
            }

            return _hit;
        }

        /// <summary>
        /// Get the next velocity to use for performing collisions.
        /// </summary>
        /// <param name="_isInstantVelocity">True if the returned velocity is an "instant" velocity.</param>
        /// <returns>Reference value of the next velocity to perform collisions for.</returns>
        private static ref Vector3 GetCollisionVelocity(CollisionOperationData3D _operation, out bool _isInstantVelocity) {

            ref Vector3 _velocity = ref _operation.Data.InstantVelocity;
            _isInstantVelocity    = !_velocity.IsNull();

            if (!_isInstantVelocity) {
                _velocity = ref _operation.Data.DynamicVelocity;
            }

            return ref _velocity;
        }
        #endregion

        #region Callbacks
        /// <summary>
        /// Called before any collision calculs to compute the <see cref="FrameVelocity"/>.
        /// </summary>
        protected virtual FrameVelocity OnComputeVelocity(Movable3D _movable, FrameVelocity _velocity) {
            return _velocity;
        }

        /// <summary>
        /// Called after a collision with another object to compute a <see cref="CollisionHit3D"/>.
        /// </summary>
        protected virtual void OnComputeCollision   (Movable3D _movable, Rigidbody _rigidbody, CollisionOperationData3D _operation, CollisionHit3D _hit) { }

        /// <summary>
        /// Called once the collision calculs are stopped, either interrupted or completed.
        /// </summary>
        protected virtual void OnCollisionBreak     (Movable3D _movable, Rigidbody _rigidbody, CollisionOperationData3D _operation) { }
        #endregion

        #region Ground
        /// <summary>
        /// Performs additional calculs before setting a <see cref="Movable3D"/> ground state.
        /// </summary>
        protected virtual bool OnComputeGround(Movable3D _movable, CollisionOperationData3D _operation) {
            CollisionData3D _data = _operation.Data;
            RaycastHit _groundHit = default;
            bool _isGrounded = _data.IsGrounded;

            if (!_isGrounded && _movable.UseGravity) {

                _operation.ClearTempHits();

                // Iterate over collision impacts to find if one of these can be considered as ground.
                // Use a reverse loop to get the last ground surface hit first.

                EnhancedCollection<CollisionHit3D> _hits = _data.HitBuffer;
                for (int i = _hits.Count; i-- > 0;) {

                    CollisionHit3D _hit    = _hits[i];
                    RaycastHit _raycastHit = _hit.RaycastHit;

                    if (IsGroundSurface(_movable, _raycastHit)) {
                        _isGrounded = true;
                        _groundHit  = _raycastHit;

                        break;
                    }

                    // Get last hit info as default - even if not grounded.
                    if (_groundHit.colliderInstanceID == 0) {
                        _groundHit = _raycastHit;
                    }
                }
            }

            // Clear hits so that new ones can be registered.
            _operation.ClearTempHits();

            // Wrapper operation(s).
            CollisionGroundData3D _groundInfo = _operation.PhysicsWrapper.ComputeGround(_movable, _operation, ref _isGrounded, ref _groundHit);

            // Compute impacts.
            if (_groundInfo.ComputeImpacts) {
                ComputeImpacts(_movable, _operation);
            }

            if (_groundInfo.ApplyData) {
                SetObjectPositionAndRotation(_movable.Rigidbody, _groundInfo.Position, _groundInfo.Rotation);
            }

            // Update ground state.
            _movable.SetGroundState(_operation, _isGrounded, _groundHit);
            return _isGrounded;
        }

        /// <inheritdoc cref="Movable3DPhysicsWrapper.IsGroundSurface"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool IsGroundSurface(Movable3D _movable, RaycastHit _hit) {
            return Movable3DPhysicsWrapper.IsGroundSurface(_movable, _hit);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Displaces an object in space and computes a velocity according to all registered impacts.
        /// </summary>
        /// <param name="_castVelocity">Velocity used for this collision cast (dynamically modified to match the new velocity after displacement).</param>
        /// <param name="_mainHit">Main hit of this collision.</param>
        /// <returns>False if the object is stuck into something and cannot be moved, false otherwise.</returns>
        /// <inheritdoc cref="CollisionPerformManualRecursively"/>
        protected static bool MoveObjectAndComputeImpacts(Movable3D _movable, Rigidbody _rigidbody, CollisionOperationData3D _operation, ref Vector3 _castVelocity, CollisionHit3D _mainHit, int _amount) {
            float _distance = _mainHit.Distance;

            // Zero distance means that the object is stuck into something and cannot move - so complete operation.
            if (_distance == 0f) {
                ComputeImpact(_movable, _operation, _mainHit);
                return false;
            }

            // Move the object and get the remaining velocity, after displacement, according to the impact normal.
            //
            // For instance, the object may have a normalized velocity of (1, -1, 1).
            // We hit something under the object - the ground -, with a normal of (0, 1, 0).
            // So we can continue to perform collision with a velocity of (1, 0, 1).
            if (Mathf.Approximately(_distance, _castVelocity.magnitude)) {

                MoveObject(_rigidbody, _castVelocity);
                _castVelocity = Vector3.zero;

            } else {

                _castVelocity = MoveObject(_rigidbody, _castVelocity, _mainHit);
            }

            ComputeImpacts(_movable, _operation, _amount);
            return true;
        }

        // -------------------------------------------
        // Movement & Rotation
        // -------------------------------------------

        /// <inheritdoc cref="MoveObject(Rigidbody, Vector3)"/>
        protected static Vector3 MoveObject(Rigidbody _rigidbody, Vector3 _velocity, CollisionHit3D _hit) {
            // To not stuck the object into another collider, be sure the compute contact offset.
            float _distance = _hit.Distance;
            float _offset;

            if (_hit.IsValid) {
                _offset = _hit.SourceCollider.contactOffset + _hit.HitCollider.contactOffset;
            } else {
                _offset = Physics3DUtility.ContactOffset;
            }

            if ((_distance -= _offset) > 0f) {
                Vector3 _move = _velocity.normalized * _distance;

                MoveObject(_rigidbody, _move);
                _velocity -= _move;
            }

            return _velocity;
        }

        /// <summary>
        /// Displaces an object in space.
        /// </summary>
        /// <param name="_rigidbody"><see cref="Rigidbody"/> of the object to move.</param>
        /// <param name="_velocity">World space position offset.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void MoveObject(Rigidbody _rigidbody, Vector3 _velocity) {
            _rigidbody.position += _velocity;
        }

        /// <summary>
        /// Set the current position and rotation in space of an object.
        /// </summary>
        /// <param name="_rigidbody"><see cref="Rigidbody"/> of the object to modify.</param>
        /// <param name="_position">New position value.</param>
        /// <param name="_rotation">New rotation value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void SetObjectPositionAndRotation(Rigidbody _rigidbody, Vector3 _position, Quaternion _rotation) {
            _rigidbody.position = _position;
            _rigidbody.rotation = _rotation;
        }

        // -------------------------------------------
        // Compute
        // -------------------------------------------

        /// <inheritdoc cref="ComputeImpacts"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ComputeImpacts(Movable3D _movable, CollisionOperationData3D _operation) {
            int _amount = _operation.Data.InternalTempBuffer.Count;
            ComputeImpacts(_movable, _operation, _amount);
        }

        /// <summary>
        /// Computes all <see cref="CollisionHit3D"/> registered during the last cast operation.
        /// </summary>
        /// <inheritdoc cref="ComputeImpact"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ComputeImpacts(Movable3D _movable, CollisionOperationData3D _operation, int _amount) {
            EnhancedCollection<CollisionHit3D> _hitBuffer = _operation.Data.InternalTempBuffer;

            for (int i = 0; i < _amount; i++) {
                ComputeImpact(_movable, _operation, _hitBuffer[i]);
            }
        }

        /// <summary>
        /// Computes a <see cref="CollisionHit3D"/> data.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instigator of the impact.</param>
        /// <param name="_operation">Data wrapper of the associated operation.</param>
        /// <param name="_hit">Hit to compute.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void ComputeImpact (Movable3D _movable, CollisionOperationData3D _operation, CollisionHit3D _hit) {
            _movable.Velocity.ComputeImpact(_movable, _operation, _hit);
            _operation.Data  .ComputeImpact(_movable, _operation, _hit);
        }
        #endregion

        // ===== Overlap ===== \\

        #region Overlap
        /// <summary>
        /// Performs an overlap for this object and get all overlapping <see cref="Collider"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> to perform an overlap for.</param>
        /// <param name="_buffer">Buffer used to store overlap results.</param>
        /// <param name="_ignoredColliders">All colliders to ignore.</param>
        /// <param name="_physicsWrapper">Wrapper used to perform physics operations.</param>
        /// <returns>Total count of overlapping colliders.</returns>
        public int OverlapPerformManual(Movable3D _movable, List<Collider> _buffer, IList<Collider> _ignoredColliders, Movable3DPhysicsWrapper _physicsWrapper) {
            return _physicsWrapper.Overlap(_movable, _buffer, _ignoredColliders);
        }

        /// <summary>
        /// Extracts this object from all overlapping <see cref="Collider"/>.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> to extract.</param>
        /// <param name="_ignoredColliders">All colliders to ignore.</param>
        /// <param name="_physicsWrapper">Wrapper used to perform physics operations.</param>
        public void ExtractPerformManual(Movable3D _movable, IList<Collider> _ignoredColliders, Movable3DPhysicsWrapper _physicsWrapper) {
            _physicsWrapper.Extract(_movable, _ignoredColliders);
        }

        // -------------------------------------------
        // Commands
        // -------------------------------------------

        /// <summary>
        /// Registers a command to perform a single overlap operation.
        /// </summary>
        public void OverlapInitOperation(Movable3D _movable, OverlapOperationCommands3D _commands, Movable3DPhysicsWrapper _physicsWrapper) {
            _physicsWrapper.RegisterOverlapCommand(_movable, _commands);
        }
        #endregion
    }

    /// <summary>
    /// Simple collision system, without any additional operation.
    /// </summary>
    internal sealed class SimplePhysicsSystem3D : PhysicsSystem3D<SimplePhysicsSystem3D> { }

    /// <summary>
    /// Creature-like collision system.
    /// <br/> Performs additional operations like step climbing and ground snapping.
    /// </summary>
    internal sealed class CreaturePhysicsSystem3D : PhysicsSystem3D<CreaturePhysicsSystem3D> {
        #region Callbacks
        private const float ClimbValidationCastOffsetCoef = 2.5f;

        // -----------------------

        protected override FrameVelocity OnComputeVelocity(Movable3D _movable, FrameVelocity _velocity) {

            // When grounded, project the object movement (relative velocity) on the ground surface.
            // Only project the object horizontal and forward velocity, to always keep a straight vertical trajectory.
            if (_movable.IsGrounded) {

                ref Vector3 _movement = ref _velocity.Movement;
                Quaternion _rotation  = _velocity.DirectionRotation;

                Vector3 _vertical = _movable.UpDirection.Rotate(_rotation) * _movement.RotateInverse(_rotation).y;
                Vector3 _flat     = Vector3.ProjectOnPlane(_movement - _vertical, _movable.GroundNormal);

                _movement = _flat + _vertical;
            }

            return base.OnComputeVelocity(_movable, _velocity);
        }

        protected override void OnComputeCollision(Movable3D _movable, Rigidbody _rigidbody, CollisionOperationData3D _operation, CollisionHit3D _hit) {
            base.OnComputeCollision(_movable, _rigidbody, _operation, _hit);

            RaycastHit _raycastHit = _hit.RaycastHit;

            // Obstacle collision.
            if (!IsGroundSurface(_movable, _raycastHit)) {

                // Define if the obstacle can be climbed by casting all along it, then move the object according to cast informations.
                Vector3 _normal = _raycastHit.normal;
                Vector3 _climb  = Vector3.ProjectOnPlane(Vector3.up, _normal).normalized * _movable.ClimbHeight;

                PerformCast(_movable, _operation, _climb, out CollisionHit3D _castHit, false, false);
                _climb -= MoveObject(_rigidbody, _climb, _castHit);

                Vector3 _validCast = Physics3DUtility.ContactOffset * ClimbValidationCastOffsetCoef * -_normal;

                // Then perform another cast in the obstacle inverse normal direction. If nothing is hit, then the step can be climbed.
                // To climb it, simply add some velocity according the objstacle surface, and set the object as grounded (so gravity won't apply).
                if (!PerformCast(_movable, _operation, _validCast, out _, false, false)) {

                    CollisionData3D _data = _operation.Data;

                    _data.DynamicVelocity += Vector3.ClampMagnitude(_climb, _data.OriginalVelocity.magnitude);
                    _data.IsGrounded = true;
                }

                // Reset the object position as before cast.
                MoveObject(_rigidbody, -_climb);
            }
        }

        protected override void OnCollisionBreak(Movable3D _movable, Rigidbody _rigidbody, CollisionOperationData3D _operation) {
            base.OnCollisionBreak(_movable, _rigidbody, _operation);

            // Ground snapping.
            // Only snap when grounded, as a falling object near the ground does not need to be snapped (and would be visually strange).
            if (!_movable.IsGrounded)
                return;

            Vector3 _direction = _movable.GravitySense;
            float _dot = Vector3.Dot(_direction, _operation.Data.OriginalVelocity);

            // Only snap if the object original vertical velocity was not positive
            // (otherwise, a jumping object would be automatically bring back to the ground).
            if (_dot < 0f)
                return;

            _direction *= _movable.SnapHeight;

            if (PerformCast(_movable, _operation, _direction, out CollisionHit3D _hit, true, true)) {
                MoveObjectAndComputeImpacts(_movable, _rigidbody, _operation, ref _direction, _hit, 1);
            }
        }
        #endregion
    }

    #region Exception
    /// <summary>
    /// Exception raised for an invalid <see cref="PhysicsSystem3DType"/>,
    /// when the int value is outside the enum limits.
    /// </summary>
    public sealed class InvalidPhysicsSystem3DTypeException : Exception {
        public InvalidPhysicsSystem3DTypeException() : base() { }

        public InvalidPhysicsSystem3DTypeException(string _message) : base(_message) { }

        public InvalidPhysicsSystem3DTypeException(string _message, Exception _innerException) : base(_message, _innerException) { }
    }
    #endregion
}
