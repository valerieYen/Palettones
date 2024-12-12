using UnityEngine;
using Lasp;
using System.IO;
using System.Collections.Generic;

[AddComponentMenu("LASP/Audio To Particles")]
public class ParticleSpawner : MonoBehaviour {
    [Header("Audio Level Trackers")]
    [SerializeField] AudioLevelTracker lowPassTracker;
    [SerializeField] AudioLevelTracker bandPassTracker;
    [SerializeField] AudioLevelTracker highPassTracker;
    [SerializeField] AudioLevelTracker amplitudeTracker;

    [Header("Spawn Settings")]
    [SerializeField, Range(0f, 1f)] float spawnThreshold = 0.5f;
    
    [Header("Position Constraints")]
    [SerializeField] Vector3 minPosition = new Vector3(-5f, 0f, -5f);
    [SerializeField] Vector3 maxPosition = new Vector3(5f, 5f, 5f);
    
    [Header("Particle Control")]
    [SerializeField, Range(0f, 100f)] float baseEmissionRate = 10f;
    [SerializeField, Range(1f, 10f)] float emissionMultiplier = 2f;
    [SerializeField, Range(0.1f, 5f)] float particleSpeed = 1f;
    [SerializeField, Range(0.1f, 5f)] float particleSize = 1f;
    [SerializeField, Range(0.1f, 10f)] float lifetime = 2f;
    [SerializeField, Range(0f, 180f)] float spread = 45f;
    [SerializeField] float systemLifetime = 5f;
    [SerializeField] Material particleMaterial;

    [Header("Path Settings")]
    [SerializeField] float pathSpeed = 1f;

    [Header("Color Settings")]
    [SerializeField, Range(0f, 1f)] float colorIntensity = 1f;
    [SerializeField, Range(0f, 1f)] float minAlpha = 0.2f;
    [SerializeField, Range(0f, 1f)] float maxAlpha = 1f;

    [Header("Random Motion Settings")]
    [SerializeField, Range(0, 5)] float curveVariation = 1f;
    [SerializeField, Range(2, 8)] int numCurvePoints = 4;
    [SerializeField, Range(0, 2)] float tangentStrength = 0.5f;
    
    [Header("Movement Settings")]
    [SerializeField] Vector3 movementSpeed = new Vector3(1f, 1f, 1f);
    [SerializeField] Vector3 movementBounds = new Vector3(5f, 5f, 5f);
    [SerializeField] float positionChangeInterval = 2f;

    private Dictionary<ParticleSystem, float> activeParticleSystems = new Dictionary<ParticleSystem, float>();
    private Dictionary<ParticleSystem, Material> particleMaterials = new Dictionary<ParticleSystem, Material>();
    private Dictionary<ParticleSystem, Vector3> targetPositions = new Dictionary<ParticleSystem, Vector3>();
    private Dictionary<ParticleSystem, float> nextPositionChangeTime = new Dictionary<ParticleSystem, float>();

    private List<ParticleSystem> systemsToRemove = new List<ParticleSystem>();
    private bool isProcessingAudioInput = false;
    private Mesh sphereMesh;

    private void Update() {
        float amplitudeLevel = amplitudeTracker.normalizedLevel;

        if (amplitudeLevel >= spawnThreshold && !isProcessingAudioInput) {
            CreateAndActivateSystem();
            isProcessingAudioInput = true;
        } else if (amplitudeLevel < spawnThreshold && isProcessingAudioInput) {
            isProcessingAudioInput = false;
        }

        UpdateSystems();
        CleanupSystems();
    }

    private void Start() {
        sphereMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        if (particleMaterial == null) {
            Debug.LogWarning("Particle material not assigned, using default Unlit material");
            particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }
    }

    private void CreateAndActivateSystem() {
        ParticleSystem newSystem = CreateParticleSystem();
        activeParticleSystems[newSystem] = -1f;
        
        // Create unique material for this system
        Material uniqueMaterial = new Material(particleMaterial);
        particleMaterials[newSystem] = uniqueMaterial;
        
        // Set up initial movement data
        targetPositions[newSystem] = GetRandomPosition();
        nextPositionChangeTime[newSystem] = Time.time + positionChangeInterval;
        
        var renderer = newSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = uniqueMaterial;
        renderer.trailMaterial = uniqueMaterial;
        
        ActivateParticles(newSystem);
    }

