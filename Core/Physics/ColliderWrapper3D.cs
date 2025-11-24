// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

#if UNITY_2022_2_OR_NEWER
#define QUERY_PARAMETERS
#define OVERLAP_COMMANDS
#endif

using EnhancedEditor;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    // ===== Base ===== \\

    /// <summary>
    /// Internal low-level wrapper for engine 3D primitive colliders.
    /// <para/>
    /// Used from the <see cref="PhysicsCollider3D"/> class for precise cast and overlap operations.
    /// </summary>
    internal abstract class ColliderWrapper3D {
        #region Global Members
        private static readonly CapsuleColliderWrapper3D capsuleWrapper = new CapsuleColliderWrapper3D(null);
        private static readonly SphereColliderWrapper3D sphereWrapper   = new SphereColliderWrapper3D(null);
        private static readonly BoxColliderWrapper3D boxWrapper         = new BoxColliderWrapper3D(null);

        protected Transform transform = null;

        /// <summary>
        /// <see cref="UnityEngine.Collider"/> of this object.
        /// </summary>
        public abstract Collider Collider { get; set; }

        /// <summary>
        /// This collider type id (1 is CapsuleCollider, 2 is SphereCollider, 3 is BoxCollider).
        /// </summary>
        public abstract int ColliderTypeId { get; }

        /// <summary>
        /// This collider type.
        /// </summary>
        public abstract Type ColliderType { get; }

        /// <summary>
        /// Contact offset of this collider.
        /// </summary>
        public float ContactOffset { get { return Collider.contactOffset; } }

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        protected ColliderWrapper3D(Collider _collider) {
            transform = _collider.IsValid() ? _collider.transform : null;
        }

        // -------------------------------------------
        // Getters
        // -------------------------------------------

        /// <summary>
        /// Creates a new appropriated <see cref="ColliderWrapper3D"/> for a specific collider.
        /// </summary>
        /// <param name="_collider">Collider to get a new wrapper for.</param>
        /// <returns>Configured wrapper for the specified collider.</returns>
        public static ColliderWrapper3D Create(Collider _collider) {
            switch (_collider) {
                case BoxCollider _box:
                    return new BoxColliderWrapper3D(_box);

                case CapsuleCollider _capsule:
                    return new CapsuleColliderWrapper3D(_capsule);

                case SphereCollider _sphere:
                    return new SphereColliderWrapper3D(_sphere);

                default:
                    throw new NonPrimitiveColliderException();
            }
        }

        /// <summary>
        /// Get a temporary appropriated <see cref="ColliderWrapper"/> for a specific collider.
        /// </summary>
        /// <param name="_collider">Collider to get a temporary wrapper for.</param>
        /// <returns>Configured wrapper for the specified collider.</returns>
        public static ColliderWrapper3D Get(Collider _collider) {
            switch (_collider) {
                case BoxCollider _box:
                    boxWrapper.Collider = _box;
                    return boxWrapper;

                case CapsuleCollider _capsule:
                    capsuleWrapper.Collider = _capsule;
                    return capsuleWrapper;

                case SphereCollider _sphere:
                    sphereWrapper.Collider = _sphere;
                    return sphereWrapper;

                default:
                    throw new NonPrimitiveColliderException();
            }
        }
        #endregion

        #region Physics
        /// <summary>
        /// Performs a raycast from this collider using specific parameters.
        /// </summary>
        public abstract int RaycastAll(Vector3 _direction, RaycastHit[] _buffer, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction);

        /// <summary>
        /// Performs a raycast from this collider using specific parameters.
        /// </summary>
        public abstract bool Raycast(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction);

        /// <summary>
        /// Performs a cast from this collider using specific parameters.
        /// </summary>
        public abstract int Cast(Vector3 _direction, RaycastHit[] _buffer, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction);

        /// <summary>
        /// Get the current overlapping colliders.
        /// </summary>
        public abstract int Overlap(Collider[] _buffer, int _mask, QueryTriggerInteraction _triggerInteraction);
        #endregion

        #region Command
        // -------------------------------------------
        // Cast
        // -------------------------------------------

        /// <summary>
        /// Get a command to perform a Capsule cast operation.
        /// </summary>
        public virtual CapsulecastCommand GetCapsuleCastCommand(Vector3 _direction, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            throw new InvalidColliderException();
        }

        /// <summary>
        /// Get a command to perform a Sphere cast operation.
        /// </summary>
        public virtual SpherecastCommand GetSphereCastCommand(Vector3 __direction, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            throw new InvalidColliderException();
        }

        /// <summary>
        /// Get a command to perform a Box cast operation.
        /// </summary>
        public virtual BoxcastCommand GetBoxCastCommand(Vector3 _direction, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            throw new InvalidColliderException();
        }

        // -------------------------------------------
        // Overlap
        // -------------------------------------------

        #if OVERLAP_COMMANDS
        /// <summary>
        /// Get a command to perform a Capsule overlap operation.
        /// </summary>
        public virtual OverlapCapsuleCommand GetCapsuleOverlapCommand(int _mask, QueryTriggerInteraction _triggerInteraction) {
            throw new InvalidColliderException();
        }

        /// <summary>
        /// Get a command to perform a Sphere overlap operation.
        /// </summary>
        public virtual OverlapSphereCommand GetSphereOverlapCommand(int _mask, QueryTriggerInteraction _triggerInteraction) {
            throw new InvalidColliderException();
        }

        /// <summary>
        /// Get a command to perform a Box overlap operation.
        /// </summary>
        public virtual OverlapBoxCommand GetBoxOverlapCommand(int _mask, QueryTriggerInteraction _triggerInteraction) {
            throw new InvalidColliderException();
        }
        #endif
        #endregion

        #region Utility
        /// <summary>
        /// Get this collider world-space non rotated bounds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bounds GetBounds() {
            return new Bounds(GetCenter(), GetExtents(true) * 2f);
        }

        /// <summary>
        /// Get this collider world-space center position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Vector3 GetCenter() {
            return Collider.bounds.center;
        }

        /// <summary>
        /// Get this collider world-space non-rotated extents.
        /// </summary>
        public abstract Vector3 GetExtents(bool _worldSpace = true);

        /// <summary>
        /// Modifies the bounding box of this collider.
        /// </summary>
        /// <param name="_center">New center of the collider bounding box (measured in the object local space).</param>
        /// <param name="_size">New size of the collider bounding box (measured in the object local space).</param>
        public abstract void SetBounds(Vector3 _center, Vector3 _size);

        /// <summary>
        /// Get the bottom position of this collider.
        /// </summary>
        public Vector3 GetBottomPosition() {
            return GetCenter() - transform.TransformVector(new Vector3(0f, GetExtents(false).y, 0f));
        }

        /// <summary>
        /// Refreshes this collider associated <see cref="Transform"/> position.
        /// </summary>
        public void UpdateTransformPosition() {

            // The collider transform local position might have changed - for some unknown reason,
            // kinematic rigidbodies still detect some collisions and can displace children colliders transform.
            //
            // Also, this ensure that the collider is aligned with the new rigidbody position.

            Rigidbody _rigidbody = Collider.attachedRigidbody;
            Transform _rigidbodyTransform = _rigidbody.transform;

            if (transform != _rigidbodyTransform) {

                _rigidbodyTransform.position = _rigidbody.position;
                _rigidbodyTransform.rotation = _rigidbody.rotation;

                transform.localRotation = Quaternion.identity;
                transform.localPosition = Vector3.zero;
            }

            //_collider.transform.position = _collider.attachedRigidbody.position;
            //_collider.transform.rotation = _collider.attachedRigidbody.rotation;
        }
        #endregion
    }

    // ===== Derived ===== \\

    internal sealed class CapsuleColliderWrapper3D : ColliderWrapper3D {
        #region Global Members
        private CapsuleCollider collider = null;

        public override Collider Collider {
            get { return collider; }
            set {
                if (value is not CapsuleCollider _capsule)
                    throw new InvalidColliderException($"Collider must be of type {typeof(CapsuleCollider).Name}");

                collider  = _capsule;
                transform = _capsule.transform;
            }
        }

        public override Type ColliderType {
            get { return typeof(CapsuleCollider); }
        }

        public override int ColliderTypeId {
            get { return 1; }
        }

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        public CapsuleColliderWrapper3D(CapsuleCollider _collider) : base(_collider) {
            collider = _collider;
        }
        #endregion

        #region Physics
        public override int RaycastAll(Vector3 _direction, RaycastHit[] _buffer, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            UpdateTransformPosition();
            _direction.Normalize();

            float _contactOffset = ContactOffset;
            Vector3 _position    = collider.ClosestPoint(GetCenter() + (_direction * GetHeight(true))) - (_direction * _contactOffset);

            return Physics.RaycastNonAlloc(_position, _direction, _buffer, _distance + _contactOffset, _mask, _triggerInteraction);
        }

        public override bool Raycast(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            UpdateTransformPosition();
            _direction.Normalize();

            float _contactOffset = ContactOffset;
            Vector3 _position    = collider.ClosestPoint(GetCenter() + (_direction * GetHeight(true))) - (_direction * _contactOffset);

            return Physics.Raycast(_position, _direction, out _hit, _distance + _contactOffset, _mask, _triggerInteraction);
        }

        public override int Cast(Vector3 _direction, RaycastHit[] _buffer, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            _direction.Normalize();

            Vector3 _offset = GetPointOffset();
            Vector3 _center = GetCenter();
            float _radius   = GetRadius(true) - ContactOffset;

            return Physics.CapsuleCastNonAlloc(_center - _offset, _center + _offset, _radius, _direction, _buffer, _distance, _mask, _triggerInteraction);
        }

        public override int Overlap(Collider[] _buffer, int _mask, QueryTriggerInteraction _triggerInteraction) {
            Vector3 _offset = GetPointOffset();
            Vector3 _center = GetCenter();
            float _radius   = GetRadius(true);

            return Physics.OverlapCapsuleNonAlloc(_center - _offset, _center + _offset, _radius, _buffer, _mask, _triggerInteraction);
        }
        #endregion

        #region Command
        // -------------------------------------------
        // Cast
        // -------------------------------------------

        public override CapsulecastCommand GetCapsuleCastCommand(Vector3 _direction, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            _direction.Normalize();

            Vector3 _offset = GetPointOffset();
            Vector3 _center = GetCenter();
            float _radius   = GetRadius(true) - ContactOffset;

            #if QUERY_PARAMETERS
            QueryParameters _parameters = new QueryParameters(_mask, true, _triggerInteraction, true);
            return new CapsulecastCommand(_center - _offset, _center + _offset, _radius, _direction, _parameters, _distance);
            #else
            return new CapsulecastCommand(_center - _offset, _center + _offset, _radius, _direction, _distance, _mask);
            #endif
        }

        // -------------------------------------------
        // Overlap
        // -------------------------------------------

        #if OVERLAP_COMMANDS
        public override OverlapCapsuleCommand GetCapsuleOverlapCommand(int _mask, QueryTriggerInteraction _triggerInteraction) {
            Vector3 _offset = GetPointOffset();
            Vector3 _center = GetCenter();
            float _radius   = GetRadius(true);

            QueryParameters _parameters = new QueryParameters(_mask, true, _triggerInteraction, true);
            return new OverlapCapsuleCommand(_center - _offset, _center + _offset, _radius, _parameters);
        }
        #endif
        #endregion

        #region Utility
        public override Vector3 GetExtents(bool _worldSpace = true) {
            float _radius = GetRadius(false);
            float _height = GetHeight(false) * .5f;
            Vector3 _extents;

            switch (collider.direction) {
                // X axis.
                case 0:
                    _extents = new Vector3(_height, _radius, _radius);
                    break;

                // Y axis.
                case 1:
                    _extents = new Vector3(_radius, _height, _radius);
                    break;

                // Z axis.
                case 2:
                    _extents = new Vector3(_radius, _radius, _height);
                    break;

                // This never happen.
                default:
                    throw new InvalidCapsuleHeightException();
            }

            if (_worldSpace) {
                _extents = transform.TransformVector(_extents);
            }

            return _extents;
        }

        public override void SetBounds(Vector3 _center, Vector3 _size) {
            CapsuleCollider _collider = collider;
            Transform _transform = transform;

            _center = _transform.InverseTransformPoint(_center);
            _size   = _transform.InverseTransformVector(_size);

            _collider.center = _center;

            _collider.radius = Mathf.Abs(_size.x * .5f);
            _collider.height = Mathf.Abs(_size.y);
        }

        // -----------------------

        public Vector3 GetPointOffset() {
            float _height = (GetHeight(false) * .5f) - GetRadius(false);
            Vector3 _offset;

            switch (collider.direction) {
                // X axis.
                case 0:
                    _offset = new Vector3(_height, 0f, 0f);
                    break;

                // Y axis.
                case 1:
                    _offset = new Vector3(0f, _height, 0f);
                    break;

                // Z axis.
                case 2:
                    _offset = new Vector3(0f, 0f, _height);
                    break;

                // This never happen.
                default:
                    throw new InvalidCapsuleHeightException();
            }

            return transform.TransformVector(_offset);
        }

        /// <summary>
        /// Get the radius of this <see cref="CapsuleCollider"/>.
        /// </summary>
        /// <param name="_worldSpace">If true, returns the radius of this collider in world space - otherwise in local space.</param>
        /// <returns>The radius of this collider.</returns>
        public float GetRadius(bool _worldSpace = true) {
            CapsuleCollider _collider = collider;
            float _radius = Mathf.Abs(_collider.radius);

            if (_worldSpace) {
                Vector3 _scale = transform.lossyScale;

                switch (_collider.direction) {
                    // X axis.
                    case 0:
                        _scale.x = 0f;
                        break;

                    // Y axis.
                    case 1:
                        _scale.y = 0f;
                        break;

                    // Z axis.
                    case 2:
                        _scale.z = 0f;
                        break;

                    // This never happen.
                    default:
                        throw new InvalidCapsuleHeightException();
                }

                _radius *= _scale.Max();
            }

            return _radius;
        }

        /// <summary>
        /// Get the height of this <see cref="CapsuleCollider"/>.
        /// </summary>
        /// <param name="_worldSpace">If true, returns the height of this collider in world space - otherwise in local space.</param>
        /// <returns>The height of this collider.</returns>
        public float GetHeight(bool _worldSpace = true) {
            float _height = Mathf.Abs(collider.height);

            if (_worldSpace) {
                float _scale;

                switch (collider.direction) {
                    // X axis.
                    case 0:
                        _scale = transform.lossyScale.x;
                        break;

                    // Y axis.
                    case 1:
                        _scale = transform.lossyScale.y;
                        break;

                    // Z axis.
                    case 2:
                        _scale = transform.lossyScale.z;
                        break;

                    // This never happen.
                    default:
                        throw new InvalidCapsuleHeightException();
                }

                _height *= _scale;
            }

            return _height;
        }
        #endregion
    }

    internal sealed class SphereColliderWrapper3D : ColliderWrapper3D {
        #region Global Members
        private SphereCollider collider = null;

        public override Collider Collider {
            get { return collider; }
            set {
                if (value is not SphereCollider _sphere)
                    throw new InvalidColliderException($"Collider must be of type {typeof(SphereCollider).Name}");

                collider  = _sphere;
                transform = _sphere.transform;
            }
        }

        public override Type ColliderType {
            get { return typeof(SphereCollider); }
        }

        public override int ColliderTypeId {
            get { return 2; }
        }

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        public SphereColliderWrapper3D(SphereCollider _collider) : base(_collider) {
            collider = _collider;
        }
        #endregion

        #region Physics
        public override int RaycastAll(Vector3 _direction, RaycastHit[] _buffer, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            UpdateTransformPosition();
            _direction.Normalize();

            Vector3 _offset = Vector3.Scale(_direction, GetExtents()).Rotate(transform.rotation);
            Vector3 _center = GetCenter();

            return Physics.RaycastNonAlloc(_center + _offset, _direction, _buffer, _distance, _mask, _triggerInteraction);
        }

        public override bool Raycast(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            UpdateTransformPosition();
            _direction.Normalize();

            Vector3 _offset = Vector3.Scale(_direction, GetExtents()).Rotate(transform.rotation);
            Vector3 _center = GetCenter();

            return Physics.Raycast(_center + _offset, _direction, out _hit, _distance, _mask, _triggerInteraction);
        }

        public override int Cast(Vector3 _velocity, RaycastHit[] _buffer, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            _velocity.Normalize();

            Vector3 _center = GetCenter();
            float _radius   = GetRadius(true) - ContactOffset;

            return Physics.SphereCastNonAlloc(_center, _radius, _velocity, _buffer, _distance, _mask, _triggerInteraction);
        }

        public override int Overlap(Collider[] _buffer, int _mask, QueryTriggerInteraction _triggerInteraction) {
            Vector3 _center = GetCenter();
            float _radius   = GetRadius(true);

            return Physics.OverlapSphereNonAlloc(_center, _radius, _buffer, _mask, _triggerInteraction);
        }
        #endregion

        #region Command
        // -------------------------------------------
        // Cast
        // -------------------------------------------

        public override SpherecastCommand GetSphereCastCommand(Vector3 _direction, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            _direction.Normalize();

            Vector3 _center = GetCenter();
            float _radius   = GetRadius(true) - ContactOffset;

            #if QUERY_PARAMETERS
            QueryParameters _parameters = new QueryParameters(_mask, true, _triggerInteraction, true);
            return new SpherecastCommand(_center, _radius, _direction, _parameters, _distance);
            #else
            return new SpherecastCommand(_center, _radius, _direction, _distance, _mask);
            #endif
        }

        // -------------------------------------------
        // Overlap
        // -------------------------------------------

        #if OVERLAP_COMMANDS
        public override OverlapSphereCommand GetSphereOverlapCommand(int _mask, QueryTriggerInteraction _triggerInteraction) {
            Vector3 _center = GetCenter();
            float _radius   = GetRadius(true);

            QueryParameters _parameters = new QueryParameters(_mask, true, _triggerInteraction, true);
            return new OverlapSphereCommand(_center, _radius, _parameters);
        }
        #endif
        #endregion

        #region Utility
        public override Vector3 GetExtents(bool _worldSpace = true) {
            return GetRadius(_worldSpace).ToVector3();
        }

        public override void SetBounds(Vector3 _center, Vector3 _size) {
            Transform _transform = transform;
            SphereCollider _collider = collider;

            _center = _transform.InverseTransformPoint(_center);
            _size   = _transform.InverseTransformVector(_size);

            _collider.center = _center;
            _collider.radius = Mathf.Abs(_size.Max() * .5f);
        }

        // -----------------------

        /// <summary>
        /// Get the radius of this <see cref="SphereCollider"/>.
        /// </summary>
        /// <param name="_worldSpace">If true, returns the radius of this collider in world space - otherwise in local space.</param>
        /// <returns>The radius of this collider.</returns>
        public float GetRadius(bool _worldSpace = true) {
            float _radius = Mathf.Abs(collider.radius);

            if (_worldSpace) {
                _radius *= transform.lossyScale.Max();
            }

            return _radius;
        }
        #endregion
    }

    internal sealed class BoxColliderWrapper3D : ColliderWrapper3D {
        #region Global Members
        private BoxCollider collider = null;

        public override Collider Collider {
            get { return collider; }
            set {
                if (value is not BoxCollider _box)
                    throw new InvalidColliderException($"Collider must be of type {typeof(BoxCollider).Name}");

                collider  = _box;
                transform = _box.transform;
            }
        }

        public override Type ColliderType {
            get { return typeof(BoxCollider); }
        }

        public override int ColliderTypeId {
            get { return 3; }
        }

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        public BoxColliderWrapper3D(BoxCollider _collider) : base(_collider) {
            collider = _collider;
        }
        #endregion

        #region Physics
        public override int RaycastAll(Vector3 _direction, RaycastHit[] _buffer, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            UpdateTransformPosition();
            _direction.Normalize();

            Vector3 _offset = Vector3.Scale(_direction, GetExtents()).Rotate(transform.rotation);
            Vector3 _center = GetCenter();

            return Physics.RaycastNonAlloc(_center + _offset, _direction, _buffer, _distance, _mask, _triggerInteraction);
        }

        public override bool Raycast(Vector3 _direction, out RaycastHit _hit, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            UpdateTransformPosition();
            _direction.Normalize();

            Vector3 _offset = Vector3.Scale(_direction, GetExtents()).Rotate(transform.rotation);
            Vector3 _center = GetCenter();

            return Physics.Raycast(_center + _offset, _direction, out _hit, _distance, _mask, _triggerInteraction);
        }

        public override int Cast(Vector3 _direction, RaycastHit[] _buffer, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            _direction.Normalize();

            Vector3 _extents = GetExtents() - ContactOffset.ToVector3();
            Vector3 _center  = GetCenter();

            return Physics.BoxCastNonAlloc(_center, _extents, _direction, _buffer, transform.rotation, _distance, _mask, _triggerInteraction);
        }

        public override int Overlap(Collider[] _buffer, int _mask, QueryTriggerInteraction _triggerInteraction) {
            Vector3 _extents = GetExtents();
            Vector3 _center  = GetCenter();

            return Physics.OverlapBoxNonAlloc(_center, _extents, _buffer, transform.rotation, _mask, _triggerInteraction);
        }
        #endregion

        #region Command
        // -------------------------------------------
        // Cast
        // -------------------------------------------

        public override BoxcastCommand GetBoxCastCommand(Vector3 _direction, float _distance, int _mask, QueryTriggerInteraction _triggerInteraction) {
            _direction.Normalize();

            Vector3 _extents = GetExtents() - ContactOffset.ToVector3();
            Vector3 _center  = GetCenter();

            #if QUERY_PARAMETERS
            QueryParameters _parameters = new QueryParameters(_mask, true, _triggerInteraction, true);
            return new BoxcastCommand(_center, _extents, transform.rotation, _direction, _parameters, _distance);
            #else
            return new BoxcastCommand(_center, _extents, transform.rotation, _direction, _distance, _mask);
            #endif
        }

        // -------------------------------------------
        // Overlap
        // -------------------------------------------

        #if OVERLAP_COMMANDS
        public override OverlapBoxCommand GetBoxOverlapCommand(int _mask, QueryTriggerInteraction _triggerInteraction) {
            Vector3 _extents = GetExtents();
            Vector3 _center  = GetCenter();

            QueryParameters _parameters = new QueryParameters(_mask, true, _triggerInteraction, true);
            return new OverlapBoxCommand(_center, _extents, transform.rotation, _parameters);
        }
        #endif
        #endregion

        #region Utility
        public override Vector3 GetExtents(bool _worldSpace = true) {
            Vector3 _extents = collider.size * .5f;

            // Negative size causes the whole Physics system to badly breaks out.
            // For the sake of security, just make sure it won't happen.
            _extents.x = Mathf.Abs(_extents.x);
            _extents.y = Mathf.Abs(_extents.y);
            _extents.z = Mathf.Abs(_extents.z);

            Transform _transform = transform;
            if (_worldSpace) {
                _extents = _transform.TransformVector(_extents);
            }

            // Get the non-oriented extents value of this collider, as oriented values
            // may be negative, and cause error if used as is and as in absolute values.
            return _transform.RelativeVector(_extents);
        }

        public override void SetBounds(Vector3 _center, Vector3 _size) {
            Transform _transform  = transform;
            BoxCollider _collider = collider;

            _center = _transform.InverseTransformPoint(_center);
            _size   = _transform.InverseTransformVector(_size);

            _collider.center = _center;

            // Negative size causes the whole Physics system to badly breaks out.
            // For the sake of security, just make sure it won't happen.
            _size.x = Mathf.Abs(_size.x);
            _size.y = Mathf.Abs(_size.y);
            _size.z = Mathf.Abs(_size.z);

            _collider.size = _size;
        }
        #endregion
    }

    #region Exceptions
    /// <summary>
    /// Exception for any non-primitive collider, forbidding
    /// the usage of complex (cast or overlap) physics operations.
    /// </summary>
    public sealed class NonPrimitiveColliderException : Exception {
        public NonPrimitiveColliderException() : base() { }

        public NonPrimitiveColliderException(string _message) : base(_message) { }

        public NonPrimitiveColliderException(string _message, Exception _innerException) : base(_message, _innerException) { }
    }

    /// <summary>
    /// Exception raised for an invalid type of <see cref="Collider"/>.
    /// </summary>
    public sealed class InvalidColliderException : Exception {
        public InvalidColliderException() : base() { }

        public InvalidColliderException(string _message) : base(_message) { }

        public InvalidColliderException(string _message, Exception _innerException) : base(_message, _innerException) { }
    }

    /// <summary>
    /// Exception for invalid capsule height axis, making
    /// the associated collider cast or overlap operation impossible.
    /// </summary>
    public sealed class InvalidCapsuleHeightException : Exception {
        public InvalidCapsuleHeightException() : base() { }

        public InvalidCapsuleHeightException(string _message) : base(_message) { }

        public InvalidCapsuleHeightException(string _message, Exception _innerException) : base(_message, _innerException) { }
    }
    #endregion
}
