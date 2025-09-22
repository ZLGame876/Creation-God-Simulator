using UnityEngine;
using System.Collections.Generic;

public class InfiniteGroundGenerator : MonoBehaviour
{
    // ========== 基础设置 ==========
    [Header("基础设置")]
    public GameObject groundChunkPrefab; // 地面区块预制体
    public Camera playerCamera;          // 玩家相机组件
    public int minRenderDistance = 5;    // 最小渲染距离（区块数）
    public int maxRenderDistance = 20;   // 最大渲染距离（区块数）
    public int chunkSize = 10;           // 单个区块的大小（单位：米）

    // ========== 性能优化 ==========
    [Header("性能优化")]
    public int maxChunks = 200;          // 最大同时存在的区块数量
    public float updateInterval = 0.1f;  // 区块更新检查间隔（秒）

    // ========== 视野覆盖 ==========
    [Header("视野覆盖")]
    public float paddingFactor = 1.5f;   // 视野填充系数

    // 活动区块字典
    private Dictionary<Vector2Int, GameObject> activeChunks = new Dictionary<Vector2Int, GameObject>();
    private Vector2Int lastPlayerChunk = Vector2Int.zero; // 上一次玩家所在的区块坐标
    private float lastUpdateTime = 0f;   // 上一次更新区块的时间
    private float lastCameraHeight = 0f; // 上一次相机高度

    // 初始化方法
    void Start()
    {
        // 如果未指定相机，自动使用主相机
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            Debug.LogWarning("未指定相机，使用主相机");
        }
        
        // 记录初始相机高度
        lastCameraHeight = playerCamera.transform.position.y;
        
