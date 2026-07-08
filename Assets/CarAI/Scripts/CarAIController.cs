using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using EVP;

public class CarAIController : MonoBehaviour
{
    public enum ControlMode
    {
        WheelCollider,
        VehicleController
    }

    [Header("Control Mode")]
    [Tooltip("Chọn chế độ điều khiển xe: WheelCollider (trực tiếp) hoặc VehicleController (EVP5)")]
    public ControlMode controlMode = ControlMode.WheelCollider;
    [Tooltip("Reference đến VehicleController (chỉ cần khi dùng VehicleController mode)")]
    public VehicleController vehicleController;

    //Wheel transforms

    [Header("Wheels")]
    public Transform frontRight;
    public Transform frontLeft;
    public Transform rearRight;
    public Transform rearLeft;

    //Wheel colliders

    public WheelCollider frontRightCollider;
    public WheelCollider frontLeftCollider;
    public WheelCollider rearRightCollider;
    public WheelCollider rearLeftCollider;

    [Header("Checkpoints And Detections")]
    //Checkpoints
    [Tooltip("The checkpoint transform that the ai checks for. Every time the car enters a checkpoint, this variable changes to the next connected checkpoint to it, or will choose one randomly if it haves multiple checkpoints connected.")]
    public Transform nextCheckpoint;
    [Tooltip("Khoảng cách tối đa từ tâm checkpoint mà xe có thể chọn làm điểm đích (theo hướng vuông góc với forward của checkpoint).")]
    public float checkpointOffsetRange = 3f;
    [Tooltip("Số checkpoint phải đi qua trước khi xe có thể đổi làn (đổi vị trí mục tiêu). Giá trị càng cao thì xe càng ít đổi làn.")]
    [Range(1, 10)]
    public int laneChangeFrequency = 3;
    [Tooltip("Xác suất xe sẽ đổi làn khi đến lúc có thể đổi (0-1). Giá trị 1 = luôn đổi, 0.3 = 30% cơ hội đổi làn.")]
    [Range(0f, 1f)]
    public float laneChangeChance = 0.3f;
    [Tooltip("A list of the positions where the ai will shoot rays that will detect objects. Placing them inside of the car's collider is ideal.")]
    public List<Transform> checks = new List<Transform> {null};
    [Tooltip("If its true, the AI will check for checkpoints.")]
    public bool CheckPointSearch = true;
    [Tooltip("If its true, it means an object is in front of the car.")]
    public bool objectDetected = false;
    [Tooltip("If its true, the vehicle will be controlled by the ai.")]
    public bool isCarControlledByAI = true;
    [Tooltip("Layers seen by the car. If you uncheck a layer the car won't react to objects from that layer.")]
    public LayerMask seenLayers = Physics.AllLayers;

    [Header("Car Settings")]

    //Speed
    [Tooltip("The speed of the vehicle measured in km/h. This is only for reading, changing it won't affect the speed of the car.")]
    public int kmh;
    [Tooltip("The speed limit applied to the ai to drive the car in km/h.")]
    public int speedLimit;
    [Tooltip("Distance to keep away from other objects.")]
    public float distanceFromObjects = 2f;
    [Tooltip("Tốc độ làm mượt góc lái (0-1). Càng cao càng mượt nhưng phản ứng chậm hơn.")]
    [Range(0f, 1f)]
    public float steeringSmoothness = 0.1f;
    [Tooltip("Giảm tốc độ khi rẽ gấp. Nếu góc > giá trị này (độ) thì xe sẽ giảm tốc.")]
    [Range(0f, 45f)]
    public float sharpTurnAngle = 20f;
    [Tooltip("Phần trăm tốc độ giảm khi rẽ gấp (0-1). VD: 0.5 = giảm còn 50% tốc độ.")]
    [Range(0f, 1f)]
    public float turnSpeedReduction = 0.6f;
    [Tooltip("The number of km/h that the car will go above/under the speed limit. For example 0=it will respect the speed limit, 10=it will go with 10 km/h above the speed limit, -10=it will go with 10 km/h below the speed limit.")]
    public int recklessnessThreshold = 0;
    [Tooltip("If true the car will switch to taxi mode, meaning that using the TaxiScript, it will go from a start checkpoint to an end checkpoint. These checkpoints need to be connected in a checkpoint network.")]
    public bool taxiMode = false;
    [Tooltip("Kích hoạt tính năng reset xe khi không di chuyển được.")]
    public bool enableStuckDetection = true;
    [Tooltip("Thời gian (giây) xe phải bị kẹt (quay ngoặc hoặc không di chuyển) trước khi reset. VD: 10 = sau 10 giây bị kẹt thì reset.")]
    public float stuckTimeThreshold = 10f;
    [Tooltip("Khoảng cách tối thiểu (đơn vị Unity) mà xe phải di chuyển mỗi giây. Nếu di chuyển ít hơn = xe bị kẹt. VD: 1 = phải di chuyển ít nhất 1m/giây.")]
    public float minMovementPerSecond = 1f;
    [Tooltip("Độ cao spawn trên checkpoint khi reset (đơn vị Unity).")]
    public float resetHeightOffset = 1f;

