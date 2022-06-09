using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZeroHero
{
    public class InputMgr : UnitySingleton<InputMgr>
    {
        public Joystick joystick;
        public event Callback PointerDown0;
        public event Callback PointerUp0;
        public event Callback PointerDown1;
        public void OnPointerDown1() { PointerDown1?.Invoke(); }
        public event Callback PointerUp1;
        public event Callback OneClickDown;
        public event Callback OneClickUp;
        public event Callback DoubleClickDown;
        public event Callback DoubleClickUp;

        public event Callback ReleaseSmallSkill;
        public void OnReleaseSmallSkill() { ReleaseSmallSkill?.Invoke(); }
        public event Callback ReleaseBigSkill;
        public void OnReleaseBigSkill() { ReleaseBigSkill?.Invoke(); }
        public event Callback KeyCode_W;
        public event Callback KeyCode_S;
        public event Callback KeyCode_A;
        public event Callback KeyCode_D;

        public event Callback SwitchTopMenu;
        public void OnSwitchTopMenu() { SwitchTopMenu?.Invoke(); }

        public event Callback SetTeam;
        public void OnSetTeam(int teamIndex) { SetTeam?.Invoke(teamIndex); }
        public event Callback ChoseTeam;
        public void OnChoseTeam(int teamIndex) { ChoseTeam?.Invoke(teamIndex); }
        public event Callback BombTargetPos;
        public void OnBombTargetPos() { BombTargetPos?.Invoke(); }


        private float clickStartTime;
        private float clickEndTime;
        private float doubleClickInterval = 0.2f;
        private float pressTime = 0f;
        private float _longPressTime = 0.8f;
        private float twoFingerDis = -1f;
        void Update()
        {
            if (GameController._gameState != GameState.游戏中) return;
            if (Input.GetMouseButton(0))
            {
                pressTime += Time.deltaTime;
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (pressTime > _longPressTime && IsOnUI()) //长按UI事件
                {
                    ////////////////////////////////
                }
                pressTime = 0;
            }
            #region [场景的点击]
            if (Input.GetMouseButtonDown(0) && !IsOnUI())
            {
                float curTime = UnityEngine.Time.realtimeSinceStartup;
                if (clickStartTime == clickEndTime)//第1次点击
                {
                    clickStartTime = curTime;
                    OneClickDown?.Invoke();
                }
                else//第2次点击
                {
                    clickEndTime = curTime;
                    if (clickEndTime - clickStartTime < doubleClickInterval)
                    {
                        //Debug.Log("双击");
                        DoubleClickDown?.Invoke();
                    }
                    clickStartTime = curTime;
                }
                PointerDown0?.Invoke();
            }
            if (Input.GetMouseButtonUp(0) && !IsOnUI())
            {
                if (clickStartTime != clickEndTime)
                {
                    OneClickUp?.Invoke();
                    Invoke("ClearClick", doubleClickInterval);
                }
                else
                {
                    DoubleClickUp?.Invoke();
                }
                PointerUp0?.Invoke();
            }
            #endregion
#if UNITY_EDITOR || UNITY_STANDALONE
            #region [鼠标键盘输入触发事件]
            if (Input.GetKeyDown(SettingDefs._keyCode_SwitchTopMenu)) SwitchTopMenu?.Invoke();
            if (Input.GetKeyDown(SettingDefs._keyCode_BombTargetPos)) BombTargetPos?.Invoke();
            if (Input.GetKeyDown(SettingDefs._keyCode_ReleaseSmallSkill)) ReleaseSmallSkill?.Invoke();
            if (Input.GetKeyDown(SettingDefs._keyCode_ReleaseBigSkill)) ReleaseBigSkill?.Invoke();
            if (Input.GetKey(KeyCode.W)) KeyCode_W?.Invoke();
            if (Input.GetKey(KeyCode.S)) KeyCode_S?.Invoke();
            if (Input.GetKey(KeyCode.A)) KeyCode_A?.Invoke();
            if (Input.GetKey(KeyCode.D)) KeyCode_D?.Invoke();
            if (Input.GetMouseButtonDown(1) && !IsOnUI())
            {
                PointerDown1?.Invoke();
            }
            if (Input.GetMouseButtonUp(1) && !IsOnUI())
            {
                PointerUp1?.Invoke();
            }
            int teamIndex =
                     Input.GetKeyDown(KeyCode.Alpha1) ? 1 :
                     Input.GetKeyDown(KeyCode.Alpha2) ? 2 :
                     Input.GetKeyDown(KeyCode.Alpha3) ? 3 :
                     Input.GetKeyDown(KeyCode.Alpha4) ? 4 :
                     Input.GetKeyDown(KeyCode.Alpha5) ? 5 :
                     Input.GetKeyDown(KeyCode.Alpha6) ? 6 :
                     Input.GetKeyDown(KeyCode.Alpha7) ? 7 :
                     Input.GetKeyDown(KeyCode.Alpha8) ? 8 :
                     Input.GetKeyDown(KeyCode.Alpha9) ? 9 :
                     Input.GetKeyDown(KeyCode.Alpha0) ? 0 : -1;
            if (teamIndex != -1)
            {
                if (Input.GetKey(KeyCode.LeftShift)) SetTeam?.Invoke(teamIndex);
                else ChoseTeam?.Invoke(teamIndex);
            }
            #endregion
#elif UNITY_ANDROID || UNITY_IOS //移动端屏幕输入
            if (!joystick)
            {
                joystick = GameObject.FindObjectOfType<VariableJoystick>();
                return;
            }
            #region [屏幕输入触发事件]
            if (joystick.Horizontal < 0) KeyCode_A?.Invoke(-joystick.Horizontal);
            if (joystick.Horizontal > 0) KeyCode_D?.Invoke(joystick.Horizontal);
            if (joystick.Vertical > 0) KeyCode_W?.Invoke(joystick.Vertical);
            if (joystick.Vertical < 0) KeyCode_S?.Invoke(-joystick.Vertical);
                        if (Input.touchCount >= 2)//双指缩放屏幕
            {
                Touch firTouch = Input.GetTouch(0);
                Touch secTouch = Input.GetTouch(1);
                if (secTouch.phase == TouchPhase.Began)
                {
                    twoFingerDis = math.distance(firTouch.position, secTouch.position);
                    Debug.Log("双指触碰！初始距离：" + twoFingerDis);
                }
                else
                {
                    float newDis = math.distance(firTouch.position, secTouch.position);
                    if (newDis != twoFingerDis)
                    {
                        //缩放屏幕
                        Debug.Log("缩放屏幕");
                        Vector3 pos = CameraMgr._worldCamera.position;
                        pos.y += (twoFingerDis - newDis) * 0.1f;
                        CameraMgr._worldCamera.position = pos;
                    }
                    twoFingerDis = newDis;
                }
            }
            else
            {
                twoFingerDis = -1f;
            }
            #endregion
#endif
        }


        private void ClearClick()
        {
            float curTime = Time.realtimeSinceStartup;
            clickStartTime = curTime;
            clickEndTime = curTime;
        }
        private bool IsOnUI()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            bool flag = EventSystem.current.IsPointerOverGameObject();
#else
            bool flag = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#endif
            //List<RaycastResult> resultList = new List<RaycastResult>();
            //EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current), resultList);
            return flag;
        }
    }
}

