using UnityEngine;

/// <summary>
/// 让物体绕指定中心点循环旋转，用于测试碰撞等
/// </summary>
public class TestClock : MonoBehaviour
{
    [Tooltip("旋转中心点，不设置则绕自身初始位置旋转")]
    public Transform center;

    [Tooltip("旋转半径")]
    public float radius = 2f;

    [Tooltip("旋转速度（度/秒），正值逆时针，负值顺时针")]
    public float speed = 90f;

    private Vector2 centerPos;
    private float angle;

    void Start()
    {
        centerPos = center != null ? (Vector2)center.position : (Vector2)transform.position;
        // 根据当前位置计算初始角度
        Vector2 offset = (Vector2)transform.position - centerPos;
        angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
    }

    void Update()
    {
        angle += speed * Time.deltaTime;
        float rad = angle * Mathf.Deg2Rad;
        transform.position = centerPos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
    }
}