    //Car values

    [Tooltip("Acceleration threshold.")]
    public float acceleration = 100f;
    [Tooltip("Breaking threshold. Tip: make it bigger than the acceleration threshold so that the car can break faster.")]
    public float breaking = 1000f;

    //Private variables
    private Stopwatch stopwatch = new Stopwatch();
    private Vector3 lastPos;
    private float steerAngle = 0f;
    private float targetSteerAngle = 0f;
    private Vector3 targetCheckpointPosition;
    private Transform lastCheckpoint;
    private int checkpointCounter = 0;
    private float currentLaneOffset = 0f;
    private float stuckTimer = 0f;
    private Vector3 lastStuckCheckPosition;
    private bool isCheckingStuck = false;

    private void FixedUpdate()
    {
        if (controlMode == ControlMode.WheelCollider)
        {
            WheelUpdate(frontRight, frontRightCollider);
            WheelUpdate(frontLeft, frontLeftCollider);
            WheelUpdate(rearRight, rearRightCollider);
            WheelUpdate(rearLeft, rearLeftCollider);
        }

        //Calculate speed
        CalculateKMH();

        //Search for checkpoints

        SearchForCheckpoints();

        // Kiểm tra xe có bị kẹt không
        if (enableStuckDetection && isCarControlledByAI && !isCheckingStuck)
        {
            isCheckingStuck = true;
            StartCoroutine(CheckIfCarIsStuck());
        }
    }

    IEnumerator CheckIfCarIsStuck()
    {
        yield return new WaitForSeconds(1f);

        lastStuckCheckPosition = transform.position;
        stuckTimer = 0f;

        while (enableStuckDetection && isCarControlledByAI)
        {
            yield return new WaitForSeconds(1f);

            if (nextCheckpoint == null)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Xe bị kẹt nếu: di chuyển quá chậm hoặc không di chuyển
            float distanceMoved = Vector3.Distance(transform.position, lastStuckCheckPosition);
            bool isStuck = distanceMoved < minMovementPerSecond;

            if (isStuck)
            {
                stuckTimer += 1f;

                // Nếu bị kẹt quá lâu, reset
                if (stuckTimer >= stuckTimeThreshold)
                {
                    UnityEngine.Debug.Log($"Xe {gameObject.name} bị kẹt {stuckTimer}s (di chuyển {distanceMoved:F2}m < {minMovementPerSecond}m/s), reset về checkpoint {nextCheckpoint.name}");
                    ResetCarToCheckpoint();
                    stuckTimer = 0f;
                    lastStuckCheckPosition = transform.position;
                }
                // KHÔNG cập nhật lastStuckCheckPosition khi bị kẹt
                // Giữ nguyên vị trí để tiếp tục so sánh với vị trí ban đầu bị kẹt
            }
            else
            {
                // Xe đang hoạt động bình thường, reset timer VÀ cập nhật vị trí tham chiếu
                stuckTimer = 0f;
                lastStuckCheckPosition = transform.position;
            }
        }

        isCheckingStuck = false;
    }

