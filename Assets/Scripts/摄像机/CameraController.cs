using UnityEngine;

public class CameraController : MonoBehaviour
{
    // ========== 移动设置 ==========
    [Header("移动设置")]
    public float moveSpeed = 10f;          // 基础移动速度
    public Vector2 moveBoundary = new Vector2(100, 100); // 移动边界（X和Z轴限制）

    // ========== 旋转设置 ==========
    [Header("旋转设置")]
    public float rotateSpeed = 500f;       // 旋转速度
    public float rotationSensitivity = 5f;  // 旋转灵敏度系数
    public float maxVerticalAngle = 80f;    // 最大俯仰角度
    public float minVerticalAngle = 10f;    // 最小俯仰角度

    // ========== 缩放设置 ==========
    [Header("缩放设置")]
    public float zoomSpeed = 5f;           // 缩放速度
    public float minZoom = 30f;             // 最小FOV（放大）
    public float maxZoom = 100f;             // 最大FOV（缩小）
    private float targetZoom = 45f;         // 目标缩放值

    // ========== 升降设置 ==========
    [Header("升降设置")]
    public float verticalMoveSpeed = 5f;    // 垂直移动速度
    public float minHeight = 10f;           // 最低高度
    public float maxHeight = 30f;          // 最高高度

    private float currentRotationX = 0f;    // 当前X轴旋转角度（俯仰）
    private float currentRotationY = 0f;    // 当前Y轴旋转角度（水平）
    private Camera mainCamera;              // 相机组件引用

    void Start()
    {
        // 获取相机组件
        mainCamera = GetComponent<Camera>();
        
        // 设置初始俯视角（向下倾斜30度）
        transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        
        // 初始化旋转角度
        currentRotationX = transform.eulerAngles.x;
        currentRotationY = transform.eulerAngles.y;
        
        // 设置初始缩放
        targetZoom = mainCamera.fieldOfView;
        
        // 确保初始高度在限制范围内
        float clampedY = Mathf.Clamp(transform.position.y, minHeight, maxHeight);
        transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
    }

    void Update()
    {
        // 每帧处理移动、旋转、缩放和升降
        HandleMovement();
        HandleRotation();
        HandleZoom();
        HandleVerticalMovement();
        
        // 应用平滑缩放
        ApplySmoothZoom();
    }

    // 处理WASD移动
    void HandleMovement()
    {
        // 初始化移动方向向量
        Vector3 moveDir = Vector3.zero;

        // 检测WASD按键输入
        if (Input.GetKey(KeyCode.W)) moveDir.z += 1;  // W键向前移动
        if (Input.GetKey(KeyCode.S)) moveDir.z -= 1;  // S键向后移动
        if (Input.GetKey(KeyCode.D)) moveDir.x += 1;  // D键向右移动
        if (Input.GetKey(KeyCode.A)) moveDir.x -= 1;  // A键向左移动

        // 如果有移动输入
        if (moveDir != Vector3.zero)
        {
            // 将移动方向转换为世界空间方向
            Vector3 worldMoveDir = transform.TransformDirection(moveDir.normalized);
            
            // 确保只在XZ平面移动（忽略Y轴分量）
            worldMoveDir.y = 0;
            
            // 应用移动
            transform.position += worldMoveDir * moveSpeed * Time.deltaTime;
        }

        // 限制相机在XZ平面的移动边界
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, -moveBoundary.x, moveBoundary.x),
            transform.position.y,
            Mathf.Clamp(transform.position.z, -moveBoundary.y, moveBoundary.y)
        );
    }

    // 处理鼠标右键旋转
    void HandleRotation()
    {
        // 当鼠标右键被按下时
        if (Input.GetMouseButton(1))
        {
            // 获取鼠标移动量
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // 计算新的旋转角度
            currentRotationY += mouseX * rotateSpeed * Time.deltaTime * rotationSensitivity;
            currentRotationX -= mouseY * rotateSpeed * Time.deltaTime * rotationSensitivity;

            // 限制俯仰角度在合理范围内
            currentRotationX = Mathf.Clamp(currentRotationX, minVerticalAngle, maxVerticalAngle);

            // 应用旋转
            transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        }
    }

    // 处理鼠标滚轮缩放
    void HandleZoom()
    {
        // 获取鼠标滚轮输入
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        // 如果有滚轮输入
        if (scroll != 0)
        {
            // 调整目标缩放值
            targetZoom -= scroll * zoomSpeed;
            
            // 确保缩放范围在限制内
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }
    
    // 应用平滑缩放
    void ApplySmoothZoom()
    {
        // 使用Lerp实现平滑缩放
        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetZoom, Time.deltaTime * 5f);
    }

    // 处理Q/E升降（添加高度限制）
    void HandleVerticalMovement()
    {
        // 初始化垂直移动量
        float verticalMove = 0f;
        
        // 检测Q/E按键输入
        if (Input.GetKey(KeyCode.E)) verticalMove += 1;  // E键上升
        if (Input.GetKey(KeyCode.Q)) verticalMove -= 1;  // Q键下降

        // 如果有垂直移动输入
        if (verticalMove != 0)
        {
            // 计算新的Y轴位置
            float newY = transform.position.y + verticalMove * verticalMoveSpeed * Time.deltaTime;
            
            // 限制在高度范围内
            newY = Mathf.Clamp(newY, minHeight, maxHeight);
            
            // 应用垂直移动
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    // 调试可视化（在Scene视图中显示高度限制）
    void OnDrawGizmosSelected()
    {
        // 绘制最低高度平面
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 半透明红色
        Vector3 minPlaneCenter = new Vector3(0, minHeight, 0);
        Vector3 minPlaneSize = new Vector3(100f, 0.1f, 100f);
        Gizmos.DrawCube(minPlaneCenter, minPlaneSize);
        
        // 绘制最高高度平面
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 半透明绿色
        Vector3 maxPlaneCenter = new Vector3(0, maxHeight, 0);
        Vector3 maxPlaneSize = new Vector3(100f, 0.1f, 100f);
        Gizmos.DrawCube(maxPlaneCenter, maxPlaneSize);
        
        // 绘制当前高度指示器
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x, minHeight, transform.position.z));
    }
}