using UnityEngine;
using Lasp;

[AddComponentMenu("LASP/Audio To Color")]
public class ColorGenerator : MonoBehaviour
{
    [Header("Audio Level Trackers")]
    [SerializeField] AudioLevelTracker lowPassTracker;
    [SerializeField] AudioLevelTracker bandPassTracker;
    [SerializeField] AudioLevelTracker highPassTracker;
    [SerializeField] AudioLevelTracker amplitudeTracker;

    [Header("Color Settings")]
    [SerializeField, Range(0f, 1f)] float colorIntensity = 1f;
    [SerializeField, Range(0f, 1f)] float minAlpha = 0.2f;
    [SerializeField, Range(0f, 1f)] float maxAlpha = 1f;

    [Header("Material Settings")]
    [SerializeField] MaterialColorMode colorMode = MaterialColorMode.MainColor;
    [SerializeField] string customColorProperty = "_Color";
    
    [Header("Target Settings")]
    [SerializeField] MaterialUpdateMode updateMode = MaterialUpdateMode.UseMaterial;
    [SerializeField] Material targetMaterial;
    [SerializeField] Renderer targetRenderer;

    // Enum to control how we update the material
    public enum MaterialUpdateMode
    {
        UseMaterial,    // Use directly assigned material
        UseRenderer     // Use renderer's material
    }

    // Enum to specify which color property to modify
    public enum MaterialColorMode
    {
        MainColor,      // Use material.color
        CustomProperty  // Use custom property name
    }

    private Color _currentColor = Color.black;
    public Color CurrentColor => _currentColor;

    private Material materialToUpdate;

    private void Start()
    {
        // Configure the trackers
        SetupTracker(lowPassTracker, FilterType.LowPass);
        SetupTracker(bandPassTracker, FilterType.BandPass);
        SetupTracker(highPassTracker, FilterType.HighPass);
        SetupTracker(amplitudeTracker, FilterType.Bypass);

        // Setup material reference
        SetupMaterialReference();
    }

    private void SetupMaterialReference()
    {
        switch (updateMode)
        {
            case MaterialUpdateMode.UseMaterial:
                materialToUpdate = targetMaterial;
                break;

            case MaterialUpdateMode.UseRenderer:
                if (targetRenderer != null)
                {
                    // Create a material instance to avoid modifying the shared material
                    materialToUpdate = new Material(targetRenderer.material);
                    targetRenderer.material = materialToUpdate;
                }
                break;
        }

        if (materialToUpdate == null)
        {
            Debug.LogWarning("AudioToColor: No valid material reference found!");
        }
    }

    private void SetupTracker(AudioLevelTracker tracker, FilterType filterType)
    {
        if (tracker != null)
        {
            tracker.filterType = filterType;
            tracker.smoothFall = true;
            tracker.fallSpeed = 0.3f;
            tracker.autoGain = true;
            tracker.dynamicRange = 20f;
        }
    }

    private void Update()
    {
        if (lowPassTracker == null || bandPassTracker == null || 
            highPassTracker == null || amplitudeTracker == null)
        {
            Debug.LogWarning("AudioToColor: Some trackers are not assigned!");
            return;
        }

        if (materialToUpdate == null) return;

        // Get the normalized levels from each tracker
        float r = highPassTracker.normalizedLevel;
        float g = bandPassTracker.normalizedLevel;
        float b = lowPassTracker.normalizedLevel;
        
        // Calculate alpha based on overall amplitude
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, amplitudeTracker.normalizedLevel);

        // Create the color with the intensity modifier
        _currentColor = new Color(
            r * colorIntensity,
            g * colorIntensity,
            b * colorIntensity,
            alpha
        );

        // Update the material color
        UpdateMaterialColor(_currentColor);
    }

    private void UpdateMaterialColor(Color newColor)
    {
        if (materialToUpdate != null)
        {
            switch (colorMode)
            {
                case MaterialColorMode.MainColor:
                    materialToUpdate.color = newColor;
                    break;

                case MaterialColorMode.CustomProperty:
                    materialToUpdate.SetColor(customColorProperty, newColor);
                    break;
            }
        }
    }

    // Helper method to get the current frequency levels
    public Vector4 GetCurrentLevels()
    {
        return new Vector4(
            highPassTracker?.normalizedLevel ?? 0f,
            bandPassTracker?.normalizedLevel ?? 0f,
            lowPassTracker?.normalizedLevel ?? 0f,
            amplitudeTracker?.normalizedLevel ?? 0f
        );
    }

    // Method to manually set a new material at runtime
    public void SetMaterial(Material newMaterial)
    {
        targetMaterial = newMaterial;
        if (updateMode == MaterialUpdateMode.UseMaterial)
        {
            materialToUpdate = targetMaterial;
        }
    }

    // Method to manually set a new renderer at runtime
    public void SetRenderer(Renderer newRenderer)
    {
        targetRenderer = newRenderer;
        if (updateMode == MaterialUpdateMode.UseRenderer)
        {
            materialToUpdate = new Material(targetRenderer.material);
            targetRenderer.material = materialToUpdate;
        }
    }
}