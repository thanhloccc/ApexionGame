#if UNITASK || UNITY_6000_0_OR_NEWER

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using EncosyTower.Initialization;
using EncosyTower.Tasks;
using UnityEngine;

namespace EncosyTower.Persistences
{
#if UNITASK
    using UnityTask = Cysharp.Threading.Tasks.UniTask;
    using UnityTaskBool = Cysharp.Threading.Tasks.UniTask<bool>;
#else
    using UnityTask = UnityEngine.Awaitable;
    using UnityTaskBool = UnityEngine.Awaitable<bool>;
#endif

    using ILogger = Logging.ILogger;

    public abstract class PersistenceBase : IDeinitializable, IDisposable
    {
        private bool _markDirtyBeforeSaving;

        protected abstract IPersistDirectory PersistDirectory { get; }

        public async UnityTaskBool TryLoadAsync(
              [NotNull] ILogger logger
            , string id
            , SourcePriority priority
            , SaveDestination destination
            , CancellationToken token = default
        )
        {
            _markDirtyBeforeSaving = true;

            PersistDirectory.Id = id;

            if (string.IsNullOrEmpty(id))
            {
                LogWarningInvalidId(logger);
                return false;
            }

            PersistDirectory.Initialize();

            await PersistDirectory.LoadEntireDirectoryAsync(priority, token);

            if (token.IsCancellationRequested)
            {
                return false;
            }

            PersistDirectory.CreateDataIfNotExist();

            var result = await OnTryLoadAsync(logger, id, priority, destination, token);

            if (result == false || token.IsCancellationRequested)
            {
                return false;
            }

            await SaveAsync(destination, token: token);

            return true;
        }

        public void Deinitialize()
        {
            PersistDirectory.Deinitialize();
            OnDeinitialize();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async UnityTask SaveAsync(SaveDestination destination, CancellationToken token = default)
        {
            if (_markDirtyBeforeSaving)
            {
                _markDirtyBeforeSaving = false;
                PersistDirectory.MarkDirty(true);
            }

            await PersistDirectory.SaveEntireDirectoryAsync(destination, token: token);
        }

        protected virtual UnityTaskBool OnTryLoadAsync(
              [NotNull] ILogger logger
            , string id
            , SourcePriority priority
            , SaveDestination destination
            , CancellationToken token
        )
        {
            return UnityTasks.GetCompleted(true);
        }

        protected virtual void OnDeinitialize() { }

        protected virtual void Dispose(bool disposing) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [HideInCallstack, StackTraceHidden]
        private static void LogWarningInvalidId(ILogger logger)
        {
            logger.LogWarning("Persist data cannot be loaded because 'id' is invalid.");

        }
    }
}

#endif
