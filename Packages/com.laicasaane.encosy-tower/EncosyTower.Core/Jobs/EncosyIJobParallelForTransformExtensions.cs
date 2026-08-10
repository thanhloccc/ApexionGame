using System.Runtime.CompilerServices;
using EncosyTower.UnityExtensions;
using Unity.Jobs;
using UnityEngine.Jobs;

namespace EncosyTower.Jobs
{
    public static class EncosyIJobParallelForTransformExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle Schedule<T>(
              this ref T jobData
            , ScheduleOnlyTransformArray transforms
            , JobHandle dependsOn = default
        )
            where T : struct, IJobParallelForTransform
        {
            return IJobParallelForTransformExtensions.Schedule(
                  jobData
                , transforms._array
                , dependsOn
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle ScheduleReadOnly<T>(
              this T jobData
            , ScheduleOnlyTransformArray transforms
            , int batchSize
            , JobHandle dependsOn = default
        )
            where T : struct, IJobParallelForTransform
        {
            // SAFETY: Unity owns the transform array and validates the scheduling contract.
            unsafe
            {
                return IJobParallelForTransformExtensions.ScheduleReadOnly(
                      jobData
                    , transforms._array
                    , batchSize
                    , dependsOn
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunReadOnly<T>(
              this T jobData
            , ScheduleOnlyTransformArray transforms
        )
            where T : struct, IJobParallelForTransform
        {
            // SAFETY: Unity owns the transform array and validates the scheduling contract.
            unsafe
            {
                IJobParallelForTransformExtensions.RunReadOnly(
                      jobData
                    , transforms._array
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle ScheduleByRef<T>(
              this ref T jobData
            , ScheduleOnlyTransformArray transforms
            , JobHandle dependsOn = default
        )
            where T : struct, IJobParallelForTransform
        {
            // SAFETY: Unity owns the transform array and validates the scheduling contract.
            unsafe
            {
                return IJobParallelForTransformExtensions.ScheduleByRef(
                      ref jobData
                    , transforms._array
                    , dependsOn
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle ScheduleReadOnlyByRef<T>(
              this ref T jobData
            , ScheduleOnlyTransformArray transforms
            , int batchSize
            , JobHandle dependsOn = default
        )
            where T : struct, IJobParallelForTransform
        {
            // SAFETY: Unity owns the transform array and validates the scheduling contract.
            unsafe
            {
                return IJobParallelForTransformExtensions.ScheduleReadOnlyByRef(
                      ref jobData
                    , transforms._array
                    , batchSize
                    , dependsOn
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunReadOnlyByRef<T>(this ref T jobData, ScheduleOnlyTransformArray transforms)
            where T : struct, IJobParallelForTransform
        {
            // SAFETY: Unity owns the transform array and validates the scheduling contract.
            unsafe
            {
                IJobParallelForTransformExtensions.RunReadOnlyByRef(
                      ref jobData
                    , transforms._array
                );
            }
        }
    }
}
