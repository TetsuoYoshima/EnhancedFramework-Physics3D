// ===== Enhanced Framework - https://github.com/LucasJoestar/EnhancedFramework-Physics3D ===== //
//
// Notes:
//
// ============================================================================================ //

#if UNITY_2022_2_OR_NEWER
#define CAST_MULTI_HIT
#define OVERLAP_COMMANDS
#endif

using EnhancedEditor;
using EnhancedFramework.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

using ReadOnly = EnhancedEditor.ReadOnlyAttribute;

namespace EnhancedFramework.Physics3D {
    // ===== Wrappers ===== \\

    /// <summary>
    /// <see cref="Movable3D"/>-related wrapper used to store information about one of its collider.
    /// </summary>
    public struct Movable3DColliderWrapper {
        #region Content
        public Movable3D MovableInstance;
        public Collider Collider;

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        public Movable3DColliderWrapper(Movable3D _movable, Collider _collider) {
            MovableInstance = _movable;
            Collider = _collider;
        }
        #endregion
    }

    /// <summary>
    /// <see cref="Movable3D"/>-related wrapper for an extract operation.
    /// </summary>
    public struct ExtractOperation3D {
        #region Global Members
        public Movable3D MovableInstance;

        // -------------------------------------------
        // Constructor(s)
        // -------------------------------------------

        public ExtractOperation3D(Movable3D _movable) {
            MovableInstance = _movable;
        }
        #endregion
    }

    /// <summary>
    /// <see cref="Movable3D"/>-related physics collision operation data wrapper.
    /// </summary>
    public sealed class CollisionOperationData3D {
        #region Global Members
        public Physics3DSettings PhysicsSettings = null;
        public CollisionData3D Data = new CollisionData3D();

        internal Movable3DPhysicsWrapper PhysicsWrapper = null;

        public Movable3D MovableInstance = null;
        public FrameVelocity Velocity    = default;

        public Quaternion OriginRotation = Quaternion.identity;
        public Vector3    OriginPosition = Vector3.zero;

        public bool PerformCollisions = false;
        public int  Recursivity       = 0;

        public List<Collider> SelfColliders = new List<Collider>();
        #endregion

        #region Behaviour
        /// <summary>
        /// Initializes this operation content.
        /// </summary>
        public void Init(Physics3DSettings _physicsSettings, Movable3D _movable, Vector3 _position, Quaternion _rotation, FrameVelocity _frameVelocity, bool _performCollisions, IList<Collider> _selfColliders) {
            PhysicsSettings = _physicsSettings;

            PerformCollisions = _performCollisions;
            MovableInstance   = _movable;
            Velocity          = _frameVelocity;

            OriginPosition = _position;
            OriginRotation = _rotation;

            PhysicsWrapper = null;
            Recursivity    = 0;

            Data.Init(_movable, _frameVelocity);
            SelfColliders.ReplaceBy(_selfColliders);
        }

        /// <summary>
        /// Setups this operation for a given <see cref="Movable3DPhysicsWrapper"/>.
        /// </summary>
        internal void Setup(Movable3DPhysicsWrapper _physicsWrapper, FrameVelocity _frameVelocity, int _recursivity) {
            PhysicsWrapper = _physicsWrapper;
            Recursivity    = _recursivity;

            Velocity = _frameVelocity;
            Data.Init(MovableInstance, _frameVelocity);

            _physicsWrapper.Setup(MovableInstance, this);
        }

        /// <summary>
        /// Updates this data applied velocity and rotation based on a given a current position and rotation.
        /// </summary>
        /// <param name="_position">Current position used to update values.</param>
        /// <param name="_rotation">Current rotation used to update values.</param>
        public void UpdateAppliedData(Vector3 _position, Quaternion _rotation) {
            Data.SetAppliedData(_position - OriginPosition, Quaternion.Inverse(OriginRotation) * _rotation);
        }

        /// <summary>
        /// Clears all temporary hits from this operation buffer.
        /// </summary>
        public void ClearTempHits() {
            Data.ClearTempHits();
        }
        #endregion
    }