    private ParticleSystem CreateParticleSystem()
    {
        GameObject particleObj = new GameObject("AudioParticles");
        particleObj.transform.parent = transform;  // Parent directly to this object
        particleObj.transform.localPosition = GetRandomPosition();

        // Add ParticleSystem
        ParticleSystem system = particleObj.AddComponent<ParticleSystem>();
        ConfigureParticleSystem(system);

        return system;
    }
    
    private void UpdateSystems() {
        var systemsList = new List<KeyValuePair<ParticleSystem, float>>(activeParticleSystems);
        foreach (var kvp in systemsList) {
            ParticleSystem system = kvp.Key;
            float deactivationTime = kvp.Value;

            if (deactivationTime == -1f) {
                UpdateParticleProperties(system);
                UpdateParticleColor(system);
                UpdateSystemPosition(system);

                if (amplitudeTracker.normalizedLevel < spawnThreshold) {
                    DeactivateParticles(system);
                    activeParticleSystems[system] = Time.time;
                }
            } else if (Time.time > deactivationTime + systemLifetime) {
                systemsToRemove.Add(system);
                targetPositions.Remove(system);
                nextPositionChangeTime.Remove(system);
            }
        }
    }

    private void UpdateSystemPosition(ParticleSystem system) {
        if (Time.time >= nextPositionChangeTime[system]) {
            targetPositions[system] = GetRandomPosition();
            nextPositionChangeTime[system] = Time.time + positionChangeInterval;
        }

        // Smoothly move towards target position
        Vector3 currentPos = system.transform.position;
        Vector3 targetPos = targetPositions[system];
        system.transform.position = Vector3.Lerp(
            currentPos, 
            targetPos, 
            Time.deltaTime * new Vector3(
                movementSpeed.x,
                movementSpeed.y,
                movementSpeed.z
            ).magnitude
        );
    }

    private void CleanupSystems() {
        foreach (var system in systemsToRemove) {
            if (system != null) {
                // Clean up the unique material
                if (particleMaterials.TryGetValue(system, out Material material)) {
                    Destroy(material);
                    particleMaterials.Remove(system);
                }
                
                activeParticleSystems.Remove(system);
                Destroy(system.gameObject);
            }
        }
        systemsToRemove.Clear();
    }

    private void OnDestroy() {
        foreach (var system in activeParticleSystems.Keys) {
            if (system != null) {
                // Clean up all materials
                if (particleMaterials.TryGetValue(system, out Material material)) {
                    Destroy(material);
                }
                Destroy(system.gameObject);
            }
        }
        activeParticleSystems.Clear();
        particleMaterials.Clear();
    }

    private void ActivateParticles(ParticleSystem system) {
        var emission = system.emission;
        emission.enabled = true;
        system.Play();
    }

    private void DeactivateParticles(ParticleSystem system) {
        var emission = system.emission;
        emission.enabled = false;
        system.Stop();
    }

    private void UpdateParticleProperties(ParticleSystem system) {
        var emission = system.emission;
        var main = system.main;
        
        float amplitudeLevel = amplitudeTracker.normalizedLevel;
        float currentEmissionRate = baseEmissionRate * (1f + (amplitudeLevel * emissionMultiplier));
        emission.rateOverTime = currentEmissionRate;
        
        // Update size based on audio input
        main.startSize = particleSize * (1f + lowPassTracker.normalizedLevel);
    }

