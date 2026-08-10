using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ApexionGame.Entities.Stats.Samples.Rts.Tests
{
    /// <summary>
    /// Keeps the two assemblies honest.
    /// </summary>
    /// <remarks>
    /// The simulation cannot be made engine-free with a compiler flag — <c>Allocator</c> and
    /// <c>NativeArray&lt;T&gt;</c> live in <c>UnityEngine.CoreModule</c>, so any assembly that touches native
    /// collections references the engine whether it wants to or not. What <i>can</i> be enforced is the part
    /// that matters: no scene objects in the simulation, and no knowledge of the presentation layer.
    /// </remarks>
    public sealed class RtsLayeringTests
    {
        [Test]
        public void TheSimulationHasNoSceneObjects()
        {
            var assembly = typeof(RtsMatch).Assembly;

            foreach (var type in assembly.GetTypes())
            {
                Assert.IsFalse(typeof(Object).IsAssignableFrom(type)
                    , $"{type.FullName} derives from UnityEngine.Object, so the simulation now needs a scene");
            }
        }

        [Test]
        public void TheSimulationDoesNotKnowAboutThePresentationLayer()
        {
            var referenced = typeof(RtsMatch).Assembly
                .GetReferencedAssemblies()
                .Select(static assembly => assembly.Name)
                .ToArray();

            Assert.IsFalse(referenced.Any(static name => name.Contains("Rts.Game"))
                , "the simulation assembly must not reference the game assembly");
        }

        [Test]
        public void ContentIsImmutable()
        {
            var assembly = typeof(RtsMatch).Assembly;

            var offenders = assembly.GetTypes()
                .Where(static type => type.Namespace != null && type.Name.StartsWith("Rts"))
                .Where(static type => type == typeof(RtsUnitArchetype)
                    || type == typeof(RtsSpell)
                    || type == typeof(RtsResearch))
                .SelectMany(static type => type.GetFields())
                .Where(static field => field.IsPublic && field.IsInitOnly == false && field.IsStatic == false)
                .Select(static field => $"{field.DeclaringType?.Name}.{field.Name}")
                .ToArray();

            Assert.IsEmpty(offenders
                , "content definitions must not expose mutable public fields — someone will re-balance the "
                + "game at runtime by accident");
        }
    }
}