        // 初始生成玩家周围的区块
        GenerateChunksAroundPlayer();
    }

    // 每帧更新方法
    void Update()
    {
        // 检查是否达到更新间隔时间
        if (Time.time - lastUpdateTime > updateInterval)
        {
            // 获取玩家当前所在的区块坐标
            Vector2Int currentPlayerChunk = GetPlayerChunkPosition();
            
            // 检查相机高度是否发生显著变化
            bool cameraHeightChanged = Mathf.Abs(playerCamera.transform.position.y - lastCameraHeight) > 1f;
            
            // 如果玩家移动到了新的区块或相机高度变化
            if (currentPlayerChunk != lastPlayerChunk || cameraHeightChanged)
            {
                // 重新生成玩家周围的区块
                GenerateChunksAroundPlayer();
                // 更新记录的上次玩家区块位置
                lastPlayerChunk = currentPlayerChunk;
                // 更新相机高度记录
                lastCameraHeight = playerCamera.transform.position.y;
            }
            
            // 更新最后更新时间
            lastUpdateTime = Time.time;
        }
    }

    // 获取玩家当前所在的区块坐标
    private Vector2Int GetPlayerChunkPosition()
    {
        // 如果玩家相机未设置，返回原点
        if (playerCamera == null) return Vector2Int.zero;
        
        // 计算X轴区块坐标：相机位置除以区块大小后取整
        int x = Mathf.FloorToInt(playerCamera.transform.position.x / chunkSize);
        // 计算Z轴区块坐标：相机位置除以区块大小后取整
        int z = Mathf.FloorToInt(playerCamera.transform.position.z / chunkSize);
        
        // 返回区块坐标
        return new Vector2Int(x, z);
    }

    // 计算实际需要的渲染距离（基于相机视野）
    private int CalculateRequiredRenderDistance()
    {
        // 如果相机未设置，返回最小渲染距离
        if (playerCamera == null) return minRenderDistance;
        
        // 获取相机高度（Y轴位置）
        float cameraHeight = playerCamera.transform.position.y;
        // 将相机的视野角度（FOV）转换为弧度
        float fovRadians = playerCamera.fieldOfView * Mathf.Deg2Rad;
        // 计算相机视野在地面上的直径
        float visibleDiameter = 2f * cameraHeight * Mathf.Tan(fovRadians / 2f);
        
        // 计算需要的区块数量（考虑填充系数）
        int requiredChunks = Mathf.CeilToInt(visibleDiameter / chunkSize * paddingFactor);
        
        // 确保渲染距离在最小和最大范围内
        return Mathf.Clamp(requiredChunks, minRenderDistance, maxRenderDistance);
    }

    // 生成玩家周围的区块
    private void GenerateChunksAroundPlayer()
    {
        // 获取玩家当前所在的中心区块
        Vector2Int centerChunk = GetPlayerChunkPosition();
        // 计算实际需要的渲染距离（基于相机视野）
        int actualRenderDistance = CalculateRequiredRenderDistance();
        
        // 计算需要加载的区块范围
        int startX = centerChunk.x - actualRenderDistance; // X轴起始区块
        int endX = centerChunk.x + actualRenderDistance;   // X轴结束区块
        int startZ = centerChunk.y - actualRenderDistance; // Z轴起始区块
        int endZ = centerChunk.y + actualRenderDistance;   // Z轴结束区块
        
        // 创建集合存储需要保留的区块坐标
        HashSet<Vector2Int> chunksToKeep = new HashSet<Vector2Int>();
        
        // 遍历X轴范围内的区块
        for (int x = startX; x <= endX; x++)
        {
            // 遍历Z轴范围内的区块
            for (int z = startZ; z <= endZ; z++)
            {
                // 创建当前区块坐标
                Vector2Int chunkCoord = new Vector2Int(x, z);
                // 添加到需要保留的集合
                chunksToKeep.Add(chunkCoord);
                
                // 如果当前区块不存在
                if (!activeChunks.ContainsKey(chunkCoord))
                {
                    // 创建新区块
                    CreateChunk(chunkCoord);
                }
            }
        }
        
        // 创建列表存储需要移除的区块
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        
        // 遍历所有活动区块
        foreach (var chunk in activeChunks)
        {
            // 如果区块不在需要保留的集合中
            if (!chunksToKeep.Contains(chunk.Key))
            {
                // 添加到移除列表
                chunksToRemove.Add(chunk.Key);
            }
        }
        
        // 遍历移除列表
        foreach (var chunkCoord in chunksToRemove)
        {
            // 销毁区块游戏对象
            Destroy(activeChunks[chunkCoord]);
            // 从活动区块字典中移除
            activeChunks.Remove(chunkCoord);
        }
    }

    // 创建单个区块
    private void CreateChunk(Vector2Int coord)
    {
        // 检查是否达到最大区块数
        if (activeChunks.Count >= maxChunks)
        {
            // 移除最远的区块
            RemoveFarthestChunk();
        }
        
        // 计算区块的世界位置
        Vector3 position = new Vector3(
            coord.x * chunkSize, // X位置
            0,                  // Y位置（地面高度）
            coord.y * chunkSize  // Z位置
        );
        
        // 实例化区块预制体
        GameObject chunk = Instantiate(
            groundChunkPrefab, // 预制体
            position,          // 位置
            Quaternion.identity, // 旋转（无旋转）
            transform          // 父对象
        );
        
        // 设置区块名称（包含坐标信息）
        chunk.name = $"GroundChunk_{coord.x}_{coord.y}";
        
        // 添加到活动区块字典
        activeChunks.Add(coord, chunk);
    }

    // 移除最远的区块
    private void RemoveFarthestChunk()
    {
        // 获取玩家当前区块
        Vector2Int playerChunk = GetPlayerChunkPosition();
        // 初始化最远区块坐标
        Vector2Int farthestChunk = Vector2Int.zero;
        // 初始化最大距离
        float maxDistance = 0f;
        
        // 遍历所有活动区块
        foreach (var chunk in activeChunks)
        {
            // 计算当前区块到玩家区块的距离
            float distance = Vector2.Distance(
                new Vector2(playerChunk.x, playerChunk.y), // 玩家区块坐标
                new Vector2(chunk.Key.x, chunk.Key.y)       // 当前区块坐标
            );
            
            // 如果距离大于当前最大距离
            if (distance > maxDistance)
            {
                // 更新最大距离
                maxDistance = distance;
                // 更新最远区块
                farthestChunk = chunk.Key;
            }
        }
        
        // 如果找到最远区块
        if (activeChunks.ContainsKey(farthestChunk))
        {
            // 销毁区块游戏对象
            Destroy(activeChunks[farthestChunk]);
            // 从活动区块字典中移除
            activeChunks.Remove(farthestChunk);
        }
    }
    
    // 调试可视化（在Scene视图中显示）
    void OnDrawGizmosSelected()
    {
        // 如果相机未设置，不绘制
        if (playerCamera == null) return;
        
        // 设置玩家位置标记颜色
        Gizmos.color = Color.green;
        // 在玩家位置绘制球体
        Gizmos.DrawSphere(playerCamera.transform.position, 1f);
        
        // 设置区块边界颜色
        Gizmos.color = Color.yellow;
        // 获取玩家当前区块
        Vector2Int centerChunk = GetPlayerChunkPosition();
        // 计算实际渲染距离
        int actualRenderDistance = CalculateRequiredRenderDistance();
        
        // 计算边界最小点
        Vector3 min = new Vector3(
            (centerChunk.x - actualRenderDistance) * chunkSize, // 最小X
            0,                                                 // Y
            (centerChunk.y - actualRenderDistance) * chunkSize  // 最小Z
        );
        
        // 计算边界最大点
        Vector3 max = new Vector3(
            (centerChunk.x + actualRenderDistance) * chunkSize, // 最大X
            0,                                                 // Y
            (centerChunk.y + actualRenderDistance) * chunkSize  // 最大Z
        );
        
        // 绘制边界线
        Gizmos.DrawLine(min, new Vector3(min.x, 0, max.z)); // 左下到左上
        Gizmos.DrawLine(new Vector3(min.x, 0, max.z), max); // 左上到右上
        Gizmos.DrawLine(max, new Vector3(max.x, 0, min.z)); // 右上到右下
        Gizmos.DrawLine(new Vector3(max.x, 0, min.z), min); // 右下到左下
        
        // 绘制视野范围
        DrawCameraFrustum();
    }
    
    // 绘制相机视锥体
    private void DrawCameraFrustum()
    {
        // 设置视锥体颜色
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 半透明橙色
        
        // 获取相机位置
        Vector3 cameraPos = playerCamera.transform.position;
        // 获取相机旋转
        Quaternion cameraRot = playerCamera.transform.rotation;
        
        // 获取视锥体参数
        float fov = playerCamera.fieldOfView;
        float aspect = playerCamera.aspect;
        float near = playerCamera.nearClipPlane;
        float far = playerCamera.farClipPlane;
        
        // 绘制视锥体
        Gizmos.matrix = Matrix4x4.TRS(cameraPos, cameraRot, Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, fov, far, near, aspect);
    }
}