    // ===== Commands ===== \\

    /// <summary>
    /// Utility class used to batch and perform <see cref="Movable3D"/>-related cast operations.
    /// </summary>
    public sealed class CastOperationCommands3D {
        #region Content
        private readonly List<Movable3DColliderWrapper> capsuleColliders = new List<Movable3DColliderWrapper>();
        private readonly List<Movable3DColliderWrapper> sphereColliders  = new List<Movable3DColliderWrapper>();
        private readonly List<Movable3DColliderWrapper> boxColliders     = new List<Movable3DColliderWrapper>();

        private readonly List<CapsulecastCommand> capsuleCommands   = new List<CapsulecastCommand>();
        private readonly List<SpherecastCommand> sphereCommands     = new List<SpherecastCommand>();
        private readonly List<BoxcastCommand> boxCommands           = new List<BoxcastCommand>();

        private NativeArray<CapsulecastCommand> capsuleCommandArray = default;
        private NativeArray<SpherecastCommand>  sphereCommandArray  = default;
        private NativeArray<BoxcastCommand>     boxCommandArray     = default;

        private NativeArray<RaycastHit> capsuleResults = default;
        private NativeArray<RaycastHit> sphereResults  = default;
        private NativeArray<RaycastHit> boxResults     = default;

        private RaycastHit[] resultBuffer = new RaycastHit[32];

        private readonly List<JobHandle> jobs = new List<JobHandle>();
        private int maxHits = 0;

        // -------------------------------------------
        // Behaviour
        // -------------------------------------------

        public void ScheduleBatch(int _minCommandPerJob, int _maxHits) {
            List<JobHandle> _jobBuffer = jobs;
            _jobBuffer.Clear();

            maxHits = _maxHits;

            // Capsules.
            List<CapsulecastCommand> _capsuleCommands = capsuleCommands;
            int _count = _capsuleCommands.Count;

            if (_count != 0) {

                capsuleCommandArray = new NativeArray<CapsulecastCommand>(_count,    Allocator.TempJob);
                capsuleResults      = new NativeArray<RaycastHit>(_count * _maxHits, Allocator.TempJob);

                ref var _commands = ref capsuleCommandArray;
                for (int i = _count; i-- > 0;) {
                    _commands[i] = _capsuleCommands[i];
                }

                JobHandle _job = CapsulecastCommand.ScheduleBatch(_commands, capsuleResults, _minCommandPerJob
                                #if CAST_MULTI_HIT
                                , _maxHits
                                #endif
                                );

                _jobBuffer.Add(_job);
            }

            // Sphere.
            List<SpherecastCommand> _sphereCommands = sphereCommands;
            _count = _sphereCommands.Count;

            if (_count != 0) {

                sphereCommandArray = new NativeArray<SpherecastCommand>(_count,     Allocator.TempJob);
                sphereResults      = new NativeArray<RaycastHit>(_count * _maxHits, Allocator.TempJob);

                ref var _commands = ref sphereCommandArray;
                for (int i = _count; i-- > 0;) {
                    _commands[i] = _sphereCommands[i];
                }

                JobHandle _job = SpherecastCommand.ScheduleBatch(_commands, sphereResults, _minCommandPerJob
                                #if CAST_MULTI_HIT
                                , _maxHits
                                #endif
                                );

                _jobBuffer.Add(_job);
            }

            // Box.
            List<BoxcastCommand> _boxCommands = boxCommands;
            _count = _boxCommands.Count;

            if (_count != 0) {

                boxCommandArray = new NativeArray<BoxcastCommand>(_count,        Allocator.TempJob);
                boxResults      = new NativeArray<RaycastHit>(_count * _maxHits, Allocator.TempJob);

                ref var _commands = ref boxCommandArray;
                for (int i = _count; i-- > 0;) {
                    _commands[i] = _boxCommands[i];
                }

                JobHandle _job = BoxcastCommand.ScheduleBatch(_commands, boxResults, _minCommandPerJob
                                #if CAST_MULTI_HIT
                                , _maxHits
                                #endif
                                );

                _jobBuffer.Add(_job);
            }
        }

