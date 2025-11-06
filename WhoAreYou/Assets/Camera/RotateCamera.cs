using UnityEngine;

namespace Camera
{
    public class RotateCamera : MonoBehaviour
    {
        [Header("Rotation Settings")]
        public float sensitivity = 2f;   // 鼠标灵敏度
        public float maxAngle = 10f;     // 最大旋转角度（防止晃太多）

        private Vector2 currentRotation;
        private Vector2 targetRotation;
        
       public void OnEnable()
        {
            Vector3 euler = transform.localRotation.eulerAngles;
    
            // Unity 的欧拉角范围是 0~360，需要转换到 -180~180
            float x = euler.x > 180 ? euler.x - 360 : euler.x;
            float y = euler.y > 180 ? euler.y - 360 : euler.y;

            currentRotation = new Vector2(x, y);
            targetRotation = new Vector2(x, y);
        }


        void Update()
        {
            // 获取鼠标输入
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            // 累加目标旋转（Y 轴为上下）
            targetRotation.x += mouseY * sensitivity;
            targetRotation.y += mouseX * sensitivity;

            // 限制范围
            targetRotation.x = Mathf.Clamp(targetRotation.x, -maxAngle, maxAngle);
            targetRotation.y = Mathf.Clamp(targetRotation.y, -maxAngle, maxAngle);

            // 插值平滑过渡，形成“轻微摇晃”的感觉
            currentRotation = Vector2.Lerp(currentRotation, targetRotation, Time.deltaTime * 5f);

            // 应用到摄像机
            transform.localRotation = Quaternion.Euler(-currentRotation.x, currentRotation.y, 0);

            //// 可选：按下右键恢复居中
            //if (Input.GetMouseButtonDown(1))
            //    targetRotation = Vector2.zero;
        }
    }
}