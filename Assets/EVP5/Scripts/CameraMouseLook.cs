using UnityEngine;

namespace EVP
{
	/// <summary>
	/// Component xoay camera bằng chuột, tương tự PlayerDriverInputFromKeyboard
	/// Gắn vào GameObject muốn xoay (thường là Camera hoặc Camera Root)
	/// </summary>
	public class CameraMouseLook : MonoBehaviour
	{
		[Header("Mouse Look Settings")]
		[Tooltip("Bật/tắt chức năng xoay camera bằng chuột")]
		public bool enableMouseLook = true;
		
		[Tooltip("Độ nhạy của chuột")]
		[Range(1f, 100f)]
		public float mouseSensitivity = 24f;
		
		[Tooltip("Góc xoay xuống tối đa (độ)")]
		[Range(0f, 89f)]
		public float minVerticalAngle = 60f;
		
		[Tooltip("Góc xoay lên tối đa (độ)")]
		[Range(0f, 89f)]
		public float maxVerticalAngle = 60f;
		
		[Tooltip("Độ mượt khi xoay camera")]
		[Range(1f, 30f)]
		public float rotationSmoothness = 12f;
		
		[Tooltip("Ngưỡng di chuyển chuột tối thiểu để camera xoay")]
		public float mouseThreshold = 0.01f;

		[Header("Cursor Settings")]
		[Tooltip("Tự động khóa và ẩn con trỏ chuột")]
		public bool lockCursor = true;

		// Private variables
		private float m_targetYaw;
		private float m_targetPitch;
		private float m_currentYaw;
		private float m_currentPitch;
		private Transform m_transform;


		void Awake()
		{
			m_transform = transform;
		}


		void OnEnable()
		{
			// Lock cursor if needed
			if (lockCursor && enableMouseLook)
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}

			// Initialize rotation angles from current transform
			Vector3 currentAngles = m_transform.localEulerAngles;
			m_targetYaw = m_currentYaw = currentAngles.y;
			m_targetPitch = m_currentPitch = currentAngles.x;
		}


		void OnDisable()
		{
			// Unlock cursor when disabled
			if (lockCursor)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}


		void Update()
		{
			if (!enableMouseLook) return;

			HandleMouseInput();
		}


		void LateUpdate()
		{
			if (!enableMouseLook) return;

			ApplyRotation();
		}


		/// <summary>
		/// Xử lý input từ chuột
		/// </summary>
		private void HandleMouseInput()
		{
			Vector2 mouseDelta = new Vector2(
				Input.GetAxis("Mouse X") * 10f, 
				-Input.GetAxis("Mouse Y") * 10f
			);

			// Chỉ xử lý khi chuột di chuyển đủ lớn
			if (mouseDelta.sqrMagnitude >= mouseThreshold)
			{
				float deltaTimeMultiplier = mouseSensitivity * Time.deltaTime * 0.3f;

				m_targetYaw += mouseDelta.x * deltaTimeMultiplier;
				m_targetPitch += mouseDelta.y * deltaTimeMultiplier;
			}

			// Clamp angles
			m_targetYaw = ClampAngle360(m_targetYaw);
			m_targetPitch = ClampAngleWithLimits(m_targetPitch, minVerticalAngle, maxVerticalAngle);
		}


		/// <summary>
		/// Áp dụng rotation lên transform
		/// </summary>
		private void ApplyRotation()
		{
			// Smooth interpolation
			m_currentYaw = Mathf.LerpAngle(m_currentYaw, m_targetYaw, rotationSmoothness * Time.deltaTime);
			m_currentPitch = Mathf.LerpAngle(m_currentPitch, m_targetPitch, rotationSmoothness * Time.deltaTime);

			// Apply rotation
			m_transform.localRotation = Quaternion.Euler(
				ClampAngleWithLimits(m_currentPitch, minVerticalAngle, maxVerticalAngle),
				ClampAngle360(m_currentYaw),
				0f
			);
		}


		/// <summary>
		/// Clamp góc trong khoảng 0-360
		/// </summary>
		private float ClampAngle360(float angle)
		{
			angle %= 360f;
			if (angle < 0f) angle += 360f;
			if (angle > 360f) angle -= 360f;
			return angle;
		}


		/// <summary>
		/// Clamp góc với giới hạn min/max (dành cho góc dọc)
		/// </summary>
		private float ClampAngleWithLimits(float angle, float min, float max)
		{
			angle %= 360f;
			if (angle < 0f) angle += 360f;
			if (angle > 360f) angle -= 360f;
			
			// Clamp vertical angle
			if (angle <= 180f && angle > min) return min;
			if (angle > 180f && angle < 360f - max) return 360f - max;
			
			return angle;
		}


		/// <summary>
		/// Reset camera về góc ban đầu
		/// </summary>
		public void ResetRotation()
		{
			m_targetYaw = m_currentYaw = 0f;
			m_targetPitch = m_currentPitch = 0f;
			m_transform.localRotation = Quaternion.identity;
		}


		/// <summary>
		/// Set góc xoay cụ thể
		/// </summary>
		public void SetRotation(float yaw, float pitch)
		{
			m_targetYaw = m_currentYaw = yaw;
			m_targetPitch = m_currentPitch = pitch;
		}


		/// <summary>
		/// Lấy góc xoay hiện tại
		/// </summary>
		public Vector2 GetCurrentRotation()
		{
			return new Vector2(m_currentYaw, m_currentPitch);
		}
	}
}
