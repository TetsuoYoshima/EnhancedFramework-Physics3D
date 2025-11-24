// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

using static EnhancedFramework.Core.AdvancedCurveValue;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// Advanced <see cref="Movable3D"/> with the addition of various creature-like behaviours.
    /// </summary>
    [AddComponentMenu(FrameworkUtility.MenuPath + "Physics [3D]/Creature Movable [3D]"), DisallowMultipleComponent]
    public sealed class CreatureMovable3D : Movable3D {
        #region Rotation Mode
        /// <summary>
        /// Determines how the object turn when following a path.
        /// </summary>
        public enum PathRotationMode {
            /// <summary>
            /// Don't turn the object.
            /// </summary>
            None                = 0,

            /// <summary>
            /// Turn in direction of the new destination before moving.
            /// </summary>
            TurnBeforeMovement  = 1,

            /// <summary>
            /// Turn while moving to the next destination.
            /// </summary>
            TurnDuringMovement  = 2,
        }
        #endregion

        #region Global Members
        [Section("Creature Movable [3D]"), PropertyOrder(0)]

        [Tooltip("Property attributes of this Movable")]
        [SerializeField, Enhanced, Required] private CreatureMovable3DAttributes attributes = null;

        [PropertyOrder(3)]

        [Tooltip("Current forward direction of the object")]
        [SerializeField, Enhanced, ReadOnly] private Vector3 forward = Vector3.zero;

        // -----------------------

        public override Movable3DGroundSettings GroundSettings {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                #if UNITY_EDITOR
                return attributes.GroundSettings;
                #else
                return groundSettings;
                #endif
            }
        }

        public override Movable3DWeightSettings WeightSettings {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                #if UNITY_EDITOR
                return attributes.WeightSettings;
                #else
                return weightSettings;
                #endif
            }
        }

        public override bool CanEditSpeed {
            get { return false; }
        }

        public override bool CanEditSettings {
            get { return false; }
        }

        public override float ClimbHeight {
            get { return attributes.ClimbHeight; }
        }

        public override float SnapHeight {
            get { return attributes.SnapHeight; }
        }
        #endregion

        #region Enhanced Behaviour
        protected override void OnInit() {
            // Settings.
            SetAttributesSettings();

            base.OnInit();
        }

        #if UNITY_EDITOR
        // -------------------------------------------
        // Editor
        // -------------------------------------------

        protected override void OnValidate() {
            base.OnValidate();

            // Settings.
            SetAttributesSettings();
        }
        #endif

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        private void SetAttributesSettings() {
            groundSettings = attributes.GroundSettings;
            weightSettings = attributes.WeightSettings;
        }
        #endregion

        #region Controller
        private ICreatureMovable3DNavigationController  navigationController    = DefaultMovable3DController.Instance;
        private ICreatureMovable3DRotationController    rotationController      = DefaultMovable3DController.Instance;
        private ICreatureMovable3DSpeedController       speedController         = DefaultMovable3DController.Instance;

        // -----------------------

        public override void RegisterController<T>(T _object) {
            base.RegisterController(_object);

            if (_object is ICreatureMovable3DNavigationController _navigation) {
                navigationController = _navigation;
            }

            if (_object is ICreatureMovable3DRotationController _rotation) {
                rotationController = _rotation;
            }

            if (_object is ICreatureMovable3DSpeedController _speed) {
                speedController = _speed;
            }
        }

        public override void UnregisterController<T>(T _object) {
            base.UnregisterController(_object);

            if ((_object is ICreatureMovable3DNavigationController _navigation) && navigationController.Equals(_navigation)) {
                navigationController = DefaultMovable3DController.Instance;
            }

            if ((_object is ICreatureMovable3DRotationController _rotation) && rotationController.Equals(_rotation)) {
                rotationController = DefaultMovable3DController.Instance;
            }

            if ((_object is ICreatureMovable3DSpeedController _speed) && speedController.Equals(_speed)) {
                speedController = DefaultMovable3DController.Instance;
            }
        }
        #endregion

        // --- Advanced --- \\

        #region Navigation
        private const float PathDelay = .01f;

        private Action setPathDelayDelegate = null;

        private DelayHandler setPathDelay = default;
        private PathHandler setPathBuffer = default;

        private Vector3 lastPathMovement = Vector3.zero;
        private PathHandler path = default;

        // -----------------------

        /// <inheritdoc cref="NavigateTo(Vector3, Quaternion, Action{bool, CreatureMovable3D})"/>
        public PathHandler NavigateTo(Vector3 _destination, Action<bool, CreatureMovable3D> _onComplete = null) {
            return SetNavigationPath(NavigationPath3DManager.Get().Setup(this, _destination, _onComplete));
        }

        /// <inheritdoc cref="NavigateTo(Vector3[], Quaternion, Action{bool, CreatureMovable3D})"/>
        public PathHandler NavigateTo(Vector3[] _path, Action<bool, CreatureMovable3D> _onComplete = null) {
            return SetNavigationPath(NavigationPath3DManager.Get().Setup(this, _path, _onComplete));
        }

        /// <summary>
        /// Set this object path to reach a specific destination position.
        /// </summary>
        /// <inheritdoc cref="NavigationPath3D.Setup(CreatureMovable3D, Vector3, Quaternion, Action{bool, CreatureMovable3D})"/>
        public PathHandler NavigateTo(Vector3 _destination, Quaternion _rotation, Action<bool, CreatureMovable3D> _onComplete = null) {
            return SetNavigationPath(NavigationPath3DManager.Get().Setup(this, _destination, _rotation, _onComplete));
        }

        /// <summary>
        /// Set this object path positions.
        /// </summary>
        /// <inheritdoc cref="NavigationPath3D.Setup(CreatureMovable3D, Vector3[], Quaternion, Action{bool, CreatureMovable3D})"/>
        public PathHandler NavigateTo(Vector3[] _path, Quaternion _rotation, Action<bool, CreatureMovable3D> _onComplete = null) {
            return SetNavigationPath(NavigationPath3DManager.Get().Setup(this, _path, _rotation, _onComplete));
        }

        /// <summary>
        /// Set this object path destination position and rotation.
        /// </summary>
        /// <inheritdoc cref="NavigationPath3D.Setup(CreatureMovable3D, Transform, bool, Action{bool, CreatureMovable3D})"/>
        [Button(ActivationMode.Play, SuperColor.HarvestGold)]
        public PathHandler NavigateTo(Transform _transform, bool _useRotation, Action<bool, CreatureMovable3D> _onComplete = null) {
            return SetNavigationPath(NavigationPath3DManager.Get().Setup(this, _transform, _useRotation, _onComplete));
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Get this object current navigation path.
        /// </summary>
        /// <param name="_path">Current path of the object.</param>
        /// <returns>True if this object path is active, false otherwise.</returns>
        public bool GetNavigationPath(out PathHandler _path) {
            _path = path;
            return _path.IsValid;
        }

        /// <summary>
        /// Completes this object current navigation path.
        /// </summary>
        /// <returns>True if the navigation could be successfully completed, false otherwise.</returns>
        public bool CompleteNavigation() {
            return path.Complete();
        }

        /// <summary>
        /// Cancels this object current navigation path.
        /// </summary>
        /// <returns>True if the navigation could be successfully canceled, false otherwise.</returns>
        public bool CancelNavigation() {
            return path.Cancel();
        }

        // -------------------------------------------
        // Callbacks
        // -------------------------------------------

        /// <summary>
        /// Called when this object navigation path is set.
        /// </summary>
        /// <param name="_path">This object path.</param>
        internal PathHandler SetNavigationPath(PathHandler _path) {
            setPathDelay.Cancel();

            // Use a delay in case the current path is being completed during the same frame.
            if (path.IsValid) {

                setPathBuffer = _path;

                setPathDelayDelegate ??= SetPathDelay;
                setPathDelay = Delayer.Call(PathDelay, setPathDelayDelegate, true);
            } else {
                SetPath(_path);
            }

            return _path;

            // ----- Local Methods ----- \\

            void SetPathDelay() {
                SetPath(setPathBuffer);
                setPathBuffer = default;
            }

            void SetPath(PathHandler _path) {
                CancelNavigation();

                path = _path;
                navigationController.OnNavigateTo(_path);
            }
        }

        /// <summary>
        /// Determines if the current navigation path should be completed or not.
        /// <para/>
        /// By default, waits for the object movement to be null (useful when using root motion).
        /// </summary>
        /// <returns>Override: <inheritdoc cref="Movable3D.Doc" path="/returns"/>.
        /// <para/>Completed: True if the path should be completed, false otherwise.</returns>
        internal bool DoCompleteNavigation(out bool _completed) {
            if (navigationController.CompletePath(out _completed)) {
                return true;
            }

            _completed = lastPathMovement.IsNull() || lastPathDirection.IsNull();
            return false;
        }

        /// <summary>
        /// Called when this object navigation path is complete.
        /// </summary>
        /// <param name="_success">True if the navigation path was successfully completed, false if canceled.</param>
        internal void OnCompleteNavigation(bool _success) {
            navigationController.OnCompleteNavigation(_success);
        }
        #endregion

        #region Orientation
        private const float MinRotationAngle = PathRotationTurnAngle - .2f;

        private Action onTurnComplete = null;
        private float turnTimeVar = 0f;

        // -----------------------

        /// <summary>
        /// Turns this object on its Y axis by a given angle.
        /// </summary>
        /// <param name="_angleIncrement">Local rotation angle increment.</param>
        /// <returns><inheritdoc cref="Movable3D.Doc" path="/returns"/></returns>
        public bool Turn(float _angleIncrement, bool _withController = true) {

            if (_withController && rotationController.OnTurn(ref _angleIncrement)) {
                return true;
            }

            OffsetRotation(Quaternion.Euler(transform.up * _angleIncrement));
            return false;
        }

        /// <summary>
        /// Turns this object on its Y axis, using a given direction.
        /// </summary>
        /// <param name="_direction">Direction in which to turn the object (1 for right, -1 for left).</param>
        public void TurnTo(float _direction) {
            if (_direction == 0f) {
                ResetTurn();
                return;
            }

            float _angle = GetTurnAngle(_direction);
            Turn(_angle);
        }

        /// <summary>
        /// Turns this object on its Y axis, to a specific forward.
        /// </summary>
        /// <param name="_forward">New forward target.</param>
        /// <param name="_onTurnComplete">Delegate to call once the rotation is complete.</param>
        /// <returns><inheritdoc cref="Movable3D.Doc" path="/returns"/></returns>
        public bool TurnTo(Vector3 _forward, Action _onTurnComplete = null) {

            // Invalid operation.
            if (_forward.IsNull()) {

                StopTurnTo(true);
                _onTurnComplete?.Invoke();

                return false;
            }

            StopTurnTo(false);

            // Controller
            if (rotationController.OnTurnTo(_forward, _onTurnComplete)) {
                return true;
            }

            forward = _forward;
            onTurnComplete = _onTurnComplete;

            return false;
        }

        /// <summary>
        /// Stops the current turn to operation.
        /// </summary>
        /// <param name="_reset">If true, resets all associated parameters.</param>
        public void StopTurnTo(bool _reset = true) {
            // Controller.
            rotationController.OnCompleteTurnTo(_reset);

            forward = Vector3.zero;

            // Callback.
            onTurnComplete?.Invoke();

            // Reset.
            if (_reset) {
                ResetTurn();
                onTurnComplete = null;
            }
        }

        /// <summary>
        /// Resets this object current turn speed.
        /// </summary>
        public void ResetTurn() {
            attributes.TurnSpeed.Reset();
            turnTimeVar = 0f;
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        /// <summary>
        /// Updates this object rotation.
        /// </summary>
        private void UpdateRotation() {
            if (forward.IsNull()) {
                return;
            }

            float _angle = GetForwardAngle(forward);

            // Rotation achieved.
            if (Mathf.Abs(_angle) < MinRotationAngle) {

                StopTurnTo();
                return;
            }

            // Rotate.
            float _forwardAngle = _angle;
            _angle = Mathf.MoveTowards(0f, _angle, GetTurnAngle(1f));

            // Do not increment if exact angle.
            bool _withController = true;

            if (Mathf.Approximately(_angle, _forwardAngle)) {
                _withController = false;
            }

            Turn(_angle, _withController);
        }

        private float GetTurnAngle(float _coef) {
            return attributes.TurnSpeed.EvaluateContinue(ref turnTimeVar, DeltaTime) * DeltaTime * _coef * 90f;
        }

        private float GetForwardAngle(Vector3 _forward) {

            // Make sure vector up is valid.
            _forward = Vector3.ProjectOnPlane(_forward, Transform.up);

            // Get angle.
            float _angle = Vector3.SignedAngle(transform.forward, _forward, transform.up);

            if (Mathf.Abs(_angle) > 180f) {
                _angle -= 360f * Mathf.Sign(_angle);
            }

            return _angle;
        }
        #endregion

        // --- Velocity --- \\

        #region Velocity
        public override bool ResetVelocity(bool _force = false) {
            if (base.ResetVelocity(_force)) {
                return true;
            }

            // Ignore when following a path.
            if (!_force && path.IsValid) {
                return true;
            }

            ResetSpeed();

            isChangingDirection = false;
            lastMovement = Vector3.zero;

            return false;
        }
        #endregion

        #region Speed
        private readonly DecreaseWrapper decreaseWrapper = new DecreaseWrapper();
        private float speedTimeVar = 0f;

        // -----------------------

        /// <summary>
        /// Updates this object speed, for this frame (increase or decrease).
        /// </summary>
        /// <returns><inheritdoc cref="Movable3D.Doc" path="/returns"/></returns>
        private bool UpdateSpeed(ref float _speed, float _deltaTime) {
            if (speedController.OnUpdateSpeed(ref _speed)) {
                return true;
            }

            // Update the speed depending on this frame movement.
            Vector3 _movement = GetRelativeVector(Velocity.Movement + Velocity.InstantMovement).Flat();

            if (_movement.IsNull()) {
                DecreaseSpeed(ref _speed, _deltaTime);
            } else {
                IncreaseSpeed(ref _speed, _deltaTime);
            }

            return false;
        }

        /// <summary>
        /// Increases this object speed.
        /// </summary>
        /// <returns><inheritdoc cref="Movable3D.Doc" path="/returns"/></returns>
        public bool IncreaseSpeed(ref float _speed, float _deltaTime) {
            if (speedController.OnIncreaseSpeed(ref _speed)) {
                return true;
            }

            PhysicsSurface3D.Settings _physicsSurface = PhysicsSurface;
            float _increase = _deltaTime * _physicsSurface.IncreaseSpeedCoef;

            if (!IsGrounded) {
                _increase *= attributes.AirAccelCoef;
            }

            _speed = attributes.MoveSpeed.EvaluateContinue(ref speedTimeVar, _increase, _physicsSurface.MaxSpeedCoef);
            decreaseWrapper.Reset();

            return false;
        }

        /// <summary>
        /// Decreases this object speed.
        /// </summary>
        /// <returns><inheritdoc cref="Movable3D.Doc" path="/returns"/></returns>
        public bool DecreaseSpeed(ref float _speed, float _deltaTime) {
            if (speedController.OnDecreaseSpeed(ref _speed)) {
                return true;
            }

            PhysicsSurface3D.Settings _physicsSurface = PhysicsSurface;
            float _decrease = _deltaTime * _physicsSurface.DecreaseSpeedCoef;

            _speed = attributes.MoveSpeed.Decrease(ref speedTimeVar, _decrease, decreaseWrapper, _physicsSurface.MaxSpeedCoef);
            return false;
        }

        /// <summary>
        /// Resets this object speed.
        /// </summary>
        /// <returns><inheritdoc cref="Movable3D.Doc" path="/returns"/></returns>
        public bool ResetSpeed(bool _isComputeVelocityCallback = false) {
            if (speedController.OnResetSpeed(_isComputeVelocityCallback)) {
                return true;
            }

            speed = attributes.MoveSpeed.Reset();

            decreaseWrapper.Reset();
            speedTimeVar = 0f;

            return false;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        /// <summary>
        /// Get this object speed-related time ratio.
        /// </summary>
        public float GetSpeedTimeRatio() {
            return attributes.MoveSpeed.GetTimeRatio(speedTimeVar, PhysicsSurface.MaxSpeedCoef);
        }

        /// <summary>
        /// Get this object speed-related value ratio.
        /// </summary>
        public float GetSpeedValueRatio() {
            return attributes.MoveSpeed.GetValueRatio(speedTimeVar);
        }

        /// <summary>
        /// Set this object speed ratio.
        /// </summary>
        public void SetSpeedRatio(float _ratio) {
            float _coef = PhysicsSurface.MaxSpeedCoef;

            speed = attributes.MoveSpeed.EvaluatePercent(_ratio, _coef);
            speedTimeVar = _ratio * attributes.MoveSpeed.Duration * _coef;
        }
        #endregion

        // --- Collision --- \\

        #region Computation
        private const float PathRotationTurnAngle = 2.5f;

        private const float PathStuckMagnitudeTolerance = .04f;
        private const float PathStuckMaxDuration = 1f;

        private Vector3 lastPathDirection = Vector3.zero;
        private float pathStuckDuration   = 0f;

        private bool isChangingDirection = false;
        private Quaternion lastDirection = Quaternion.identity;
        private Vector3 lastMovement     = Vector3.zero;

        // -----------------------

        protected override bool OnPreComputeVelocity(float _deltaTime, out float _speed) {
            if (base.OnPreComputeVelocity(_deltaTime, out _speed)) {
                return true;
            }

            bool _isStuck = false;

            // Follow path.
            if (path.GetNextDirection(out Vector3 _direction)) {

                if (FollowPath(_direction.normalized)) {

                    // When following the path, check if something is preventing the object from moving.
                    float _difference = Mathf.Abs(lastPathDirection.sqrMagnitude - _direction.sqrMagnitude);
                    if (_difference < PathStuckMagnitudeTolerance) {

                        pathStuckDuration += _deltaTime;

                        // If stuck for too long, cancel path.
                        if (pathStuckDuration > PathStuckMaxDuration) {
                            CompleteNavigation();
                        } else {
                            _isStuck = true;
                        }
                    }
                }

                // ----- Local Method ----- \\

                bool FollowPath(Vector3 _direction) {
                    switch (attributes.PathRotationMode) {

                        case PathRotationMode.TurnBeforeMovement: {
                            TurnTo(_direction);

                            // Don't move while not facing direction.
                            if (!Mathm.IsInRange(GetForwardAngle(_direction), -PathRotationTurnAngle, PathRotationTurnAngle)) {
                                return false;
                            }
                        }
                        break;

                        case PathRotationMode.TurnDuringMovement: {
                            TurnTo(_direction);
                        }
                        break;

                        case PathRotationMode.None:
                        default:
                            break;
                    }

                    Move(_direction);
                    return true;
                }
            }

            // Position update.
            if (!_isStuck) {
                lastPathDirection = _direction;
                pathStuckDuration = 0f;
            }

            ComputeMovementVelocity(_deltaTime);
            return false;
        }

        protected override bool OnPostComputeVelocity(float _deltaTime, ref FrameVelocity _velocity) {
            if (base.OnPostComputeVelocity(_deltaTime, ref _velocity)) {
                return true;
            }

            // Reset speed if no movement, to avoid starting back movement at full speed.
            if (Velocity.Movement.IsNull()) {
                ResetSpeed(true);
            }

            // Clamp path velocity magnitude.
            if (path.GetNextDirection(out Vector3 _direction)) {

                // Cache unclamped movement.
                Vector3 _movement = _velocity.Movement;
                lastPathMovement  = _movement;

                // Clamping the vector magnitude does not guarantee that the movement
                // will not be oriented in the wrong direction,
                // so let's clamp its X & Z components independently.
                _movement.x = Mathf.Min(Mathf.Abs(_movement.x), Mathf.Abs(_direction.x)) * Mathf.Sign(_movement.x);
                _movement.z = Mathf.Min(Mathf.Abs(_movement.z), Mathf.Abs(_direction.z)) * Mathf.Sign(_movement.z);

                _velocity.Movement = _movement;
            }

            return false;
        }

        // -------------------------------------------
        // Utility
        // -------------------------------------------

        protected override float ComputeSpeed(ref float _speed, float _deltaTime) {
            _speed = base.ComputeSpeed(ref _speed, _deltaTime);
            UpdateSpeed(ref _speed, _deltaTime);

            return _speed;
        }

        /// <summary>
        /// Computes this object movement-related velocity.
        /// </summary>
        private void ComputeMovementVelocity(float _deltaTime) {

            bool _deceleration = !attributes.InstantDeceleration;
            bool _turnAround   = !attributes.InstantTurnAround;

            // Early return.
            if (!_deceleration && !_turnAround)
                return;

            Vector3 _lastMovement = lastMovement;
            Vector3 _movement     = Velocity.Movement;

            if (attributes.PreserveOrientation) {
                _lastMovement = lastMovement.RotateInverse(lastDirection).Rotate(DirectionRotation);
            }

            // Turn around:
            bool _isTurningAround = isChangingDirection;

            if (_turnAround && !_movement.IsNull() && (_isTurningAround || (!_lastMovement.IsNull() && (Vector3.Dot(_lastMovement, _movement) < 0f)))) {

                Vector3 _originMovement = _movement;

                float _speed = attributes.TurnAroundSpeed * PhysicsSurface.ChangeDirectionCoef;
                _movement    = Vector3.MoveTowards(_lastMovement, _movement, _speed * _deltaTime);

                if (_movement == _originMovement) {
                    _isTurningAround = false;
                } else {
                    _isTurningAround = true;
                }
            } else {
                _isTurningAround = false;
            }

            isChangingDirection = _isTurningAround;

            // Deceleration:
            if (_deceleration && _movement.IsNull() && !_lastMovement.IsNull()) {

                float _speed = attributes.DecelerationSpeed * PhysicsSurface.DecelerationCoef;
                _movement    = Vector3.MoveTowards(_lastMovement, _movement, _speed * _deltaTime);
            }

            // Update data.
            Velocity.Movement = _movement;
            lastMovement      = _movement;
            lastDirection     = DirectionRotation;
        }
        #endregion

        #region Collision
        protected override bool OnAppliedVelocity(CollisionOperationData3D _operation) {
            if (base.OnAppliedVelocity(_operation)) {
                return true;
            }

            // Path update - remove reference on complete.
            if (path.IsValid && !path.UpdatePath()) {
                path = default;
            }

            // Forward rotation.
            UpdateRotation();
            return false;
        }
        #endregion
    }
}