        public void CompleteJobs() {
            // Complete jobs.
            int _count = jobs.Count;

            for (int i = 0; i < _count; i++) {
                jobs[i].Complete();
            }

            int _resultCount = maxHits;

            List<Movable3DColliderWrapper> _wrapperSpan;
            ref RaycastHit[] _buffer = ref resultBuffer;

            // Capsules.
            _wrapperSpan = capsuleColliders;
            _count = _wrapperSpan.Count;

            if (_count != 0) {

                // NativeArray is faster to copy and use the destination array to iterate than to directly iterate over.
                ArrayUtility.Realloc(ref _buffer, capsuleResults.Length);
                NativeArray<RaycastHit>.Copy(capsuleResults, _buffer);

                for (int i = _count; i-- > 0;) {
                    Movable3DColliderWrapper _wrapper = _wrapperSpan[i];
                    int _startIndex = i * _resultCount;

                    Movable3DManager.OnCollision(_wrapper.MovableInstance, _wrapper.Collider, _buffer, _startIndex, _resultCount);
                }
            }

            // Spheres.
            _wrapperSpan = sphereColliders;
            _count = _wrapperSpan.Count;

            if (_count != 0) {

                ArrayUtility.Realloc(ref _buffer, sphereResults.Length);
                NativeArray<RaycastHit>.Copy(sphereResults, _buffer);

                for (int i = _count; i-- > 0;) {
                    Movable3DColliderWrapper _wrapper = _wrapperSpan[i];
                    int _startIndex = i * _resultCount;

                    Movable3DManager.OnCollision(_wrapper.MovableInstance, _wrapper.Collider, _buffer, _startIndex, _resultCount);
                }
            }

            // Boxes.
            _wrapperSpan = boxColliders;
            _count = _wrapperSpan.Count;

            if (_count != 0) {

                ArrayUtility.Realloc(ref _buffer, boxResults.Length);
                NativeArray<RaycastHit>.Copy(boxResults, _buffer);

                for (int i = _count; i-- > 0;) {
                    Movable3DColliderWrapper _wrapper = _wrapperSpan[i];
                    int _startIndex = i * _resultCount;

                    Movable3DManager.OnCollision(_wrapper.MovableInstance, _wrapper.Collider, _buffer, _startIndex, _resultCount);
                }
            }

            // Complete.
            Clear();
        }

        public void Clear() {
            // Clear all content.
            capsuleColliders.Clear();
            sphereColliders .Clear();
            boxColliders    .Clear();

            capsuleCommands.Clear();
            sphereCommands .Clear();
            boxCommands    .Clear();

            jobs.Clear();

            // Dispose TempJobs allocations and release memory.
            if (capsuleResults.IsCreated) {
                capsuleCommandArray.Dispose();
                capsuleResults.Dispose();
            }

            if (sphereResults.IsCreated) {
                sphereCommandArray.Dispose();
                sphereResults.Dispose();
            }

            if (boxResults.IsCreated) {
                boxCommandArray.Dispose();
                boxResults.Dispose();
            }
        }

        // -------------------------------------------
        // Registration
        // -------------------------------------------

