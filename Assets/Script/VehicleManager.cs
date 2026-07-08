using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Quản lý danh sách xe và chuyển đổi giữa các xe
/// Gắn vào GameObject quản lý xe
/// </summary>
public class VehicleManager : MonoBehaviour
{
	[Header("Vehicle List")]
	[Tooltip("Danh sách các xe có sẵn trong scene")]
	public GameObject[] vehicles;
	
	[Tooltip("Index xe hiện tại (0 = xe đầu tiên)")]
	[SerializeField]
	private int currentVehicleIndex = 0;

	[Header("Events")]
	[Tooltip("Sự kiện được gọi khi đổi xe, truyền GameObject của xe mới")]
	public UnityEvent<GameObject> onVehicleChanged;


	void Start()
	{
		if (vehicles.Length == 0)
		{
			Debug.LogWarning("[VehicleManager] Danh sách xe trống!");
			return;
		}

		// Deactive tất cả xe trừ xe hiện tại
		for (int i = 0; i < vehicles.Length; i++)
		{
			if (vehicles[i] != null)
			{
				vehicles[i].SetActive(i == currentVehicleIndex);
			}
		}

		Debug.Log($"[VehicleManager] Xe khởi tạo: {vehicles[currentVehicleIndex].name}");
	}


	/// <summary>
	/// Đổi sang xe theo index
	/// </summary>
	public void ChangeVehicle(int index)
	{
		if (vehicles.Length == 0)
		{
			Debug.LogError("[VehicleManager] Danh sách xe trống!");
			return;
		}

		// Clamp index
		index = Mathf.Clamp(index, 0, vehicles.Length - 1);

		if (index == currentVehicleIndex)
		{
			Debug.Log($"[VehicleManager] Đã là xe {index} rồi!");
			return;
		}

		currentVehicleIndex = index;
		ActivateVehicle(index);
	}


	/// <summary>
	/// Đổi sang xe tiếp theo
	/// </summary>
	public void NextVehicle()
	{
		int nextIndex = (currentVehicleIndex + 1) % vehicles.Length;
		ChangeVehicle(nextIndex);
	}


	/// <summary>
	/// Đổi sang xe trước đó
	/// </summary>
	public void PreviousVehicle()
	{
		int prevIndex = currentVehicleIndex - 1;
		if (prevIndex < 0) prevIndex = vehicles.Length - 1;
		ChangeVehicle(prevIndex);
	}


	/// <summary>
	/// Active xe được chọn, deactive các xe khác
	/// </summary>
	private void ActivateVehicle(int index)
	{
		if (vehicles[index] == null)
		{
			Debug.LogError($"[VehicleManager] Xe tại index {index} là null!");
			return;
		}

		// Deactive tất cả xe
		for (int i = 0; i < vehicles.Length; i++)
		{
			if (vehicles[i] != null)
			{
				vehicles[i].SetActive(false);
			}
		}

		// Active xe hiện tại
		vehicles[index].SetActive(true);

		Debug.Log($"[VehicleManager] Đã đổi sang xe: {vehicles[index].name}");

		// Trigger event
		onVehicleChanged?.Invoke(vehicles[index]);
	}


	/// <summary>
	/// Lấy GameObject của xe hiện tại
	/// </summary>
	public GameObject GetCurrentVehicle()
	{
		if (currentVehicleIndex >= 0 && currentVehicleIndex < vehicles.Length)
		{
			return vehicles[currentVehicleIndex];
		}
		return null;
	}


	/// <summary>
	/// Lấy index của xe hiện tại
	/// </summary>
	public int GetCurrentVehicleIndex()
	{
		return currentVehicleIndex;
	}


	/// <summary>
	/// Lấy tên của xe hiện tại
	/// </summary>
	public string GetCurrentVehicleName()
	{
		if (currentVehicleIndex >= 0 && currentVehicleIndex < vehicles.Length && vehicles[currentVehicleIndex] != null)
		{
			return vehicles[currentVehicleIndex].name;
		}
		return "None";
	}


	/// <summary>
	/// Lấy số lượng xe trong danh sách
	/// </summary>
	public int GetVehicleCount()
	{
		return vehicles.Length;
	}
}