    /// <summary>
    /// Reset xe về checkpoint hiện tại với vị trí cao hơn mặt đường
    /// </summary>
    private void ResetCarToCheckpoint()
    {
        if (nextCheckpoint == null)
        {
            UnityEngine.Debug.LogWarning("Không thể reset xe: không có checkpoint!");
            return;
        }

        UnityEngine.Debug.Log("Xe " + gameObject.name + " bị kẹt, reset về checkpoint: " + nextCheckpoint.name);

        // Tính vị trí reset: checkpoint + offset cao + hướng forward một chút
        Vector3 resetPosition = nextCheckpoint.position + Vector3.up * resetHeightOffset + nextCheckpoint.forward * 2f;
        
        // Đặt xe về vị trí và hướng của checkpoint
        transform.position = resetPosition;
        transform.rotation = nextCheckpoint.rotation;

        // Reset velocity của rigidbody
        Rigidbody rb = controlMode == ControlMode.VehicleController && vehicleController != null 
            ? vehicleController.cachedRigidbody 
            : GetComponent<Rigidbody>();
            
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset góc lái
        steerAngle = 0f;
        targetSteerAngle = 0f;
        Turn(0f);

        // Reset timer và position tracking
        stuckTimer = 0f;
        lastStuckCheckPosition = resetPosition;
    }

    private void WheelUpdate(Transform transform, WheelCollider collider)
    {
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        transform.position = pos;
        transform.rotation = rot;
    }

    /// <summary>
    /// Accelerates the vehicle by the value given.
    /// </summary>
    public void Accelerate(float value)
    {
        if (controlMode == ControlMode.WheelCollider)
        {
            frontRightCollider.motorTorque = value;
            frontLeftCollider.motorTorque = value;
        }
        else if (controlMode == ControlMode.VehicleController && vehicleController != null)
        {
            // VehicleController sử dụng throttleInput từ -1 đến 1
            vehicleController.throttleInput = Mathf.Clamp(value / acceleration, -1f, 1f);
        }
    }

    /// <summary>
    /// Breaks the vehicle by the value given.
    /// </summary>
    public void Break(float value)
    {
        if (controlMode == ControlMode.WheelCollider)
        {
            frontRightCollider.brakeTorque = value;
            frontLeftCollider.brakeTorque = value;
            rearRightCollider.brakeTorque = value;
            rearLeftCollider.brakeTorque = value;
        }
        else if (controlMode == ControlMode.VehicleController && vehicleController != null)
        {
            // VehicleController sử dụng brakeInput từ 0 đến 1
            vehicleController.brakeInput = Mathf.Clamp01(value / breaking);
        }
    }

    /// <summary>
    /// Turns the front wheels at the angle given.
    /// </summary>
    public void Turn(float angle)
    {
        if (controlMode == ControlMode.WheelCollider)
        {
            frontRightCollider.steerAngle = angle;
            frontLeftCollider.steerAngle = angle;
        }
        else if (controlMode == ControlMode.VehicleController && vehicleController != null)
        {
            // VehicleController sử dụng steerInput từ -1 đến 1
            // Giả sử maxSteerAngle của VehicleController là 35 độ (mặc định)
            float maxSteer = vehicleController.maxSteerAngle;
            vehicleController.steerInput = Mathf.Clamp(angle / maxSteer, -1f, 1f);
        }
    }

    private void CalculateKMH()
    {
        if (controlMode == ControlMode.VehicleController && vehicleController != null)
        {
            // Lấy tốc độ từ VehicleController
            kmh = (int)(Mathf.Abs(vehicleController.speed) * 3.6f); // m/s to km/h
            return;
        }

        // WheelCollider mode - tính toán bình thường
        if(stopwatch.IsRunning)
        {
            stopwatch.Stop();

            float distance = (transform.position - lastPos).magnitude;
            float time = stopwatch.Elapsed.Milliseconds / (float)1000;

            kmh = (int)(3600 * distance / time / 1000);

            lastPos = transform.position;
            stopwatch.Reset();
            stopwatch.Start();

        }
        else
        {
            lastPos = transform.position;
            stopwatch.Reset();
            stopwatch.Start();
        }
    }

    /// <summary>
    /// Sets the speed of the vehicle to the one given in the parameter.
    /// </summary>
    public void SetSpeed(int speedLimit)
    {
        if (kmh > speedLimit)
        {
            Break(breaking);
            Accelerate(0);
        }
        else if (kmh < speedLimit)
        {
            Accelerate(acceleration);
            Break(0);
        }
    }

