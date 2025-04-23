using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using ST.HUD;

public class FloatingTextRenderer : MonoBehaviour
{
    [SerializeField] int MAX_RENDER_COUNT = 51200;
    [SerializeField] Material material;
    [SerializeField] Font font;
    [SerializeField] [Range(0, 31)] int layer;
    
    private Bounds renderBounds = new Bounds();
    private Mesh renderMesh;

    private MaterialPropertyBlock propertyBlock;
    private ComputeBuffer argsBuffer;
    private ComputeBuffer uvsBuffer;
    private ComputeBuffer vetsBuffer;
    
    private FloatingTextBuffer floatingTextBuffer;
    private int computekernel;
    public ComputeShader computeShader;
    private ComputeBuffer instanceBuffer;
    private ComputeBuffer visibleBuffer;

    private static readonly int TextUvs = Shader.PropertyToID("_TextUvs");
    private static readonly int TextVets = Shader.PropertyToID("_TextVets");
    private static readonly int InstanceBuffer = Shader.PropertyToID("_InstanceBuffer");
    private static readonly int VisibleBuffer = Shader.PropertyToID("_VisibleBuffer");
    private static readonly int ElapsedTime = Shader.PropertyToID("_ElapsedTime");
    private static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
    private static readonly int Duration = Shader.PropertyToID("_Duration");

    public static readonly char[,] Chars = new char[,]{
        { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' },
        { 'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p' },
        { 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'L' },
        { 'z', 'x', 'c', 'v', 'b', 'n', 'm', 'M', 'N', 'B' }
    };
    
    void Start()
    {
        floatingTextBuffer = new FloatingTextBuffer(MAX_RENDER_COUNT);
        renderBounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        
        renderMesh = FontMeshHelper.CreateMesh(font);
        var args = new uint[] { renderMesh.GetIndexCount(0), 0, renderMesh.GetIndexStart(0), renderMesh.GetBaseVertex(0), 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);
        
        InitFontBuffer();
        InitAnimationBuffer();
        
        propertyBlock = new MaterialPropertyBlock();
        propertyBlock.SetBuffer(TextUvs, uvsBuffer);
        propertyBlock.SetBuffer(TextVets, vetsBuffer);
        propertyBlock.SetBuffer(InstanceBuffer, instanceBuffer);
        propertyBlock.SetBuffer(VisibleBuffer, visibleBuffer);
    }

    void InitFontBuffer()
    {
        var chars = Chars;
        int x = chars.GetLength(0);
        int y = chars.GetLength(1);
        var fontVertices = new float2[4 * x * y + 4];
        var fontUvs = new float2[4 * x * y + 4];

        int index = 0;
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                char c = chars[i, j];
                if (font.GetCharacterInfo(c, out var info))
                {
                    fontVertices[index + 0] = new float2(info.minX, info.maxY) * FontMeshHelper.TextScale;
                    fontVertices[index + 1] = new float2(info.maxX, info.maxY) * FontMeshHelper.TextScale;
                    fontVertices[index + 2] = new float2(info.minX, info.minY) * FontMeshHelper.TextScale;
                    fontVertices[index + 3] = new float2(info.maxX, info.minY) * FontMeshHelper.TextScale;

                    fontUvs[index + 0] = info.uvTopLeft;
                    fontUvs[index + 1] = info.uvTopRight;
                    fontUvs[index + 2] = info.uvBottomLeft;
                    fontUvs[index + 3] = info.uvBottomRight;
                    index += 4;
                }
            }
        }

        // 空格
        fontVertices[index + 0] = new float2(FontMeshHelper.SpaceVet.x, FontMeshHelper.SpaceVet.w);
        fontVertices[index + 1] = new float2(FontMeshHelper.SpaceVet.z, FontMeshHelper.SpaceVet.w);
        fontVertices[index + 2] = new float2(FontMeshHelper.SpaceVet.x, FontMeshHelper.SpaceVet.y);
        fontVertices[index + 3] = new float2(FontMeshHelper.SpaceVet.z, FontMeshHelper.SpaceVet.y);

        fontUvs[index + 0] = FontMeshHelper.SpaceUvTL;
        fontUvs[index + 1] = FontMeshHelper.SpaceUvTR;
        fontUvs[index + 2] = FontMeshHelper.SpaceUvBL;
        fontUvs[index + 3] = FontMeshHelper.SpaceUvBR;
        
        uvsBuffer = new ComputeBuffer(fontUvs.Length, 8);
        vetsBuffer = new ComputeBuffer(fontVertices.Length, 8);
        uvsBuffer.SetData(fontUvs);
        vetsBuffer.SetData(fontVertices);
    }

    void InitAnimationBuffer()
    {
        instanceBuffer = new ComputeBuffer(MAX_RENDER_COUNT, Marshal.SizeOf(typeof(FloatingText)), ComputeBufferType.IndirectArguments);
        visibleBuffer = new ComputeBuffer(MAX_RENDER_COUNT, sizeof(uint), ComputeBufferType.Append);

        computekernel = computeShader.FindKernel("UpdateAnimations");
        computeShader.SetBuffer(computekernel, InstanceBuffer, instanceBuffer);
        computeShader.SetBuffer(computekernel, VisibleBuffer, visibleBuffer);
        computeShader.SetFloat(Duration, 1.0f);
    }
    
    private void LateUpdate()
    {
        floatingTextBuffer.TryAppendData(instanceBuffer);

        visibleBuffer.SetCounterValue(0);
        computeShader.SetFloat(ElapsedTime, Time.time);
        computeShader.SetFloat(DeltaTime, Time.deltaTime);

        var threadGroups = Mathf.CeilToInt(instanceBuffer.count / 64.0f);
        computeShader.Dispatch(computekernel, threadGroups, 1, 1);

        ComputeBuffer.CopyCount(visibleBuffer, argsBuffer, sizeof(uint));
        Graphics.DrawMeshInstancedIndirect(renderMesh, 0, material, renderBounds, argsBuffer, 0, propertyBlock,
            ShadowCastingMode.Off, false, layer);
    }

    void OnDestroy()
    {
        propertyBlock?.Clear();
        argsBuffer?.Dispose();
        uvsBuffer?.Dispose();
        vetsBuffer?.Dispose();
        instanceBuffer?.Dispose();
        visibleBuffer?.Dispose();
    }
    
}
