using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class WaveController : MonoBehaviour
{
    private Material material;
    
    [Range(1, 10)]
    public float waveCount = 4f;
    
    [Range(0.1f, 0.9f)]
    public float waveThickness = 0.4f;
    
    [Range(0, 20)]
    public float waveFrequency = 2f;
    
    [Range(0, 5)]
    public float waveSpeed = 1f;
    
    [Range(0, 2)]
    public float horizontalOffset = 1f;

    public Color foregroundColor = Color.black;
    public Color backgroundColor = Color.white;

    void Start()
    {
        material = new Material(Shader.Find("Custom/AnimatedWaves"));
        GetComponent<Image>().material = material;
        UpdateShaderProperties();
    }

    void Update()
    {
        UpdateShaderProperties();
    }

    void UpdateShaderProperties()
    {
        material.SetFloat("_WaveCount", waveCount);
        material.SetFloat("_WaveThickness", waveThickness);
        material.SetFloat("_WaveFrequency", waveFrequency);
        material.SetFloat("_WaveSpeed", waveSpeed);
        material.SetFloat("_HorizontalOffset", horizontalOffset);
        material.SetColor("_ForegroundColor", foregroundColor);
        material.SetColor("_BackgroundColor", backgroundColor);
    }
}