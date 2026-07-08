using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;

public class PlayerDriverInputFromKeyboard : MonoBehaviour
{
    [Header("- Sử dụng phím W để chạy động cơ \n" +
            "- Sử dụng phím A D để rẽ trái phải \n" +
            "- Sử dụng Phím S để phanh \n" +
            "- Sử dụng phím Q để giảm gear xuống, E để tăng gear lên\n" +
            "Lưu Ý: " +
            "   + Trong Script này có bộ Gear, khi gear là số dương W sẽ tiến, gear là số âm W sẽ lùi \n" +
            "   + Với số dương, gear càng cao thì xe chạy càng chậm, gear 1 là nhanh nhất nhưng khó điều khiển nhất")]

    [Header("Camera Control")]
    [Tooltip("Độ nhạy của chuột")]
    public float _sensitive = 24;
    [Tooltip("Góc xoay xuống tối đa")]
    public float BottomClamp;
    [Tooltip("Góc xoay lên tối đa")]
    public float TopClamp;
    [Tooltip("Lớn hơn độ dài này thì camera mới được phép di chuyển")]
    public float _threshold = 0.01f;
    [Tooltip("Có khóa Camera hay không")]
    public bool LockCameraPosition = false;
    [Tooltip("Camera lùi phóng to")]
    public Transform BackwardCameraZoom;

    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    [SerializeField]
    private Transform _cinemachineCameraTarget;
    private float Angle = 0.0f;

    [Space(10)]

    [Header("Kiểm Soát Chế Độ Lái")]

    [Tooltip("Chế độ lái của xe: \n" +
        "1: Khi nhận tín hiệu từ bàn phím, bánh sẽ sẽ quay sang trái hoặc sang phải ngay lập tức," +
            "bánh xe quay trở về khi không nhận tín hiệu từ bàn phím nữa \n" +
        "2: Khi nhận Khi nhận tín hiệu từ bàn phím, bánh sẽ sẽ quay sang trái hoặc sang phải" +
            " từ từ theo WheelTurn * Time.deltaTime, với giá trị WheelTurn ở bên dưới. " +
            " Khi không nhận tín hiệu từ bàn phím nữa thì bánh xe sẽ tự động quay về \n" +
        "3: Tương tự số 2 nhưng bánh xe sẽ không tự động quay về, hệ số chuyển hướng được giảm đi 10 lần" +
            " (thực tế nhất nhưng dùng bàn phím, rất khó lái)\n" +
        "4: Tương tự số 2, bánh xe quay từ từ và tự động quay về với tốc độ wheelReturnSpeed khi thả phím")]
    [Range(1, 4, order = 1)]
    public int _driveMode = 1;
    [Tooltip("Thả vô lăng vào đây")]
    public XRKnob SteerObject;

    [Space(10)]

    [Header("Thông số của xe")]
    [Tooltip("Hệ số chuyển hướng (rẽ) của bánh")]
    public float wheelTurnSpeed = 3;
    public float wheelReturnSpeed = 12; 
    [SerializeField]
    private float steer;
    [SerializeField]
    private float accel;
    [SerializeField]
    private float brake;
    [SerializeField]
    private float handBrake;

    /// <summary>
    /// là số đang được điều chỉnh trong hộp số
    /// </summary>
    [Range(-1, 1, order = 0)]
    public int gear;

    #region Getters & Setter 
    /// <summary>
    /// Góc xoay hiện tại của bánh trước xe (chuẩn hóa theo 1)
    /// </summary>
    public float Steer { get { return steer; } }
    /// <summary>
    /// Tốc độ nạp vào hiện tại của bánh sau xe (chuẩn hóa theo 1)
    /// </summary>
    public float Accel { get { return accel; } }
    /// <summary>
    /// Mức độ phanh của xe (chuẩn hóa theo 1)
    /// </summary>
    public float Brake { get { return brake; } } 
    /// <summary>
    /// Tốc độ hiện tại của xe
    /// </summary>
    public float Velocity { get { return rb.linearVelocity.magnitude; } }
    #endregion

    private PlayerDriveInputManager _carControl;
    private Rigidbody rb;

    void Awake()
    {
        //lock mouse
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        //get car control & rigidbody
        _carControl = GetComponent<PlayerDriveInputManager>();
        rb = GetComponentInParent<Rigidbody>();

        //Import Root & Virtual Camera
        //VirtualCamera = GameObject.FindGameObjectWithTag("VirtualCamera").transform;
    }

    void Start() 
    {
        //Change max and min angle of Steer
        SteerObject.maxAngle = 90;
        SteerObject.minAngle = -90; 
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (BackwardCameraZoom != null) BackwardCameraZoom.gameObject.SetActive(!BackwardCameraZoom.gameObject.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) _carControl.TurnLeftSign();
        if (Input.GetKeyDown(KeyCode.Alpha3)) _carControl.TurnRightSign();

        HandleRotateInput();
        CarInput(true);
        UpdateSteerObject();
    }

