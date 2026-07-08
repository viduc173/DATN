using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component gắn vào các nút ảo trong game
/// Khi được nhìn vào sẽ highlight (nổi lên), khi click sẽ trigger event
/// </summary>
public class VirtualButton : MonoBehaviour
{
	public enum MovementDirection
	{
		TowardsCamera,    // Tiến về phía camera
		AwayFromCamera,   // Lùi ra xa camera
		Forward,          // Tiến về phía trước (local Z+)
		Backward,         // Lùi về phía sau (local Z-)
		Up,              // Lên trên (local Y+)
		Down,            // Xuống dưới (local Y-)
		Right,           // Sang phải (local X+)
		Left             // Sang trái (local X-)
	}

	[Header("Button Settings")]
	[Tooltip("Tên hiển thị của nút")]
	public string buttonName = "Button";
	
	[Tooltip("Khoảng cách tối đa có thể tương tác")]
	public float maxInteractionDistance = 5f;

	[Header("Visual Feedback")]
	[Tooltip("Độ cao nổi lên khi được nhìn vào")]
	public float highlightOffset = 0.1f;
	
	[Tooltip("Hướng di chuyển khi hover")]
	public MovementDirection movementDirection = MovementDirection.TowardsCamera;
	
	[Tooltip("Tốc độ nổi lên/xuống")]
	public float highlightSpeed = 10f;
	
	[Tooltip("Thời gian chờ sau khi hover exit trước khi có thể hover lại (giây)")]
	public float hoverCooldown = 0.3f;
	
	[Tooltip("Màu sắc khi highlight (nếu có Renderer)")]
	public Color highlightColor = new Color(1f, 1f, 0f, 1f); // Vàng
	
	[Tooltip("Màu sắc bình thường")]
	public Color normalColor = Color.white;

	[Header("Events")]
	[Tooltip("Sự kiện được gọi khi nút được click")]
	public UnityEvent onButtonClick;
	
	[Tooltip("Sự kiện được gọi khi bắt đầu nhìn vào nút")]
	public UnityEvent onButtonHoverEnter;
	
	[Tooltip("Sự kiện được gọi khi không còn nhìn vào nút")]
	public UnityEvent onButtonHoverExit;

	// Private variables
	private Vector3 m_originalPosition;
	private Vector3 m_targetPosition;
	private bool m_isHighlighted = false;
	private Renderer m_renderer;
	private Material m_material;
	private Color m_currentColor;
	private float m_cooldownTimer = 0f;


	void Awake()
	{
		m_originalPosition = transform.position;
		m_targetPosition = m_originalPosition;
		
		// Lấy renderer để đổi màu
		m_renderer = GetComponent<Renderer>();
		if (m_renderer != null)
		{
			// Tạo instance riêng của material để không ảnh hưởng material gốc
			m_material = m_renderer.material;
			m_currentColor = normalColor;
			m_material.color = normalColor;
		}
	}


	void Update()
	{
		// Update cooldown timer
		if (m_cooldownTimer > 0f)
		{
			m_cooldownTimer -= Time.deltaTime;
		}
		
		// Smooth di chuyển đến vị trí target
		transform.position = Vector3.Lerp(
			transform.position, 
			m_targetPosition, 
			highlightSpeed * Time.deltaTime
		);

		// Smooth đổi màu
		if (m_material != null)
		{
			m_material.color = Color.Lerp(
				m_material.color, 
				m_currentColor, 
				highlightSpeed * Time.deltaTime
			);
		}
	}


	/// <summary>
	/// Gọi khi bắt đầu được nhìn vào (hover)
	/// </summary>
	public void OnHoverEnter(Vector3 cameraPosition)
	{
		// Kiểm tra cooldown - không cho hover nếu còn trong thời gian chờ
		if (m_cooldownTimer > 0f)
		{
			return;
		}
		
		if (m_isHighlighted) return;
		
		m_isHighlighted = true;
		
		// Tính hướng di chuyển dựa trên setting
		Vector3 moveDirection = Vector3.zero;
		
		switch (movementDirection)
		{
			case MovementDirection.TowardsCamera:
				moveDirection = (cameraPosition - transform.position).normalized;
				break;
			
			case MovementDirection.AwayFromCamera:
				moveDirection = (transform.position - cameraPosition).normalized;
				break;
			
			case MovementDirection.Forward:
				moveDirection = transform.forward;
				break;
			
			case MovementDirection.Backward:
				moveDirection = -transform.forward;
				break;
			
			case MovementDirection.Up:
				moveDirection = transform.up;
				break;
			
			case MovementDirection.Down:
				moveDirection = -transform.up;
				break;
			
			case MovementDirection.Right:
				moveDirection = transform.right;
				break;
			
			case MovementDirection.Left:
				moveDirection = -transform.right;
				break;
		}
		
		// Di chuyển theo hướng đã chọn
		m_targetPosition = m_originalPosition + moveDirection * highlightOffset;
		
		// Đổi màu
		m_currentColor = highlightColor;
		
		// Trigger event
		onButtonHoverEnter?.Invoke();
		
		Debug.Log($"[VirtualButton] Hover Enter: {buttonName}");
	}


	/// <summary>
	/// Gọi khi không còn được nhìn vào
	/// </summary>
	public void OnHoverExit()
	{
		if (!m_isHighlighted) return;
		
		m_isHighlighted = false;
		
		// Bắt đầu cooldown timer
		m_cooldownTimer = hoverCooldown;
		
		// Trở về vị trí ban đầu
		m_targetPosition = m_originalPosition;
		
		// Đổi về màu bình thường
		m_currentColor = normalColor;
		
		// Trigger event
		onButtonHoverExit?.Invoke();
		
		Debug.Log($"[VirtualButton] Hover Exit: {buttonName}");
	}


	/// <summary>
	/// Gọi khi nút được click
	/// </summary>
	public void OnButtonPressed()
	{
		if (!m_isHighlighted) return;
		
		Debug.Log($"[VirtualButton] Button Pressed: {buttonName}");
		
		// Trigger event
		onButtonClick?.Invoke();
	}


	/// <summary>
	/// Kiểm tra xem có đang được highlight không
	/// </summary>
	public bool IsHighlighted()
	{
		return m_isHighlighted;
	}


	void OnDestroy()
	{
		// Cleanup material instance
		if (m_material != null)
		{
			Destroy(m_material);
		}
	}
}
