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
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

#if DOTWEEN_ENABLED
using DG.Tweening;
#endif

using Min   = EnhancedEditor.MinAttribute;
using Range = EnhancedEditor.RangeAttribute;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Interface to inherit any sensitive moving object on which to maintain control from.
    /// <para/>
    /// Provides multiple common utilities to properly move an object in space.
    /// </summary>
    public interface IMovable3D {
        #region Content
        /// <summary>
        /// This object <see cref="UnityEngine.Rigidbody"/>.
        /// </summary>
        Rigidbody Rigidbody { get; }

        /// <summary>
        /// Get / set this object world position.
        /// <br/> Use this instead of <see cref="Transform.position"/>.
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// Get / set this object world rotation.
        /// <br/> Use this instead of <see cref="Transform.rotation"/>.
        Quaternion Rotation { get; set; }
        #endregion
    }

    // ===== Velocity Data ===== \\

    /// <summary>
    /// <see cref="Movable3D"/> global velocity wrapper.
    /// </summary>
    [Serializable]
    public sealed class Velocity {
        #region Velocity
        /// <summary>
        /// Velocity of the object itself, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/second.
        /// </summary>
        [Tooltip("Velocity of the object itself, in absolute world coordinates\n\nIn unit/second")]
        public Vector3 Movement = Vector3.zero;

        /// <summary>
        /// Velocity of the object itself, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/frame.
        /// </summary>
        [Tooltip("Instant velocity applied on the object, for this frame only, in absolute world coordinates\n\nIn unit/frame")]
        public Vector3 InstantMovement = Vector3.zero;

        /// <summary>
        /// External velocity applied on the object, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/second.
        /// </summary>
        [Tooltip("External velocity applied on the object, in absolute world coordinates\n\nIn unit/second")]
        public Vector3 Force = Vector3.zero;

        /// <summary>
        /// Instant velocity applied on the object, for this frame only, in absolute world coordinates
        /// <br/> (non object-oriented).
        /// <para/>
        /// In unit/frame.
        /// </summary>
        [Tooltip("Instant velocity applied on the object, for this frame only, in absolute world coordinates\n\nIn unit/frame")]
        public Vector3 Instant = Vector3.zero;

        [Space(10f)]

        /// <summary>
        /// Velocity applied over time on the object, using a specific curve and duration.
        /// <br/> Prevents from performing any movement velocity while active.
        /// </summary>
        public List<VelocityOverTime> VelocityOverTime = new List<VelocityOverTime>();
        #endregion

        #region Utility
        /// <summary>
        /// Computes this object velocity for this frame.
        /// </summary>
        internal void ComputeVelocity(float _deltaTime) {

            int _count = VelocityOverTime.Count;
            if (_count == 0)
                return;

            // Remove any movement while applying velocity.
            Movement.Set(0f, 0f, 0f);

            // Apply.
            for (int i = _count; i-- > 0;) {
                VelocityOverTime _velocityOver = VelocityOverTime[i];

                if (_velocityOver.Evaluate(_deltaTime, out Vector3 _velocity)) {
                    VelocityOverTime.RemoveAt(i);
                } else {
                    VelocityOverTime[i] = _velocityOver;
                }

                // Ignore gravity and other opposite vertical force.
                if ((_velocity.y > 0f) && (Force.y < 0f)) {
                    Force.y = 0f;
                }

                Instant += _velocity;
            }
        }

        /// <summary>
        /// Computes an impact on this object velocity, and modifies its vector(s) accordingly.
        /// </summary>
        internal void ComputeImpact(Movable3D _movable, CollisionOperationData3D _operation, CollisionHit3D _hit) {

            // Transfer.
            if (_hit.GetMovable(out Movable3D _other) && _movable.TransferVelocity(_other))
                return;

            // Force.
            Force = _movable.ComputeImpact(_operation, Force, _hit);

            // Velocity over time.
            for (int i = VelocityOverTime.Count; i-- > 0;) {

                VelocityOverTime _velocity = VelocityOverTime[i];
                Vector3 _movement = _movable.ComputeImpact(_operation, _velocity.Movement, _hit);

                if (_movement.IsNull()) {
                    VelocityOverTime.RemoveAt(i);
                } else {
                    _velocity.UpdateMovement(_movement);
                    VelocityOverTime[i] = _velocity;
                }
            }
        }

        /// <summary>
        /// Transfers this object velocity to another <see cref="Movable3D"/>.
        /// </summary>
        internal void TransferVelocity(Movable3D _other, float _reduceSelfCoef, float _applyCoef) {

            Velocity _otherVelocity = _other.Velocity;
            Vector3 _force = Force.SetY(0f) * _applyCoef;

            // Force.
            if (_force != Vector3.zero) {
                Vector3 _otherForce = _otherVelocity.Force;

                _other.AddForceVelocity(_force);
                _otherVelocity.Force = Vector3.ClampMagnitude(_otherVelocity.Force.SetY(0f), Mathf.Max(_force.magnitude, _otherForce.SetY(0f).magnitude)) + new Vector3(0f, _otherForce.y, 0f);

                Force = (_force * _reduceSelfCoef).SetY(Force.y);
            }

            // Velocity over time.
            for (int i = VelocityOverTime.Count; i-- > 0;) {

                VelocityOverTime _velocity = VelocityOverTime[i];
                if (!_velocity.CanTransferVelocity)
                    continue;

                _velocity = _velocity.Reduce(_applyCoef);
                if (_otherVelocity.VelocityOverTime.Count != 0) {
                    _otherVelocity.VelocityOverTime.Clear();
                }

                _other.AddVelocityOverTime(_velocity);
                VelocityOverTime[i] = _velocity.Reduce(_reduceSelfCoef);
            }
        }

        // -----------------------

        /// <summary>
        /// Resets this velocity frame-related vectors.
        /// </summary>
        internal void ResetFrameVelocity() {

            InstantMovement.Set(0f, 0f, 0f);
            Instant        .Set(0f, 0f, 0f);
            Movement       .Set(0f, 0f, 0f);
        }

        /// <summary>
        /// Resets all this velocity vectors.
        /// </summary>
        internal void Reset() {

            InstantMovement .Set(0f, 0f, 0f);
            Instant         .Set(0f, 0f, 0f);
            Movement        .Set(0f, 0f, 0f);
            Force           .Set(0f, 0f, 0f);
            VelocityOverTime.Clear();
        }
        #endregion
    }

    /// <summary>
    /// Data wrapper used to apply a velocity to a <see cref="Movable3D"/> over time.
    /// </summary>
    [Serializable]
    public struct VelocityOverTime {
        #region Global Members
        public Vector3 Movement;
        public float Duration;
        public AnimationCurve Curve;

        public bool CanTransferVelocity;
        public float Timer;
        public Vector3 LastMovement;

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <inheritdoc cref="VelocityOverTime"/>
        public VelocityOverTime(Vector3 _movement, float _duration, AnimationCurve _curve, bool canTransferVelocity = true) {
            Movement = _movement;
            Duration = _duration;
            Curve    = _curve;

            CanTransferVelocity = canTransferVelocity;
            Timer = 0f;
            LastMovement = Vector3.zero;
        }

        /// <inheritdoc cref="VelocityOverTime"/>
        public VelocityOverTime(VelocityOverTime _template, float _time) {
            Movement = _template.Movement;
            Duration = _template.Duration;
            Curve    = _template.Curve;

            CanTransferVelocity = _template.CanTransferVelocity;
            Timer = Mathf.Clamp(_time, 0f, Duration);

            float _duration = Duration;
            if (_duration != 0f) {
                LastMovement = DOVirtual.EasedValue(Vector3.zero, Movement, Timer / _duration, Curve);
            } else {
                LastMovement = _template.LastMovement;
            }
        }
        #endregion

        #region Utility
        /// <summary>
        /// Evaluates this movement value.
        /// </summary>
        public bool Evaluate(float _deltaTime, out Vector3 _movement) {
            float _duration = Duration;
            if (_duration == 0f) {

                _movement = Movement;
                return true;
            }

            float _time = Timer + _deltaTime;
            float _percent;
            bool _isOver;

            if (_time >= _duration) {
                _percent = 1f;
                _isOver  = true;
            } else {
                _percent = _time / _duration;
                _isOver  = false;
            }

            #if DOTWEEN_ENABLED
            Vector3 _fullMovement = DOVirtual.EasedValue(Vector3.zero, Movement, _percent, Curve);
            #else
            Vector3 _fullMovement = Vector3.Lerp(Vector3.zero, Movement, Curve.Evaluate(_percent));
            #endif
            _movement = _fullMovement - LastMovement;

            Timer = _time;
            LastMovement = _fullMovement;

            return _isOver;
        }

        /// <summary>
        /// Updates this velocity movement.
        /// </summary>
        public void UpdateMovement(Vector3 _movement) {
            Movement = _movement;

            float _duration = Duration;
            if (_duration != 0f) {
                #if DOTWEEN_ENABLED
                LastMovement = DOVirtual.EasedValue(Vector3.zero, _movement, Timer / _duration, Curve);
                #else
                LastMovement = Vector3.Lerp(Vector3.zero, _movement, Curve.Evaluate(Timer / _duration));
                #endif
            }
        }

        /// <summary>
        /// Reduces this velocity using a given coefficient.
        /// </summary>
        public readonly VelocityOverTime Reduce(float _coef) {
            return new VelocityOverTime(this, Duration - ((Duration - Timer) * _coef));
        }

        // -----------------------

        public readonly override string ToString() {
            return $"Mv: {Movement} - Dr: {Duration} - Tm: {Timer} - LstMv: {LastMovement}";
        }
        #endregion
    }

    /// <summary>
    /// <see cref="Velocity"/> frame wrapper.
    /// </summary>
    [Serializable]
    public struct FrameVelocity {
        #region Velocity
        public Vector3 Movement;
        public Vector3 Force;
        public Vector3 Instant;

        public Quaternion DirectionRotation;

        /// <summary>
        /// This frame time delta.
        /// </summary>
        public float DeltaTime;

        // -----------------------

        /// <summary>
        /// Is this frame velocity valid to perform collisions?
        /// </summary>
        public readonly bool IsValid {
            get { return !Movement.IsNull() || !Force.IsNull() || !Instant.IsNull(); }
        }
        #endregion
    }

    // ===== Settings Data ===== \\

    /// <summary>
    /// <see cref="Movable3D"/>-related ground settings data.
    /// </summary>
    [Serializable]
    public class Movable3DGroundSettings {
        #region Content
        [Tooltip("Percentage on which to orientate this object according to its current ground surface")]
        [Enhanced, Range(0f, 1f)] public float GroundOrientationFactor = 1f;

        [Tooltip("Speed used to orientate this object according to its current ground surface\n\nIn quarter-circle per second")]
        [Enhanced, Range(0f, 100f)] public float GroundOrientationSpeed = 1f;

        [Space(10f)]

        [Tooltip("Minimum and maximum angles used to rotate this object according to its up direction when in the air (not touching ground)")]
        [Enhanced, MinMax(-180f, 180f)] public Vector2 AirRotationAngles = new Vector2(0f, 0f);

        [Tooltip("Speed used to rotate this object when in the air (not touching ground)\n\nIn quarter-circle per second")]
        [Enhanced, Range(0f, 100f)] public float AirRotationSpeed = 0f;

        [Space(10f)]

        [Tooltip("Speed used to orientate this object according to its current ground surface when a foot is touching ground\n\nIn quarter-circle per second")]
        [Enhanced, Range(0f, 100f)] public float FootHitAdjustRotationSpeed = 1f;

        [Tooltip("Speed used to orientate this object according to its current ground surface when no foot is touching ground\n\nIn quarter-circle per second")]
        [Enhanced, Range(0f, 100f)] public float FootNoHitAdjustRotationSpeed = 1f;

        [HelpBox("Options for adjusting the object rotation based on its feet currently touching the ground - ignore if not foot is specified", MessageType.Info, false, 5f)]

        [Tooltip("Speed used to reset this object orientation when no foot is not touching ground\n\nIn quarter-circle per second")]
        [Enhanced, Range(0f, 100f)] public float FootNoHitResetRotationSpeed = 1f;
        #endregion
    }

    /// <summary>
    /// <see cref="Movable3D"/>-related weight settings data.
    /// </summary>
    [Serializable]
    public class Movable3DWeightSettings {
        #region Content
        [Tooltip("Weight of this object - used to determine how other objects interact with it")]
        [Enhanced, Min(0f)] public float Weight = 1f;

        [Space(10f)]

        [Tooltip("First is the minimum weight this object can push but will start to lose velocity\n\nSecond is the maximum weight this object can push")]
        public Vector2 PushRange = new Vector2(0f, 0f);

        [Tooltip("Curve used to evaluate the above range and how this object velocity is modified when pushing other objects\n\n(0 for full velocity, 1 for when this object cannot push the other)")]
        [Enhanced, EnhancedCurve(0f, 0f, 1f, 1f, SuperColor.Lime)] public AnimationCurve PushCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Space(10f)]

        [Tooltip("First is the minimum weight this object can transfer velocity to but will start to dampen its own\n\nSecond is the maximum weight this object can transfer values to")]
        public Vector2 TransferVelocityRange = new Vector2(0f, 0f);

        [Tooltip("Curve used to evaluate the above range and how this object velocity is modified when transfering it to other objects\n\n(0 for full transfer, 1 as the limit)")]
        [Enhanced, EnhancedCurve(0f, 0f, 1f, 1f)] public AnimationCurve TransferVelocityCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [Space(10f)]

        [Tooltip("Coefficient combined with the above curve-range parameters to transfer this object velocity")]
        [Enhanced, Range(0f, 1f)] public float TransferVelocityApply = .6f;

        [Tooltip("Coefficient combined with the above curve-range parameters to dampen this object own velocity during transfer")]
        [Enhanced, Range(0f, 1f)] public float TransferVelocitySelfReduce = .5f;
        #endregion
    }

    // ===== Enum ===== \\

    /// <summary>
    /// Object-related gravity mode.
    /// </summary>
    public enum GravityMode {
        [Tooltip("Uses the world global vectors for gravity")]
        World   = 0,

        [Tooltip("Uses the surface of the nearest ground as the reference gravity vector")]
        Dynamic = 1,
    }

    /// <summary>
    /// Various <see cref="Movable3D"/>-related options.
    /// </summary>
    [Flags]
    public enum MovableOption {
        None = 0,

        // ----
        [Separator(SeparatorPosition.Top)]

        [Tooltip("Allows this object to move in space and perform collision calculs")]
        Move                = 1 << 0,

        [Tooltip("Automatically extracts this object from any overlapping collider")]
        AutoExtract         = 1 << 1,

        [Tooltip("If true, continuously refresh this object position every frame, even when no velocity was applied")]
        RefreshContinuously = 1 << 2,

        [Tooltip("Makes sure that when this object stops moving, its Velocity is equalized based on the previous frame instead of continuing on its actual Force")]
        EqualizeVelocity    = 1 << 3,

        [Separator(SeparatorPosition.Top)]

        [Tooltip("Always try to push obstacle instead of extracting from them - always keep current position")]
        RockBehaviour       = 1 << 11,

        [Tooltip("Slide against obstacle surfaces if the velocity and the collision angle allow it")]
        SlideOnSurfaces     = 1 << 12,

        [Tooltip("Checks for Physics Surfaces on ground GameObjects")]
        PhysicsSurface      = 1 << 13,

        [Separator(SeparatorPosition.Top)]

        [Tooltip("Adjust this object shadow rotation according on the ground normal")]
        ShadowRotation      = 1 << 16,

        [Tooltip("Adjust this object shadow Y position if the object is not touching ground")]
        ShadowHeight        = 1 << 17,

        [Separator(SeparatorPosition.Top)]

        [Tooltip("If overlapping with another collider at the start of the frame, only try to extract from it and do not perform collision")]
        SkipCollisionIfOverlap  = 1 << 21,

        [Tooltip("Do not perform any extract operation unless this object overlaps with another collider")]
        ExtractOnlyIfOverlap    = 1 << 22,

        [Tooltip("Do not perform any extract operation unless this object is in contact with another collider")]
        ExtractOnlyIfContact    = 1 << 23,

        // ----
        [Ethereal]
        All = Move | AutoExtract | RefreshContinuously | EqualizeVelocity
            | RockBehaviour | SlideOnSurfaces | PhysicsSurface
            | SkipCollisionIfOverlap | ExtractOnlyIfOverlap | ExtractOnlyIfContact,
    }

    // ===== Component ===== \\

    /// <summary>
    /// Base class for every moving object of the game using complex velocity and collision detections.
    /// </summary>
    [SelectionBase, RequireComponent(typeof(Rigidbody))]
    #pragma warning disable
    public abstract class Movable3D : EnhancedBehaviour, IMovable3D, ITriggerActor {
        #region Feet
        /// <summary>
        /// Wrapper for a single foot data.
        /// </summary>
        [Serializable]
        public class Foot {
            [Enhanced, Duo(nameof(IsGrounded))] public Collider Collider = null;
            [Enhanced, HideInInspector] public bool IsGrounded = false;

            [Enhanced, HideInInspector] public Vector3 BottomPosition = Vector3.zero;

            // -------------------------------------------
            // Constructor(s)
            // -------------------------------------------

            /// <inheritdoc cref="Foot"/>
            public Foot(Collider _collider) {
                Collider   = _collider;
                IsGrounded = false;
            }

            // -------------------------------------------
            // Utility
            // -------------------------------------------

            public void UpdateData(Vector3 _bottomPosition, bool _isGrounded) {
                BottomPosition = _bottomPosition;
                IsGrounded     = _isGrounded;
            }
        }
        #endregion

        public override UpdateRegistration UpdateRegistration => base.UpdateRegistration | UpdateRegistration.Init;

        #region Global Members
        [PropertyOrder(1)]

        [Tooltip("Collider(s) used for detecting physics collisions")]
        [SerializeField] private Collider collider = null;

        [Tooltip("Collider used for detecting triggers")]
        [SerializeField] private Collider trigger = null;

        [Space(10f)]

        [Tooltip("Shadow transform reference")]
        [SerializeField, Enhanced, ShowIf(nameof(HasShadow))] private Transform shadow = null;

        [Tooltip("All colliders to be considered as feet on this object - can be left empty if not using multiple colliders")]
        [SerializeField, Enhanced] internal EnhancedCollection<Foot> feet = new EnhancedCollection<Foot>();

        // -----------------------

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f), PropertyOrder(3)]

        [Tooltip("Current coefficient applied on this object Velocity")]
        [SerializeField, Enhanced, ReadOnly] protected float velocityCoef = 1f;

        [Tooltip("Current speed of this object")]
        [SerializeField, Enhanced, ReadOnly(nameof(CanEditSpeed))] protected float speed = 1f;

        [Space(10f), PropertyOrder(5)]

        [Tooltip("System used to perform various physics operations (collision, overlap, extraction...)")]
        [SerializeField] private PhysicsSystem3DType physicsSystem = PhysicsSystem3DType.Intermediate;

        [Tooltip("Additional options used to define this object behaviour")]
        [SerializeField, Enhanced, ValidationMember(nameof(SetOption))] private MovableOption options = MovableOption.Move | MovableOption.AutoExtract | MovableOption.SlideOnSurfaces;

        [Space(10f)]

        [Tooltip("All objects with one of these tags will be treated as exceptions for sliding surfaces - slide on if disabled, or don't if enabled")]
        [SerializeField] private TagGroup slideSurfaceExceptions = new TagGroup();

        [Space(10f), PropertyOrder(10)]

        [Tooltip("If true, ignores and resets this object velocity every frame")]
        [SerializeField] private bool isAsleep = false;

        [Tooltip("Sends a log about this object hit Colliders every frame")]
        [SerializeField] private bool debugCollisions = false;

        [Tooltip("Sends a log about this object Frame Velocity every frame")]
        [SerializeField] private bool debugVelocity = false;

        [Space(10f)]

        /// <summary>
        /// Global velocity of this object.
        /// </summary>
        public Velocity Velocity = new Velocity();

        [SerializeField, Enhanced, ReadOnly] protected PhysicsSurface3D.Settings physicsSurface = PhysicsSurface3D.Settings.Default;

        [Space(10f)]

        [Tooltip("Specified axies will not be affected by velocity")]
        [SerializeField] private AxisConstraints moveFreezeAxis = AxisConstraints.None;

        [Tooltip("Axies not affected by collision surface sliding")]
        [SerializeField] private AxisConstraints slidingFreezeAxis = AxisConstraints.None;

        // -----------------------

        [Space(10f, order = 0),         HorizontalLine(SuperColor.Grey, 1f, order = 1), Space(10f, order = 2), PropertyOrder(20)]
        [Title("Gravity", order = 4),   Space(5f, order = 5)]

        [Tooltip("Applies gravity on this object, every frame")]
        [SerializeField] private bool useGravity = true;

        [Tooltip("Mode used to apply gravity on this object")]
        [SerializeField, Enhanced, DisplayName("Mode")] private GravityMode gravityMode = GravityMode.World;

        [Space(5f)]

        [Tooltip("Upwards reference direction of this object in absolute world coordinates, used to apply gravity in the opposite direction - different than Transform.up")]
        [SerializeField, Enhanced, DisplayName("Up Dir.")] private Vector3 upDirection = Vector3.up;

        [Tooltip("Coefficient applied to this object gravity")]
        [SerializeField, Enhanced, DisplayName("Coef")] private float gravityFactor = 1f;

        // -----------------------

        [Space(20f, order = 0), Title("Ground", order = 1), Space(5f, order = 2)]

        [Tooltip("Is this object currently on a ground surface?")]
        [SerializeField, Enhanced, ReadOnly(true)] private bool isGrounded = false;

        [Tooltip("Normal of this object current ground surface")]
        [SerializeField, Enhanced, ReadOnly] private Vector3 groundNormal = Vector3.up;

        [Space(10f)]

        [SerializeField, Enhanced, ReadOnly(nameof(CanEditSettings)), Block] protected Movable3DGroundSettings groundSettings = new Movable3DGroundSettings();

        [Space(20f, order = 0), Title("Weight", order = 1), Space(5f, order = 2), PropertyOrder(22)]

        [SerializeField, Enhanced, ReadOnly(nameof(CanEditSettings)), Block] protected Movable3DWeightSettings weightSettings = new Movable3DWeightSettings();

        // -----------------------

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f), PropertyOrder(50)]

        [Tooltip("Frame displacement Velocity at the last frame")]
        [SerializeField, Enhanced, ReadOnly] private FrameVelocity lastFrameVelocity  = new FrameVelocity();

        [Space(5f)]

        [Tooltip("Total velocity frame displacement applied on this object during the last frame")]
        [SerializeField, Enhanced, ReadOnly] private Vector3 lastFrameAppliedVelocity = Vector3.zero;

        [Tooltip("Last recorded position of this object")]
        [SerializeField, Enhanced, ReadOnly] private Vector3 lastPosition = new Vector3();

        [Tooltip("Current mode of this object ground detection related feet mode")]
        [SerializeField, Enhanced, ReadOnly, ShowIf(nameof(HasMultipleColliders))] internal int groundFeetMode = 0;

        // -----------------------

        [SerializeField, HideInInspector] protected new Rigidbody rigidbody = null;
        [SerializeField, HideInInspector] protected new Transform transform = null;
        [SerializeField, HideInInspector] protected List<Collider> selfColliders = new List<Collider>();

        private bool forceRefreshGravity = false;
        private bool shouldBeRefreshed   = false;

        // -------------------------------------------
        // Properties
        // -------------------------------------------

        /// <summary>
        /// Collider used for detecting physics collisions.
        /// </summary>
        public PhysicsCollider3D PhysicsCollider {
            get { return PhysicsCollider3D.GetTemp(collider, colliderMask); }
        }

        /// <summary>
        /// Collider used for detecting triggers.
        /// </summary>
        public PhysicsCollider3D PhysicsTrigger {
            get { return PhysicsCollider3D.GetTemp(trigger, triggerMask); }
        }

        /// <summary>
        /// This object main <see cref="UnityEngine.Collider"/> used to detect physics collisions.
        /// </summary>
        public Collider Collider {
            get { return collider; }
        }

        /// <summary>
        /// This object <see cref="UnityEngine.Collider"/> used to detect triggers.
        /// </summary>
        public Collider Trigger {
            get { return trigger; }
        }

        /// <summary>
        /// All <see cref="UnityEngine.Collider"/> of this object.
        /// </summary>
        public List<Collider> SelfColliders {
            get { return selfColliders; }
        }

        /// <summary>
        /// Current <see cref="PhysicsSurface3DSettings"/> affecting this object.
        /// </summary>
        public PhysicsSurface3D.Settings PhysicsSurface {
            get { return physicsSurface; }
        }

        /// <summary>
        /// The current position of this object.
        /// </summary>
        public Vector3 Position {
            get { return rigidbody.position; }
            set { SetPosition(value); }
        }

        /// <summary>
        /// The current rotation of this object.
        /// </summary>
        public Quaternion Rotation {
            get { return rigidbody.rotation; }
            set { SetRotation(value); }
        }

        /// <summary>
        /// <see cref="PhysicsSystem3DType"/> used to calculate physics operations and how this object moves and collides with other objects in space.
        /// </summary>
        public PhysicsSystem3DType PhysicsSystem {
            get { return physicsSystem; }
        }

        /// <summary>
        /// Indicates if this object has more than one collider used for physics operations.
        /// </summary>
        public bool HasMultipleColliders {
            get { return (int)physicsSystem > 20; }
        }

        /// <summary>
        /// Indicates if this object has more than one foot.
        /// </summary>
        public bool HasFeet {
            get { return HasMultipleColliders && (feet.Count > 1); }
        }

        /// <summary>
        /// Current speed of this object.
        /// </summary>
        public float Speed {
            get { return speed; }
        }

        /// <summary>
        /// Is this object currently on a ground surface?
        /// </summary>
        public bool IsGrounded {
            get { return isGrounded; }
        }

        /// <summary>
        /// Is this object currently asleep and ignoring collisions?
        /// </summary>
        public bool IsAsleep {
            get { return isAsleep; }
        }

        /// <summary>
        /// Mode used to apply gravity on this object.
        /// </summary>
        public GravityMode GravityMode {
            get { return gravityMode; }
            set {
                gravityMode = value;
                this.LogMessage($"New GravityMode assigned: {value.ToString().Bold()}");
            }
        }

        /// <summary>
        /// Direction in which to apply gravity on this object, in absolute world coordinates.
        /// </summary>
        public Vector3 GravitySense {
            get { return -upDirection; }
        }

        /// <summary>
        /// Normal on this object current ground surface.
        /// </summary>
        public Vector3 GroundNormal {
            get { return groundNormal; }
        }

        /// <summary>
        /// Reference upwards direction of this object - related to <see cref="GravitySense"/>, not <see cref="Transform.up"/>.
        /// </summary>
        public Vector3 UpDirection {
            get { return upDirection; }
        }

        /// <summary>
        /// This object direction rotation, using its <see cref="UpDirection"/> on the Y axis with a cross product of <see cref="Transform.forward"/> on the others.
        /// </summary>
        public Quaternion DirectionRotation {
            get { return Quaternion.FromToRotation(Vector3.forward, Vector3.ProjectOnPlane(Transform.forward, upDirection)).normalized; }
        }

        /// <summary>
        /// Indicates if this object exercise a control on its shadow or not.
        /// </summary>
        public bool HasShadow {
            get {
                if (Application.isPlaying) {
                    return shadowRotation || shadowHeight;
                }

                return HasOption(MovableOption.ShadowRotation) || HasOption(MovableOption.ShadowHeight);
            }
        }

        /// <summary>
        /// If true, applies gravity on this object every frame.
        /// </summary>
        public bool UseGravity {
            get { return useGravity; }
            set { useGravity = value; }
        }

        /// <summary>
        /// Coefficient applied to this object gravity.
        /// </summary>
        public float GravityFactor {
            get { return gravityFactor; }
            set { gravityFactor = value; }
        }

        /// <summary>
        /// Specified axies will not be affected by this object velocity (axis is in local space - relative the object rotation).
        /// </summary>
        public AxisConstraints FreezeAxis {
            get { return moveFreezeAxis; }
        }

        /// <summary>
        /// Frame displacement velocity during the last frame.
        /// </summary>
        public FrameVelocity LastFrameVelocity {
            get { return lastFrameVelocity; }
        }

        /// <summary>
        /// Total velocity frame displacement applied on this object during the last frame.
        /// </summary>
        public Vector3 LastFrameAppliedVelocity {
            get { return lastFrameAppliedVelocity; }
        }

        // -----------------------

        /// <summary>
        /// Ground-related settings of this object.
        /// </summary>
        public virtual Movable3DGroundSettings GroundSettings {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return groundSettings; }
        }

        /// <summary>
        /// Weight-related settings of this object.
        /// </summary>
        public virtual Movable3DWeightSettings WeightSettings {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return weightSettings; }
        }

        /// <summary>
        /// Whether this object speed value is editable in the inspector or not.
        /// </summary>
        public virtual bool CanEditSpeed {
            get { return true; }
        }

        /// <summary>
        /// Whether this object special settings (ground, weight, etc.) values are editable in the inspector or not.
        /// </summary>
        public virtual bool CanEditSettings {
            get { return true; }
        }

        /// <summary>
        /// Weight of this object - used to determine how other objects interact with it.
        /// </summary>
        public float Weight {
            get { return WeightSettings.Weight; }
        }

        /// <summary>
        /// Maximum height used to climb steps and surfaces (Creature collisions only).
        /// </summary>
        public virtual float ClimbHeight {
            get { return Physics3DSettings.I.ClimbHeight; }
        }

        /// <summary>
        /// Maximum height used for snapping to the nearest surface (Creature collisions only).
        /// </summary>
        public virtual float SnapHeight {
            get { return Physics3DSettings.I.SnapHeight; }
        }

        // -----------------------

        public Rigidbody Rigidbody {
            get { return rigidbody; }
        }

        public override Transform Transform {
            get { return transform; }
        }
        #endregion

        #region Enhanced Behaviour
        protected override void OnBehaviourEnabled() {
            base.OnBehaviourEnabled();

            // Registration.
            Movable3DManager.RegisterMovable(this);

            // Enable colliders.
            EnableColliders(true);
        }

        protected override void OnInit() {
            base.OnInit();

            // Initialization.
            InitCollisionMasks();
            RefreshOption();

            rigidbody.isKinematic = true;
        }

        protected override void OnBehaviourDisabled() {
            base.OnBehaviourDisabled();

            // Unregistration.
            Movable3DManager.UnregisterMovable(this);

            // Clear state.
            ExitTriggers();
            ResetParentState();

            // Disable colliders.
            EnableColliders(false);
        }

        #if UNITY_EDITOR
        // -------------------------------------------
        // Editor
        // -------------------------------------------

        protected override void OnValidate() {
            base.OnValidate();

            // Editor required components validation.
            if (Application.isPlaying) {
                return;
            }

            if (!transform) {
                transform = GetComponent<Transform>();
            }

            if (!rigidbody) {
                rigidbody = GetComponent<Rigidbody>();
            }

            GetComponentsInChildren<Collider>(selfColliders);
        }

        private void OnDrawGizmos() {

            // Feet.
            for (int i = feet.Count; i-- > 0;) {
                Foot _foot = feet[i];

                using (var _scope = EnhancedGUI.GizmosColor.Scope(_foot.IsGrounded ? SuperColor.Green.Get() : SuperColor.Crimson.Get())) {
                    Gizmos.DrawCube(_foot.BottomPosition, .2f.ToVector3());
                }
            }

            // Ground hit and feet adjustment.
            using (var _scope = EnhancedGUI.GizmosColor.Scope(SuperColor.Lavender.Get())) {
                Gizmos.DrawSphere(groundHit.point, .2f);
            }

            using (var _scope = EnhancedGUI.GizmosColor.Scope(SuperColor.White.Get())) {
                Gizmos.DrawLine(groundHit.point, groundHit.point - groundHit.normal);
            }

            using (var _scope = EnhancedGUI.GizmosColor.Scope(SuperColor.Aquamarine.Get())) {
                Gizmos.DrawSphere(feetDebugCenter, .2f);
            }

            using (var _scope = EnhancedGUI.GizmosColor.Scope(SuperColor.Sapphire.Get())) {
                Gizmos.DrawLine(feetDebugCenter, feetDebugCenter + feetDebugOffset);
            }

            using (var _scope = EnhancedGUI.GizmosColor.Scope(SuperColor.SalmonPink.Get())) {
                Gizmos.DrawLine(Transform.position, Transform.position - (GravitySense * 2f));
            }

            // Ground feet mode.
            Color _modeColor;
            switch (groundFeetMode) {
                case -3:
                    _modeColor = SuperColor.Brown.Get();
                    break;

                case -2:
                    _modeColor = SuperColor.Crimson.Get();
                    break;

                case -1:
                    _modeColor = SuperColor.HarvestGold.Get();
                    break;

                case 0:
                    _modeColor = SuperColor.Yellow.Get();
                    break;

                case 1:
                    _modeColor = SuperColor.Green.Get();
                    break;

                case 2:
                    _modeColor = SuperColor.Aquamarine.Get();
                    break;

                case 3:
                    _modeColor = SuperColor.Sapphire.Get();
                    break;

                case 9:
                    _modeColor = SuperColor.Brown.Get();
                    break;

                default:
                    _modeColor = SuperColor.Lavender.Get();
                    break;
            }

            using (var _scope = EnhancedGUI.GizmosColor.Scope(_modeColor)) {
                Gizmos.DrawCube(Transform.position + (Transform.up * 1.5f), .1f.ToVector3());
            }
        }
        #endif
        #endregion

        #region Controller
        private IMovable3DComputationController computationController   = DefaultMovable3DController.Instance;
        private IMovable3DCollisionController   collisionController     = DefaultMovable3DController.Instance;
        private IMovable3DColliderController    colliderController      = DefaultMovable3DController.Instance;
        private IMovable3DVelocityController    velocityController      = DefaultMovable3DController.Instance;
        private IMovable3DTriggerController     triggerController       = DefaultMovable3DController.Instance;
        private IMovable3DUpdateController      updateController        = DefaultMovable3DController.Instance;

        // -----------------------

        /// <summary>
        /// Registers a controller for this object.
        /// </summary>
        /// <typeparam name="T">Object type to register.</typeparam>
        /// <param name="_object">Controller to register.</param>
        public virtual void RegisterController<T>(T _object) {
            if (_object is IMovable3DComputationController _computation) {
                computationController = _computation;
            }

            if (_object is IMovable3DCollisionController _collision) {
                collisionController = _collision;
            }

            if (_object is IMovable3DColliderController _collider) {
                colliderController = _collider;
            }

            if (_object is IMovable3DVelocityController _velocity) {
                velocityController = _velocity;
            }

            if (_object is IMovable3DTriggerController _trigger) {
                triggerController = _trigger;
            }

            if (_object is IMovable3DUpdateController _update) {
                updateController = _update;
            }
        }

        /// <summary>
        /// Unregisters a controller from this object.
        /// </summary>
        /// <typeparam name="T">Object type to unregister.</typeparam>
        /// <param name="_object">Controller to unregister.</param>
        public virtual void UnregisterController<T>(T _object) {
            if ((_object is IMovable3DComputationController _computation) && computationController.Equals(_computation)) {
                computationController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DCollisionController _collision) && collisionController.Equals(_collision)) {
                collisionController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DColliderController _collider) && colliderController.Equals(_collider)) {
                colliderController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DVelocityController _velocity) && velocityController.Equals(_velocity)) {
                velocityController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DTriggerController _trigger) && triggerController.Equals(_trigger)) {
                triggerController = DefaultMovable3DController.Instance;
            }

            if ((_object is IMovable3DUpdateController _update) && updateController.Equals(_update)) {
                updateController = DefaultMovable3DController.Instance;
            }
        }
        #endregion

        // --- Logic --- \\

        #region General
        private readonly CollisionOperationData3D collisionOperation = new CollisionOperationData3D();

        private bool preventRefreshThisFrame = false;
        private bool checkForOverlapStuck    = false;
        private bool isStuckThisFrame        = false;
        private int  resetVelocityAtFrame    = 0;

        // -------------------------------------------
        // Manual [Legacy]
        // -------------------------------------------

        /// <summary>
        /// Performs this object full manual update.
        /// </summary>
        internal void LogicManualUpdate(float _deltaTime, Physics3DSettings _settings) {

            // Avoid calculs when the game is paused - reset frame velocity to also avoid cumulating force and movement from the previous frames.
            if (Mathm.ApproximatelyZero(_deltaTime) || isAsleep) {

                Velocity.ResetFrameVelocity();
                return;
            }

            // Pre-update callback.
            OnPreUpdate(_deltaTime);

            // --------------------
            // ---  Main logic  ---
            // --------------------

            // Early.
            if (LogicEarlyUpdate(false, out bool _performCollisions, out ExtractOperation3D _)) {

                // Extract.
                ExtractPerformManual();
            }

            // Collisions.
            if (_performCollisions && LogicPrepareCollisions(_deltaTime, _settings, out CollisionOperationData3D _collisionOperation)) {

                PhysicsSystem.CollisionPerformManual(this, _collisionOperation);

                // Complete.
                if (LogicOnCollisionComplete(_collisionOperation, out _)) {

                    // Extract.
                    ExtractPerformManual();
                }

                // Late.
                LogicLateUpdate(_collisionOperation);
            }

            // --------------------
            // ---  Main logic  ---
            // --------------------

            // Post-update callback.
            OnPostUpdate(_deltaTime);
        }

        // -------------------------------------------
        // Logic
        // -------------------------------------------

        /// <summary>
        /// First logic update to be called.
        /// </summary>
        /// <returns>True to perform an extract operation, false otherwise.</returns>
        internal bool LogicEarlyUpdate(bool _isPaused, out bool _performCollisions, out ExtractOperation3D _extractOperation) {

            // Avoid calculs when the game is paused - reset frame velocity to also avoid cumulating force and movement from the previous frames.
            if (_isPaused || isAsleep) {

                Velocity.ResetFrameVelocity();

                _performCollisions = false;
                _extractOperation  = default;
                return false;
            }

            // Stand object by its parent.
            FollowParent();

            // Object moved in space since last frame - call SetPosition to update the object rigidbody position.
            //
            // Unity only update the rigidbody during FixedUpdate (or anyway, not every frame),
            // so it's very important to refresh it before anything else.
            Vector3 _position = transform.position;
            if (_position != lastPosition) {
                SetPosition(_position);
            }

            // Special feature - only refresh and extract this object if stuck onto another collider.
            preventRefreshThisFrame = ExtractOnlyIfOverlap || ExtractOnlyIfContact;
            isStuckThisFrame = false;

            _performCollisions = CanMove;

            // Perform an overlap before trying to move this object - if overlapping with something, do not perform any collision.
            if (SkipCollisionIfOverlap) {

                checkForOverlapStuck = true;

                _extractOperation = new ExtractOperation3D(this);
                return true;

            } else if (shouldBeRefreshed && CanExtractFromOverlaps()) {

                // Object position or rotation changed, so it requires to be refreshed and verified.
                //
                // For instance, an object could have been teleported and now overlapping with another object,
                // so we would need to extract it before applying velocity.

                _extractOperation = new ExtractOperation3D(this);
                return true;
            }

            _extractOperation = default;
            return false;
        }

        /// <summary>
        /// Main collisions logic update.
        /// </summary>
        /// <returns>True to perform a collision operation, false otherwise.</returns>
        internal bool LogicPrepareCollisions(float _deltaTime, Physics3DSettings _settings, out CollisionOperationData3D _collisionOperation) {
            _deltaTime *= chronos;

            // Apply gravity (only if not grounded, for optimization purpose).
            if (useGravity && (!isGrounded || forceRefreshGravity)) {
                ApplyGravity(_settings, _deltaTime);
                forceRefreshGravity = false;
            }

            // Compute velocity - lerps force and movement to smooth opposite directions.
            OnPreComputeVelocity (_deltaTime, out float _speed);
            ComputeVelocity      (_settings, _speed, _deltaTime, out FrameVelocity _velocity);
            OnPostComputeVelocity(_deltaTime, ref _velocity);

            // Perform collisions:
            //
            // • CollisionData is a static shared class instance used to store collision informations and results.
            // • Perform collisions using this object velocity and ignore self colliders.
            Quaternion _rotation = Rotation;
            Vector3    _position = Position;

            checkForOverlapStuck = false;

            bool _performCollisions = !isStuckThisFrame && _velocity.IsValid;

            // Only reset velocity the second frame in a row where the object has no velocity.
            if (!_velocity.IsValid && !Mathm.ApproximatelyZero(_deltaTime)) {

                int _frame = Time.frameCount;
                if (_frame == resetVelocityAtFrame) {
                    ResetVelocity(false);
                } else {
                    resetVelocityAtFrame = _frame + 1;
                }
            }

            // Operation.
            _collisionOperation = collisionOperation;
            _collisionOperation.Init(_settings, this, _position, _rotation, _velocity, _performCollisions, selfColliders);

            return true;
        }

        /// <summary>
        /// Post-collision logic update.
        /// </summary>
        /// <returns>True to perform an extract operation, false otherwise.</returns>
        internal bool LogicOnCollisionComplete(CollisionOperationData3D _operation, out ExtractOperation3D _extractOperation) {

            // Call SetPosition and SetRotation to update the object Transform and set "shouldBeRefreshed" to True.
            if (_operation.Data.UpdatePosition) {
                SetPosition(Position);
            }

            if (_operation.Data.UpdateRotation) {
                SetRotation(Rotation);
            }

            // Reset frame-dependant velocity (movement and instant velocity).
            Velocity.ResetFrameVelocity();

            // Force extract only if stuck into something.
            if (isStuckThisFrame || (preventRefreshThisFrame && _operation.Data.HitBuffer.SafeFirst(out CollisionHit3D _hit)
                                                             && (ExtractOnlyIfContact || (ExtractOnlyIfOverlap && (_hit.Distance == 0f))))) {

                preventRefreshThisFrame = false;
                shouldBeRefreshed       = true;
            }

            // Callback.
            OnAppliedVelocity(_operation);

            // Refresh this object position after moving it to avoid any overlap and perform additional operations.
            if ((shouldBeRefreshed || RefreshContinuously) && CanExtractFromOverlaps()) {

                _extractOperation = new ExtractOperation3D(this);
                return true;
            }

            _extractOperation = default;
            return false;
        }

        /// <summary>
        /// Last logic update to be called.
        /// </summary>
        internal void LogicLateUpdate(CollisionOperationData3D _operation) {

            _operation.UpdateAppliedData(Position, Rotation);
            OnRefreshedObject(_operation);

            // Cache this frame velocity.
            lastFrameAppliedVelocity = _operation.Data.AppliedVelocity; // Value might have changed during OnRefreshObject callback.
            lastFrameVelocity        = _operation.Velocity;

            #if DEVELOPMENT
            // --- Debug --- \\

            if (debugVelocity) {

                FrameVelocity _frameVelocity = lastFrameVelocity;
                this.LogWarning($"{name.Bold()} Velocity {UnicodeEmoji.RightTriangle.Get()} " +
                                $"M{_frameVelocity.Movement} | F{_frameVelocity.Force} | I{_frameVelocity.Instant} | Final{lastFrameAppliedVelocity}");
            }
            #endif
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        /// <summary>
        /// Pre-update callback.
        /// </summary>
        internal virtual void OnPreUpdate(float _deltaTime) {
            updateController.OnPreUpdate();
        }

        /// <summary>
        /// Post-update callback.
        /// </summary>
        internal virtual void OnPostUpdate(float _deltaTime) {
            updateController.OnPostUpdate();
        }
        #endregion

        #region Collision
        // -------------------------------------------
        // Manual
        // -------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CollisionPerformManual(CollisionOperationData3D _operation) {
            PhysicsSystem.CollisionPerformManual(this, _operation);
        }

        // -------------------------------------------
        // Async
        // -------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CollisionInitOperation        (CollisionOperationData3D _operation) {
            PhysicsSystem.CollisionInitOperation(this, _operation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool CollisionPerformOperation     (CollisionOperationData3D _operation, CastOperationCommands3D _commands) {
            return PhysicsSystem.CollisionPerformOperation(this, _operation, _commands);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool CollisionComputeOperation     (CollisionOperationData3D _operation) {
            return PhysicsSystem.CollisionComputeOperation(this, _operation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CollisionFinalizeOperation    (CollisionOperationData3D _operation) {
            PhysicsSystem.CollisionFinalizeOperation(this, _operation);
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CollisionOnOperationResults(Collider _collider, RaycastHit[] _results, int _startIndex, int _count) {
            PhysicsSystem.CollisionOnOperationResults(this, collisionOperation, _collider, _results, _startIndex, _count);
        }
        #endregion

        #region Extract
        // -------------------------------------------
        // Manual
        // -------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExtractPerformManual() {
            PhysicsSystem.ExtractPerformManual(this, selfColliders);
            OnPositionRefreshed();
        }

        // -------------------------------------------
        // Async
        // -------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ExtractInitOperation(OverlapOperationCommands3D _commands) {
            PhysicsSystem.OverlapInitOperation(this, _commands);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ExtractFinalizeOperation() {
            OnPositionRefreshed();
        }

        // -------------------------------------------
        // Callback(s)
        // -------------------------------------------

        #if OVERLAP_COMMANDS
        internal void ExtractOnOperationResults(Collider _collider, ColliderHit[] _results, int _startIndex, int _count) {

            for (int i = 0; i < _count; i++) {
                ColliderHit _hit = _results[_startIndex + i];

                // Reach result limit.
                if (_hit.instanceID == 0)
                    break;

                // Extract while authorized.
                if (!ExtractFromCollider(_collider, _hit.collider, true))
                    break;
            }
        }
        #endif

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        private bool CanExtractFromOverlaps() {
            return !preventRefreshThisFrame && AutoExtract;
        }
        #endregion

        // --- Velocity --- \\

        #region Coefficient
        private readonly PairCollection<int, float> velocityCoefBuffer = new PairCollection<int, float>();

        /// <summary>
        /// Total count of velocity coefficients currently applied.
        /// </summary>
        public int VelocityCoefCount {
            get { return velocityCoefBuffer.Count; }
        }

        // -----------------------

        /// <summary>
        /// Applies a coefficient to this object velocity.
        /// <param name="_id">Unique id associated with this coefficient - use the same id to pop it.</param>
        /// <param name="_coef">Coefficient to apply.</param>
        /// </summary>
        public void PushVelocityCoef(int _id, float _coef) {
            velocityCoefBuffer.Set(_id, _coef);
            RefreshVelocityCoef();
        }

        /// <summary>
        /// Removes a coefficient from this object velocity.
        /// </summary>
        /// <param name="_id">Id of the coefficient to remove - the same used to push it.</param>
        public void PopVelocityCoef(int _id) {
            if (!velocityCoefBuffer.Remove(_id)) {
                this.LogWarningMessage($"Trying to remove an invalid velocity coefficient id ({_id})");
                return;
            }

            RefreshVelocityCoef();
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        /// <summary>
        /// Refreshes this object velocity coefficient.
        /// </summary>
        private void RefreshVelocityCoef() {
            float _value = 1f;

            for (int i = velocityCoefBuffer.Count; i-- > 0;) {
                _value *= velocityCoefBuffer[i].Second;
            }

            velocityCoef = _value;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Get the applied velocity coef at a given index.
        /// <para/> Use <see cref="VelocityCoefCount"/> to get the amount of currently applied coefficients.
        /// </summary>
        /// <param name="_index">Index to get the coef at.</param>
        /// <returns>The velocity coef at the given index.</returns>
        public float GetVelocityCoefAt(int _index) {
            return velocityCoefBuffer[_index].Second;
        }

        /// <summary>
        /// Get the applied velocity coef associated with a given id.
        /// </summary>
        /// <param name="_id">Id of the coefficient to get.</param>
        /// <returns>The velocity coef associated with the given id.</returns>
        public float GetVelocityCoefFor(int _id) {
            return velocityCoefBuffer.TryGetValue(_id, out float _coef) ? _coef : 1f;
        }
        #endregion

        #region Collider
        private int colliderMask = -1;
        private int triggerMask  = -1;

        // -----------------------

        /// <summary>
        /// Get the default collision mask used for this object physics collisions.
        /// </summary>
        public int GetColliderMask() {
            return colliderMask;
        }

        /// <summary>
        /// Get the default collision mask used for this object trigger collisions.
        /// </summary>
        public int GetTriggerMask() {
            return triggerMask;
        }

        /// <summary>
        /// Overrides this object physics collision mask.
        /// </summary>
        /// <param name="_mask">New collision mask value.</param>
        public void SetColliderMask(int _mask) {
            colliderMask = _mask;
        }

        /// <summary>
        /// Overrides this object trigger collision mask.
        /// </summary>
        /// <param name="_mask">New collision mask value.</param>
        public void SetTriggerMask(int _mask) {
            triggerMask = _mask;
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        private void InitCollisionMasks() {

            // Collider.
            Collider _collider = collider;
            SetColliderMask(GetMask(_collider, colliderController.InitColliderMask(_collider)));

            // Trigger.
            Collider _trigger = trigger;
            SetTriggerMask (GetMask(_trigger,  colliderController.InitTriggerMask(_trigger)));

            // ----- Local Method ----- \\

            static int GetMask(Collider _collider, int _controllerMask) {

                if (_controllerMask == -1) {
                    _controllerMask = Physics3DUtility.GetLayerCollisionMask(_collider.gameObject);
                }

                return _controllerMask;
            }
        }
        #endregion

        #region Velocity
        /// <summary>
        /// Adds a relative movement velocity to this object:
        /// <para/>
        /// Velocity of the object itself, in local coordinates.
        /// <para/> In unit/second.
        /// </summary>
        public void AddRelativeMovementVelocity(Vector3 _movement) {
            AddMovementVelocity(GetWorldVector(_movement));
        }

        /// <summary>
        /// Adds a movement velocity to this object:
        /// <para/> <inheritdoc cref="Velocity.Movement" path="/summary"/>
        /// </summary>
        public void AddMovementVelocity(Vector3 _movement) {
            if (!CanAddVelocity())
                return;

            Velocity.Movement += _movement;
        }

        /// <summary>
        /// Adds an instant movement velocity to this object:
        /// <para/> <inheritdoc cref="Velocity.InstantMovement" path="/summary"/>
        /// </summary>
        public void AddInstantMovementVelocity(Vector3 _movement) {
            if (!CanAddVelocity())
                return;

            Velocity.InstantMovement += _movement;
        }

        /// <summary>
        /// Adds a force velocity to this object:
        /// <para/> <inheritdoc cref="Velocity.Force" path="/summary"/>
        /// </summary>
        public void AddForceVelocity(Vector3 _force) {
            if (!CanAddVelocity())
                return;

            Velocity.Force += _force;
        }

        /// <summary>
        /// Adds an instant force velocity to this object:
        /// <para/> <inheritdoc cref="Velocity.Instant" path="/summary"/>
        /// </summary>
        public void AddInstantVelocity(Vector3 _velocity) {
            if (!CanAddVelocity())
                return;

            Velocity.Instant += _velocity;
        }

        /// <summary>
        /// Adds an velocity over time to this object.
        /// <para/> <inheritdoc cref="Velocity.VelocityOverTime" path="/summary"/>
        /// </summary>
        public void AddVelocityOverTime(VelocityOverTime _velocity) {
            if (!CanAddVelocity())
                return;

            Velocity.VelocityOverTime.Add(_velocity);
        }

        // -------------------------------------------
        // Move
        // -------------------------------------------

        /// <summary>
        /// Makes this object move in a given direction.
        /// </summary>
        /// <param name="_direction">Direction in which to move this object.</param>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        public virtual bool Move(Vector3 _direction) {
            if (velocityController.Move(_direction)) {
                return true;
            }

            AddMovementVelocity(_direction);
            return false;
        }

        /// <summary>
        /// Makes this object move towards a given point.
        /// </summary>
        /// <param name="_targetPosition">Position the object moves towards.</param>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        public void MoveTowards(Vector3 _targetPosition) {
            Vector3 _direction = _targetPosition - Transform.position;
            float _speed = Speed;

            if (_speed != 0f) {
                _direction = Vector3.ClampMagnitude(_direction, _speed * DeltaTime);
                AddInstantMovementVelocity(_direction);

                return;
            }

            Move(_direction);
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Completely resets this object velocity back to zero.
        /// <br/> Does not reset its coefficient.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        public virtual bool ResetVelocity(bool _force = false) {
            if (velocityController.OnResetVelocity(_force)) {
                return true;
            }

            Velocity.Reset();
            return false;
        }

        /// <summary>
        /// Resets this object velocity coefficient back to 1, and clear its buffer.
        /// </summary>
        public void ResetVelocityCoef() {
            velocityCoefBuffer.Clear();
            RefreshVelocityCoef();
        }

        /// <summary>
        /// Can velocity be applied on this object?
        /// </summary>
        public bool CanAddVelocity() {
            return !isAsleep;
        }
        #endregion

        #region Gravity
        /// <summary>
        /// Applies the gravity on this object.
        /// <para/>
        /// Override this to use a specific gravity.
        /// <br/> Use <see cref="AddGravity"/> for a quick implementation.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool ApplyGravity(Physics3DSettings _settings, float _deltaTime) {
            if (velocityController.OnApplyGravity()) {
                return true;
            }

            AddGravity(_settings, _deltaTime);
            return false;
        }

        /// <summary>
        /// Adds gravity as a force velocity on this object.
        /// <br/> Uses the game global gravity (see <see cref="Physics3DSettings.Gravity"/>).
        /// </summary>
        /// <param name="_gravityCoef">Coefficient applied to the gravity.</param>
        /// <param name="_maxGravityCoef">Coefficient applied to the maximum allowed gravity value.</param>
        public void AddGravity(Physics3DSettings _settings, float _deltaTime, float _gravityCoef = 1f, float _maxGravityCoef = 1f) {

            float _maxGravity    = _settings.MaxGravity * _maxGravityCoef;
            float _gravity       = GetDirectionRelativeVector(Velocity.Force).y;

            if (_gravity > _maxGravity) {
                _gravity = Mathf.Max(_settings.Gravity * _deltaTime * _gravityCoef * gravityFactor, _maxGravity - _gravity);
                AddForceVelocity(GravitySense * -_gravity);
            }
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Refreshes this object ground state by forcing to apply gravity.
        /// </summary>
        public void RefreshGravity() {
            forceRefreshGravity = true;
        }
        #endregion

        #region Parenting
        private Transform parent = null;
        private bool isParented  = false;

        private Vector3 previousParentPosition    = new Vector3();
        private Quaternion previousParentRotation = Quaternion.identity;

        // -----------------------

        /// <summary>
        /// Parents this movable to a specific <see cref="Transform"/>.
        /// </summary>
        [Button(ActivationMode.Play, SuperColor.Lavender)]
        public void Parent(Transform _parent) {
            parent     = _parent;
            isParented = true;

            ResetParentState();
        }

        /// <summary>
        /// Unparents this object from any <see cref="Transform"/>.
        /// </summary>
        [Button(ActivationMode.Play, SuperColor.SalmonPink)]
        public void Unparent() {
            parent     = null;
            isParented = false;
        }

        /// <summary>
        /// Resets this movable parent relative state.
        /// </summary>
        public void ResetParentState() {
            if (!GetParent(out Transform _parent))
                return;

            previousParentPosition = _parent.position;
            previousParentRotation = _parent.rotation;
        }

        /// <summary>
        /// Get this object current parent <see cref="UnityEngine.Transform"/>.
        /// </summary>
        /// <param name="_parent">This object current parent <see cref="UnityEngine.Transform"/> (null if none).</param>
        /// <returns>True if this object is currently attached to a parent, false otherwise.</returns>
        public bool GetParent(out Transform _parent) {
            _parent = parent;
            return isParented;
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        /// <summary>
        /// Makes this object follow its reference parent <see cref="UnityEngine.Transform"/>.
        /// </summary>
        private void FollowParent() {
            if (!GetParent(out Transform _parent))
                return;

            Vector3 _parentPos    = _parent.position;
            Quaternion _parentRot = _parent.rotation;

            Vector3 _positionDifference    = _parentPos - previousParentPosition;
            Quaternion _rotationDifference = _parentRot * Quaternion.Inverse(previousParentRotation);

            previousParentPosition = _parentPos;
            previousParentRotation = _parentRot;

            Vector3 _newPosition = Position + _positionDifference;
            Vector3 _difference  = _newPosition - _parentPos;

            _newPosition = _parentPos + (_rotationDifference * _difference);
            Quaternion _newRotation = Rotation * _rotationDifference;

            SetPositionAndRotation(_newPosition, _newRotation);
        }
        #endregion

        // --- Physics --- \\

        #region Computation
        /// <summary>
        /// Called before computing this object frame velocity.
        /// <br/> Use this to perform additional operations, like incrementing the object speed.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnPreComputeVelocity(float _deltaTime, out float _speed) {
            if (computationController.OnPreComputeVelocity(_deltaTime, out _speed)) {
                return true;
            }

            // No movement when upside-down.
            const float MinDot = .1f;

            if (Vector3.Dot(Transform.up, UpDirection) < MinDot) {
                Velocity.Movement = Vector3.zero;
            }

            // Speed.
            _speed = ComputeSpeed(ref speed, _deltaTime);
            return false;
        }

        /// <summary>
        /// Computes this object velocity just before its collision calculs.
        /// </summary>
        /// <param name="_frameVelocity"><see cref="Physics3DSettings"/> of the project.</param>
        /// <param name="_deltaTime">This frame object-related delta time.</param>
        /// <param name="_frameVelocity">Velocity to apply this frame.</param>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool ComputeVelocity(Physics3DSettings _settings, float _speed, float _deltaTime, out FrameVelocity _frameVelocity) {

            Velocity _velocity = this.Velocity;
            _frameVelocity = new FrameVelocity();

            if (computationController.OnComputeVelocity(_velocity, _speed, _deltaTime, ref _frameVelocity)) {
                return true;
            }

            // Base computations.
            _velocity.ComputeVelocity(_deltaTime);

            // Get the movement and force velocity relative to this object local space.
            // Prefere caching the transform rotation value for optimization.
            Quaternion _directionRotation = DirectionRotation;

            Vector3 _movement = GetRelativeVector(_velocity.Movement, _directionRotation);
            Vector3 _force    = GetRelativeVector(_velocity.Force   , _directionRotation);

            // Add instant movement.
            if ((_deltaTime != 0f) && (_speed != 0f)) {
                Vector3 _instantMovement = GetRelativeVector(_velocity.InstantMovement, _directionRotation);
                _movement += (_instantMovement.Flat() / (_deltaTime * _speed)).SetY(_instantMovement.y);
            }

            // If movement and force have opposite vertical velocity, accordingly reduce them.
            if (Mathm.HaveDifferentSignAndNotNull(_movement.y, _force.y)) {
                float _absMovement = Mathf.Abs(_movement.y);

                _movement.y = Mathf.MoveTowards(_movement.y, 0f, Mathf.Abs(_force.y));
                _force.y    = Mathf.MoveTowards(_force.y,    0f, _absMovement);
            }

            // Compute movement and force flat velocity.
            Vector3 _flatMovement = _movement.Flat() * _speed;
            Vector3 _flatForce    = _force   .Flat();

            if (!_flatMovement.IsNull() && !_flatForce.IsNull()) {
                _movement = Vector3.MoveTowards(_flatMovement, _flatMovement.PerpendicularSurface(_flatForce   ), _flatForce   .magnitude * _deltaTime).SetY(_movement.y);
                _force    = Vector3.MoveTowards(_flatForce,    _flatForce   .PerpendicularSurface(_flatMovement), _flatMovement.magnitude * _deltaTime).SetY(_force   .y);
            } else {
                _movement = _flatMovement.SetY(_movement.y);
            }

            // When movement is added to the opposite force direction, the resulting velocity is the addition of both.
            // But when this opposite movement is stopped, we need to resume the velocity where it previously was.
            if (EqualizeVelocity) {

                Quaternion _lastDirection = lastFrameVelocity.DirectionRotation;
                Vector3 _previousMovement = GetRelativeVector(lastFrameVelocity.Movement, _lastDirection).SetY(0f);
                Vector3 _previousForce    = GetRelativeVector(lastFrameVelocity.Force   , _lastDirection).SetY(0f);

                if (_flatMovement.IsNull() && !_previousMovement.IsNull() && !_previousForce.IsNull()) {
                    _force = (_previousMovement + _previousForce) + (_force - _previousForce);
                }
            }

            // Get this frame velocity.
            float _veloctyDelta = _deltaTime * velocityCoef;
            _frameVelocity = new FrameVelocity() {

                Movement   = GetWorldVector(GetAxisVelocity(_movement, false), _directionRotation) * _veloctyDelta,
                Force      = GetWorldVector(GetAxisVelocity(_force,    false), _directionRotation) * _veloctyDelta,
                Instant    = _velocity.Instant,

                DeltaTime  = _veloctyDelta,
                DirectionRotation = _directionRotation,
            };

            // Reduce flat force velocity for the next frame.
            if (!_force.Flat().IsNull()) {
                float _forceDeceleration = isGrounded
                                         ? _settings.GroundForceDeceleration
                                         : _settings.AirForceDeceleration;

                _force = Vector3.MoveTowards(_force, new Vector3(0f, _force.y, 0f), _forceDeceleration * _deltaTime);
            }

            // Update velocity.
            _velocity.Force = GetWorldVector(_force, _directionRotation);
            return false;
        }

        /// <summary>
        /// Called after computing this object frame velocity.
        /// <br/> Use this to perform additional operations.
        /// </summary>
        /// <param name="_velocity">Velocity to apply this frame.</param>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnPostComputeVelocity(float _deltaTime, ref FrameVelocity _velocity) {
            return computationController.OnPostComputeVelocity(_deltaTime, ref _velocity);
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Computes this object speed for this frame.
        /// </summary>
        /// <param name="_speed"></param>
        /// <returns></returns>
        protected virtual float ComputeSpeed(ref float _speed, float _deltaTime) {
            return _speed;
        }

        /// <summary>
        /// Get the compute velocity with frozen axises.
        /// </summary>
        internal Vector3 GetAxisVelocity(Vector3 _velocity, bool _isWorldVelocity = true) {

            AxisConstraints _moveFreezeAxis = moveFreezeAxis;
            if (_moveFreezeAxis != AxisConstraints.None) {

                if (_isWorldVelocity) {
                    _velocity = _velocity.RotateInverse(Rotation);
                }

                _velocity.x = ClampAxis(_moveFreezeAxis, _velocity.x, AxisConstraints.X);
                _velocity.y = ClampAxis(_moveFreezeAxis, _velocity.y, AxisConstraints.Y);
                _velocity.z = ClampAxis(_moveFreezeAxis, _velocity.z, AxisConstraints.Z);

                if (_isWorldVelocity) {
                    _velocity = _velocity.Rotate(Rotation);
                }
            }

            return _velocity;

            // ----- Local Method ----- \\

            static float ClampAxis(AxisConstraints _moveFreezeAxis, float _value, AxisConstraints _axis) {
                return _moveFreezeAxis.HasFlagUnsafe(_axis) ? 0f : _value;
            }
        }
        #endregion

        #region Collision
        private const float DynamicGravityDetectionDistance = 15f;
        private RaycastHit groundHit = default;

        private Vector3 feetDebugOffset = Vector3.zero;
        private Vector3 feetDebugCenter = Vector3.zero;

        // -----------------------

        /// <summary>
        /// Called after velocity is applied on this object, but before extracting the object from overlapping collider(s).
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnAppliedVelocity(CollisionOperationData3D _operation) {
            if (collisionController.OnAppliedVelocity(_operation)) {
                return true;
            }

            // Rotation.
            if (!isGrounded) {
                AirRotation();
            } else if (!HasMultipleColliders) {
                GroundRotation();
            }

            #if DEVELOPMENT
            // --- Debug --- \\

            if (debugCollisions) {

                EnhancedCollection<CollisionHit3D> _hits = _operation.Data.HitBuffer;
                for (int i = 0; i < _hits.Count; i++) {
                    this.LogMessage("Hit Collider => " + _hits[i].HitCollider.name + " [" + i + "]", _hits[i].HitCollider);
                }
            }
            #endif

            return false;

            // ----- Local Methods ----- \\

            void GroundRotation() {

                Movable3DGroundSettings _settings = GroundSettings;

                // Ignore.
                float _speed = _settings.GroundOrientationSpeed;
                if (_speed == 0f)
                    return;

                // Get "Up" direction.
                Vector3 _up = Vector3.Lerp(upDirection, groundNormal, _settings.GroundOrientationFactor).normalized;

                Transform _transform = Transform;
                if (_transform.up == _up)
                    return;

                // Rotate.
                Quaternion _offset = Quaternion.FromToRotation(_transform.up, _up);
                RotateObject(_transform, _offset, _speed);
            }

            void AirRotation() {

                Movable3DGroundSettings _settings = GroundSettings;

                // Ignore.
                float _speed = _settings.AirRotationSpeed;
                if (_speed == 0f)
                    return;

                // Get target angle.
                Transform _transform  = Transform;
                Vector3 _lastVelocity = LastFrameAppliedVelocity;

                Vector3 _forward = _transform.forward;
                Vector3 _up      = upDirection;

                Vector3 _projectVelocity = Vector3.ProjectOnPlane(_lastVelocity, _up);

                // Only rotate if non-null forward velocity.
                if (_projectVelocity.z == 0f)
                    return;

                Vector3 _projectForward = Vector3.ProjectOnPlane(_forward, _up);
                float _sign = Vector3.Dot(_projectForward, _projectVelocity) * -Mathf.Sign(_lastVelocity.y);

                Vector2 _angles     = _settings.AirRotationAngles;
                float _maxAngle     = (_sign < 0f) ? _angles.x : _angles.y;
                float _currentAngle = Vector3.SignedAngle(_projectForward, _forward, _transform.right);

                // Rotate if rotation is not over target angle.
                if (Mathm.HaveDifferentSign(_currentAngle, _maxAngle) || (Mathf.Abs(_currentAngle) < Mathf.Abs(_maxAngle))) {

                    Quaternion _offset = Quaternion.AngleAxis(_maxAngle - _currentAngle, _transform.right);
                    RotateObject(_transform, _offset, _speed);
                }
            }

            void RotateObject(Transform _transform, Quaternion _offset, float _speed) {

                Quaternion _from = _transform.rotation;
                _offset = Mathm.GetOffsetRotation(_from, _offset);

                Quaternion _rotation = Quaternion.RotateTowards(_from, _from * _offset, _speed * _operation.Velocity.DeltaTime * 90f);
                SetRotation(_rotation);
            }
        }

        /// <summary>
        /// Called at the end of the update, after all velocity calculs and overlap operations have been performed.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnRefreshedObject(CollisionOperationData3D _operation) {
            if (collisionController.OnRefreshedObject(_operation)) {
                return true;
            }

            UpdateShadow(_operation);
            return false;
        }

        /// <summary>
        /// Called when this object extracts from a collider.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        internal protected virtual bool OnExtractFromCollider(Collider _colliderA, Collider _colliderB, Vector3 _direction, float _distance) {
            if (collisionController.OnExtractFromCollider(_colliderA, _colliderB, _direction, _distance)) {
                return true;
            }

            Vector3 _offset = _direction * _distance;

            // Apply collision effects.
            if (_colliderB.TryGetComponentInParent(out Movable3D _other) && _other.enabled) {

                // Rock behaviour means that the object should always stick to its position when interacting with other objects.
                // For instance, a tree should never be moved when overlapping with a leave - it's the leave that should be moved.
                if (!RockBehaviour) {

                    _offset += PushObject(_other, -_offset);

                } else if (!_other.RockBehaviour) {

                    _other.OffsetPosition(_offset);
                }
            }

            SetPosition(Position + _offset);

            #if DEVELOPMENT
            // --- Debug --- \\

            if (debugCollisions) {
                _colliderA.LogMessage("Extract from Collider => " + _colliderB.name + " - " + _offset.ToStringX(3) + " - " + RockBehaviour);
            }
            #endif

            return false;
        }

        /// <summary>
        /// Called whenever this object collides with another <see cref="Movable3D"/>.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        internal protected virtual bool OnHitByMovable(Movable3D _other, CollisionHit3D _collision) {
            return collisionController.OnHitByMovable(_other, _collision);
        }

        // -------------------------------------------
        // Ground
        // -------------------------------------------

        /// <summary>
        /// Set this object ground state, from collision results.
        /// </summary>
        /// <param name="_isGrounded">Is the object grounded at the end of the collisions.</param>
        /// <param name="_hit">Collision ground hit (default is not grounded).</param>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        internal protected virtual bool SetGroundState(CollisionOperationData3D _operation, bool _isGrounded, RaycastHit _hit) {
            if (collisionController.OnSetGroundState(ref _isGrounded, _hit)) {
                return true;
            }

            // Changed ground state callback.
            if (isGrounded != _isGrounded) {
                OnGrounded(_operation, _isGrounded);
            }

            bool _isDynamicGravity = gravityMode == GravityMode.Dynamic;
            Vector3 _groundNormal;

            // Only update normal when grounded (hit is set to default when not).
            if (isGrounded) {
                _groundNormal = _hit.normal;

                if (_isDynamicGravity) {
                    upDirection = _hit.normal;
                }
            } else if (_isDynamicGravity && PhysicsCollider.Cast(-upDirection, out _hit, DynamicGravityDetectionDistance, QueryTriggerInteraction.Ignore, true, selfColliders)
                                         && Physics3DUtility.IsGroundSurface(_hit, upDirection)) {

                // When using dynamic gravity, detect nearest ground and use it as reference surface.
                _groundNormal = _hit.normal;
                upDirection   = _groundNormal;

            } else {
                _groundNormal = upDirection;
            }

            // Update values.
            groundNormal = _groundNormal;
            groundHit    = _hit;

            // Physics surface.
            RefreshPhysicsSurface(_operation, _isGrounded, _hit);
            return false;
        }

        /// <summary>
        /// Called when this object ground state is changed.
        /// </summary>
        /// <returns><inheritdoc cref="Doc" path="/returns"/></returns>
        protected virtual bool OnGrounded(CollisionOperationData3D _operation, bool _isGrounded) {
            if (collisionController.OnGrounded(_isGrounded)) {
                return true;
            }

            isGrounded = _isGrounded;

            // Dampens force velocity when hitting ground.
            if (_isGrounded && !Velocity.Force.IsNull()) {

                Vector3 _force = GetDirectionRelativeVector(Velocity.Force).SetY(0f);
                _force        *= _operation.PhysicsSettings.OnGroundedForceMultiplier;

                Velocity.Force = GetDirectionWorldVector(_force);
            }

            return false;
        }

        /// <summary>
        /// Set special infos about this object feet-related ground state.
        /// </summary>
        internal void SetGroundFeetInfos(int _mode, Vector3 _origin, Vector3 _direction) {
            // Mode.
            groundFeetMode = _mode;

            // Infos.
            feetDebugCenter = _origin;
            feetDebugOffset = _direction;
        }

        // -------------------------------------------
        // Shadow
        // -------------------------------------------

        /// <summary>
        /// Updates this object shadow transform position and / or rotation.
        /// </summary>
        public void UpdateShadow(CollisionOperationData3D _operation) {
            if (!HasShadow) {
                return;
            }

            const float HeightOffset = .1f;
            RaycastHit _mainHit = _operation.Data.HitBuffer.SafeLast(out CollisionHit3D _hit) ? _hit.RaycastHit : new RaycastHit();

            // Manage height.
            if (shadowHeight) {

                const float ShadowCastMaxDistance = 20f;

                Transform _transform = Transform;
                Transform _shadow    = shadow;
                Vector3 _direction   = GravitySense;
                Vector3 _position    = _transform.position;

                // If not grounded, cast to get the closest ground surface.
                if (isGrounded) {
                    _mainHit = groundHit;

                } else if ((_mainHit.colliderInstanceID == 0) && !PhysicsCollider.Cast(_direction, out _mainHit, ShadowCastMaxDistance, QueryTriggerInteraction.Ignore, true, selfColliders)) {

                    _mainHit.point  = _position + new Vector3(0f, -100f, 0f);
                    _mainHit.normal = groundNormal;
                }

                _position.y     += (_mainHit.point.y - _position.y) + HeightOffset;
                _shadow.position = _position;

                // Rotation.
                if (shadowRotation) {

                    _shadow.forward = Vector3.ProjectOnPlane(_transform.forward, UpDirection).normalized;

                    Vector3 _normal = _mainHit.normal;
                    Vector3 _up   = _shadow.up;

                    Vector3 _axis = Vector3.Cross      (_normal, _up);
                    float _angle  = Vector3.SignedAngle(_up, _normal, _axis);

                    _shadow.RotateAround(_mainHit.point, _axis, _angle);
                }
            } else if (shadowRotation) {

                // Simple rotation.
                shadow.up = groundNormal;
            }
        }

        // -------------------------------------------
        // Impact
        // -------------------------------------------

        /// <summary>
        /// Computes a collision impact.
        /// </summary>
        internal Vector3 ComputeImpact(CollisionOperationData3D _operation, Vector3 _velocity, CollisionHit3D _hit) {
            RaycastHit _raycastHit = _hit.RaycastHit;
            Collider _hitCollider  = _raycastHit.collider;

            // Security.
            if (_operation.PhysicsSettings.CheckForNAN && _raycastHit.normal.IsAnyNaN()) {
                _raycastHit.normal = Vector3.one;
                this.LogErrorMessage("NaN detected => " + _raycastHit.collider.name + " - " + _raycastHit.distance);
            }

            // Slide on surfaces option.
            bool _slide = SlideOnSurfaces;
            if (_hitCollider.HasAnyTag(slideSurfaceExceptions, false)) {
                _slide = !_slide;
            }

            Vector3 _newVelocity = _velocity.PerpendicularSurface(_raycastHit.normal, _slide);

            // Clamp axis.
            AxisConstraints _freezeAxis = slidingFreezeAxis;
            {
                _newVelocity.x = ClampAxis(_freezeAxis, _newVelocity.x, _velocity.x, _hitCollider, AxisConstraints.X);
                _newVelocity.y = ClampAxis(_freezeAxis, _newVelocity.y, _velocity.y, _hitCollider, AxisConstraints.Y);
                _newVelocity.z = ClampAxis(_freezeAxis, _newVelocity.z, _velocity.z, _hitCollider, AxisConstraints.Z);
            }

            return GetAxisVelocity(_newVelocity);

            // ----- Local Method ----- \\

            static float ClampAxis(AxisConstraints _freezeAxis, float _value, float _velocity, Collider _hitCollider, AxisConstraints _axis) {
                if (_freezeAxis.HasFlagUnsafe(_axis) && (_value > 0f) && (_velocity <= 0f) && !Physics3DUtility.IsGroundSurface(_hitCollider)) {
                    _value = 0f;
                }

                return _value;
            }
        }
        #endregion

        #region Trigger
        private static readonly List<ITrigger> getTriggerComponentBuffer    = new List<ITrigger>();
        private static readonly List<ITrigger> triggerBuffer                = new List<ITrigger>();

        // All triggers currently overlapping with this object - use an EnhancedCollection to use reference equality comparer.
        protected readonly EnhancedCollection<ITrigger> overlappingTriggers = new EnhancedCollection<ITrigger>();

        /// <inheritdoc cref="ITriggerActor.Behaviour"/>
        EnhancedBehaviour ITriggerActor.Behaviour {
            get { return this; }
        }

        // -----------------------

        /// <summary>
        /// Refreshes this object trigger interaction.
        /// </summary>
        protected void RefreshTriggers() {

            // Overlapping triggers.
            int _amount = TriggerOverlap();

            List<ITrigger> _overlappingTriggers = overlappingTriggers.collection;
            List<ITrigger> _getComponentBuffer  = getTriggerComponentBuffer;
            List<ITrigger> _buffer = triggerBuffer;

            _buffer.Clear();

            for (int i = 0; i < _amount; i++) {
                Collider _overlap = GetOverlapAt(i);

                if (!_overlap.isTrigger) {
                    continue;
                }

                _getComponentBuffer.Clear();

                // If there is a LevelTrigger, ignore any other trigger.
                if (_overlap.TryGetComponent(out LevelTrigger _levelTrigger) && _levelTrigger.enabled) {

                    _getComponentBuffer.Add(_levelTrigger);

                } else {

                    _overlap.GetComponents(_getComponentBuffer);
                }

                // Activation.
                int _count = _getComponentBuffer.Count;
                for (int j = 0; j < _count; j++) {

                    ITrigger _trigger = _getComponentBuffer[j];
                    if ((_trigger is Behaviour _behaviour) && !_behaviour.enabled) {
                        continue;
                    }

                    _buffer.Add(_trigger);

                    // Trigger enter.
                    if (HasEnteredTrigger(_trigger, _overlappingTriggers)) {

                        _overlappingTriggers.Add(_trigger);
                        OnEnterTrigger(_trigger);
                    }
                }
            }

            // Exits from no more detected triggers.
            for (int i = _overlappingTriggers.Count; i-- > 0;) {

                ITrigger _trigger = _overlappingTriggers[i];
                if (HasExitedTrigger(_trigger, _buffer)) {

                    _overlappingTriggers.RemoveAt(i);
                    OnExitTrigger(_trigger);
                }
            }

            // ----- Local Methods ----- \\

            static bool HasEnteredTrigger(ITrigger _trigger, List<ITrigger> _overlappingTriggers) {

                for (int i = _overlappingTriggers.Count; i-- > 0;) {
                    if (_trigger.Equals(_overlappingTriggers[i])) {
                        return false;
                    }
                }

                return true;
            }

            static bool HasExitedTrigger(ITrigger _trigger, List<ITrigger> _buffer) {

                for (int i = _buffer.Count; i-- > 0;) {
                    if (_trigger.Equals(_buffer[i])) {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Exits from all overlapping triggers.
        /// </summary>
        protected void ExitTriggers() {

            List<ITrigger> _overlappingTriggers = overlappingTriggers.collection;
            for (int i = _overlappingTriggers.Count; i-- > 0;) {

                ITrigger _trigger = _overlappingTriggers[i];
                OnExitTrigger(_trigger);
            }

            _overlappingTriggers.Clear();
        }

        // -------------------------------------------
        // Callbacks
        // -------------------------------------------

        /// <summary>
        /// Called when this object enters in a trigger.
        /// </summary>
        /// <param name="_trigger">Entering <see cref="ITrigger"/>.</param>
        protected virtual void OnEnterTrigger(ITrigger _trigger) {

            _trigger.OnEnterTrigger(this);
            triggerController.OnEnterTrigger(_trigger);
        }

        /// <summary>
        /// Called when this object exits from a trigger.
        /// </summary>
        /// <param name="_trigger">Exiting <see cref="ITrigger"/>.</param>
        protected virtual void OnExitTrigger(ITrigger _trigger) {

            _trigger.OnExitTrigger(this);
            triggerController.OnExitTrigger(_trigger);
        }

        // -------------------------------------------
        // Trigger Actor
        // -------------------------------------------

        /// <inheritdoc cref="ITriggerActor.ExitTrigger(ITrigger)"/>
        void ITriggerActor.ExitTrigger(ITrigger _trigger) {

            EnhancedCollection<ITrigger> _overlappingTriggers = overlappingTriggers;

            // Remove from list.
            int _index = _overlappingTriggers.IndexOf(_trigger);
            if (_index != -1) {
                _overlappingTriggers.RemoveAt(_index);
            }

            OnExitTrigger(_trigger);
        }
        #endregion

        #region Overlap
        // -------------------------------------------
        // Extract
        // -------------------------------------------

        /// <summary>
        /// Extracts this object from a given collider.
        /// </summary>
        /// <param name="_colliderA">This object collider overlapping with the other.</param>
        /// <param name="_colliderB">Other collider to extract from.</param>
        /// <param name="_ignoreSelfColliders">If true, checks if the colliders to extract from should be ignored.</param>
        /// <returns>False if extraction should be interrupted and stopped, true otherwise.</returns>
        internal bool ExtractFromCollider(Collider _colliderA, Collider _colliderB, bool _ignoreSelfColliders = true) {

            // Stuck - early return.
            if (checkForOverlapStuck && isStuckThisFrame)
                return false;

            if (_ignoreSelfColliders && selfColliders.Contains(_colliderB))
                return true;

            Transform _aTransform = _colliderA.transform;
            Transform _bTransform = _colliderB.transform;

            if (Physics.ComputePenetration(_colliderA, _aTransform.position, _aTransform.rotation,
                                           _colliderB, _bTransform.position, _bTransform.rotation,
                                           out Vector3 _direction, out float _distance)) {
                // Check if overlap.
                if (checkForOverlapStuck) {

                    if (_distance >= _colliderA.contactOffset) {
                        isStuckThisFrame = true;
                        return false;
                    }

                    return true;
                }

                // Collider extraction.
                OnExtractFromCollider(_colliderA, _colliderB, _direction, _distance);
            }

            return true;
        }

        /// <summary>
        /// Post-overlap extract callback, once position was refreshed.
        /// </summary>
        internal void OnPositionRefreshed() {
            RefreshTriggers();

            shouldBeRefreshed = false;
            lastPosition = transform.position;
        }

        // -------------------------------------------
        // Operation
        // -------------------------------------------

        /// <summary>
        /// Get all <see cref="UnityEngine.Collider"/> currently overlapping with this object.
        /// </summary>
        /// <param name="_buffer">Buffer used to store all overlapping <see cref="UnityEngine.Collider"/>.</param>
        /// <returns>Total count of overlapping objects.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOverlappingColliders(List<Collider> _buffer) {
            return PhysicsSystem.OverlapPerformManual(this, _buffer, selfColliders);
        }

        /// <summary>
        /// Performs an overlap operation using this object main physics collider.
        /// </summary>
        /// <returns>Total count of overlapping colliders.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ColliderOverlap() {
            return PhysicsCollider.Overlap(selfColliders, QueryTriggerInteraction.Ignore);
        }

        /// <summary>
        /// Performs an overlap operation using this object trigger.
        /// </summary>
        /// <returns>Total count of overlapping triggers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int TriggerOverlap() {
            return PhysicsTrigger.Overlap(selfColliders, QueryTriggerInteraction.Collide);
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Get the overlapping collider or trigger at a given index.
        /// <para/>
        /// Use <see cref="ColliderOverlap"/> or <see cref="TriggerOverlap"/> to get the count of overlapping objects.
        /// </summary>
        /// <param name="_index">Index of the collider to get.</param>
        /// <returns>The overlapping collider at the given index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Collider GetOverlapAt(int _index) {
            return PhysicsCollider3D.GetOverlapCollider(_index);
        }
        #endregion

        #region Physics Surface
        /// <summary>
        /// Refreshes this object current physics surface.
        /// </summary>
        private void RefreshPhysicsSurface(CollisionOperationData3D _operation, bool _isGrounded, RaycastHit _hit) {
            if (!HasOption(MovableOption.PhysicsSurface))
                return;

            PhysicsSurface3D.Settings _surface = PhysicsSurface3D.Settings.Default;

            // Use default surface.
            if (!isGrounded) {
                SetPhysicsSurface(_surface);
                return;
            }

            // Get material associated surface.
            if (_hit.GetSharedMaterial(out Material _material)) {
                _surface = _operation.PhysicsSettings.GetPhysicsSurface(_material, this);
            }

            SetPhysicsSurface(_surface);
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        /// <summary>
        /// Sets and updates this object current physics surface.
        /// </summary>
        private void SetPhysicsSurface(PhysicsSurface3D.Settings _surfaceSettings) {
            physicsSurface = _surfaceSettings;
        }
        #endregion

        #region Collision Effect
        public const float MinEffectWeight = .01f;

        /// <summary>
        /// Is this object configured so that it can push other objects?
        /// </summary>
        public bool CanPushObject {
            get { return (WeightSettings.PushRange.y >= MinEffectWeight); }
        }

        /// <summary>
        /// Is this object configured so that it transfer its velocity to other objects?
        /// </summary>
        public bool CanTransferVelocity {
            get { return WeightSettings.TransferVelocityRange.y >= MinEffectWeight; }
        }

        // -------------------------------------------
        // Effects are used to interact with other
        // encountered Movable instances.
        //
        // For instance, when entering in contact with another object,
        // this Movable can push it according to its velocity and not consider it as an obstacle.
        // -------------------------------------------

        /// <summary>
        /// Pushes another <see cref="Movable3D"/> according to a given velocity.
        /// </summary>
        /// <param name="_other">The other <see cref="Movable3D"/> colliding with this object.</param>
        /// <param name="_velocity">Original velocity of this object.</param>
        /// <returns>Computed velocity of this object after collision.</returns>
        public Vector3 PushObject(Movable3D _other, Vector3 _velocity) {
            if (_other.RockBehaviour)
                return Vector3.zero;

            _velocity *= GetPushVelocityCoef(_other);
            _other.OffsetPosition(_velocity);

            return _velocity;
        }

        /// <summary>
        /// Transfers this object velocity to another <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_other">The other <see cref="Movable3D"/> colliding with this object.</param>
        /// <returns>True if velocity could be transfered, false otherwise.</returns>
        public bool TransferVelocity(Movable3D _other) {
            if (_other.RockBehaviour)
                return false;

            if (!GetTransferVelocityValues(_other, out float _reduceSelfCoef, out float _applyCoef))
                return false;

            Velocity.TransferVelocity(_other, _reduceSelfCoef, _applyCoef);
            return true;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Indicates if this object can apply any collision effect for a given <see cref="CollisionOperationData3D"/>.
        /// </summary>
        public bool CanApplyCollisionEffect(CollisionOperationData3D _operation) {
            // Push.
            if (CanPushObject)
                return true;

            // Transfer.
            if (CanTransferVelocity && ((_operation.Velocity.Force.SetY(0f) != Vector3.zero) || (Velocity.VelocityOverTime.Count != 0))) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the velocity coefficient to apply when this object collides with another <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_other">The other <see cref="Movable3D"/> colliding with this object.</param>
        /// <returns>Percentage of this object velocity after collision (between 0 and 1).</returns>
        public float GetPushVelocityCoef(Movable3D _other) {
            if (_other.RockBehaviour)
                return 0f;

            Movable3DWeightSettings _settings = WeightSettings;
            return GetCollisionEffectValue(_other.Weight, _settings.PushRange, _settings.PushCurve);
        }

        /// <summary>
        /// Get the velocity coefficient to apply for transfer when this object collides with another <see cref="Movable3D"/>.
        /// </summary>
        /// <param name="_other">The other <see cref="Movable3D"/> colliding with this object.</param>
        /// <param name="_reduceSelfCoef">Velocity coefficient to apply to this object velocity.</param>
        /// <param name="_applyCoef">Velocity coefficient to use for transfering this object velocity.</param>
        /// <returns>True if this object can transfer velocity, false otherwise.</returns>
        public bool GetTransferVelocityValues(Movable3D _other, out float _reduceSelfCoef, out float _applyCoef) {

            if (_other.RockBehaviour) {

                _reduceSelfCoef = 0f;
                _applyCoef      = 0f;

                return false;
            }

            Movable3DWeightSettings _settings = WeightSettings;
            float _value = GetCollisionEffectValue(_other.Weight, _settings.TransferVelocityRange, _settings.TransferVelocityCurve);

            _reduceSelfCoef = _settings.TransferVelocitySelfReduce * _value;
            _applyCoef      = _settings.TransferVelocityApply      * _value;

            return _value > 0f;
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        protected float GetCollisionEffectValue(float _weight, Vector2 _fullRange, AnimationCurve _curve) {
            if ((_fullRange.y < MinEffectWeight) || (_weight > _fullRange.y))
                return 0f;

            float _start = _weight      - _fullRange.x;
            float _range = _fullRange.y - _fullRange.x;

            if (Mathm.ApproximatelyZero(_range)) {
                _range = 1f;
            }

            float _percent = 1f - Mathf.Clamp01(_start / _range);
            return _curve.Evaluate(_percent);
        }
        #endregion

        // --- Utility --- \\

        #region Transform
        /// <summary>
        /// Sets this object position.
        /// <br/> Use this instead of setting <see cref="Transform.position"/>.
        /// </summary>
        public virtual void SetPosition(Vector3 _position) {
            rigidbody.position = _position;
            transform.position = _position;

            shouldBeRefreshed = true;
        }

        /// <summary>
        /// Sets this object rotation.
        /// <br/> Use this instead of setting <see cref="Transform.rotation"/>.
        /// </summary>
        public virtual void SetRotation(Quaternion _rotation) {
            rigidbody.rotation = _rotation;
            transform.rotation = _rotation;

            shouldBeRefreshed = true;
        }

        /// <summary>
        /// Sets this object position and rotation.
        /// <br/> Use this instead of setting <see cref="Transform.position"/> and <see cref="Transform.rotation"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPositionAndRotation(Vector3 _position, Quaternion _rotation) {
            SetPosition(_position);
            SetRotation(_rotation);
        }

        /// <inheritdoc cref="SetPositionAndRotation(Vector3, Quaternion)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPositionAndRotation(Transform _transform, bool _useLocal = false) {
            Vector3 _position;
            Quaternion _rotation;

            if (_useLocal) {
                _position = _transform.localPosition;
                _rotation = _transform.localRotation;
            } else {
                _position = _transform.position;
                _rotation = _transform.rotation;
            }

            SetPositionAndRotation(_position, _rotation);
        }

        // -----------------------

        /// <summary>
        /// Adds an offset to this object position.
        /// </summary>
        /// <param name="_offset">Transform position offset.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OffsetPosition(Vector3 _offset) {
            SetPosition(Position + _offset);
        }

        /// <summary>
        /// Adds an offset to this object rotation.
        /// </summary>
        /// <param name="_offset">Transform rotation offset.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OffsetRotation(Quaternion _offset) {
            SetRotation(Rotation * _offset);
        }

        /// <summary>
        /// Rotates this object in space around a given pivot world position.
        /// </summary>
        /// <param name="_pivot">World space pivot position to rotate this object around.</param>
        /// <param name="_axis">Axis to rotate this object.</param>
        /// <param name="_angle">Rotation angle value.</param>
        public void RotateAround(Vector3 _pivot, Vector3 _axis, float _angle) {
            Vector3    _position = Position;
            Quaternion _rotation = Rotation;

            Mathm.RotateAround(_pivot, _axis, _angle, ref _position, ref _rotation);
            SetPositionAndRotation(_position, _rotation);
        }

        // -------------------------------------------
        // Editor
        // -------------------------------------------

        [Button(ActivationMode.Play, SuperColor.Raspberry, IsDrawnOnTop = false)]
        #pragma warning disable IDE0051
        private void SetTransformValues(Transform _transform, bool _usePosition = true, bool _useRotation = true) {
            if (_usePosition) {
                SetPosition(_transform.position);
            }

            if (_useRotation) {
                SetRotation(_transform.rotation);
            }
        }
        #endregion

        #region Option
        [SerializeField, HideInInspector] private bool skipCollisionIfOverlap   = false;
        [SerializeField, HideInInspector] private bool extractOnlyIfOverlap     = false;
        [SerializeField, HideInInspector] private bool extractOnlyIfContact     = false;

        [SerializeField, HideInInspector] private bool shadowRotation           = false;
        [SerializeField, HideInInspector] private bool shadowHeight             = false;

        [SerializeField, HideInInspector] private bool usePhysicsSurface        = false;
        [SerializeField, HideInInspector] private bool slideOnSurfaces          = false;
        [SerializeField, HideInInspector] private bool rockBehaviour            = false;

        [SerializeField, HideInInspector] private bool refreshContinuously      = false;
        [SerializeField, HideInInspector] private bool equalizeVelocity         = false;
        [SerializeField, HideInInspector] private bool autoExtract              = false;
        [SerializeField, HideInInspector] private bool canMove                  = false;

        // -------------------------------------------
        // Option
        // -------------------------------------------

        /// <summary>
        /// Get if a specific option is active on this object.
        /// </summary>
        /// <param name="_option">The option to check activation.</param>
        /// <returns>True if this option is active on this object, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasOption(MovableOption _option) {
            return options.HasFlagUnsafe(_option);
        }

        /// <summary>
        /// Sets this object option.
        /// </summary>
        /// <param name="_option">New option of this object.</param>
        public void SetOption(MovableOption _option) {
            options = _option;
            RefreshOption();
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        /// <summary>
        /// Refreshes this object option-related caches values.
        /// <para/>
        /// Flags can be performance heavy to check regularly, so cache values into booleans.
        /// </summary>
        private void RefreshOption() {

            // Overlap & extract.
            skipCollisionIfOverlap = HasOption(MovableOption.SkipCollisionIfOverlap);
            extractOnlyIfOverlap   = HasOption(MovableOption.ExtractOnlyIfOverlap);
            extractOnlyIfContact   = HasOption(MovableOption.ExtractOnlyIfContact);

            // Shadow.
            shadowRotation         = HasOption(MovableOption.ShadowRotation);
            shadowHeight           = HasOption(MovableOption.ShadowHeight);

            // Special features.
            usePhysicsSurface      = HasOption(MovableOption.PhysicsSurface);
            slideOnSurfaces        = HasOption(MovableOption.SlideOnSurfaces);
            rockBehaviour          = HasOption(MovableOption.RockBehaviour);

            // Basic features.
            refreshContinuously    = HasOption(MovableOption.RefreshContinuously);
            equalizeVelocity       = HasOption(MovableOption.EqualizeVelocity);
            autoExtract            = HasOption(MovableOption.AutoExtract);
            canMove                = HasOption(MovableOption.Move);
        }

        // -------------------------------------------
        // Getter
        // -------------------------------------------

        /// <summary>
        /// If overlapping with another collider at the start of the frame, only try to extract from it and do not perform collision.
        /// </summary>
        public bool SkipCollisionIfOverlap {
            get { return skipCollisionIfOverlap; }
        }

        /// <summary>
        /// Do not perform any extract operation unless this object overlaps with another collider.
        /// </summary>
        public bool ExtractOnlyIfOverlap {
            get { return extractOnlyIfOverlap; }
        }

        /// <summary>
        /// Do not perform any extract operation unless this object is in contact with another collider.
        /// </summary>
        public bool ExtractOnlyIfContact {
            get { return extractOnlyIfContact; }
        }

        // -----------------------

        /// <summary>
        /// Adjust this object shadow rotation according on the ground normal.
        /// </summary>
        public bool ShadowRotation {
            get { return shadowRotation; }
        }

        /// <summary>
        /// Adjust this object shadow Y position if the object is not touching ground.
        /// </summary>
        public bool ShadowHeight {
            get { return shadowHeight; }
        }

        // -----------------------

        /// <summary>
        /// Checks for Physics Surfaces on ground GameObjects.
        /// </summary>
        public bool UsePhysicsSurface {
            get { return usePhysicsSurface; }
        }

        /// <summary>
        /// Slide against obstacle surfaces if the velocity and the collision angle allow it.
        /// </summary>
        public bool SlideOnSurfaces {
            get { return slideOnSurfaces; }
        }

        /// <summary>
        /// Always try to push obstacle instead of extracting from them - always keep current position.
        /// </summary>
        public bool RockBehaviour {
            get { return rockBehaviour; }
        }

        // -----------------------

        /// <summary>
        /// If true, continuously refresh this object position every frame, even when no velocity was applied.
        /// </summary>
        public bool RefreshContinuously {
            get { return refreshContinuously; }
        }

        /// <summary>
        /// Makes sure that when this object stops moving,
        /// its Velocity is equalized based on the previous frame instead of continuing on its actual Force.
        /// </summary>
        public bool EqualizeVelocity {
            get { return equalizeVelocity; }
        }

        /// <summary>
        /// Automatically extracts this object from any overlapping collider.
        /// </summary>
        public bool AutoExtract {
            get { return autoExtract; }
        }

        /// <summary>
        /// Allows this object to move in space and perform collision calculs.
        /// </summary>
        public bool CanMove {
            get { return canMove; }
        }
        #endregion

        #region Utility
        /// <summary>
        /// Get the value of a specific world-coordinates vector relative to this object forward and reference upwards direction.
        /// </summary>
        /// <param name="_vector">Vector to get relative value.</param>
        /// <returns>Relative vector value according to this object forward and reference upwards direction.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetDirectionRelativeVector(Vector3 _vector) {
            return GetRelativeVector(_vector, DirectionRotation);
        }

        /// <summary>
        /// Get the value of a specific relative vector in world space.
        /// </summary>
        /// <param name="_vector">Vector to get world-space value.</param>
        /// <returns>World-space vector value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetDirectionWorldVector(Vector3 _vector) {
            return GetWorldVector(_vector, DirectionRotation);
        }

        /// <summary>
        /// Enables/Disables this object colliders.
        /// </summary>
        /// <param name="_enabled">Whether to enable or disable colliders.</param>
        public void EnableColliders(bool _enabled) {
            collider.enabled = _enabled;
            trigger .enabled = _enabled;

            for (int i = selfColliders.Count; i-- > 0;) {
                selfColliders[i].enabled = _enabled;
            }
        }

        /// <summary>
        /// Makes this object fall asleep, or wake it up.
        /// <para/> No velocity can be applied on the object while asleep.
        /// </summary>
        /// <param name="_isAsleep">True to make this object fall asleep, false to wake it up.</param>
        public void SetAsleep(bool _isAsleep) {
            isAsleep = _isAsleep;
        }
        #endregion

        #region Documentation
        /// <summary>
        /// Documentation only method.
        /// </summary>
        /// <returns>True to override this behaviour from the controller, false to call the base definition.</returns>
        protected bool Doc() { return false; }
        #endregion
    }
}
