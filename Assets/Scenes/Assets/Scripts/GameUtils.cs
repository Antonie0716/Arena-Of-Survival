using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Camada de entrada do teclado.
/// Funciona com o Input Manager antigo (padrão no Unity 2022) e também com o
/// Input System novo (padrão em alguns projetos do Unity 6), sem exigir alterações.
/// </summary>
public static class GameInput
{
    public static bool Held(KeyCode key)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(key);
#elif ENABLE_INPUT_SYSTEM
        KeyControl control = Map(key);
        return control != null && control.isPressed;
#else
        return false;
#endif
    }

    public static bool Pressed(KeyCode key)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(key);
#elif ENABLE_INPUT_SYSTEM
        KeyControl control = Map(key);
        return control != null && control.wasPressedThisFrame;
#else
        return false;
#endif
    }

    // Movimento (WASD, com setas como alternativa)
    public static bool UpHeld => Held(KeyCode.W) || Held(KeyCode.UpArrow);
    public static bool DownHeld => Held(KeyCode.S) || Held(KeyCode.DownArrow);
    public static bool LeftHeld => Held(KeyCode.A) || Held(KeyCode.LeftArrow);
    public static bool RightHeld => Held(KeyCode.D) || Held(KeyCode.RightArrow);

    public static bool UpPressed => Pressed(KeyCode.W) || Pressed(KeyCode.UpArrow);
    public static bool DownPressed => Pressed(KeyCode.S) || Pressed(KeyCode.DownArrow);
    public static bool LeftPressed => Pressed(KeyCode.A) || Pressed(KeyCode.LeftArrow);
    public static bool RightPressed => Pressed(KeyCode.D) || Pressed(KeyCode.RightArrow);

    // Ações
    public static bool AttackPressed => Pressed(KeyCode.Space);
    public static bool PausePressed => Pressed(KeyCode.Escape);

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    static KeyControl Map(KeyCode key)
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return null;
        switch (key)
        {
            case KeyCode.W: return kb.wKey;
            case KeyCode.A: return kb.aKey;
            case KeyCode.S: return kb.sKey;
            case KeyCode.D: return kb.dKey;
            case KeyCode.UpArrow: return kb.upArrowKey;
            case KeyCode.DownArrow: return kb.downArrowKey;
            case KeyCode.LeftArrow: return kb.leftArrowKey;
            case KeyCode.RightArrow: return kb.rightArrowKey;
            case KeyCode.Space: return kb.spaceKey;
            case KeyCode.Escape: return kb.escapeKey;
            case KeyCode.Return: return kb.enterKey;
            default: return null;
        }
    }
#endif
}

/// <summary>
/// Compatibilidade de física 2D entre Unity 2022 (velocity) e Unity 6 (linearVelocity).
/// </summary>
public static class Physics2DCompat
{
    public static Vector2 GetVelocity(Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVelocity(Rigidbody2D rb, Vector2 value)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = value;
#else
        rb.velocity = value;
#endif
    }
}
