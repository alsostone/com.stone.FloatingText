using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class FontMeshHelper
{
    public static int TextLength => textTemp.Length;

    // x:minX,y:minY,z:maxX,w:maxY
    public static readonly int4 SpaceVet = new int4(0, -6, 2, -5);
    public static readonly float2 SpaceUvBL = new float2(0.98f, 1f);
    public static readonly float2 SpaceUvBR = new float2(0.98f, 1f);
    public static readonly float2 SpaceUvTL = new float2(0.98f, 1f);
    public static readonly float2 SpaceUvTR = new float2(0.98f, 1f);

    /// <summary>
    /// 设置Mesh索引
    /// </summary>
    /// <param name="index"></param>
    /// <param name="triangles"></param>
    public static void SetTriangles(int index, ref int[] triangles)
    {
        triangles[6 * index + 0] = 4 * index + 0;
        triangles[6 * index + 1] = 4 * index + 1;
        triangles[6 * index + 2] = 4 * index + 2;

        triangles[6 * index + 3] = 4 * index + 2;
        triangles[6 * index + 4] = 4 * index + 1;
        triangles[6 * index + 5] = 4 * index + 3;
    }

    /// <summary>
    /// 创建Mesh
    /// </summary>
    /// <returns></returns>
    public static Mesh CreateMesh(Font font)
    {
        var mesh = new Mesh();
        var totalLen = textTemp.Length;
        var vertices = new Vector3[totalLen * 4];
        var triangles = new int[totalLen * 6];
        var uv = new Vector2[totalLen * 4];

        for (int i = 0; i < totalLen; i++)
        {
            SetTriangles(i, ref triangles);
        }

        SetTextUVVet(textTemp, font, ref vertices, ref uv);
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uv);

        return mesh;
    }

    public static readonly float TextScale = 0.02f;

    /// <summary>
    /// 伤害数字字符串
    /// 默认四位数
    /// </summary>
    private static string textTemp = "123456789";

    public static void SetTextUVVet(string str, Font font, ref Vector3[] vertices, ref Vector2[] uv)
    {
        Vector3 posf2 = new Vector3();

        var strSpan = MemoryMarshal.AsMemory(textTemp.AsMemory()).Span;
        int len = textTemp.Length;
        strSpan.Fill(' ');
        str.AsSpan().CopyTo(strSpan.Slice(0, len));

        for (int i = 0; i < len; i++)
        {
            CharacterInfo ch;
            font.GetCharacterInfo(textTemp[i], out ch);

            int idx = i * 4;
            vertices[idx + 0] = (posf2 + new Vector3(ch.minX, ch.maxY)) * TextScale;
            vertices[idx + 1] = (posf2 + new Vector3(ch.maxX, ch.maxY)) * TextScale;
            vertices[idx + 2] = (posf2 + new Vector3(ch.minX, ch.minY)) * TextScale;
            vertices[idx + 3] = (posf2 + new Vector3(ch.maxX, ch.minY)) * TextScale;

            uv[idx + 0] = ch.uvTopLeft;
            uv[idx + 1] = ch.uvTopRight;
            uv[idx + 2] = ch.uvBottomLeft;
            uv[idx + 3] = ch.uvBottomRight;

            posf2 += new Vector3(ch.advance, 0);
        }
    }


    public static readonly int RENDER_CHAR_LENGTH = 9;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetIndex(int style, int text, ref uint3x3 index)
    {
        var dig = GetNumberDigits(text);
        NativeArray<int> bits = new NativeArray<int>(dig, Allocator.Temp);
        PrintBits(text, ref bits, dig);
        int len = bits.Length;
        uint empty = PackUint4(new uint4(160, 161, 162, 163)); // 空格

        for (int i = 0; i < RENDER_CHAR_LENGTH; i++)
        {
            int x = i % 3;
            int y = i / 3;
            if (len > i)
            {
                uint a = (uint)(style * 40 + bits[i] * 4);
                uint b = a + 1;
                uint c = a + 2;
                uint d = a + 3;
                index[x][y] = PackUint4(new uint4(a, b, c, d));
            }
            else
            {
                index[x][y] = empty;
            }
        }
    }

    /// <summary>
    /// 获取数字的位数的每个值
    /// </summary>
    /// <param name="value"></param>
    /// <param name="bits"></param>
    /// <param name="dig"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrintBits(int value, ref NativeArray<int> bits, int dig)
    {
        int i = dig - 1;
        while (math.floor(value) > 0)
        {
            // 提取最低位的十进制值
            int digit = value % 10;
            bits[i] = digit;
            // 除以 10 来移除最低位的十进制值
            value /= 10;
            i--;
        }
    }

    /// <summary>
    /// 获取数字的位数
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetNumberDigits(int value)
    {
        return value == 0 ? 1 : (int)math.log10(value) + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint PackUint4(in uint4 value)
    {
        const int BitsPerValue = 8;

        uint combined = (value.x << (3 * BitsPerValue)) |
                        (value.y << (2 * BitsPerValue)) |
                        (value.z << (1 * BitsPerValue)) |
                        (value.w << (0 * BitsPerValue));

        return combined;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint4 UnpackUint4(uint value)
    {
        const int BitsPerValue = 8;

        uint a = (value >> (3 * BitsPerValue)) & 0xFF;
        uint b = (value >> (2 * BitsPerValue)) & 0xFF;
        uint c = (value >> (1 * BitsPerValue)) & 0xFF;
        uint d = (value >> (0 * BitsPerValue)) & 0xFF;

        return new uint4(a, b, c, d);
    }
}