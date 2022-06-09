using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using ZeroHero;

public class AttributePanel : UIBehavior
{
    private AttributeBase attributeBase;
    private Attribute attribute;
    private SkillsTimer skillsTimer;
    private RoleCfg roleCfg;
    private const string normalColor = "#FFD79B";
    private const string extraColor = "#00FF15";
    private const int normalSize = 30;
    private const int extraSize = 35;
    private void Start()
    {
        SetOnClick("SmallSkill", _SmallSkill);
        SetOnClick("BigSkill", _BigSkill);
    }
    private void OnEnable()
    {
        _UpdateText(args);
        EntityEventSystem.Instance.onChosenCharacterUpdate += _UpdateText;
    }
    private void OnDisable()
    {
        EntityEventSystem.Instance.onChosenCharacterUpdate -= _UpdateText;
    }
    private void _SmallSkill(params object[] args)
    {
        InputMgr.Instance.OnReleaseSmallSkill();
    }

    private void _BigSkill(params object[] args)
    {
        InputMgr.Instance.OnReleaseBigSkill();
    }
    private void _UpdateText(object[] args)
    {
        attribute = (Attribute)args[0];
        attributeBase = (AttributeBase)args[1];
        skillsTimer = (SkillsTimer)args[2];
        CampType campType = (CampType)args[3];
        switch (campType)
        {
            case CampType.敌军:
                RawImage_SetImage("HealthBar/Fill Area/Fill", 1011);
                break;
            case CampType.友军:
                RawImage_SetImage("HealthBar/Fill Area/Fill", 1010);
                break;
            default:
                break;
        }
        if (attribute.health <= 0)
        {
            UIMgr.CloseUI(ui.uiName);
            return;
        }
        roleCfg = RoleCfgMgr.GetByCharacterType(attributeBase.characterType);
        var iconID = roleCfg.IconId;
        Image_SetImage("HeadIcon", iconID);
        Text_SetText("HealthBar/Health", attribute.health > attributeBase.health ? attributeBase.health : attribute.health);
        Text_SetText("HealthBar/HealthMax", attributeBase.health);
        Text_SetText("EnergyBar/Energy", attribute.energy > attributeBase.energy ? attributeBase.energy : attribute.energy);
        Text_SetText("EnergyBar/EnergyMax", attributeBase.energy);
        Text_SetText("AttributeInfo/atkStrength/Num", attribute.atkStrength.ToString("0.#"));
        Text_SetText("AttributeInfo/atkSpeed/Num", attribute.atkSpeed.ToString("0.#"));
        Text_SetText("AttributeInfo/atkRange/Num", attribute.atkRange.ToString("0.#"));
        Text_SetText("AttributeInfo/moveSpeed/Num", attribute.moveSpeed.ToString("0.#"));
        if (attribute.atkStrength > attributeBase.atkStrength)
        {
            Text_SetColor("AttributeInfo/atkStrength/Num", extraColor);
            Text_SetFontSize("AttributeInfo/atkStrength/Num", extraSize);
        }
        else
        {
            Text_SetColor("AttributeInfo/atkStrength/Num", normalColor);
            Text_SetFontSize("AttributeInfo/atkStrength/Num", normalSize);
        }
        if (attribute.atkSpeed > attributeBase.atkSpeed)
        {
            Text_SetColor("AttributeInfo/atkSpeed/Num", extraColor);
            Text_SetFontSize("AttributeInfo/atkSpeed/Num", extraSize);
        }
        else
        {
            Text_SetColor("AttributeInfo/atkSpeed/Num", normalColor);
            Text_SetFontSize("AttributeInfo/atkSpeed/Num", normalSize);
        }
        if (attribute.atkRange > attributeBase.atkRange)
        {
            Text_SetColor("AttributeInfo/atkRange/Num", extraColor);
            Text_SetFontSize("AttributeInfo/atkRange/Num", extraSize);
        }
        else
        {
            Text_SetColor("AttributeInfo/atkRange/Num", normalColor);
            Text_SetFontSize("AttributeInfo/atkRange/Num", normalSize);
        }
        if (attribute.moveSpeed > attributeBase.moveSpeed)
        {
            Text_SetColor("AttributeInfo/moveSpeed/Num", extraColor);
            Text_SetFontSize("AttributeInfo/moveSpeed/Num", extraSize);
        }
        else
        {
            Text_SetColor("AttributeInfo/moveSpeed/Num", normalColor);
            Text_SetFontSize("AttributeInfo/moveSpeed/Num", normalSize);
        }

        Text_SetText("Level/Num", attributeBase.level);
        if (skillsTimer.smallSkillTimer >= attribute.smallSkillCD * (1 - attribute.cdShrink))
        {
            Text_SetText("SmallSkill/Text", "Q");
            Text_SetAlignment("SmallSkill/Text", TextAnchor.MiddleCenter);
            Image_SetFillAmount("SmallSkill/Image", 1);
        }
        else
        {
            float skillCD = attribute.smallSkillCD * (1 - attribute.cdShrink);
            float cd = skillCD - skillsTimer.smallSkillTimer;
            Text_SetText("SmallSkill/Text", cd.ToString("0.#") + "s");
            Text_SetAlignment("SmallSkill/Text", TextAnchor.MiddleLeft);
            Image_SetFillAmount("SmallSkill/Image", skillsTimer.smallSkillTimer / skillCD);
        }
        if (skillsTimer.bigSkillTimer >= attribute.bigSkillCD * (1 - attribute.cdShrink))
        {
            Text_SetText("BigSkill/Text", "R");
            Text_SetAlignment("BigSkill/Text", TextAnchor.MiddleCenter);
            Image_SetFillAmount("BigSkill/Image", 1);
        }
        else
        {
            float skillCD = attribute.bigSkillCD * (1 - attribute.cdShrink);
            float cd = skillCD - skillsTimer.bigSkillTimer;
            Text_SetText("BigSkill/Text", cd.ToString("0.#") + "s");
            Text_SetAlignment("BigSkill/Text", TextAnchor.MiddleLeft);
            Image_SetFillAmount("BigSkill/Image", skillsTimer.bigSkillTimer / skillCD);
        }
        float val1 = attributeBase.health == 0 ? 0 : (attribute.health / attributeBase.health);
        float val2 = attributeBase.energy == 0 ? 0 : (attribute.energy / attributeBase.energy);
        Slider_SetVal("HealthBar", val1);
        Slider_SetVal("EnergyBar", val2);
    }
}
