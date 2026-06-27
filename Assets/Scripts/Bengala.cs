using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Transform))]
public class Bengala : MonoBehaviour
{
    public Light strongLight;
    public Light normalLight;

    public float strongBaseIntensity = 200f;
    public float normalBaseIntensity = 50f;
    public float minNormalIntensity = 0f;
    public float blinkNoiseSpeed = 1.8f;
    public float noiseSeed = 0f;

    public float burstChancePerSecond = 0.6f;
    public float burstMultiplier = 1.8f;
    public float burstDuration = 0.08f;

    public Color baseColor = Color.white;
    public float colorNoiseSpeed = 1.1f;
    public float colorNoiseAmplitude = 0.18f;

    public float explosionIntensity = 2000f;
    public float explosionRangeMultiplier = 3f;
    public float explosionDuration = 0.5f;

    public AudioClip flareSound;
    public AudioClip explosionSound;
    private AudioSource audioSource;

    public string enemyTag = "Enemy";

    private float strongOriginalRange;
    private float normalOriginalRange;
    private bool isBlinking = true;
    private bool hasExploded = false;

    private void Awake()
    {
        if (strongLight == null || normalLight == null)
        {
            var lights = GetComponentsInChildren<Light>(true);
            if (lights != null && lights.Length > 0)
            {
                if (strongLight == null) strongLight = lights[0];
                if (normalLight == null && lights.Length > 1) normalLight = lights[1];
                if (normalLight == null && lights.Length == 1) normalLight = lights[0];
            }
        }

        if (normalLight == null && strongLight == null)
        {
            enabled = false;
            return;
        }

        if (noiseSeed == 0f) noiseSeed = Random.Range(0f, 1000f);

        if (strongLight != null)
        {
            strongBaseIntensity = Mathf.Max(0f, strongBaseIntensity);
            strongLight.intensity = strongBaseIntensity;
            strongOriginalRange = strongLight.range;
        }

        if (normalLight != null)
        {
            normalBaseIntensity = Mathf.Max(0f, normalBaseIntensity);
            normalLight.intensity = normalBaseIntensity;
            normalOriginalRange = normalLight.range;
            if (baseColor == default) baseColor = normalLight.color; else normalLight.color = baseColor;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource != null && flareSound != null)
        {
            audioSource.clip = flareSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void OnEnable()
    {
        isBlinking = true;
        StartCoroutine(BlinkLoop());
        StartCoroutine(AutoExplode());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();
    }

    private void Update()
    {
    }

    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            if (!isBlinking) { yield return null; continue; }

            float time = Time.time;
            float noise = Mathf.PerlinNoise(noiseSeed, time * blinkNoiseSpeed);
            float normalIntensity = Mathf.Lerp(minNormalIntensity, normalBaseIntensity, noise);

            if (Random.value < burstChancePerSecond * Time.deltaTime) StartCoroutine(BurstPulse());

            float cNoise = Mathf.PerlinNoise(noiseSeed + 37.13f, time * colorNoiseSpeed);
            float colorVariation = (cNoise - 0.5f) * 2f * colorNoiseAmplitude;
            Color col = baseColor * (1f + colorVariation);

            if (normalLight != null)
            {
                normalLight.color = col;
                normalLight.intensity = normalIntensity;
            }

            if (strongLight != null)
            {
                strongLight.intensity = Mathf.Lerp(strongBaseIntensity * 0.85f, strongBaseIntensity, noise * 0.6f + 0.4f);
            }

            yield return null;
        }
    }

    private IEnumerator BurstPulse()
    {
        if (normalLight == null) yield break;
        float start = normalLight.intensity;
        float target = Mathf.Max(start, normalBaseIntensity) * burstMultiplier;
        float t = 0f;
        while (t < burstDuration)
        {
            t += Time.deltaTime;
            normalLight.intensity = Mathf.Lerp(start, target, t / burstDuration);
            yield return null;
        }

        float decayTime = burstDuration * 1.5f;
        t = 0f;
        while (t < decayTime)
        {
            t += Time.deltaTime;
            normalLight.intensity = Mathf.Lerp(target, normalBaseIntensity, t / decayTime);
            yield return null;
        }

        normalLight.intensity = normalBaseIntensity;
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        isBlinking = false;
        if (audioSource != null)
        {
            audioSource.Stop();
            if (explosionSound != null)
                audioSource.PlayOneShot(explosionSound);
        }

        GameObject[] enemies = null;
        try { enemies = GameObject.FindGameObjectsWithTag(enemyTag); } catch { enemies = null; }
        if (enemies != null)
        {
            foreach (var e in enemies)
            {
                if (e == null) continue;
                var be = e.GetComponent<BaseEnemy>();
                if (be != null)
                {
                    be.TakeDamage(999999f, 1f);
                    continue;
                }
                var en = e.GetComponent<Enemy>();
                if (en != null) en.TakeDamage(999999f, 1f);
            }
        }

        StartCoroutine(ExplosionCoroutine());
    }

    private IEnumerator AutoExplode()
    {
        yield return new WaitForSeconds(3f);
        if (!hasExploded) Explode();
    }

    private IEnumerator ExplosionCoroutine()
    {
        float prevStrongIntensity = strongLight != null ? strongLight.intensity : 0f;
        float prevNormalIntensity = normalLight != null ? normalLight.intensity : 0f;
        float prevStrongRange = strongLight != null ? strongLight.range : 0f;
        float prevNormalRange = normalLight != null ? normalLight.range : 0f;
        Color prevNormalColor = normalLight != null ? normalLight.color : Color.white;

        if (strongLight != null)
        {
            strongLight.intensity = explosionIntensity;
            strongLight.range = strongOriginalRange * explosionRangeMultiplier;
        }

        if (normalLight != null)
        {
            normalLight.intensity = explosionIntensity * 0.6f;
            normalLight.range = normalOriginalRange * (explosionRangeMultiplier * 0.8f);
            normalLight.color = baseColor;
        }

        yield return new WaitForSeconds(explosionDuration);

        if (strongLight != null)
        {
            strongLight.intensity = strongBaseIntensity;
            strongLight.range = prevStrongRange;
        }

        if (normalLight != null)
        {
            normalLight.intensity = normalBaseIntensity;
            normalLight.range = prevNormalRange;
            normalLight.color = prevNormalColor;
        }

        yield return null;
        Destroy(gameObject);
    }
}
