using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerInputFromVRController : MonoBehaviour
{
    public InputActionProperty LeftHand_Move;
    public InputActionProperty RightHand_Move;
    public PlayerDriveInputManager _carControl;
    [Range(-1, 1, order = 0)]
    public int gear;

    [Header("Nitro (đẩy CẢ 2 tay lên, giữ vài giây)")]
    [Tooltip("Cả 2 stick phải đẩy lên (y > ngưỡng này) mới tính là đang giữ nitro.")]
    public float nitroStickThreshold = 0.1f;
    [Tooltip("Giữ cả 2 tay đẩy lên đủ số giây này thì nitro mới kích hoạt.")]
    public float nitroActivateDelay = 2f;

    private float accel = 0;
    private float brake = 0;
    private float steer = 0;
    private float _swithGearTimeout = -1;

    private NitroController _nitro;   // tự tìm trong scene (NitroController trên PlayerController)
    private float _nitroHoldTimer = 0; // thời gian đã giữ cả 2 tay lên liên tục

    void Start()
    {
        _carControl = GetComponentInParent<PlayerDriveInputManager>();
        _nitro = FindFirstObjectByType<NitroController>();
    }

    public void HandleSteer(float value)
    {
        steer = -(value - 0.5f) / 0.5f;
    }

    void Update()
    {
        if (_swithGearTimeout >= 0) _swithGearTimeout -= Time.deltaTime;
        brake = 0;
        accel = 0;

        float leftY = LeftHand_Move.action.ReadValue<Vector2>().y;
        float rightY = RightHand_Move.action.ReadValue<Vector2>().y;

        accel = leftY;
        Debug.Log("Accel value: " + accel);

        if (accel < 0)
        {
            brake = Mathf.Abs(accel);
            accel = 0;
        }

        if (rightY > 0 && _swithGearTimeout < 0)
        {
            gear += 1;
            _swithGearTimeout = 0.2f;
        }
        else
        if (rightY < 0 && _swithGearTimeout < 0)
        {
            gear -= 1;
            _swithGearTimeout = 0.2f;
        }

        gear = Mathf.Clamp(gear, -1, 1);

        switch (gear)
        {
            case -1: accel = Mathf.Clamp(accel / -1.5f, -1f, -0.2f); break;
            case 0: accel = 0; brake = 1; break;
        }

        if (gear > 0 || accel > 0) accel = Mathf.Clamp(accel, 0.2f, 1);

        _carControl.SetValue(steer, accel, brake, gear);

        HandleNitro(leftY, rightY);
    }

    // Đẩy CẢ 2 tay lên (y > ngưỡng) liên tục đủ nitroActivateDelay giây → phun nitro.
    // Nhả 1 trong 2 tay → reset đồng hồ và tắt nitro. Nitro tự quản charge/VFX/boost trong NitroController.
    private void HandleNitro(float leftY, float rightY)
    {
        if (_nitro == null) return;

        bool bothUp = leftY > nitroStickThreshold && rightY > nitroStickThreshold;
        _nitroHoldTimer = bothUp ? _nitroHoldTimer + Time.deltaTime : 0f;

        _nitro.SetExternalFire(bothUp && _nitroHoldTimer >= nitroActivateDelay);
    }
}
