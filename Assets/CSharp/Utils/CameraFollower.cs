using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using ZeroHero;

public class CameraFollower : MonoBehaviour
{
    public static CameraFollower Instance;

    [Header("相机镜头移动速度")]
    [SerializeField] private float speed;
    [Header("自由视角变化速度")]
    [SerializeField] private float view_speed;
    [Header("相机镜头移动平滑度")]
    [SerializeField] private float smooth;
    [Header("RTS模式：相机最大范围")]
    [SerializeField] private static float cameraY;
    [Header("RTS模式：相机Z轴偏移量")]
    [SerializeField] private static float offsetZ;
    private float3 initPos;
    private Quaternion initRot;
    private static float3 posOffset = float3.zero;
    private static float3 rotateOffset = float3.zero;
    public static GameObject followTarget { get; private set; }
    private enum CameraType
    {
        自由模式,
        RTS模式

    }
    private CameraType type = CameraType.RTS模式;
    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Start()
    {
        initPos = transform.position;
        initRot = transform.rotation;
        var cfg = CameraCfgMgr.GetByCameraName("地图相机");
        cameraY = cfg.PosY;
        offsetZ = cfg.OffsetZ;
    }
    private void OnEnable()
    {
        EntityEventSystem.Instance.onCameraSetFollowTarget += SetFollowTarget;
        InputMgr.Instance.KeyCode_W += MoveForwad;
        InputMgr.Instance.KeyCode_S += MoveBackward;
        InputMgr.Instance.KeyCode_A += MoveLeft;
        InputMgr.Instance.KeyCode_D += MoveRight;
    }
    private void OnDisable()
    {
        EntityEventSystem.Instance.onCameraSetFollowTarget -= SetFollowTarget;
    }
    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            if (type == CameraType.自由模式)
            {
                type = CameraType.RTS模式;
                transform.position = initPos;
                transform.rotation = initRot;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (type == CameraType.RTS模式)
            {
                type = CameraType.自由模式;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (type == CameraType.RTS模式) RTSMode();
        if (type == CameraType.自由模式) FreeMode();
        if (!posOffset.Equals(float3.zero))
        {
            float3 toPosition = (float3)transform.position + posOffset;
            if (toPosition.y < 0) toPosition.y = 0;
            if (followTarget && toPosition.y > cameraY) toPosition.y = cameraY;
            transform.position = Vector3.Lerp(transform.position, toPosition, smooth);
            posOffset = float3.zero;
        }
        if (transform.position.y < 0) transform.position = new float3(transform.position.x, 0, transform.position.z);
    }

    public void MoveForwad(params object[] args)
    {
        if (followTarget != null) return;
        if (args.Length > 0) posOffset += new float3(0, 0, speed * (float)args[0]);
        else posOffset += new float3(0, 0, speed);
    }
    public void MoveBackward(params object[] args)
    {
        if (followTarget != null) return;
        if (args.Length > 0) posOffset += new float3(0, 0, -speed * (float)args[0]);
        else posOffset += new float3(0, 0, -speed);
    }
    public void MoveLeft(params object[] args)
    {
        if (followTarget != null) return;
        if (args.Length > 0) posOffset += new float3(-speed * (float)args[0], 0, 0);
        else posOffset += new float3(-speed, 0, 0);
    }
    public void MoveRight(params object[] args)
    {
        if (followTarget != null) return;
        if (args.Length > 0) posOffset += new float3(speed * (float)args[0], 0, 0);
        else posOffset += new float3(speed, 0, 0);
    }
    private void SetFollowTarget(object[] args)
    {
        if (args.Length == 0)
        {
            followTarget = null;
            return;
        }
        followTarget = args[0] as GameObject;
    }
    private void FreeMode()
    {
        float3 posOffset = float3.zero;
        float3 rotateOffset = float3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            posOffset += speed * (float3)transform.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            posOffset += -speed * (float3)transform.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            posOffset += -speed * (float3)transform.right;
        }
        if (Input.GetKey(KeyCode.D))
        {
            posOffset += speed * (float3)transform.right;
        }
        rotateOffset = new float3(-Input.GetAxis("Mouse Y") * view_speed, Input.GetAxis("Mouse X") * view_speed, 0);
        if (!posOffset.Equals(float3.zero))
        {
            float3 toPosition = (float3)transform.position + posOffset;
            if (toPosition.y < 0) toPosition.y = 0;
            transform.position = Vector3.Lerp(transform.position, toPosition, smooth);
        }
        if (!rotateOffset.Equals(float3.zero))
        {
            //rotateOffset = rotateOffset.x >= 0 && rotateOffset.x <= 60 ? rotateOffset : 60;
            //rotateOffset = rotateOffset.x < 0 && rotateOffset.x >= -60 ? rotateOffset : -60;
            float3 toRotation = (float3)transform.localEulerAngles + rotateOffset;
            transform.localEulerAngles = Vector3.Lerp(transform.localEulerAngles, toRotation, smooth);
        }
    }
    private void RTSMode()
    {
        if (followTarget != null)
        {
            float3 targetPos = followTarget.transform.position;
            targetPos.y = transform.position.y;
            targetPos.z += offsetZ;
            float3 selfPos = transform.position;
            posOffset = targetPos - selfPos;
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            posOffset += new float3(0, speed * 10f, 0);
        }
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            posOffset += new float3(0, -speed * 10f, 0);
        }
    }

}
