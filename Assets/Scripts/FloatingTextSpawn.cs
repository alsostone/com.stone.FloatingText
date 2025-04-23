using ST.HUD;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class FloatingTextSpawn : MonoBehaviour
{
    public Text textCount;
    public Text textFPS;
    Unity.Mathematics.Random _random = Unity.Mathematics.Random.CreateFromIndex(0);
    private float elapsedTime;
    
    public void ShowDamage()
    {
        var elapsedTime = Time.time;
        var wpos = _random.NextFloat3(new float3(-500f,1,-500f),new float3(500f,1, 500f));
        int damage = _random.NextInt(999999999);
        int style = _random.NextInt(0, 3);
        
        
        uint3x3 index = new uint3x3();
        FontMeshHelper.SetIndex(style, damage, ref index);
        
        FloatingText data = new FloatingText
        {
            wpos = wpos,
            uvVexIdx = index,
            scale = new float2(1f, 1f),
            fixedTime = (float)elapsedTime
        };
        FloatingTextBuffer.Instance.Enqueue(data);
    }

    private void Update()
    {
        for (int i = 0; i < 100; i++)
        {
            ShowDamage();
        }
        
        elapsedTime -= Time.deltaTime;
        if (elapsedTime <= 0)
        {
            textCount.text = FloatingTextBuffer.Instance.Count.ToString();
            textFPS.text = (1.0f / Time.unscaledDeltaTime).ToString("F0");
            elapsedTime += 1.0f;
        }
    }
}