        public void RegisterCommand(Movable3D _movable, Collider _collider, Vector3 _direction, float _distance, int _mask) {
            const QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;

            List<Movable3DColliderWrapper> _wrapperBuffer;
            ColliderWrapper3D _wrapper;

            try {
                if (!_collider.enabled)
                    return;

                _wrapper = ColliderWrapper3D.Get(_collider);

            } catch (Exception e) {

                if ((e is MissingReferenceException) || (e is NullReferenceException)) {
                    Debug.LogException(e);
                    return;
                }

                throw;
            }

            float _contactOffset = _collider.contactOffset;
            _distance += _contactOffset * 2f;

            switch (_wrapper.ColliderTypeId) {
                // Capsule.
                case 1:
                    CapsulecastCommand _capsuleCommand = _wrapper.GetCapsuleCastCommand(_direction, _distance, _mask, TriggerInteraction);
                    capsuleCommands.Add(_capsuleCommand);

                    _wrapperBuffer = capsuleColliders;
                    break;

                // Sphere.
                case 2:
                    SpherecastCommand _sphereCommand = _wrapper.GetSphereCastCommand(_direction, _distance, _mask, TriggerInteraction);
                    sphereCommands.Add(_sphereCommand);

                    _wrapperBuffer = sphereColliders;
                    break;

                // Box.
                case 3:
                    BoxcastCommand _boxCommand = _wrapper.GetBoxCastCommand(_direction, _distance, _mask, TriggerInteraction);
                    boxCommands.Add(_boxCommand);

                    _wrapperBuffer = boxColliders;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            _wrapperBuffer.Add(new Movable3DColliderWrapper(_movable, _collider));
        }
        #endregion
    }

    /// <summary>
    /// Utility class used to batch and perform <see cref="Movable3D"/>-related overlap operations.
    /// </summary>
    public sealed class OverlapOperationCommands3D {
        #region Content
        #if CAST_MULTI_HIT
        private readonly List<Movable3DColliderWrapper> capsuleColliders = new List<Movable3DColliderWrapper>();
        private readonly List<Movable3DColliderWrapper> sphereColliders  = new List<Movable3DColliderWrapper>();
        private readonly List<Movable3DColliderWrapper> boxColliders     = new List<Movable3DColliderWrapper>();

        private readonly List<OverlapCapsuleCommand> capsuleCommands   = new List<OverlapCapsuleCommand>();
        private readonly List<OverlapSphereCommand> sphereCommands     = new List<OverlapSphereCommand>();
        private readonly List<OverlapBoxCommand> boxCommands           = new List<OverlapBoxCommand>();

        private NativeArray<OverlapCapsuleCommand> capsuleCommandArray = default;
        private NativeArray<OverlapSphereCommand>  sphereCommandArray  = default;
        private NativeArray<OverlapBoxCommand>     boxCommandArray     = default;

        private NativeArray<ColliderHit> capsuleResults = default;
        private NativeArray<ColliderHit> sphereResults  = default;
        private NativeArray<ColliderHit> boxResults     = default;

        private ColliderHit[] resultBuffer = new ColliderHit[32];

        private readonly List<JobHandle> jobs = new List<JobHandle>();
        private int maxHits = 0;

        // -------------------------------------------
        // Behaviour
        // -------------------------------------------

        public void ScheduleBatch(int _minCommandsPerJob, int _maxHits) {
            List<JobHandle> _jobBuffer = jobs;
            _jobBuffer.Clear();

            maxHits = _maxHits;

            // Capsules.
            List<OverlapCapsuleCommand> _capsuleCommands = capsuleCommands;
            int _count = _capsuleCommands.Count;

            if (_count != 0) {

                capsuleCommandArray = new NativeArray<OverlapCapsuleCommand>(_count,  Allocator.TempJob);
                capsuleResults      = new NativeArray<ColliderHit>(_count * _maxHits, Allocator.TempJob);

                ref var _commands = ref capsuleCommandArray;
                for (int i = _count; i-- > 0;) {
                    _commands[i] = _capsuleCommands[i];
                }

                JobHandle _job = OverlapCapsuleCommand.ScheduleBatch(_commands, capsuleResults, _minCommandsPerJob, _maxHits);
                _jobBuffer.Add(_job);
            }

            // Sphere.
            List<OverlapSphereCommand> _sphereCommands = sphereCommands;
            _count = _sphereCommands.Count;

            if (_count != 0) {

                sphereCommandArray = new NativeArray<OverlapSphereCommand>(_count,   Allocator.TempJob);
                sphereResults      = new NativeArray<ColliderHit>(_count * _maxHits, Allocator.TempJob);

                ref var _commands = ref sphereCommandArray;
                for (int i = _count; i-- > 0;) {
                    _commands[i] = _sphereCommands[i];
                }

                JobHandle _job = OverlapSphereCommand.ScheduleBatch(_commands, sphereResults, _minCommandsPerJob, _maxHits);
                _jobBuffer.Add(_job);
            }

            // Box.
            List<OverlapBoxCommand> _boxCommands = boxCommands;
            _count = boxCommands.Count;

            if (_count != 0) {
                boxCommandArray = new NativeArray<OverlapBoxCommand>(_count,      Allocator.TempJob);
                boxResults      = new NativeArray<ColliderHit>(_count * _maxHits, Allocator.TempJob);

                ref var _commands = ref boxCommandArray;
                for (int i = _count; i-- > 0;) {
                    _commands[i] = _boxCommands[i];
                }

                JobHandle _job = OverlapBoxCommand.ScheduleBatch(_commands, boxResults, _minCommandsPerJob, _maxHits);
                _jobBuffer.Add(_job);
            }
        }

