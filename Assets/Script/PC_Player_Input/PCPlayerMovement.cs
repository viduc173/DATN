using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script điều khiển di chuyển nhân vật bằng bàn phím (PC).
/// Sử dụng: Gắn vào GameObject Player (có CharacterController hoặc Rigidbody).
/// 
/// Input:
///   - W / UpArrow      : Tiến
///   - S / DownArrow     : Lùi
///   - A / LeftArrow     : Trái
///   - D / RightArrow    : Phải
///   - Space             : Nhảy (nếu bật cho phép nhảy)
///   - LeftShift         : Chạy nhanh (Sprint)
/// 
/// Hướng di chuyển được tính dựa trên hướng nhìn của camera.
/// </summary>
public class PCPlayerMovement : MonoBehaviour
{
    [Header("Tốc độ di chuyển")]
    [Tooltip("Tốc độ đi bộ bình thường (m/s)")]
    public float walkSpeed = 4.0f;

    [Tooltip("Tốc độ chạy nhanh khi giữ LeftShift (m/s)")]
    public float sprintSpeed = 7.0f;

    [Header("Trọng lực & Nhảy")]
    [Tooltip("Cho phép nhảy hay không")]
    public bool enableJump = true;

    [Tooltip("Lực nhảy")]
    public float jumpForce = 1.2f;

    [Tooltip("Giá trị trọng lực")]
    public float gravity = -15.0f;

    [Tooltip("Thời gian chờ trước khi được nhảy lại")]
    public float jumpCooldown = 0.15f;

    [Header("Kiểm tra mặt đất")]
    [Tooltip("Offset từ vị trí chân nhân vật để kiểm tra mặt đất")]
    public float groundCheckOffset = -0.14f;

    [Tooltip("Bán kính của sphere kiểm tra mặt đất")]
    public float groundCheckRadius = 0.28f;

    [Tooltip("Layer nào được coi là mặt đất")]
    public LayerMask groundLayers;

    [Header("Nâng cao")]
    [Tooltip("Tốc độ tăng tốc / giảm tốc khi di chuyển")]
    public float speedChangeRate = 10.0f;

    // ---- Trạng thái nội bộ ----
    private CharacterController _controller;
    private float _speed;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;
    private float _jumpTimeoutDelta;
    private bool _isGrounded;
    private bool _hasAnimator;
    private Animator _animator;
    private float _lockedYPosition;

    // Animator hash IDs (nếu có Animator)
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    // ---- Input đọc từ bàn phím ----
    private Vector2 _moveInput;
    private bool _jumpInput;
    private bool _sprintInput;

    // ---- Trạng thái focus ----
    // Khi Unity Editor maximize/restore Game tab bằng bàn phím (Shift+Space),
    // Game view panel có thể không nhận keyboard focus tự động.
    // Cần track để clear input khi mất focus → tránh nhân vật bị kẹt di chuyển.
    private bool _isApplicationFocused = true;

    #region Getters công khai
    /// <summary>
    /// Tốc độ thực tế hiện tại của nhân vật
    /// </summary>
    public float CurrentSpeed => _speed;

    /// <summary>
    /// Nhân vật có đang đứng trên mặt đất không
    /// </summary>
    public bool IsGrounded => _isGrounded;

