using UnityEngine;

/// <summary>
/// Component gắn vào tay VR (tay phải) để tương tác với VirtualButton
/// Sử dụng raycast để phát hiện button và trigger để click
/// </summary>
[RequireComponent(typeof(HandInputValue))]
public class VRButtonInteractor : MonoBehaviour
{
	[Header("Raycast Settings")]
	[Tooltip("Chiều dài tia raycast")]
	public float rayDistance = 10f;
	
	[Tooltip("Layer mask của các button có thể tương tác")]
	public LayerMask buttonLayer = ~0; // Mặc định là tất cả layers
	
	[Header("Trigger Settings")]
	[Tooltip("Ngưỡng trigger để kích hoạt button (0-1)")]
	[Range(0f, 1f)]
	public float triggerThreshold = 0.5f;
	
	[Header("Visual Feedback")]
	[Tooltip("Hiển thị tia raycast trong Scene view")]
	public bool showRayDebug = true;
	
	[Tooltip("Màu của tia khi không trúng gì")]
	public Color rayNormalColor = Color.white;
	
	[Tooltip("Màu của tia khi trúng button")]
	public Color rayHitColor = Color.green;
	
	[Tooltip("Prefab để hiển thị điểm trúng (tùy chọn)")]
	public GameObject hitIndicatorPrefab;

	// Private variables
	private HandInputValue m_handInput;
	private VirtualButton m_currentButton;
	private bool m_wasTriggering = false;
	private GameObject m_hitIndicator;
	private Vector3 m_hitPoint;
	private bool m_hasHit = false;


	void Awake()
	{
		m_handInput = GetComponent<HandInputValue>();
		
		// Tạo hit indicator nếu có prefab
		if (hitIndicatorPrefab != null)
		{
			m_hitIndicator = Instantiate(hitIndicatorPrefab);
			m_hitIndicator.SetActive(false);
		}
	}


	void Update()
	{
		PerformRaycast();
		HandleTriggerInput();
	}


	/// <summary>
	/// Thực hiện raycast từ vị trí tay
	/// </summary>
	private void PerformRaycast()
	{
		Ray ray = new Ray(transform.position, transform.forward);
		RaycastHit hit;
		
		m_hasHit = false;
		
		if (Physics.Raycast(ray, out hit, rayDistance, buttonLayer))
		{
			m_hasHit = true;
			m_hitPoint = hit.point;
			
			// Kiểm tra xem có phải VirtualButton không
			VirtualButton button = hit.collider.GetComponent<VirtualButton>();
			
			if (button != null)
			{
				// Nếu đây là button mới (khác button trước đó)
				if (button != m_currentButton)
				{
					// Thoát khỏi button cũ nếu có
					if (m_currentButton != null)
					{
						m_currentButton.OnHoverExit();
					}
					
					// Hover vào button mới
					m_currentButton = button;
					
					// Lấy vị trí camera (hoặc có thể dùng vị trí tay)
					Vector3 cameraPos = Camera.main != null ? Camera.main.transform.position : transform.position;
					m_currentButton.OnHoverEnter(cameraPos);
				}
				
				// Hiển thị hit indicator tại vị trí trúng
				if (m_hitIndicator != null)
				{
					m_hitIndicator.SetActive(true);
					m_hitIndicator.transform.position = hit.point;
					m_hitIndicator.transform.rotation = Quaternion.LookRotation(hit.normal);
				}
			}
			else
			{
				// Trúng object khác, không phải button
				ClearCurrentButton();
			}
		}
		else
		{
			// Không trúng gì cả
			ClearCurrentButton();
		}
		
		// Debug visualization
		if (showRayDebug)
		{
			Color rayColor = m_currentButton != null ? rayHitColor : rayNormalColor;
			Debug.DrawRay(transform.position, transform.forward * rayDistance, rayColor);
		}
	}


	/// <summary>
	/// Xử lý input từ trigger
	/// </summary>
	private void HandleTriggerInput()
	{
		if (m_handInput == null) return;
		
		float currentTriggerValue = m_handInput.triggerValue;
		bool isTriggering = currentTriggerValue > triggerThreshold;
		
		// Phát hiện khi bắt đầu bóp trigger (rising edge)
		if (isTriggering && !m_wasTriggering)
		{
			// Nếu đang hover button thì click nó
			if (m_currentButton != null)
			{
				m_currentButton.OnButtonPressed();
				
				// Có thể thêm haptic feedback ở đây
				TriggerHapticFeedback();
			}
		}
		
		m_wasTriggering = isTriggering;
	}


	/// <summary>
	/// Xóa button hiện tại (khi không còn nhìn vào)
	/// </summary>
	private void ClearCurrentButton()
	{
		if (m_currentButton != null)
		{
			m_currentButton.OnHoverExit();
			m_currentButton = null;
		}
		
		if (m_hitIndicator != null)
		{
			m_hitIndicator.SetActive(false);
		}
	}


	/// <summary>
	/// Kích hoạt haptic feedback (rung tay)
	/// </summary>
	private void TriggerHapticFeedback()
	{
		// TODO: Implement haptic feedback cho VR controller
		// Ví dụ với XR Interaction Toolkit:
		// if (TryGetComponent<XRController>(out var controller))
		// {
		//     controller.SendHapticImpulse(0.5f, 0.1f);
		// }
		
		Debug.Log("[VRButtonInteractor] Haptic feedback triggered");
	}


	/// <summary>
	/// Lấy button hiện đang được hover
	/// </summary>
	public VirtualButton GetCurrentButton()
	{
		return m_currentButton;
	}


	/// <summary>
	/// Kiểm tra xem có đang hover button nào không
	/// </summary>
	public bool IsHoveringButton()
	{
		return m_currentButton != null;
	}


	void OnDisable()
	{
		// Cleanup khi disable
		ClearCurrentButton();
	}


	void OnDestroy()
	{
		// Cleanup hit indicator
		if (m_hitIndicator != null)
		{
			Destroy(m_hitIndicator);
		}
	}


	// Vẽ gizmos trong Scene view để dễ debug
	void OnDrawGizmos()
	{
		if (!showRayDebug) return;
		
		Gizmos.color = m_currentButton != null ? rayHitColor : rayNormalColor;
		Gizmos.DrawLine(transform.position, transform.position + transform.forward * rayDistance);
		
		if (m_hasHit)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(m_hitPoint, 0.05f);
		}
	}
}
