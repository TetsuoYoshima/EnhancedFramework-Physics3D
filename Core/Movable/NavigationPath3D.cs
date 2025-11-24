// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using EnhancedFramework.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace EnhancedFramework.Physics3D {
    /// <summary>
    /// <see cref="NavigationPath3D"/>-related wrapper for a single path operation.
    /// </summary>
    public struct PathHandler : IHandler<NavigationPath3D> {
        #region Global Members
        private Handler<NavigationPath3D> handler;

        // -----------------------

        public int ID {
            get { return handler.ID; }
        }

        public bool IsValid {
            get { return GetHandle(out _); }
        }

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        /// <inheritdoc cref="PathHandler(NavigationPath3D, int)"/>
        public PathHandler(NavigationPath3D _path) {
            handler = new Handler<NavigationPath3D>(_path);
        }

        /// <param name="_path"><see cref="NavigationPath3D"/> used for navigation.</param>
        /// <param name="_id">ID of the associated navigation operation.</param>
        /// <inheritdoc cref="PathHandler"/>
        public PathHandler(NavigationPath3D _path, int _id) {
            handler = new Handler<NavigationPath3D>(_path, _id);
        }
        #endregion

        #region Navigation
        /// <inheritdoc cref="NavigationPath3D.UpdatePath"/>
        public bool UpdatePath() {
            if (!GetHandle(out NavigationPath3D _path)) {
                return false;
            }

            return _path.UpdatePath();
        }

        /// <inheritdoc cref="NavigationPath3D.GetNextPosition(out Vector3)"/>
        public bool GetNextPosition(out Vector3 _position) {
            if (!GetHandle(out NavigationPath3D _path)) {

                _position = Vector3.zero;
                return false;
            }

            return _path.GetNextPosition(out _position);
        }

        /// <inheritdoc cref="NavigationPath3D.GetNextDistance(out Vector3)"/>
        public bool GetNextDistance(out Vector3 _distance) {
            if (!GetHandle(out NavigationPath3D _path)) {

                _distance = Vector3.zero;
                return false;
            }

            return _path.GetNextDistance(out _distance);
        }

        /// <inheritdoc cref="NavigationPath3D.GetNextDirection(out Vector3)"/>
        public bool GetNextDirection(out Vector3 _direction) {
            if (!GetHandle(out NavigationPath3D _path)) {

                _direction = Vector3.zero;
                return false;
            }

            return _path.GetNextDirection(out _direction);
        }
        #endregion

        #region Utility
        public bool GetHandle(out NavigationPath3D _path) {
            return handler.GetHandle(out _path) && _path.IsActive;
        }

        /// <summary>
        /// Resumes this handle associated <see cref="NavigationPath3D"/>.
        /// </summary>
        /// <inheritdoc cref="NavigationPath3D.Resume()"/>
        public bool Resume() {
            if (!GetHandle(out NavigationPath3D _path)) {
                return false;
            }

            return _path.Resume();
        }

        /// <summary>
        /// Pauses this handle associated <see cref="NavigationPath3D"/>.
        /// </summary>
        /// <inheritdoc cref="NavigationPath3D.Pause"/>
        public bool Pause() {
            if (!GetHandle(out NavigationPath3D _path)) {
                return false;
            }

            return _path.Pause();
        }

        /// <summary>
        /// Stops this handle associated <see cref="NavigationPath3D"/>.
        /// </summary>
        /// <inheritdoc cref="NavigationPath3D.Cancel"/>
        public bool Cancel() {
            if (!GetHandle(out NavigationPath3D _player)) {
                return false;
            }

            return _player.Cancel();
        }

        /// <summary>
        /// Stops this handle associated <see cref="NavigationPath3D"/>.
        /// </summary>
        /// <inheritdoc cref="NavigationPath3D.Complete"/>
        public bool Complete() {
            if (!GetHandle(out NavigationPath3D _path)) {
                return false;
            }

            return _path.Complete();
        }
        #endregion
    }

    /// <summary>
    /// <see cref="CreatureMovable3D"/>-related path wrapper class.
    /// </summary>
    [Serializable]
    public sealed class NavigationPath3D : IHandle, IPoolableObject {
        #region State
        /// <summary>
        /// References all available states for an <see cref="NavigationPath3D"/>.
        /// </summary>
        public enum State {
            Inactive    = 0,

            Active      = 1,
            Paused      = 2,
        }
        #endregion

        #region Global Members
        private State state = State.Inactive;
        private int id = 0;

        private CreatureMovable3D movable = null;

        private readonly List<Vector3> path = new List<Vector3>();
        private int index = -1;

        private Quaternion finalRotation = Quaternion.identity;
        private bool useFinalRotation = false;

        /// <summary>
        /// Called when this path is completed.
        /// <para/>
        /// Parameters are: 
        /// <br/> • A boolean indicating whether the path was fully completed, or prematurely stopped.
        /// <br/> • The associated <see cref="CreatureMovable3D"/>.
        /// </summary>
        public Action<bool, CreatureMovable3D> OnComplete = null;

        // -----------------------

        /// <inheritdoc cref="IHandle.ID"/>
        public int ID {
            get { return id; }
        }

        /// <summary>
        /// Current state of this path.
        /// </summary>
        public State Status {
            get { return state; }
        }

        /// <summary>
        /// Index of the current path destination position.
        /// </summary>
        public int Index {
            get { return index; }
        }

        /// <summary>
        /// Indicate whether this path is currently active or not.
        /// </summary>
        public bool IsActive {
            get { return (index != -1) && (state != State.Inactive); }
        }
        #endregion

        #region Path
        /// <summary>
        /// Maximum distance used to sample the nav mesh data for navigation.
        /// </summary>
        public const float MaxNavMeshDistance = 9f;

        private static NavMeshPath navMeshPath = null; // Constructor is not allowed to be called on initialization.
        private static Vector3[] pathBuffer    = new Vector3[16];
        private static int lastID = 0;

        // -----------------------

        /// <summary>
        /// Set this navigation path destination position.
        /// </summary>
        /// <param name="_movable">Object to set this navigation path for.</param>
        /// <param name="_destination">Destination position of this path.</param>
        /// <param name="_onComplete"><inheritdoc cref="OnComplete" path="/summary"/></param>
        /// <returns><see cref="PathHandler"/> of this navigation operation.</returns>
        internal PathHandler Setup(CreatureMovable3D _movable, Vector3 _destination, Action<bool, CreatureMovable3D> _onComplete = null) {
            // Cancel previous operation.
            Cancel();

            navMeshPath.ClearCorners();
            path.Clear();

            // Calculate path.
            if (NavMesh.SamplePosition(_destination, out NavMeshHit _hit, MaxNavMeshDistance, NavMesh.AllAreas)
             && NavMesh.CalculatePath(_movable.Transform.position, _hit.position, NavMesh.AllAreas, navMeshPath)) {

                int _pathCount = GetCorners();

                // Reallocates buffer size while path result is too large to fit in.
                while (_pathCount == pathBuffer.Length) {

                    Array.Resize(ref pathBuffer, _pathCount * 2);
                    _pathCount = GetCorners();
                }

                for (int i = 0; i < _pathCount; i++) {
                    path.Add(pathBuffer[i]);
                }
            } else {

                path.Add(_destination);
                this.LogWarningMessage("Path calcul could not be performed");
            }

            index = 0;
            return CreateHandler(_movable, _onComplete);

            // ----- Local Method ----- \\

            static int GetCorners() {
                return navMeshPath.GetCornersNonAlloc(pathBuffer);
            }
        }

        /// <summary>
        /// Set all this navigation path positions and destination.
        /// </summary>
        /// <param name="_path">All positions to initialize this path with.</param>
        /// <inheritdoc cref="Setup(CreatureMovable3D, Vector3, Action{bool, CreatureMovable3D})"/>
        internal PathHandler Setup(CreatureMovable3D _movable, Vector3[] _path, Action<bool, CreatureMovable3D> _onComplete = null) {

            // Cancel previous operation.
            Cancel();

            path.Clear();
            path.AddRange(_path);

            index = (_path.Length != 0) ? 0 : -1;
            return CreateHandler(_movable, _onComplete);
        }

        /// <param name="_finalRotation">Final rotation of the object.</param>
        /// <inheritdoc cref="Setup(CreatureMovable3D, Vector3, Action{bool, CreatureMovable3D})"/>
        internal PathHandler Setup(CreatureMovable3D _movable, Vector3 _destination, Quaternion _finalRotation, Action<bool, CreatureMovable3D> _onComplete = null) {
            PathHandler _handler = Setup(_movable, _destination, _onComplete);
            SetFinalRotation(_finalRotation);

            return _handler;
        }

        /// <param name="_finalRotation">Final rotation of the object.</param>
        /// <inheritdoc cref="Setup(CreatureMovable3D, Vector3[], Action{bool, CreatureMovable3D})"/>
        internal PathHandler Setup(CreatureMovable3D _movable, Vector3[] _path, Quaternion _finalRotation, Action<bool, CreatureMovable3D> _onComplete = null) {
            PathHandler _handler = Setup(_movable, _path, _onComplete);
            SetFinalRotation(_finalRotation);

            return _handler;
        }

        /// <param name="_destination">Destination position and rotation of this path.</param>
        /// <param name="_useRotation">Whether to rotate the object in the transform direction on completion or not.</param>
        /// <inheritdoc cref="Setup(CreatureMovable3D, Vector3, Action{bool, CreatureMovable3D})"/>
        internal PathHandler Setup(CreatureMovable3D _movable, Transform _destination, bool _useRotation, Action<bool, CreatureMovable3D> _onComplete = null) {
            PathHandler _handler = Setup(_movable, _destination.position, _onComplete);

            if (_useRotation) {
                SetFinalRotation(_destination.rotation);
            }

            return _handler;
        }

        // -------------------------------------------
        // Behaviour
        // -------------------------------------------

        /// <summary>
        /// Pauses this path current navigation.
        /// </summary>
        public bool Pause() {

            // Ignore if not active.
            if (state != State.Active) {
                return false;
            }

            SetState(State.Paused);
            return true;
        }

        /// <summary>
        /// Resumes this path current navigation.
        /// </summary>
        public bool Resume() {

            // Ignore if not paused.
            if (state != State.Paused) {
                return false;
            }

            SetState(State.Active);
            return true;
        }

        /// <summary>
        /// Cancels this path current navigation.
        /// </summary>
        public bool Cancel() {
            return Stop(false);
        }

        /// <summary>
        /// Completes this path current navigation.
        /// </summary>
        public bool Complete() {

            // Ignore if already inactive.
            if (state == State.Inactive) {
                return false;
            }

            // Teleport to destination.
            if (path.SafeLast(out Vector3 _position)) {

                if (useFinalRotation) {
                    movable.SetPositionAndRotation(_position, finalRotation);
                } else {
                    movable.SetPosition(_position);
                }
            }

            Stop(true);
            return true;
        }

        // -------------------------------------------
        // Internal
        // -------------------------------------------

        private PathHandler CreateHandler(CreatureMovable3D _movable, Action<bool, CreatureMovable3D> _onComplete) {
            OnComplete = _onComplete;
            movable    = _movable;

            SetState(State.Active);

            id = ++lastID;
            return new PathHandler(this, id);
        }

        private void SetFinalRotation(Quaternion _rotation) {
            useFinalRotation = true;
            finalRotation    = _rotation;
        }

        private bool Stop(bool _isCompleted) {

            // Ignore if already inactive.
            if (state == State.Inactive) {
                return false;
            }

            // State.
            SetState(State.Inactive);
            id = 0;

            index = -1;
            useFinalRotation = false;
            movable.OnCompleteNavigation(_isCompleted);

            // Callback.
            OnComplete?.Invoke(_isCompleted, movable);

            OnComplete = null;
            movable    = null;

            NavigationPath3DManager.Release(this);
            return true;
        }
        #endregion

        #region Navigation
        /// <summary>
        /// Minimum distance from the current destination to be considered as reached, on the Y axis.
        /// </summary>
        public const float MinVerticalDestinationDistance = .25f;

        /// <summary>
        /// Minimum distance from the current destination to be considered as reached, on the X and Z axises.
        /// </summary>
        public const float MinFlatDestinationDistance     = .01f;

        // -----------------------

        /// <summary>
        /// Update this path.
        /// </summary>
        /// <returns><inheritdoc cref="GetNextDistance(out Vector3)" path="/returns"/></returns>
        public bool UpdatePath() {
            if (!GetNextDistance(out Vector3 _distance)) {
                return false;
            }

            _distance = movable.GetRelativeVector(_distance);

            if ((_distance.Flat().magnitude <= MinFlatDestinationDistance) && (Mathf.Abs(_distance.y) <= MinVerticalDestinationDistance)) {

                // Path end.
                if (index == (path.Count - 1)) {

                    // Wait for completion.
                    movable.DoCompleteNavigation(out bool _completed);
                    if (!_completed) {
                        return true;
                    }

                    // Final rotation.
                    if (useFinalRotation) {
                        movable.TurnTo(finalRotation.ToDirection());
                    }

                    Stop(true);
                    return false;
                }

                index++;
            }

            return true;
        }

        /// <summary>
        /// Get the next target position on this path.
        /// </summary>
        /// <param name="_position">Next destination position on the path.</param>
        /// <returns>True if the path is active and a new destination was found, false otherwise.</returns>
        public bool GetNextPosition(out Vector3 _position) {
            if (!IsActive) {
                _position = Vector3.zero;
                return false;
            }

            _position = path[index];
            return true;
        }

        /// <summary>
        /// Get the distance between an object and the next desination position.
        /// </summary>
        /// <param name="_distance">Distance between the object and its next destination position, not normalized.</param>
        /// <returns>True if the path is active and a new destination was found, false otherwise.</returns>
        public bool GetNextDistance(out Vector3 _distance) {
            if (!IsActive) {
                _distance = Vector3.zero;
                return false;
            }

            // Paused state.
            if (state == State.Paused) {
                _distance = Vector3.zero;
                return true;
            }

            Transform _transform = movable.Transform;
            _distance = path[index] - _transform.position;

            return true;
        }

        /// <summary>
        /// Get the direction to the next destination position.
        /// <para/>
        /// Similar to <see cref="GetNextDistance(out Vector3)"/>,
        /// but only on the object X and Z axises.
        /// </summary>
        /// <param name="_direction">Direction to the next destination position, not normalized.</param>
        /// <returns><inheritdoc cref="GetNextDistance(out Vector3)" path="/returns"/></returns>
        public bool GetNextDirection(out Vector3 _direction) {
            if (!GetNextDistance(out _direction)) {
                return false;
            }

            // Removes the vertical component of the direction.
            Quaternion _rotation = movable.Transform.rotation;
            _direction = _direction.RotateInverse(_rotation).SetY(0f).Rotate(_rotation);

            return true;
        }
        #endregion

        #region Pool
        void IPoolableObject.OnCreated(IObjectPool _pool) {

            // Create nav mesh helper.
            navMeshPath ??= new NavMeshPath();
        }

        void IPoolableObject.OnRemovedFromPool() { }

        void IPoolableObject.OnSentToPool() {

            // Make sure the navigation is not active.
            Cancel();
        }
        #endregion

        #region Utility
        /// <summary>
        /// Sets the state of this object.
        /// </summary>
        /// <param name="_state">New state of this object.</param>
        private void SetState(State _state) {
            state = _state;
        }
        #endregion
    }

    /// <summary>
    /// <see cref="NavigationPath3D"/> pool managing class.
    /// </summary>
    internal sealed class NavigationPath3DManager : IObjectPoolManager<NavigationPath3D> {
        #region Pool
        private static readonly ObjectPool<NavigationPath3D> pool = new ObjectPool<NavigationPath3D>(1);
        public static readonly NavigationPath3DManager Instance   = new NavigationPath3DManager();

        /// <inheritdoc cref="NavigationPath3DManager"/>
        private NavigationPath3DManager() {

            // Pool initialization.
            pool.Initialize(this);
        }

        // -----------------------

        /// <summary>
        /// Get a <see cref="NavigationPath3D"/> instance from the pool.
        /// </summary>
        /// <inheritdoc cref="ObjectPool{T}.GetPoolInstance"/>
        public static NavigationPath3D Get() {
            return pool.GetPoolInstance();
        }

        /// <summary>
        /// Releases a specific <see cref="NavigationPath3D"/> instance and sent it back to the pool.
        /// </summary>
        /// <inheritdoc cref="ObjectPool{T}.ReleasePoolInstance(T)"/>
        public static bool Release(NavigationPath3D _call) {
            return pool.ReleasePoolInstance(_call);
        }

        /// <summary>
        /// Clears the <see cref="NavigationPath3D"/> pool content.
        /// </summary>
        /// <inheritdoc cref="ObjectPool{T}.ClearPool(int)"/>
        public static void ClearPool(int _capacity = 1) {
            pool.ClearPool(_capacity);
        }

        // -------------------------------------------
        // Pool
        // -------------------------------------------

        NavigationPath3D IObjectPool<NavigationPath3D>.GetPoolInstance() {
            return Get();
        }

        bool IObjectPool<NavigationPath3D>.ReleasePoolInstance(NavigationPath3D _call) {
            return Release(_call);
        }

        void IObjectPool.ClearPool(int _capacity) {
            ClearPool(_capacity);
        }

        NavigationPath3D IObjectPoolManager<NavigationPath3D>.CreateInstance() {
            return new NavigationPath3D();
        }

        void IObjectPoolManager<NavigationPath3D>.DestroyInstance(NavigationPath3D _call) {
            // Cannot destroy instances, so simply ignore the object and wait for the garbage collector to pick it up.
        }
        #endregion
    }
}
