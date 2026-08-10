using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

using static EncosyTower.Debugging.ValidationDefines;

namespace EncosyTower.StringIds
{
    internal static class GlobalStringVault
    {
        internal readonly static StringVault s_vault = new(256);

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode, UnityEngine.Scripting.Preserve]
        private static void InitWhenDomainReloadDisabled()
        {
#if ENCOSY_CLEAR_GLOBAL_STRING_VAULT_ON_ENTER_PLAY_MODE
            // The user has explicitly opted in to clearing the vault on entering play mode
            // by defining ENCOSY_CLEAR_GLOBAL_STRING_VAULT_ON_ENTER_PLAY_MODE in Player Settings.
            // They are responsible for re-making every StringId cached in their own static fields,
            // otherwise those ids become dangling or silently remap to different strings.
            s_vault.Clear();

#if !ENCOSY_DISABLE_LOG_INFO
            Logging.StaticDevLogger.LogInfo(
                "GlobalStringVault has been cleared on entering play mode " +
                "because ENCOSY_CLEAR_GLOBAL_STRING_VAULT_ON_ENTER_PLAY_MODE is defined. " +
                "Any StringId cached in static fields must be re-made via StringToId.Get()."
            );
#endif
#else
            // DO NOT clear the `s_vault`!!!
            // StringId values cached in static fields persist when Domain Reload is disabled.
            // A StringId is an index into this vault; clearing the vault would leave those
            // persisted ids dangling or silently remap them to different strings.
            // To opt in to clearing anyway, define ENCOSY_CLEAR_GLOBAL_STRING_VAULT_ON_ENTER_PLAY_MODE
            // in Player Settings and re-make every cached StringId manually.
#endif
        }
#endif

        public static int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_vault.Capacity;
        }

        public static int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_vault.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDefined(StringId key)
            => s_vault.ContainsId(key.Id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringId GetOrMakeId([NotNull] string str)
        {
            return s_vault.GetOrMakeId(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetManagedString(StringId key)
        {
            s_vault.TryGetManagedString(key.Id, out var result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringId GetOrMakeId(in UnmanagedString str)
        {
            return s_vault.GetOrMakeId(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnmanagedString GetUnmanagedString(StringId key)
        {
            s_vault.TryGetUnmanagedString(key.Id, out var result);
            return result;
        }

        [HideInCallstack, StackTraceHidden]
        [Conditional(UNITY_EDITOR), Conditional(DEBUG)]
        [Conditional(RUNTIME_CHECKS), Conditional(COLLECTIONS_CHECKS)]
        [Conditional(UNITY_COLLECTIONS_CHECKS)]
        internal static void ThrowIfNotDefined([DoesNotReturnIf(false)] bool check, StringId key)
        {
            if (check == false)
            {
                throw CreateException(key);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            static InvalidOperationException CreateException(StringId key)
                => new(
                    $"No StringId has not been globally defined with id \"{key}\". " +
                    $"To define one, use StringToId.Get() API."
                );
        }
    }
}