    private void SearchForCheckpoints()
    {
        if (CheckPointSearch && isCarControlledByAI)
        {
            // Kiểm tra nếu checkpoint thay đổi
            if (lastCheckpoint != nextCheckpoint)
            {
                lastCheckpoint = nextCheckpoint;
                checkpointCounter++;
                
                // Chỉ tính toán vị trí đích mới nếu đã đi qua đủ số checkpoint và có xác suất đổi làn
                if (checkpointCounter >= laneChangeFrequency)
                {
                    if (Random.value <= laneChangeChance)
                    {
                        CalculateRandomTargetPosition();
                    }
                    else
                    {
                        // Giữ nguyên làn hiện tại
                        UpdateTargetPositionWithCurrentLane();
                    }
                    checkpointCounter = 0;
                }
                else
                {
                    // Chưa đến lúc đổi làn, giữ nguyên offset hiện tại
                    UpdateTargetPositionWithCurrentLane();
                }
            }

            Vector3 nextCheckpointRelative = transform.InverseTransformPoint(targetCheckpointPosition);

            targetSteerAngle = nextCheckpointRelative.x / nextCheckpointRelative.magnitude;
            float xangle = nextCheckpointRelative.y / nextCheckpointRelative.magnitude;

            targetSteerAngle = Mathf.Asin(Mathf.Clamp(targetSteerAngle, -1f, 1f)) * 180f / 3.14f;
            xangle = Mathf.Asin(Mathf.Clamp(xangle, -1f, 1f)) * 180f / 3.14f;

            // Làm mượt góc lái
            steerAngle = Mathf.Lerp(steerAngle, targetSteerAngle, 1f - steeringSmoothness);

            Turn(steerAngle);

            // Vẽ line debug để xem điểm đích
            #if UNITY_EDITOR
            UnityEngine.Debug.DrawLine(nextCheckpoint.position, targetCheckpointPosition, Color.yellow);
            #endif

            float maxDistance = kmh * kmh / 100f + distanceFromObjects;

            RaycastHit carHit = new RaycastHit();

            int objectInFront = 0;

            for(int i = 0; i < checks.Count; i++)
            {
                checks[i].localRotation = Quaternion.Euler(-xangle, steerAngle, 0);
                bool isObjectInFront = Physics.Raycast(checks[i].position, checks[i].forward, out carHit, maxDistance, seenLayers, QueryTriggerInteraction.Ignore);

                #if UNITY_EDITOR
                UnityEngine.Debug.DrawRay(checks[i].position, checks[i].forward * maxDistance, Color.green);
                #endif
                
                if(isObjectInFront == true)
                    objectInFront++;
            }
           
            if (objectInFront > 0)
            {
                SetSpeed(0);
                objectDetected = true;
            }
            else
            {
                objectDetected = false;
                int speed = speedLimit + recklessnessThreshold;
                if(speedLimit == 0)
                {
                    speed = 0;
                }
                if(speed == 0)
                {
                    speed = speedLimit;
                }

                // Giảm tốc khi rẽ gấp
                if (Mathf.Abs(targetSteerAngle) > sharpTurnAngle)
                {
                    speed = (int)(speed * turnSpeedReduction);
                }

                SetSpeed(speed);
            }
        }
    }

    /// <summary>
    /// Tính toán vị trí đích ngẫu nhiên trên đường vuông góc với forward của checkpoint
    /// </summary>
    private void CalculateRandomTargetPosition()
    {
        if (nextCheckpoint == null)
        {
            targetCheckpointPosition = transform.position;
            return;
        }

        // Lấy vector right của checkpoint (vuông góc với forward)
        Vector3 checkpointRight = nextCheckpoint.right;
        
        // Chọn offset ngẫu nhiên trong khoảng [-checkpointOffsetRange, checkpointOffsetRange]
        currentLaneOffset = Random.Range(-checkpointOffsetRange, checkpointOffsetRange);
        
        // Tính vị trí đích = vị trí checkpoint + offset theo hướng right
        targetCheckpointPosition = nextCheckpoint.position + checkpointRight * currentLaneOffset;
    }

    /// <summary>
    /// Cập nhật vị trí đích với offset làn hiện tại (không đổi làn)
    /// </summary>
    private void UpdateTargetPositionWithCurrentLane()
    {
        if (nextCheckpoint == null)
        {
            targetCheckpointPosition = transform.position;
            return;
        }

        // Giữ nguyên offset hiện tại, chỉ cập nhật vị trí checkpoint
        Vector3 checkpointRight = nextCheckpoint.right;
        targetCheckpointPosition = nextCheckpoint.position + checkpointRight * currentLaneOffset;
    }
}