    private void FixedUpdate()
    {
        HandleCameraRotation();
    }

    #region  ___________________________________ Camera Control ___________________________________
    /// <summary>
    /// Claim góc trục hoành
    /// </summary>
    private static float ClampAngleY(float lfAngle)
    {
        lfAngle %= 360;
        if (lfAngle < 0) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return lfAngle;
    }
    /// <summary>
    /// Clamp góc trục tung
    /// </summary>
    private static float ClampAngleX(float lfAngle, float Bottom, float Top)
    {
        lfAngle %= 360;
        if (lfAngle < 0) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        if (lfAngle <= 180 && lfAngle > Bottom) return Bottom;
        if (lfAngle > 180 && lfAngle < 360 - Top) return 360 - Top;
        return lfAngle;
    }
    private void HandleRotateInput()
    {
        // Con trỏ đang mở khóa = đang mở menu/pause/bảng kết quả → không xoay camera theo chuột
        // (tránh camera xoay loạn khi người chơi đang thao tác UI).
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        var look = new Vector2(Input.GetAxis("Mouse X") * 10, -Input.GetAxis("Mouse Y") * 10);

        if (look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            // độ nhạy chạm màn hình
            float deltaTimeMultiplier = _sensitive * Time.deltaTime * 0.3f;

            _cinemachineTargetYaw += look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += look.y * deltaTimeMultiplier;
        }

        _cinemachineTargetYaw = ClampAngleY(_cinemachineTargetYaw);
        _cinemachineTargetPitch = ClampAngleX(_cinemachineTargetPitch, BottomClamp, TopClamp);

        //điều chỉnh cách rotate của camera, điều chỉnh này gây ảnh hưởng đến xe khi xoay nên ta sẽ có 1 biến _input.Onlooking ghi nhận là đang di chuyển screen
        look = Vector2.zero;
    }
    private void HandleCameraRotation()
    {
        _cinemachineCameraTarget.transform.localRotation =
          Quaternion.Slerp(_cinemachineCameraTarget.transform.localRotation, 
            Quaternion.Euler(
                ClampAngleX(_cinemachineTargetPitch, BottomClamp, TopClamp),ClampAngleY(_cinemachineTargetYaw),0),
                12.0f);
    }
    #endregion
    private void CarInput(bool showMess = false)
    {
        accel = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1f : 0f;

        // LƯU Ý: LeftShift đã chuyển sang phun NITRO (NitroController) — KHÔNG còn dùng để phanh.
        brake = (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) ? 1f : 0;

        handBrake = Input.GetKey(KeyCode.Space) ? 1 : 0f;

        if (_driveMode == 1)
        {
            steer = ((Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? -1f : 0)
                            + ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1f : 0);
        }
        else
        if (_driveMode == 2)
        {
            var target_steer = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? -1f : 0
                                + ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1f : 0);

            float speed;
            if ((steer < 0 && target_steer < 0) || (steer > 0 && target_steer > 0))
                speed = wheelTurnSpeed;
            else
                speed = wheelReturnSpeed;

            steer = Mathf.Lerp(steer, target_steer, speed * Time.deltaTime);
        }
        else
        if (_driveMode == 3)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                steer = Mathf.Lerp(steer, -1, wheelTurnSpeed / 10 * Time.deltaTime);
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                steer = Mathf.Lerp(steer, 1, wheelTurnSpeed / 10 * Time.deltaTime);
        }
        else
        if (_driveMode == 4)
        {
            var target_steer = (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) ? -1f : 0
                                + ((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) ? 1f : 0);

            float speed;
            if (target_steer == 0)
                speed = wheelReturnSpeed;
            else if ((steer < 0 && target_steer < 0) || (steer > 0 && target_steer > 0))
                speed = wheelTurnSpeed / 3;
            else
                speed = wheelReturnSpeed;

            steer = Mathf.Lerp(steer, target_steer, speed * Time.deltaTime);
        }

        gear += (Input.GetKeyDown(KeyCode.Q) ? -1 : 0) + (Input.GetKeyDown(KeyCode.E) ? 1 : 0);
        gear = Mathf.Clamp(gear, -1, 1);

        switch (gear)
        {
            case -1: accel = Mathf.Clamp(accel / -1.5f, -1f, -0.2f); break;
            case 0: accel = 0; brake = 1; break;
        }

        if (gear > 0 || accel > 0) accel = Mathf.Clamp(accel, 0.2f, 1);

        //Cập nhật giá trị
        _carControl.SetValue(steer, accel, brake, handBrake, gear);

        // if (showMess)
        // {
        //     Debug.Log("Góc lái là " + steer);
        //     Debug.Log("Gas là " + accel);
        //     Debug.Log("Phanh là " + brake);
        // }

    }
    private void UpdateSteerObject()
    {
        if (_driveMode == 1)
            SteerObject.value = Mathf.Lerp(SteerObject.value, (1 - steer) / 2, 12 * Time.deltaTime);
        else
            SteerObject.value = (1 - steer) / 2;
    }    
}
