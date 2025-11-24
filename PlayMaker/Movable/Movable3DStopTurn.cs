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
    /// Base <see cref="FsmStateAction"/> used to stop the current turn operation of a <see cref="CreatureMovable3D"/>.
    /// </summary>
    public abstract class BaseMovable3DStopTurn : BaseCreatureMovable3DFSM {
        #region Behaviour
        public override void OnEnter() {
            base.OnEnter();

            Stop();
            Finish();
        }

        // -------------------------------------------
        // Behaviour
        // -------------------------------------------

        private void Stop() {
            if (GetMovable(out CreatureMovable3D _movable)) {
                _movable.StopTurnTo();
            }
        }
        #endregion
    }

    /// <summary>
    /// <see cref="FsmStateAction"/> used to stop the current turn operation of a <see cref="CreatureMovable3D"/>.
    /// </summary>
    [Tooltip("Stops the current turn operation of a Movable3D.")]
    [ActionCategory(CategoryName)]
    public sealed class Movable3DStopTurn : BaseMovable3DStopTurn {
        #region Global Members
        // -------------------------------------------
        // Movable
        // -------------------------------------------

        [Tooltip("The Movable instance to complete the turn operation.")]
        [RequiredField, ObjectType(typeof(CreatureMovable3D))]
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

        public override bool GetMovable(out CreatureMovable3D _movable) {

            if (Movable.Value is CreatureMovable3D _temp) {
                _movable = _temp;
                return true;
            }

            _movable = null;
            return false;
        }
        #endregion
    }
}