        public void CompleteJobs() {

            // Complete jobs.
            int _count = jobs.Count;

            for (int i = 0; i < _count; i++) {
                jobs[i].Complete();
            }

            int _resultCount = maxHits;

            List<Movable3DColliderWrapper> _wrapperSpan;
            ref ColliderHit[] _buffer = ref resultBuffer;

            // Capsules.
            _wrapperSpan = capsuleColliders;
            _count = _wrapperSpan.Count;

            if (_count != 0) {

                // NativeArray is faster to copy and use the destination array to iterate than to directly iterate over.
                ArrayUtility.Realloc(ref _buffer, capsuleResults.Length);
                NativeArray<ColliderHit>.Copy(capsuleResults, _buffer);

                for (int i = _count; i-- > 0;) {
                    Movable3DColliderWrapper _wrapper = _wrapperSpan[i];
                    int _startIndex = i * _resultCount;

                    Movable3DManager.OnExtractOverlap(_wrapper.MovableInstance, _wrapper.Collider, _buffer, _startIndex, _resultCount);
                }
            }

            // Spheres.
            _wrapperSpan = sphereColliders;
            _count = _wrapperSpan.Count;

            if (_count != 0) {

                ArrayUtility.Realloc(ref _buffer, sphereResults.Length);
                NativeArray<ColliderHit>.Copy(sphereResults, _buffer);

                for (int i = _count; i-- > 0;) {
                    Movable3DColliderWrapper _wrapper = _wrapperSpan[i];
                    int _startIndex = i * _resultCount;

                    Movable3DManager.OnExtractOverlap(_wrapper.MovableInstance, _wrapper.Collider, _buffer, _startIndex, _resultCount);
                }
            }

            // Boxes.
            _wrapperSpan = boxColliders;
            _count = _wrapperSpan.Count;

            if (_count != 0) {

                ArrayUtility.Realloc(ref _buffer, boxResults.Length);
                NativeArray<ColliderHit>.Copy(boxResults, _buffer);

                for (int i = _count; i-- > 0;) {
                    Movable3DColliderWrapper _wrapper = _wrapperSpan[i];
                    int _startIndex = i * _resultCount;

                    Movable3DManager.OnExtractOverlap(_wrapper.MovableInstance, _wrapper.Collider, _buffer, _startIndex, _resultCount);
                }
            }

            // Complete.
            Clear();
        }

        public void Clear() {
            // Clear all content.
            capsuleCommands.Clear();
            sphereCommands .Clear();
            boxCommands    .Clear();

            capsuleColliders.Clear();
            sphereColliders .Clear();
            boxColliders    .Clear();

            jobs.Clear();

            // Dispose TempJobs allocations and release memory.
            if (capsuleResults.IsCreated) {
                capsuleCommandArray.Dispose();
                capsuleResults.Dispose();
            }

            if (sphereResults.IsCreated) {
                sphereCommandArray.Dispose();
                sphereResults.Dispose();
            }

            if (boxResults.IsCreated) {
                boxCommandArray.Dispose();
                boxResults.Dispose();
            }
        }

