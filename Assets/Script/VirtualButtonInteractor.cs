using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Component gắn vào Camera để raycast và tương tác với VirtualButton
/// Nhìn vào button sẽ highlight, click chuột sẽ trigger event
/// Hỗ trợ cả UI Canvas và 3D Objects
/// </summary>
public class VirtualButtonInteractor : MonoBehaviour
{
	[Header("Raycast Settings")]
	[Tooltip("Camera để raycast (nếu null sẽ tự động lấy Camera component)")]
	public Camera targetCamera;
	
	[Tooltip("Khoảng cách raycast tối đa")]
	public float maxRaycastDistance = 10f;
	
	[Tooltip("Layer mask cho raycast")]
	public LayerMask raycastLayerMask = ~0; // All layers

	[Tooltip("Raycast cho UI Canvas")]
	public bool enableUIRaycast = true;

	[Header("Input Settings")]
	[Tooltip("Nút chuột để click button (0=Left, 1=Right, 2=Middle)")]
	public int mouseButton = 0;
	
	[Tooltip("Key để thay thế click chuột (nếu muốn)")]
	public KeyCode interactKey = KeyCode.None;

	[Header("Visual Feedback")]
	[Tooltip("Hiển thị crosshair khi nhìn vào button")]
	public bool showCrosshair = false;
	
	[Tooltip("Màu crosshair khi nhìn vào button")]
	public Color crosshairHighlightColor = Color.yellow;
	
	[Tooltip("Màu crosshair bình thường")]
	public Color crosshairNormalColor = Color.white;

	[Header("Debug")]
	[Tooltip("Hiển thị debug ray trong Scene view")]
	public bool showDebugRay = true;

	private EventSystem m_eventSystem;
	private PointerEventData m_pointerEventData;
	// Private variables
	private VirtualButton m_currentButton;
	private Camera m_camera;


	void Awake()
	{
		// Lấy camera
		if (targetCamera == null)
		{
			m_camera = GetComponent<Camera>();
			if (m_camera == null)
			{
				m_camera = Camera.main;
			}
		}
		else
		{
			m_camera = targetCamera;
		}

		if (m_camera == null)
		{

		// Lấy EventSystem cho UI raycast
		m_eventSystem = EventSystem.current;
		if (m_eventSystem == null && enableUIRaycast)
		{
			Debug.LogWarning("[VirtualButtonInteractor] Không tìm thấy EventSystem! UI Raycast sẽ không hoạt động.");
		}
			Debug.LogError("[VirtualButtonInteractor] Không tìm thấy Camera!");
		}
	}


	void Update()
	{
		if (m_camera == null) return;

		// Raycast từ giữa màn hình
		PerformRaycast();

		// Xử lý input
		HandleInput();
	}


	/// <summary>
	/// Thực hiện raycast từ camera
	/// </summary>
	private void PerformRaycast()
	{
		// Raycast từ giữa màn hình
		Ray ray = m_camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
		RaycastHit hit;

		// Debug ray
		if (showDebugRay)
		{
			Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.red);
		}

		// Thực hiện raycast
		if (Physics.Raycast(ray, out hit, maxRaycastDistance, raycastLayerMask))
		{
            Debug.Log("Hit: " + hit.collider.name);
			// Kiểm tra xem có VirtualButton không
			VirtualButton button = hit.collider.GetComponent<VirtualButton>();

			if (button != null)
			{
				// Kiểm tra khoảng cách
				float distance = Vector3.Distance(m_camera.transform.position, hit.point);
				
				if (distance <= button.maxInteractionDistance)
				{
					// Nếu đây là button mới
					if (m_currentButton != button)
					{
						// Exit button cũ
						if (m_currentButton != null)
						{
							m_currentButton.OnHoverExit();
						}

						// Enter button mới
						m_currentButton = button;
						m_currentButton.OnHoverEnter(m_camera.transform.position);
					}

					return;
				}
			}
		}

		// Không raycast trúng button nào
		if (m_currentButton != null)
		{
			m_currentButton.OnHoverExit();
			m_currentButton = null;
		}
	}


	/// <summary>
	/// Xử lý input để click button
	/// </summary>
	private void HandleInput()
	{
		// Kiểm tra input
		bool clickInput = Input.GetMouseButtonDown(mouseButton);
		
		if (interactKey != KeyCode.None)
		{
			clickInput = clickInput || Input.GetKeyDown(interactKey);
		}

		// Nếu có click và đang nhìn vào button
		if (clickInput && m_currentButton != null)
		{
			m_currentButton.OnButtonPressed();
		}
	}


	/// <summary>
	/// Vẽ crosshair (gọi từ OnGUI nếu muốn)
	/// </summary>
	void OnGUI()
	{
		if (!showCrosshair) return;

		// Màu crosshair
		Color crosshairColor = m_currentButton != null ? crosshairHighlightColor : crosshairNormalColor;

		// Vẽ crosshair giữa màn hình
		float centerX = Screen.width / 2f;
		float centerY = Screen.height / 2f;
		float size = 10f;

		// Tạo texture màu
		Texture2D texture = new Texture2D(1, 1);
		texture.SetPixel(0, 0, crosshairColor);
		texture.Apply();

		// Vẽ các đường crosshair
		GUI.DrawTexture(new Rect(centerX - size, centerY - 1, size * 2, 2), texture);
		GUI.DrawTexture(new Rect(centerX - 1, centerY - size, 2, size * 2), texture);

		// Hiển thị tên button nếu đang nhìn vào
		if (m_currentButton != null)
		{
			GUIStyle style = new GUIStyle(GUI.skin.label);
			style.alignment = TextAnchor.MiddleCenter;
			style.normal.textColor = crosshairHighlightColor;
			style.fontSize = 16;

			GUI.Label(new Rect(centerX - 100, centerY + 30, 200, 30), m_currentButton.buttonName, style);
		}

		Destroy(texture);
	}


	/// <summary>
	/// Lấy button hiện tại đang được nhìn vào
	/// </summary>
	public VirtualButton GetCurrentButton()
	{
		return m_currentButton;
	}
}
