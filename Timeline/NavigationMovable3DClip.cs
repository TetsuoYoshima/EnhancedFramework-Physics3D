// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

using EnhancedEditor;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;

using DisplayName = System.ComponentModel.DisplayNameAttribute;

namespace EnhancedFramework.Physics3D.Timeline {
    /// <summary>
    /// Makes a <see cref="CreatureMovable3D"/> navigate to a specific position.
    /// </summary>
    [DisplayName(NamePrefix + "Navigation")]
    public sealed class NavigationMovable3DClip : CreatureMovable3DPlayableAsset<NavigationMovable3DBehaviour> {
        #region Global Members
        [Space(10f)]

        public ExposedReference<Transform> Position = new ExposedReference<Transform>();
        #endregion

        #region Behaviour
        public override Playable CreatePlayable(PlayableGraph _graph, GameObject _owner) {
            Template.Position = Position.Resolve(_graph.GetResolver());
            return base.CreatePlayable(_graph, _owner);
        }
        #endregion

        #region Utility
        public override string ClipDefaultName {
            get { return "Navigation Creature"; }
        }
        #endregion
    }

    /// <summary>
    /// <see cref="NavigationMovable3DClip"/>-related <see cref="PlayableBehaviour"/>.
    /// </summary>
    [Serializable]
    public class NavigationMovable3DBehaviour : CreatureMovable3DPlayableBehaviour {
        #region Global Members
        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f)]

        [Tooltip("Whether to use target Transform rotation or not")]
        public bool UseRotation = true;

        [Tooltip("If true, completes the navigation path when exiting this clip")]
        public bool CompleteOnExit = true;

        // -----------------------

        [NonSerialized] public Transform Position = null;
        #endregion

        #region Behaviour
        #if UNITY_EDITOR
        // Editor preview cache.
        private Vector3 fromPosition = Vector3.zero;
        private Quaternion fromRotation = Quaternion.identity;
        #endif

        private PathHandler navigationPath = default;

        // -----------------------

        protected override void OnPlay(Playable _playable, FrameData _info) {
            base.OnPlay(_playable, _info);

            if (!IsValid()) {
                return;
            }

            #if UNITY_EDITOR
            if (!Application.isPlaying) {

                // Preview origin.
                if (fromPosition.IsNull()) {
                    Transform _transform = Movable.transform;

                    fromPosition = _transform.position;
                    fromRotation = _transform.rotation;
                }

                return;
            }
            #endif

            // Navigate.
            navigationPath = Movable.NavigateTo(Position, UseRotation);
        }

        public override void ProcessFrame(Playable _playable, FrameData _info, object _playerData) {
            base.ProcessFrame(_playable, _info, _playerData);

            #if UNITY_EDITOR
            if (!IsValid()) {
                return;
            }

            if (!Application.isPlaying) {

                // Navigation preview.
                Vector3 _position    = Vector3.Lerp   (fromPosition, Position.position, GetNormalizedTime(_playable));
                Quaternion _rotation = UseRotation
                                     ? Quaternion.Lerp(fromRotation, Position.rotation, GetNormalizedTime(_playable))
                                     : fromRotation;

                Movable.SetPositionAndRotation(_position, _rotation);
                return;
            }
            #endif
        }

        protected override void OnStop(Playable _playable, FrameData _info, bool _completed) {
            base.OnStop(_playable, _info, _completed);

            #if UNITY_EDITOR
            if (!Application.isPlaying) {

                if (!IsValid()) {
                    return;
                }

                // Complete preview.
                Vector3 _position       = _completed ? Position.position : fromPosition;
                Quaternion _rotation    = _completed ? Position.rotation : fromRotation;

                if (UseRotation) {
                    Movable.SetPositionAndRotation(_position, _rotation);
                } else {
                    Movable.SetPosition(_position);
                }

                return;
            }
            #endif

            // Complete navigation.
            if (CompleteOnExit) {
                navigationPath.Complete();
            }
        }

        // -------------------------------------------
        // Button
        // -------------------------------------------

        /// <summary>
        /// Get if this clip is valid.
        /// </summary>
        public bool IsValid() {
            return (Movable != null) && (Position != null);
        }
        #endregion
    }
}
