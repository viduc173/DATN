using UnityEngine;

public class Wheel : MonoBehaviour
{
    [Header("Gán Object Cha (Holder) của các bánh xe vào đây")]
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelRL;
    public Transform wheelRR;

    // ---------------------------------------------------------
    // CÁC HÀM PUBLIC ĐỂ GỌI TỪ BÊN NGOÀI (BUTTON / INPUT)
    // ---------------------------------------------------------

    // Gọi hàm này để đổi sang bánh tiếp theo (Next)
    [ContextMenu("Next Wheel")] // Cho phép test nhanh trong Inspector
    public void NextWheel()
    {
        ChangeWheelStep(1);
    }

    // Gọi hàm này để đổi sang bánh trước đó (Previous)
    [ContextMenu("Previous Wheel")]
    public void PreviousWheel()
    {
        ChangeWheelStep(-1);
    }

    // ---------------------------------------------------------
    // LOGIC XỬ LÝ
    // ---------------------------------------------------------

    private void ChangeWheelStep(int step)
    {
        // Thực hiện lệnh cho cả 4 bánh
        CycleChild(wheelFL, step);
        CycleChild(wheelFR, step);
        CycleChild(wheelRL, step);
        CycleChild(wheelRR, step);
    }

    private void CycleChild(Transform parent, int step)
    {
        if (parent == null || parent.childCount == 0) return;

        int childCount = parent.childCount;
        int activeIndex = -1;

        // 1. Tìm index của con đang active hiện tại
        for (int i = 0; i < childCount; i++)
        {
            if (parent.GetChild(i).gameObject.activeSelf)
            {
                activeIndex = i;
                break;
            }
        }

        // 2. Tắt object cũ
        if (activeIndex != -1)
        {
            parent.GetChild(activeIndex).gameObject.SetActive(false);
        }
        else
        {
            // Nếu chưa có cái nào active, ta mặc định bắt đầu từ -1 để logic bên dưới tính toán ra 0
            // Tuy nhiên, để an toàn, nếu chưa active cái nào thì luôn bật cái đầu tiên (index 0)
            activeIndex = 0; 
            // Reset step về 0 để chỉ bật cái đầu tiên chứ không nhảy cóc, 
            // hoặc giữ nguyên logic tùy bạn. Ở đây mình cho nó bật cái đầu tiên luôn.
            parent.GetChild(0).gameObject.SetActive(true);
            return;
        }

        // 3. Tính toán Index mới (Hỗ trợ cả Tiến và Lùi vòng tròn)
        // Công thức: (IndexHiệnTại + BướcNhảy + TổngSố) % TổngSố
        // Việc cộng thêm childCount trước khi chia lấy dư (%) giúp xử lý trường hợp số âm khi lùi.
        int nextIndex = (activeIndex + step + childCount) % childCount;

        // 4. Bật object mới
        parent.GetChild(nextIndex).gameObject.SetActive(true);
    }
}