        // -------------------------------------------
        // Registration
        // -------------------------------------------

        public void RegisterCommand(Movable3D _movable, Collider _collider, int _mask) {
            const QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;

            List<Movable3DColliderWrapper> _wrapperBuffer;
            ColliderWrapper3D _wrapper;

            try {
                if (!_collider.enabled)
                    return;

                _wrapper = ColliderWrapper3D.Get(_collider);

            } catch (Exception e) {

                if ((e is MissingReferenceException) || (e is NullReferenceException)) {
                    Debug.LogException(e);
                    return;
                }

                throw;
            }

            switch (_wrapper.ColliderTypeId) {
                // Capsule.
                case 1:
                    OverlapCapsuleCommand _capsuleCommand = _wrapper.GetCapsuleOverlapCommand(_mask, TriggerInteraction);
                    capsuleCommands.Add(_capsuleCommand);

                    _wrapperBuffer = capsuleColliders;
                    break;

                // Sphere.
                case 2:
                    OverlapSphereCommand _sphereCommand = _wrapper.GetSphereOverlapCommand(_mask, TriggerInteraction);
                    sphereCommands.Add(_sphereCommand);

                    _wrapperBuffer = sphereColliders;
                    break;

                // Box.
                case 3:
                    OverlapBoxCommand _boxCommand = _wrapper.GetBoxOverlapCommand(_mask, TriggerInteraction);
                    boxCommands.Add(_boxCommand);

                    _wrapperBuffer = boxColliders;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            _wrapperBuffer.Add(new Movable3DColliderWrapper(_movable, _collider));
        }
        #endif
        #endregion
    }

    // ===== Manager ===== \\

    /// <summary>
    /// <see cref="Movable3D"/>-related manager singleton class.
    /// </summary>
    [ScriptGizmos(false, true)]
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu(FrameworkUtility.MenuPath + "Physics [3D]/Movable Manager [3D]"), DisallowMultipleComponent]
    public sealed class Movable3DManager : EnhancedSingleton<Movable3DManager>, IMovableUpdate {
        public override UpdateRegistration UpdateRegistration => base.UpdateRegistration | UpdateRegistration.Movable;

        #region Global Members
        [Section("Movable Manager [3D]")]

        #if !OVERLAP_COMMANDS
        [Enhanced, HelpBox("Batches are recommanded only for Unity version 2022.2 and above\n[Not available for casts on previous versions]", MessageType.Warning, true, 10f)]
        #endif

        [SerializeField] private bool legacyBehaviour = true;
        [SerializeField] private bool overlapBatches  = false;
        [SerializeField] private bool castBatches     = false;

        [Space(10f), HorizontalLine(SuperColor.Grey, 1f), Space(10f)]

        [SerializeField, Enhanced, ReadOnly(nameof(overlapBatches))] private int extractMinCommandsPerJob = 1;
        [SerializeField, Enhanced, ReadOnly(nameof(overlapBatches))] private int extractMaxHits = 3;

        [Space(10f)]

        [SerializeField, Enhanced, ReadOnly(nameof(castBatches))] private int castMinCommandsPerJob = 1;
        [SerializeField, Enhanced, ReadOnly(nameof(castBatches))] private int castMaxHits = 16;
        #endregion

        #region Enhanced Behaviour
        private static readonly List<CollisionOperationData3D> dynamicCollisionBuffer = new List<CollisionOperationData3D>();
        private static readonly List<CollisionOperationData3D> collisionBuffer = new List<CollisionOperationData3D>();
        private static readonly List<ExtractOperation3D>   extractBuffer   = new List<ExtractOperation3D>();

        private static readonly OverlapOperationCommands3D overlapCommands = new OverlapOperationCommands3D();
        private static readonly CastOperationCommands3D    castCommands    = new CastOperationCommands3D();

