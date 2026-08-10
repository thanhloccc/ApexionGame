using UnityEngine;
using UnityEngine.Rendering;

namespace ApexionGame.Entities.Stats.Samples.Rts.Game
{
    /// <summary>
    /// Every visual in this sample, built at runtime out of Unity's primitives.
    /// </summary>
    /// <remarks>
    /// <b>No art assets on purpose.</b> The folder can be copied into any project and run, and a stat
    /// library's sample has no business shipping sprites.
    /// <para>
    /// Primitives with <c>Universal Render Pipeline/Unlit</c> rather than sprites, because this project is
    /// configured with the 3D Universal Renderer — the sprite path there is a pink-material trap that has
    /// nothing to do with what the sample is teaching.
    /// </para>
    /// </remarks>
    public static class RtsPrimitives
    {
        private static Shader s_unlit;

        public static Shader UnlitShader
        {
            get
            {
                if (s_unlit != null)
                {
                    return s_unlit;
                }

                s_unlit = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Unlit/Color")
                    ?? Shader.Find("Sprites/Default");

                return s_unlit;
            }
        }

        public static Material NewUnlit(Color color)
        {
            var material = new Material(UnlitShader) { name = "RtsUnlit" };

            SetColor(material, color);

            return material;
        }

        /// <summary>
        /// URP's Unlit uses <c>_BaseColor</c>; the built-in fallbacks use <c>_Color</c>.
        /// </summary>
        /// <remarks>Write whichever the shader actually has, so nothing depends on the guess.</remarks>
        public static void SetColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        /// <summary>A primitive with no collider — nothing in this sample raycasts.</summary>
        public static GameObject NewPrimitive(PrimitiveType type, Transform parent, Material material)
        {
            var go = GameObject.CreatePrimitive(type);

            go.transform.SetParent(parent, false);

            if (go.GetComponent<Collider>() is { } collider)
            {
                Object.Destroy(collider);
            }

            if (go.GetComponent<MeshRenderer>() is { } renderer)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return go;
        }

        /// <summary>Destroys the materials a subtree created with <c>new Material(...)</c>, then the subtree.</summary>
        public static void DestroyWithMaterials(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            var renderers = root.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].sharedMaterial != null)
                {
                    Object.Destroy(renderers[i].sharedMaterial);
                }
            }

            Object.Destroy(root);
        }
    }
}
