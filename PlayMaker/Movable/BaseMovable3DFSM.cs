// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using HutongGames.PlayMaker;

namespace EnhancedFramework.Physics3D.PlayMaker {
    /// <summary>
    /// Base <see cref="FsmStateAction"/> for a <see cref="Movable3D"/>.
    /// </summary>
    public abstract class BaseMovable3DFSM : FsmStateAction {
        #region Global Members
        public const string CategoryName = "Movable [3D]";

        // -----------------------

        /// <summary>
        /// Get this fsm movable instance.
        /// </summary>
        /// <param name="_movable">Movable instance.</param>
        /// <returns>True if the movable could be successfully retrieved, false otherwise.</returns>
        public abstract bool GetMovable(out Movable3D _movable);
        #endregion
    }

    /// <summary>
    /// Base <see cref="FsmStateAction"/> for a <see cref="CreatureMovable3D"/>.
    /// </summary>
    public abstract class BaseCreatureMovable3DFSM : FsmStateAction {
        #region Global Members
        public const string CategoryName = BaseMovable3DFSM.CategoryName;

        // -----------------------

        /// <summary>
        /// Get this fsm movable instance.
        /// </summary>
        /// <param name="_movable">Movable instance.</param>
        /// <returns>True if the movable could be successfully retrieved, false otherwise.</returns>
        public abstract bool GetMovable(out CreatureMovable3D _movable);
        #endregion
    }
}