        private static readonly List<Movable3D> updateBuffer = new List<Movable3D>();

        // -------------------------------------------
        // Update
        // -------------------------------------------

        void IMovableUpdate.Update() {
            // Buffers.
            List<CollisionOperationData3D> _collisionBuffer = collisionBuffer;
            List<ExtractOperation3D>       _extractBuffer   = extractBuffer;
            List<Movable3D>                _movableBuffer   = updateBuffer;

            _collisionBuffer.Clear();
            _extractBuffer  .Clear();
            _movableBuffer  .Clear();

            // Setup.
            ref List<Movable3D> _movables = ref movables;
            int _count = _movables.Count;

            if (_count == 0)
                return;

            _count = BufferUtility.RemovePending(_movables, _count, pendingRemove);
            if (_count == 0)
                return;

            // Variables.
            Physics3DSettings _settings = Physics3DSettings.I;
            float _deltaTime = DeltaTime;

            // Legacy.
            if (legacyBehaviour) {
                for (int i = _count; i-- > 0;) {
                    _movables[i].LogicManualUpdate(_deltaTime, _settings);
                }

                return;
            }

            bool _isPaused = Mathm.ApproximatelyZero(_deltaTime);

            // --- Updates --- \\

            // Pre-update.
            for (int i = _count; i-- > 0;) {
                _movables[i].OnPreUpdate(_deltaTime);
            }

            // --------------------
            // ---  Main logic  ---
            // --------------------

            // Early.
            for (int i = _count; i-- > 0;) {
                Movable3D _movable = _movables[i];

                if (_movable.LogicEarlyUpdate(_isPaused, out bool _performCollisions, out ExtractOperation3D _extractOperation)) {
                    _extractBuffer.Add(_extractOperation);
                }

                if (_performCollisions) {
                    _movableBuffer.Add(_movable);
                }
            }

            PerformExtract(_extractBuffer);

            // Collisions.
            for (int i = _movableBuffer.Count; i-- > 0;) {

                if (_movableBuffer[i].LogicPrepareCollisions(_deltaTime, _settings, out CollisionOperationData3D _collisionOperation)) {
                    _collisionBuffer.Add(_collisionOperation);
                }
            }

            PerformCollisions(_collisionBuffer);

            for (int i = _collisionBuffer.Count; i-- > 0;) {
                CollisionOperationData3D _collision = _collisionBuffer[i];

                if (_collision.MovableInstance.LogicOnCollisionComplete(_collision, out ExtractOperation3D _extractOperation)) {
                    _extractBuffer.Add(_extractOperation);
                }
            }

            PerformExtract(_extractBuffer);

            // Late.
            for (int i = _collisionBuffer.Count; i-- > 0;) {
                CollisionOperationData3D _collision = _collisionBuffer[i];
                _collision.MovableInstance.LogicLateUpdate(_collision);
            }

            // --------------------
            // ---  Main logic  ---
            // --------------------

            // Post-update.
            for (int i = _count; i-- > 0;) {
                _movables[i].OnPostUpdate(_deltaTime);
            }
        }

        // -------------------------------------------
        // Extract
        // -------------------------------------------

        private void PerformExtract(List<ExtractOperation3D> _operations) {
            int _amount = _operations.Count;
            if (_amount == 0)
                return;

            // Manual extract - no command nor batching.
            if (!overlapBatches) {
                PerformLegacyExtract(_amount);
                return;
            }

            #if OVERLAP_COMMANDS
            OverlapOperationCommands3D _commands = overlapCommands;

            // ---------- Init ---------- \\

            for (int i = 0; i < _amount; i++) {
                _operations[i].MovableInstance.ExtractInitOperation(_commands);
            }

            _commands.ScheduleBatch(extractMinCommandsPerJob, extractMaxHits);
            _commands.CompleteJobs();

            // ---------- Finalize ---------- \\

            for (int i = 0; i < _amount; i++) {
                _operations[i].MovableInstance.ExtractFinalizeOperation();
            }

            // Clear buffer.
            _operations.Clear();
            #else
            PerformLegacyExtract(_amount);
            #endif

            // ----- Local Method ----- \\

            void PerformLegacyExtract(int _amount) {
                for (int i = 0; i < _amount; i++) {
                    _operations[i].MovableInstance.ExtractPerformManual();
                }
            }
        }