    /// <summary>
    /// Nhân vật có đang chạy nhanh không
    /// </summary>
    public bool IsSprinting => _sprintInput && _moveInput.magnitude > 0;
    #endregion

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        _jumpTimeoutDelta = jumpCooldown;
        _lockedYPosition = transform.position.y;
    }

    private void Update()
    {
        // Đọc input từ bàn phím
        ReadKeyboardInput();

        // Kiểm tra mặt đất
        GroundCheck();

        // Thực hiện di chuyển
        Move();
    }

    /// <summary>
    /// Unity callback: game được focus / mất focus (alt-tab, maximize OS window, v.v.)
    /// 
    /// LƯU Ý QUAN TRỌNG VỀ UNITY EDITOR:
    /// OnApplicationFocus KHÔNG trigger khi maximize Game tab bằng Shift+Space
    /// vì đó là thay đổi panel trong cùng OS window, không phải mất focus toàn App.
    /// 
    /// Tuy nhiên nó SẼ trigger khi:
    ///   - Alt-Tab ra ngoài rồi quay lại
    ///   - Click vào app khác rồi quay lại Unity
    ///   - Minimize/restore cửa sổ Unity
    /// </summary>
    private void OnApplicationFocus(bool hasFocus)
    {
        _isApplicationFocused = hasFocus;

        // Khi mất focus → clear input ngay lập tức để tránh nhân vật bị kẹt di chuyển
        if (!hasFocus)
        {
            _moveInput = Vector2.zero;
            _jumpInput = false;
            _sprintInput = false;
        }
    }

    /// <summary>
    /// Unity callback: game bị pause (ví dụ khi Editor Pause button được nhấn).
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            _moveInput = Vector2.zero;
            _jumpInput = false;
            _sprintInput = false;
        }
    }

    /// <summary>
    /// Đọc input từ bàn phím mỗi frame.
    /// 
    /// GHI CHÚ - TẠI SAO WASD KHÔNG NHẬN KHI MAXIMIZE GAME TAB:
    ///   Trong Unity Editor, Game tab là một panel, không phải OS window riêng.
    ///   Khi maximize bằng Shift+Space hoặc nút ⤢, keyboard focus vẫn ở Editor panel.
    ///   Input.GetKey() chỉ nhận được khi Game view panel đang có keyboard focus.
    ///   
    ///   → GIẢI PHÁP: Sau khi maximize, CLICK MỘT LẦN vào Game view để nó nhận focus.
    ///   → Trong Build thực tế thì vấn đề này KHÔNG tồn tại vì chỉ có 1 OS window.
    /// </summary>
    private void ReadKeyboardInput()
    {
        // Nếu app mất focus (alt-tab, click ra ngoài) → không đọc input
        // Tránh nhân vật bị kẹt di chuyển khi cửa sổ mất focus
        if (!_isApplicationFocused)
        {
            _moveInput = Vector2.zero;
            _jumpInput = false;
            _sprintInput = false;
            return;
        }

        // Di chuyển: WASD hoặc Arrow Keys
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;

        _moveInput = new Vector2(horizontal, vertical);
        // Clamp để đi chéo không nhanh hơn đi thẳng
        if (_moveInput.magnitude > 1f) _moveInput.Normalize();

        // Sprint (chạy nhanh)
        _sprintInput = Input.GetKey(KeyCode.LeftShift);

        // Nhảy
        _jumpInput = Input.GetKey(KeyCode.Space);
    }

    /// <summary>
    /// Kiểm tra nhân vật có đang đứng trên mặt đất không
    /// </summary>
    private void GroundCheck()
    {
        /*
        Vector3 spherePosition = new Vector3(
            transform.position.x,
            transform.position.y + groundCheckOffset,
            transform.position.z
        );

        _isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
        */

        // Tạm bỏ kiểm tra ground, mặc định luôn bằng true
        _isGrounded = true;
    }

    /// <summary>
    /// Xử lý di chuyển nhân vật dựa trên hướng Player đang nhìn (do PCCameraController xoay).
    /// Script này KHÔNG xoay Player — chỉ di chuyển theo transform.forward / transform.right.
    /// 
    /// Cấu trúc hierarchy:
    ///   Player (PCPlayerMovement + PCCameraController) ← Camera Controller xoay Yaw ở đây
    ///   ├── Camera                                     ← Camera Controller xoay Pitch ở đây
    ///   └── Offset
    /// 
    /// Vì Camera là con của Player, nếu script này xoay Player thì Camera cũng bị xoay theo
    /// → gây lỗi camera xoay khi bấm A/D. Giải pháp: để Camera Controller quản lý toàn bộ rotation.
    /// </summary>
    private void Move()
    {
        // Xác định tốc độ mục tiêu
        float targetSpeed = _sprintInput ? sprintSpeed : walkSpeed;

        // Nếu không có input di chuyển, tốc độ = 0
        if (_moveInput == Vector2.zero) targetSpeed = 0.0f;

        float inputMagnitude = _moveInput.magnitude;

        // Tăng tốc hoặc giảm tốc mượt mà dựa trên _speed cũ (Bỏ dùng _controller.velocity vì nó rất nhiễu gây giật khựng)
        _speed = Mathf.Lerp(_speed, targetSpeed * inputMagnitude, Time.deltaTime * speedChangeRate);

        // ===== HƯỚNG DI CHUYỂN DỰA TRÊN PLAYER TRANSFORM =====
        // Player đã được PCCameraController xoay theo chuột (Yaw),
        // nên transform.forward = hướng camera đang nhìn.
        Vector3 moveDirection = Vector3.zero;

        if (_moveInput != Vector2.zero)
        {
            moveDirection = transform.forward * _moveInput.y + transform.right * _moveInput.x;
            moveDirection.y = 0f; // Giữ di chuyển trên mặt phẳng ngang
            moveDirection.Normalize();
        }

        _verticalVelocity = 0f;

        // Di chuyển nhân vật trên mặt phẳng ngang, sau đó khóa lại độ cao ban đầu.
        _controller.Move(moveDirection * (_speed * Time.deltaTime));
        Vector3 lockedPosition = transform.position;
        lockedPosition.y = _lockedYPosition;
        transform.position = lockedPosition;
    }

    /// <summary>
    /// Vẽ Gizmo kiểm tra mặt đất trong Editor
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        Gizmos.color = _isGrounded ? transparentGreen : transparentRed;

        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y + groundCheckOffset, transform.position.z),
            groundCheckRadius
        );
    }
}
