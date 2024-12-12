using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIBlurController : MonoBehaviour
{
    
    [Range(0, 10)]
    public float blurSize = 3f;
    
    [Range(0, 1)]
    public float blurIntensity = 0.5f;
    public Shader blurShader; // Assign this in the Inspector
    private Material blurMaterial;
    
    void Start()
    {
        if (blurShader == null)
        {
            Debug.LogError("Please assign a blur shader in the inspector!");
            enabled = false;
            return;
        }
        
        blurMaterial = new Material(blurShader);
        GetComponent<Image>().material = blurMaterial;
    }

    void Update()
    {
        UpdateShaderProperties();
    }

    void UpdateShaderProperties()
    {
        blurMaterial.SetFloat("_BlurSize", blurSize);
        blurMaterial.SetFloat("_BlurIntensity", blurIntensity);
    }
}