        #if OVERLAP_COMMANDS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void OnExtractOverlap(Movable3D _movable, Collider _collider, ColliderHit[] _results, int _startIndex, int _count) {
            _movable.ExtractOnOperationResults(_collider, _results, _startIndex, _count);
        }
        #endif

        // -------------------------------------------
        // Collisions
        // -------------------------------------------

        private void PerformCollisions(List<CollisionOperationData3D> _operations) {
            // Manual cast - no command nor batching.
            if (!castBatches) {
                int _amount = _operations.Count;
                for (int i = 0; i < _amount; i++) {

                    CollisionOperationData3D _collision = _operations[i];
                    if (_collision.PerformCollisions) {
                        _collision.MovableInstance.CollisionPerformManual(_collision);
                    }
                }

                return;
            }

            // Perform.
            List<CollisionOperationData3D> _dynamicOperations = dynamicCollisionBuffer;
            CastOperationCommands3D _commands = castCommands;
            int _count = _operations.Count;

            _dynamicOperations.Clear();

            // ---------- Init ---------- \\

            for (int i = 0; i < _count; i++) {
                CollisionOperationData3D _collision = _operations[i];

                if (_collision.PerformCollisions) {
                    _collision.MovableInstance.CollisionInitOperation(_collision);
                    _dynamicOperations.Add(_collision);
                }
            }

            // ---------- Velocity Collisions ---------- \\

            int _dynamicCount = _dynamicOperations.Count;
            while (_dynamicCount != 0) {

                for (int i = 0; i < _dynamicCount; i++) {
                    CollisionOperationData3D _collision = _dynamicOperations[i];

                    if (_collision.MovableInstance.CollisionPerformOperation(_collision, _commands)) {
                        _dynamicOperations[i] = _dynamicOperations[--_dynamicCount];
                        i--;
                    }
                }

                _commands.ScheduleBatch(castMinCommandsPerJob, castMaxHits);
                _commands.CompleteJobs();

                for (int i = 0; i < _dynamicCount; i++) {
                    CollisionOperationData3D _collision = _dynamicOperations[i];

                    if (_collision.MovableInstance.CollisionComputeOperation(_collision)) {
                        _dynamicOperations[i] = _dynamicOperations[--_dynamicCount];
                        i--;
                    }
                }
            }

            // ---------- Finalize ---------- \\

            for (int i = 0; i < _count; i++) {
                CollisionOperationData3D _collision = _operations[i];

                if (_collision.PerformCollisions) {
                    _collision.MovableInstance.CollisionFinalizeOperation(_collision);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void OnCollision(Movable3D _movable, Collider _collider, RaycastHit[] _results, int _startIndex, int _count) {
            _movable.CollisionOnOperationResults(_collider, _results, _startIndex, _count);
        }
        #endregion

        #region Registration
        private static readonly List<Movable3D> pendingRemove = new List<Movable3D>();
        private static          List<Movable3D> movables      = new List<Movable3D>();

        // -------------------------------------------
        // Register
        // -------------------------------------------

        /// <summary>
        /// Registers a new <see cref="Movable3D"/> instance for update.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance to register.</param>
        public static void RegisterMovable(Movable3D _movable) {
            movables.Add(_movable);
        }

        /// <summary>
        /// Unregisters a <see cref="Movable3D"/> instance from update.
        /// </summary>
        /// <param name="_movable"><see cref="Movable3D"/> instance to unregister.</param>
        public static void UnregisterMovable(Movable3D _movable) {
            pendingRemove.Add(_movable);
        }
        #endregion
    }
}
