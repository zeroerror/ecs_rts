using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
public class GameWinPanel : UIBehavior
{
    private void OnEnable()
    {
        //本次成绩
        PlayerInfo info = GameController.GetPlayerInfo();
        int minutes = info.ThisSecond / 60;
        int seconds = info.ThisSecond % 60;
        StringBuilder sb = new StringBuilder();
        if (minutes != 0) sb.Append(minutes).Append(" 分 ");
        sb.Append(seconds).Append(" 秒");
        Text_SetText("ThisRecord/CostTime", sb.ToString());
        //最佳成绩
        minutes = info.ShortestSecond / 60;
        seconds = info.ShortestSecond % 60;
        sb.Clear();
        if (minutes != 0) sb.Append(minutes).Append(" 分 ");
        sb.Append(seconds).Append(" 秒");
        Text_SetText("OldRecord/CostTime", sb.ToString());
        SetActive("NewRecord", GameController.isBreakRecord);
    }
}
