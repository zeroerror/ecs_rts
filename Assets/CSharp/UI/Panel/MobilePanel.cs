using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeroHero;

public class MobilePanel : UIBehavior
{
    private const int teamMax = 6;
    private bool isEditTeam = false;
    private bool[] setTeam = new bool[teamMax];
    private void Start()
    {
        SetOnClick("AttackBtn", _ClickAtkBtn);
        SetOnClick("AtkArea", _ClickArea);
        SetOnClick("BombBtn", _ClickBombBtn);
        SetOnClick("BombArea", _ClickBombArea);
        SetOnClick("LockCamBtn", _LockCam);
        SetOnClick("TeamEditBtn", _TeamEditMode);
        //编队按钮
        for (int i = 1; i <= teamMax; i++)
        {
            SetOnClick("TeamGroup/TeamItem" + i, _ClickTeamItem, i);
        }
    }
    private void OnEnable()
    {
        SetActive("AtkArea", false);
        EntityEventSystem.Instance.onSetTeamSuccess -= _OnSetTeamSuccess;
        EntityEventSystem.Instance.onSetTeamSuccess += _OnSetTeamSuccess;
    }
    private void OnDisable()
    {
        EntityEventSystem.Instance.onSetTeamSuccess -= _OnSetTeamSuccess;
    }
    private void _LockCam(params object[] args)
    {
        if (EntityGameObject._curSelectedGameObj == null)
        {
            EntityEventSystem.Instance.OnCameraSetFollowTarget();
        }
        else if (CameraFollower.followTarget != EntityGameObject._curSelectedGameObj)
        {
            EntityEventSystem.Instance.OnCameraSetFollowTarget(EntityGameObject._curSelectedGameObj);
            Debug.Log("设置相机对象 : " + EntityGameObject._curSelectedGameObj);
        }
        else
        {
            EntityEventSystem.Instance.OnCameraSetFollowTarget();
        }
    }
    private void _ClickAtkBtn(params object[] args)
    {
        SetActive("AtkArea", true);
    }

    private void _ClickArea(params object[] args)
    {
        InputMgr.Instance.OnPointerDown1();
        SetActive("AtkArea", false);
    }
    private void _ClickBombBtn(params object[] args)
    {
        SetActive("BombArea", true);
    }

    private void _ClickBombArea(params object[] args)
    {
        InputMgr.Instance.OnBombTargetPos();    
        SetActive("BombArea", false);
    }
    private void _TeamEditMode(params object[] args)
    {
        isEditTeam = !isEditTeam;
        if (isEditTeam)
        {
            //开启编队模式
            Text_SetText("TeamEditBtn/Text", "完成");
            Image_SetColor("TeamEditBtn", "#E35757");
            ResetChosenTeam();
        }
        else
        {
            //关闭编队模式
            Text_SetText("TeamEditBtn/Text", "编辑");
            Image_SetColor("TeamEditBtn", "#FFFFFF");
        }
    }
    private void _ClickTeamItem(params object[] args)
    {
        int teamIndex = (int)args[0];
        if (isEditTeam)
        {
            InputMgr.Instance.OnSetTeam(teamIndex);
        }
        else
        {
            InputMgr.Instance.OnChoseTeam(teamIndex);
            ResetChosenTeam();
            string teamItemName = "TeamGroup/TeamItem" + teamIndex;
            SetActive(teamItemName + "/Chosen", true);
        }
    }
    private void ResetChosenTeam()
    {
        for (int i = 1; i <= teamMax; i++)
        {
            string name = "TeamGroup/TeamItem" + i;
            SetActive(name + "/Chosen", false);
        }
    }
    private void _OnSetTeamSuccess(params object[] args)
    {
        //设置队伍成功，刷新UI
        int teamIndex = (int)args[0];
        setTeam[teamIndex] = true;
        string name = "TeamGroup/TeamItem" + teamIndex;
        SetActive(name + "/Set", true);
    }

}
