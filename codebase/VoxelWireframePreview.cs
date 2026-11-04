using System.Collections.Generic;
using UnityEngine;

public class VoxelWireframePreview : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public LayerMask voxelWallMask;

    [Header("Grid & Aim Assist")]
    public float voxelSize = 0.25f;
    public float previewReach = 2.4f;
    public float aimAssistRadius = 0.22f;

    [Header("Wireframe Colors")]
    public Color centerVoxelColor = new Color(1f, 0.55f, 0.1f, 1f);
    public Color outerVoxelColor = new Color(1f, 1f, 1f, 0.85f);

    [Header("Pattern Upgrade & Charge Scaling")]
    [Range(1, 3)] public int basePatternRadius = 1;
    public float chargeExpandDelay = 0.18f;
    public float maxChargeTime = 1.15f;

    private Mesh wireframeCubeMesh;
    private Material centerMat;
    private Material outerMat;

    private bool isPreviewEnabled = true;
    private bool isHoldingCharge = false;
    private float holdTimer = 0f;

    private readonly List<Matrix4x4> outerMatrices = new List<Matrix4x4>(32);

    void Awake()
    {
        if (!playerCamera) playerCamera = Camera.main;

        wireframeCubeMesh = BuildWireframeCubeMesh(voxelSize * 1.02f);

        Shader lineShader = Shader.Find("Sprites/Default");
        centerMat = new Material(lineShader) { color = centerVoxelColor };
        outerMat = new Material(lineShader) { color = outerVoxelColor };
    }

    public void OnPunchButtonDown()
    {
        isHoldingCharge = true;
        holdTimer = 0f;
    }

    public void OnPunchButtonUp()
    {
        isHoldingCharge = false;
        holdTimer = 0f;
    }

    public void SetPreviewActive(bool active)
    {
        isPreviewEnabled = active;
    }

    void Update()
    {
        if (!isPreviewEnabled || !playerCamera) return;

        if (isHoldingCharge) holdTimer += Time.deltaTime;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        bool hasHit = Physics.Raycast(ray, out hit, previewReach, voxelWallMask) ||
                      Physics.SphereCast(ray, aimAssistRadius, out hit, previewReach, voxelWallMask);

        if (!hasHit) return;

        Vector3 insideVoxelPos = hit.point - hit.normal * (voxelSize * 0.45f);
        Vector3 centerSnapped = SnapToVoxelGrid(insideVoxelPos);

        int activeRadius = basePatternRadius;
        if (isHoldingCharge && holdTimer >= chargeExpandDelay)
        {
            float chargeRatio = Mathf.Clamp01((holdTimer - chargeExpandDelay) / (maxChargeTime - chargeExpandDelay));
            activeRadius = basePatternRadius + Mathf.RoundToInt(chargeRatio * 2f);
        }

        GetWallTangentAxes(hit.normal, out Vector3 rightAxis, out Vector3 upAxis);

        Matrix4x4 centerMatrix = Matrix4x4.TRS(centerSnapped, Quaternion.identity, Vector3.one);
        Graphics.DrawMesh(wireframeCubeMesh, centerMatrix, centerMat, 0);

        outerMatrices.Clear();
        Vector3 checkHalfExtents = Vector3.one * (voxelSize * 0.35f);

        for (int x = -activeRadius; x <= activeRadius; x++)
        {
            for (int y = -activeRadius; y <= activeRadius; y++)
            {
                int manhattanDist = Mathf.Abs(x) + Mathf.Abs(y);
                if (manhattanDist == 0 || manhattanDist > activeRadius) continue;

                Vector3 sampleCenter = centerSnapped + (rightAxis * x + upAxis * y) * voxelSize;

                if (Physics.CheckBox(sampleCenter, checkHalfExtents, Quaternion.identity, voxelWallMask))
                {
                    outerMatrices.Add(Matrix4x4.TRS(sampleCenter, Quaternion.identity, Vector3.one));
                }
            }
        }

        for (int i = 0; i < outerMatrices.Count; i++)
        {
            Graphics.DrawMesh(wireframeCubeMesh, outerMatrices[i], outerMat, 0);
        }
    }

    private Vector3 SnapToVoxelGrid(Vector3 worldPos)
    {
        float x = (Mathf.Floor(worldPos.x / voxelSize) + 0.5f) * voxelSize;
        float y = (Mathf.Floor(worldPos.y / voxelSize) + 0.5f) * voxelSize;
        float z = (Mathf.Floor(worldPos.z / voxelSize) + 0.5f) * voxelSize;
        return new Vector3(x, y, z);
    }

    private void GetWallTangentAxes(Vector3 normal, out Vector3 tangentRight, out Vector3 tangentUp)
    {
        Vector3 n = new Vector3(Mathf.Round(normal.x), Mathf.Round(normal.y), Mathf.Round(normal.z));
        if (Mathf.Abs(n.y) > 0.5f)
        {
            tangentRight = Vector3.right;
            tangentUp = Vector3.forward;
        }
        else if (Mathf.Abs(n.x) > 0.5f)
        {
            tangentRight = Vector3.forward;
            tangentUp = Vector3.up;
        }
        else
        {
            tangentRight = Vector3.right;
            tangentUp = Vector3.up;
        }
    }

    private Mesh BuildWireframeCubeMesh(float size)
    {
        float h = size * 0.5f;
        Vector3[] verts = new Vector3[]
        {
            new Vector3(-h, -h, -h), new Vector3( h, -h, -h),
            new Vector3( h,  h, -h), new Vector3(-h,  h, -h),
            new Vector3(-h, -h,  h), new Vector3( h, -h,  h),
            new Vector3( h,  h,  h), new Vector3(-h,  h,  h)
        };

        int[] indices = new int[]
        {
            0,1, 1,2, 2,3, 3,0,
            4,5, 5,6, 6,7, 7,4,
            0,4, 1,5, 2,6, 3,7
        };

        Mesh mesh = new Mesh { name = "ProceduralWireframeVoxel" };
        mesh.vertices = verts;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
