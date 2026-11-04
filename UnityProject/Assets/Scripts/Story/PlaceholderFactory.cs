using BreakToEscape.Core.World;
using UnityEngine;

/// <summary>
/// Builds simple primitive placeholder objects for story items at runtime, so the escape chain is
/// playable before final models exist (see docs/ASSET_REQUIREMENTS.md for the real models).
/// Colliders are removed: interaction uses grid line of sight, not physics.
/// </summary>
public static class PlaceholderFactory
{
    private static MaterialPropertyBlock block;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public static GameObject Build(InteractableType type, Material material, Transform parent)
    {
        var root = new GameObject(type.ToString());
        root.transform.SetParent(parent, false);
        switch (type)
        {
            case InteractableType.StoreTerminal:
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0.7f, 0f), new Vector3(0.7f, 1.4f, 0.4f), new Color(0.95f, 0.78f, 0.1f), material);
                Part(root, PrimitiveType.Cube, new Vector3(0f, 1.0f, -0.21f), new Vector3(0.5f, 0.35f, 0.02f), new Color(0.2f, 0.9f, 0.4f), material);
                break;
            case InteractableType.CrewLocker:
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 1.8f, 0.5f), new Color(0.32f, 0.42f, 0.5f), material);
                break;
            case InteractableType.Logbook:
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.45f, 0.3f, 0.18f), material);
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0.53f, 0f), new Vector3(0.26f, 0.05f, 0.34f), new Color(0.55f, 0.12f, 0.1f), material);
                break;
            case InteractableType.HardHat:
                Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0f), new Vector3(0.32f, 0.18f, 0.36f), new Color(1f, 0.82f, 0.1f), material);
                break;
            case InteractableType.WallNote:
                Part(root, PrimitiveType.Cube, new Vector3(0f, 1.3f, 0f), new Vector3(0.32f, 0.42f, 0.03f), new Color(0.93f, 0.9f, 0.8f), material);
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0.65f, 0f), new Vector3(0.06f, 1.3f, 0.06f), new Color(0.4f, 0.28f, 0.16f), material);
                break;
            case InteractableType.RepairPart:
                Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.15f, 0f), new Vector3(0.22f, 0.15f, 0.22f), new Color(0.9f, 0.45f, 0.12f), material);
                break;
            case InteractableType.FuelCell:
                Part(root, PrimitiveType.Capsule, new Vector3(0f, 0.3f, 0f), new Vector3(0.22f, 0.3f, 0.22f), new Color(0.3f, 1f, 0.45f), material);
                break;
            case InteractableType.Generator:
            {
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 1.0f, 0.9f), new Color(0.25f, 0.27f, 0.3f), material);
                Part(root, PrimitiveType.Cylinder, new Vector3(0.5f, 1.1f, 0f), new Vector3(0.15f, 0.2f, 0.15f), new Color(0.6f, 0.6f, 0.62f), material);
                var lampObj = new GameObject("GeneratorLamp");
                lampObj.transform.SetParent(root.transform, false);
                lampObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                Light lamp = lampObj.AddComponent<Light>();
                lamp.type = LightType.Point;
                lamp.range = 7f;
                lamp.intensity = 2.5f;
                lamp.color = new Color(1f, 0.85f, 0.55f);
                lamp.enabled = false;
                break;
            }
            case InteractableType.WinchCable:
                Part(root, PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.35f, 0.7f), new Color(0.2f, 0.2f, 0.22f), material).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            case InteractableType.DynamiteCrate:
                Part(root, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(0.55f, 0.5f, 0.4f), new Color(0.8f, 0.15f, 0.1f), material);
                break;
            case InteractableType.LiftGate:
                Part(root, PrimitiveType.Cube, new Vector3(-1.2f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.3f), new Color(0.95f, 0.78f, 0.1f), material);
                Part(root, PrimitiveType.Cube, new Vector3(1.2f, 1.5f, 0f), new Vector3(0.2f, 3f, 0.3f), new Color(0.95f, 0.78f, 0.1f), material);
                Part(root, PrimitiveType.Cube, new Vector3(0f, 3.05f, 0f), new Vector3(2.6f, 0.2f, 0.3f), new Color(0.95f, 0.78f, 0.1f), material);
                var door = new GameObject("GateDoor");
                door.transform.SetParent(root.transform, false);
                for (int i = 0; i < 6; i++)
                    Part(door, PrimitiveType.Cube, new Vector3(-0.9f + i * 0.36f, 1.45f, 0f), new Vector3(0.08f, 2.9f, 0.08f), new Color(0.35f, 0.37f, 0.4f), material);
                Part(root, PrimitiveType.Cylinder, new Vector3(0.8f, 1.2f, -0.2f), new Vector3(0.25f, 0.04f, 0.25f), new Color(1f, 0.85f, 0.2f), material).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                break;
        }
        return root;
    }

    private static GameObject Part(GameObject parent, PrimitiveType shape, Vector3 localPos, Vector3 scale, Color color, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.transform.SetParent(parent.transform, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = scale;
        Collider c = part.GetComponent<Collider>();
        if (c) Object.Destroy(c);
        MeshRenderer r = part.GetComponent<MeshRenderer>();
        if (material) r.sharedMaterial = material;
        if (block == null) block = new MaterialPropertyBlock();
        block.Clear();
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        r.SetPropertyBlock(block);
        return part;
    }
}