    private void ConfigureParticleSystem(ParticleSystem system) {
        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = lifetime;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;

        // Configure initial velocity and direction
        main.startSpeed = particleSpeed;
        
        var emission = system.emission;
        emission.enabled = false;
        emission.rateOverTime = baseEmissionRate;

        // Configure shape module for emission
        var shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        shape.radiusThickness = 1f;
        shape.arc = spread;
        shape.randomDirectionAmount = 1f; 

        // Configure color over lifetime
        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;

        // Configure velocity over lifetime for random movement
        var velocityOverLifetime = system.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = CreateRandomCurve();
        velocityOverLifetime.y = CreateRandomCurve();
        velocityOverLifetime.z = CreateRandomCurve();

        // Configure trail module
        var trails = system.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.ratio = 1.0f;
        trails.lifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        
        trails.sizeAffectsWidth = true;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(
            1.0f,
            new AnimationCurve(new Keyframe[] {
                new Keyframe(0, 1),
                new Keyframe(1, 0)
            })
        );
        
        trails.inheritParticleColor = true;
        trails.colorOverLifetime = new Gradient() {
            alphaKeys = new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        };

        trails.generateLightingData = true;
        trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
        
        // Set up renderer with proper material handling for URP
        SetupRenderer(system);
    }

    private ParticleSystem.MinMaxCurve CreateRandomCurve()
    {
        // Create two random curves for min/max range
        AnimationCurve minCurve = GenerateRandomCurve();
        AnimationCurve maxCurve = GenerateRandomCurve();
        
        return new ParticleSystem.MinMaxCurve(pathSpeed, minCurve, maxCurve);
    }

    private AnimationCurve GenerateRandomCurve()
    {
        AnimationCurve curve = new AnimationCurve();
        
        // Always start at 0
        curve.AddKey(new Keyframe(0f, 0f, 0f, Random.Range(-tangentStrength, tangentStrength)));
        
        // Add random points in between
        for (int i = 1; i < numCurvePoints - 1; i++)
        {
            float time = i / (float)(numCurvePoints - 1);
            float value = Random.Range(-curveVariation, curveVariation);
            float inTangent = Random.Range(-tangentStrength, tangentStrength);
            float outTangent = Random.Range(-tangentStrength, tangentStrength);
            
            curve.AddKey(new Keyframe(time, value, inTangent, outTangent));
        }
        
        // Always end at 0
        curve.AddKey(new Keyframe(1f, 0f, Random.Range(-tangentStrength, tangentStrength), 0f));
        
        return curve;
    }

    private void SetupRenderer(ParticleSystem system) {
    var renderer = system.GetComponent<ParticleSystemRenderer>();
    renderer.renderMode = ParticleSystemRenderMode.Mesh;
    renderer.mesh = sphereMesh;
    renderer.material = particleMaterial;
    
    // Important: Set the trail material to be the same as the particle material
    renderer.trailMaterial = particleMaterial;
}

    private Vector3 GetRandomPosition() {
        return new Vector3(
            Random.Range(minPosition.x, maxPosition.x),
            Random.Range(minPosition.y, maxPosition.y),
            Random.Range(minPosition.z, maxPosition.z)
        );
    }

    private void UpdateParticleColor(ParticleSystem system) {
        var main = system.main;
        var colorOverLifetime = system.colorOverLifetime;
        var trails = system.trails;
        
        float r = highPassTracker.normalizedLevel;
        float g = bandPassTracker.normalizedLevel;
        float b = lowPassTracker.normalizedLevel;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, amplitudeTracker.normalizedLevel);
        
        Color currentColor = new Color(r * colorIntensity, g * colorIntensity, b * colorIntensity, 1f);
        
        // Create a gradient that fades out both color and alpha
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(currentColor, 0.0f),
                new GradientColorKey(currentColor * 0.7f, 0.5f),
                new GradientColorKey(currentColor * 0.3f, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(alpha, 0.0f),
                new GradientAlphaKey(alpha * 0.6f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        
        if (particleMaterials.TryGetValue(system, out Material systemMaterial)) {
            systemMaterial.SetColor("_BaseColor", currentColor);
            systemMaterial.SetColor("_Emission", currentColor);
        }
        
        colorOverLifetime.color = gradient;
        main.startColor = currentColor;
        
        // Update trail gradient with more sophisticated fade
        var trailGradient = new Gradient();
        trailGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(currentColor, 0.0f),
                new GradientColorKey(currentColor * 0.8f, 0.3f),
                new GradientColorKey(currentColor * 0.5f, 0.7f),
                new GradientColorKey(currentColor * 0.2f, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(alpha, 0.0f),
                new GradientAlphaKey(alpha * 0.7f, 0.3f),
                new GradientAlphaKey(alpha * 0.3f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trails.colorOverLifetime = trailGradient;
    }
}