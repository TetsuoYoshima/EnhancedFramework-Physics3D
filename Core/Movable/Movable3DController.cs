// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedFramework.Core;
using System;
using UnityEngine;

namespace EnhancedFramework.Physics3D {
    #region Controllers
    // -------------------------------------------
    // Movable
    // -------------------------------------------

    /// <summary>
    /// Controller for a <see cref="Movable3D"/> colliders.
    /// </summary>
    public interface IMovable3DColliderController {
        /// <inheritdoc cref="Movable3D.GetColliderMask"/>
        /// <returns>-1 to use the movable default collision mask implementation, otherwise the collision mask to be used.</returns>
        int InitColliderMask(Collider _collider);

        /// <inheritdoc cref="Movable3D.GetTriggerMask"/>
        /// <returns><inheritdoc cref="InitColliderMask" path="/returns"/></returns>
        int InitTriggerMask(Collider _trigger);
    }

    /// <summary>
    /// Controller for a <see cref="Movable3D"/> velocity.
    /// </summary>
    public interface IMovable3DVelocityController {
        /// <inheritdoc cref="Movable3D.Move"/>
        bool Move(Vector3 _direction);

        /// <inheritdoc cref="Movable3D.ResetVelocity"/>
        bool OnResetVelocity(bool _force = false);

        /// <inheritdoc cref="Movable3D.ApplyGravity"/>
        bool OnApplyGravity();
    }

    /// <summary>
    /// Controller for a <see cref="Movable3D"/> update.
    /// </summary>
    public interface IMovable3DUpdateController {
        /// <inheritdoc cref="Movable3D.OnPreUpdate"/>
        void OnPreUpdate();

        /// <inheritdoc cref="Movable3D.OnPostUpdate"/>
        void OnPostUpdate();
    }

    /// <summary>
    /// Controller for a <see cref="Movable3D"/> computations.
    /// </summary>
    public interface IMovable3DComputationController {
        /// <inheritdoc cref="Movable3D.OnPreComputeVelocity"/>
        bool OnPreComputeVelocity(float _deltaTime, out float _speed);

        /// <param name="_velocity">Actual velocity of the object</param>
        /// <param name="_frameVelocity"><inheritdoc cref="Movable3D.ComputeVelocity" path="/returns"/></param>
        /// <inheritdoc cref="Movable3D.ComputeVelocity"/>
        bool OnComputeVelocity(Velocity _velocity, float _speed, float _deltaTime, ref FrameVelocity _frameVelocity);

        /// <inheritdoc cref="Movable3D.OnPostComputeVelocity"/>
        bool OnPostComputeVelocity(float _deltaTime, ref FrameVelocity _frameVelocity);
    }

    /// <summary>
    /// Controller for a <see cref="Movable3D"/> collisions.
    /// </summary>
    public interface IMovable3DCollisionController {
        /// <inheritdoc cref="Movable3D.SetGroundState"/>
        bool OnSetGroundState(ref bool _isGrounded, RaycastHit _hit);

        /// <inheritdoc cref="Movable3D.OnAppliedVelocity"/>
        bool OnAppliedVelocity(CollisionOperationData3D _operation);

        /// <inheritdoc cref="Movable3D.OnRefreshedObject"/>
        bool OnRefreshedObject(CollisionOperationData3D _operation);

        /// <inheritdoc cref="Movable3D.OnGrounded"/>
        bool OnGrounded(bool _isGrounded);

        /// <inheritdoc cref="Movable3D.OnExtractFromCollider"/>
        bool OnExtractFromCollider(Collider _colliderA, Collider _colliderB, Vector3 _direction, float _distance);

        /// <inheritdoc cref="Movable3D.OnHitByMovable"/>
        bool OnHitByMovable(Movable3D _other, CollisionHit3D _collision);
    }

    /// <summary>
    /// Controller for a <see cref="Movable3D"/> trigger.
    /// </summary>
    public interface IMovable3DTriggerController {
        /// <inheritdoc cref="Movable3D.OnEnterTrigger"/>
        void OnEnterTrigger(ITrigger _trigger);

        /// <inheritdoc cref="Movable3D.OnExitTrigger"/>
        void OnExitTrigger(ITrigger _trigger);
    }

    // -------------------------------------------
    // Creature
    // -------------------------------------------

    /// <summary>
    /// Controller for an <see cref="CreatureMovable3D"/> speed.
    /// </summary>
    public interface ICreatureMovable3DSpeedController {
        /// <inheritdoc cref="CreatureMovable3D.UpdateSpeed"/>
        bool OnUpdateSpeed(ref float _speed);

        /// <inheritdoc cref="CreatureMovable3D.IncreaseSpeed"/>
        bool OnIncreaseSpeed(ref float _speed);

        /// <inheritdoc cref="CreatureMovable3D.DecreaseSpeed"/>
        bool OnDecreaseSpeed(ref float _speed);

