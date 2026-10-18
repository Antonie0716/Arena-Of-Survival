using UnityEngine;

/// <summary>
/// Câmera fixa: ajusta o tamanho ortográfico para que a arena inteira
/// (32 x 18 unidades, incluindo a faixa do HUD) caiba em qualquer proporção de tela.
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

    void Apply()
    {
        float aspect = Mathf.Max(0.1f, cam.aspect);
        float sizeByHeight = targetHeight * 0.5f;
        float sizeByWidth = targetWidth * 0.5f / aspect;
        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
    }
}
