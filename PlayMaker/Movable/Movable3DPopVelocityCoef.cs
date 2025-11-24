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
    /// Base <see cref="FsmStateAction"/> used to pop a velocity coefficient from a <see cref="Movable3D"/>.
    /// </summary>
    public abstract class BaseMovable3DPopVelocityCoef : BaseMovable3DFSM {
        #region Global Members
        // -------------------------------------------
        // Coefficient
        // -------------------------------------------

        [Tooltip("Id of the coefficient to pop - same as used to push it (safe to use with 0 as value).")]
        [RequiredField]
        public FsmInt Id;
        #endregion

        #region Behaviour
        public override void Reset() {
            base.Reset();

            Id = null;
        }

        public override void OnEnter() {
            base.OnEnter();

            Pop();
            Finish();
        }

        // -------------------------------------------
        // Behaviour
        // -------------------------------------------

        private void Pop() {

            if (GetMovable(out Movable3D _movable)) {
                _movable.PopVelocityCoef(BaseMovable3DPushVelocityCoef.GetVelocityId(Id.Value));
            }
        }
        #endregion
    }

    /// <summary>
    /// <see cref="FsmStateAction"/> used to pop a velocity coefficient from a <see cref="Movable3D"/>.
    /// </summary>
    [Tooltip("Pops and remove a velocity coefficient from a Movable3D.")]
    [ActionCategory(CategoryName)]
    public sealed class Movable3DPopVelocityCoef : BaseMovable3DPopVelocityCoef {
        #region Global Members
        // -------------------------------------------
        // Movable
        // -------------------------------------------

        [Tooltip("The Movable instance to pop a velocity coefficient from.")]
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