        /// <inheritdoc cref="CreatureMovable3D.ResetSpeed"/>
        bool OnResetSpeed(bool _isComputeVelocityCallback);
    }

    /// <summary>
    /// Controller for an <see cref="CreatureMovable3D"/> rotation.
    /// </summary>
    public interface ICreatureMovable3DRotationController {
        /// <inheritdoc cref="CreatureMovable3D.Turn"/>
        bool OnTurn(ref float _angleIncrement);

        /// <inheritdoc cref="CreatureMovable3D.TurnTo(Vector3, Action)"/>
        bool OnTurnTo(Vector3 _forward, Action _onComplete);

        /// <inheritdoc cref="CreatureMovable3D.StopTurnTo"/>
        void OnCompleteTurnTo(bool _reset);
    }

    /// Controller for an <see cref="CreatureMovable3D"/> navigation path callbacks.
    /// </summary>
    public interface ICreatureMovable3DNavigationController {
        /// <inheritdoc cref="CreatureMovable3D.DoCompleteNavigation"/>
        bool CompletePath(out bool _completed);

        /// <inheritdoc cref="CreatureMovable3D.SetNavigationPath"/>
        void OnNavigateTo(PathHandler _path);

        /// <inheritdoc cref="CreatureMovable3D.OnCompleteNavigation"/>
        void OnCompleteNavigation(bool _success);
    }
    #endregion

    /// <summary>
    /// Default controller used when no other controller is specified.
    /// </summary>
    internal sealed class DefaultMovable3DController : IMovable3DColliderController,      IMovable3DVelocityController,         IMovable3DUpdateController,
                                                       IMovable3DComputationController,   IMovable3DCollisionController,        IMovable3DTriggerController,
                                                       ICreatureMovable3DSpeedController, ICreatureMovable3DRotationController, ICreatureMovable3DNavigationController {
        #region Instance
        /// <summary>
        /// Static instance of this class.
        /// </summary>
        public static readonly DefaultMovable3DController Instance = new DefaultMovable3DController();
        #endregion

        // ----- Movable ----- \\

        #region Velocity
        public bool Move(Vector3 _direction) {
            return false;
        }

        public bool OnApplyGravity() {
            return false;
        }

        public bool OnResetVelocity(bool _force = false) {
            return false;
        }
        #endregion

        #region Update
        public void OnPreUpdate() { }

        public void OnPostUpdate() { }
        #endregion

        #region Computation
        public bool OnPreComputeVelocity(float _deltaTime, out float _speed) {
            _speed = 0f;
            return false;
        }

        public bool OnComputeVelocity(Velocity _velocity, float _speed, float _deltaTime, ref FrameVelocity _frameVelocity) {
            return false;
        }

        public bool OnPostComputeVelocity(float _deltaTime, ref FrameVelocity _frameVelocity) {
            return false;
        }
        #endregion

        #region Collision
        public bool OnAppliedVelocity(CollisionOperationData3D _operation) {
            return false;
        }

        public bool OnRefreshedObject(CollisionOperationData3D _operation) {
            return false;
        }

        public bool OnGrounded(bool _isGrounded) {
            return false;
        }

        public bool OnSetGroundState(ref bool _isGrounded, RaycastHit _hit) {
            return false;
        }

        public bool OnExtractFromCollider(Collider _colliderA, Collider _colliderB, Vector3 _direction, float _distance) {
            return false;
        }

        public bool OnHitByMovable(Movable3D _other, CollisionHit3D _collision) {
            return false;
        }
        #endregion

        #region Collider
        public int InitColliderMask(Collider _collider) {
            return -1;
        }

        public int InitTriggerMask(Collider _collider) {
            return -1;
        }
        #endregion

        #region Trigger
        public void OnEnterTrigger(ITrigger _trigger) { }

        public void OnExitTrigger(ITrigger _trigger) { }
        #endregion

        // ----- Creature ----- \\

        #region Speed
        public bool OnUpdateSpeed(ref float _speed) {
            return false;
        }

        public bool OnIncreaseSpeed(ref float _speed) {
            return false;
        }

        public bool OnDecreaseSpeed(ref float _speed) {
            return false;
        }

        public bool OnResetSpeed(bool _isComputeVelocityCallback) {
            return false;
        }
        #endregion

        #region Rotation
        public bool OnTurn(ref float _angleIncrement) {
            return false;
        }

        public bool OnTurnTo(Vector3 _forward, Action _onComplete) {
            return false;
        }

        public void OnCompleteTurnTo(bool _reset) { }
        #endregion

        #region Navigation
        public bool CompletePath(out bool _completed) {
            _completed = false;
            return false;
        }

        public void OnNavigateTo(PathHandler _path) { }

        public void OnCompleteNavigation(bool _success) { }
        #endregion
    }
}
