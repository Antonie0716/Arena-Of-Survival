using UnityEngine;

/// <summary>
/// Ajusta a câmera para que toda a arena
/// caiba corretamente na tela.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFit : MonoBehaviour
{
    public float targetWidth = 32f;
    public float targetHeight = 18f;

    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();

        cam.orthographic = true;

        Apply();
    }

    void LateUpdate()
    {
        Apply();
    }

    /// <summary>
    /// Calcula e aplica o tamanho correto da câmera.
    /// </summary>
    void Apply()
    {
        float aspect = cam.aspect;

        // Evita que o valor da proporção fique muito pequeno.
        if (aspect < 0.1f)
        {
            aspect = 0.1f;
        }

        float sizeByHeight = targetHeight * 0.5f;

        float sizeByWidth =
            targetWidth * 0.5f / aspect;

        // Usa uma função para descobrir qual tamanho é maior.
        float cameraSize =
            GetLargerValue(sizeByHeight, sizeByWidth);

        cam.orthographicSize = cameraSize;
    }

    /// <summary>
    /// Recebe dois valores e retorna o maior.
    /// </summary>
    float GetLargerValue(float value1, float value2)
    {
        if (value1 > value2)
        {
            return value1;
        }
        else
        {
            return value2;
        }
    }
}