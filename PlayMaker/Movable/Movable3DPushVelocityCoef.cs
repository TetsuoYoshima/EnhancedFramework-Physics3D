// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using HutongGames.PlayMaker;
using UnityEngine;

using Tooltip = HutongGames.PlayMaker.TooltipAttribute;

namespace EnhancedFramework.Physics3D.PlayMaker {
    /// <summary>
    /// Base <see cref="FsmStateAction"/> used to push a velocity coefficient on a <see cref="Movable3D"/>.
    /// </summary>
    public abstract class BaseMovable3DPushVelocityCoef : BaseMovable3DFSM {
        #region Global Members
        // -------------------------------------------
        // Coefficient
        // -------------------------------------------

        [Tooltip("Velocity coefficient to push and apply.")]
        [RequiredField]
        public FsmFloat Coefficient;

        [Tooltip("Unique id associated with this coefficient - use the same to pop it (safe to use with 0 as value).")]
        [RequiredField]
        public FsmInt Id;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Coefficient = null;
            Id          = null;
        }

        public override void OnEnter() {
            base.OnEnter();

            Push();
            Finish();
        }

        // -------------------------------------------
        // Behaviour
        // -------------------------------------------

        private void Push() {
            if (GetMovable(out Movable3D _movable)) {
                _movable.PushVelocityCoef(GetVelocityId(Id.Value), Coefficient.Value);
            }
        }
        #endregion

        #region Utility
        public const int VelocityDefaultId = 7106843;

        // -----------------------

        public static int GetVelocityId(int _value) {
            return (_value != 0) ? _value : VelocityDefaultId;
        }
        #endregion
    }

    /// <summary>
    /// <see cref="FsmStateAction"/> used to push a velocity coefficient on a <see cref="Movable3D"/>.
    /// </summary>
    [Tooltip("Pushes and apply a velocity coefficient on a Movable3D.")]
    [ActionCategory(CategoryName)]
    public sealed class Movable3DPushVelocityCoef : BaseMovable3DPushVelocityCoef {
        #region Global Members
        // -------------------------------------------
        // Movable
        // -------------------------------------------

        [Tooltip("The Movable instance to push a velocity coefficient on.")]
        [RequiredField, ObjectType(typeof(Movable3D))]
        public FsmObject Movable;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Movable = null;
        }

        // -------------------------------------------
        // Behaviour
        // -------------------------------------------

        public override bool GetMovable(out Movable3D _movable) {

            if (Movable.Value is Movable3D _temp) {
                _movable = _temp;
                return true;
            }

            _movable = null;
            return false;
        }
        #endregion
    }
}
