using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Controla as entradas do teclado.
/// Funciona tanto com o sistema antigo de entrada
/// quanto com o novo Input System do Unity.
/// </summary>
public static class GameInput
{
    /// <summary>
    /// Verifica se uma tecla está sendo segurada.
    /// </summary>
    public static bool Held(KeyCode key)
    {
#if ENABLE_LEGACY_INPUT_MANAGER

        return Input.GetKey(key);

#elif ENABLE_INPUT_SYSTEM

        KeyControl control =
            Map(key);

        if (control != null)
        {
            if (control.isPressed)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

#else

        return false;

#endif
    }

    /// <summary>
    /// Verifica se uma tecla foi pressionada
    /// neste momento.
    /// </summary>
    public static bool Pressed(KeyCode key)
    {
#if ENABLE_LEGACY_INPUT_MANAGER

        return Input.GetKeyDown(key);

#elif ENABLE_INPUT_SYSTEM

        KeyControl control =
            Map(key);

        if (control != null)
        {
            if (control.wasPressedThisFrame)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

#else

        return false;

#endif
    }

    // =========================================
    // MOVIMENTO
    // =========================================

    public static bool UpHeld
    {
        get
        {
            if (Held(KeyCode.W) || Held(KeyCode.UpArrow))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static bool DownHeld
    {
        get
        {
            if (Held(KeyCode.S) || Held(KeyCode.DownArrow))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static bool LeftHeld
    {
        get
        {
            if (Held(KeyCode.A) || Held(KeyCode.LeftArrow))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static bool RightHeld
    {
        get
        {
            if (Held(KeyCode.D) || Held(KeyCode.RightArrow))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    // =========================================
    // MOVIMENTO - TECLA PRESSIONADA
    // =========================================

    public static bool UpPressed
    {
        get
        {
            if (Pressed(KeyCode.W) || Pressed(KeyCode.UpArrow))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static bool DownPressed
    {
        get
        {
            if (Pressed(KeyCode.S) || Pressed(KeyCode.DownArrow))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static bool LeftPressed
    {
        get
        {
            if (Pressed(KeyCode.A) || Pressed(KeyCode.LeftArrow))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static bool RightPressed
    {
        get
        {
            if (Pressed(KeyCode.D) || Pressed(KeyCode.RightArrow))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    // =========================================
    // AÇÕES
    // =========================================

    public static bool AttackPressed
    {
        get
        {
            if (Pressed(KeyCode.Space))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static bool PausePressed
    {
        get
        {
            if (Pressed(KeyCode.Escape))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER

    /// <summary>
    /// Converte uma tecla do KeyCode
    /// para o novo Input System.
    /// </summary>
    static KeyControl Map(KeyCode key)
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            return null;
        }

        if (key == KeyCode.W)
        {
            return keyboard.wKey;
        }
        else if (key == KeyCode.A)
        {
            return keyboard.aKey;
        }
        else if (key == KeyCode.S)
        {
            return keyboard.sKey;
        }
        else if (key == KeyCode.D)
        {
            return keyboard.dKey;
        }
        else if (key == KeyCode.UpArrow)
        {
            return keyboard.upArrowKey;
        }
        else if (key == KeyCode.DownArrow)
        {
            return keyboard.downArrowKey;
        }
        else if (key == KeyCode.LeftArrow)
        {
            return keyboard.leftArrowKey;
        }
        else if (key == KeyCode.RightArrow)
        {
            return keyboard.rightArrowKey;
        }
        else if (key == KeyCode.Space)
        {
            return keyboard.spaceKey;
        }
        else if (key == KeyCode.Escape)
        {
            return keyboard.escapeKey;
        }
        else if (key == KeyCode.Return)
        {
            return keyboard.enterKey;
        }
        else
        {
            return null;
        }
    }

#endif
}

/// <summary>
/// Compatibilidade de física 2D
/// entre versões diferentes do Unity.
/// </summary>
public static class Physics2DCompat
{
    /// <summary>
    /// Retorna a velocidade atual do objeto.
    /// </summary>
    public static Vector2 GetVelocity(Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER

        return rb.linearVelocity;

#else

        return rb.velocity;

#endif
    }

    /// <summary>
    /// Define uma nova velocidade para o objeto.
    /// </summary>
    public static void SetVelocity(
        Rigidbody2D rb,
        Vector2 value
    )
    {
#if UNITY_6000_0_OR_NEWER

        rb.linearVelocity =
            value;

#else

        rb.velocity =
            value;

#endif
    }
}