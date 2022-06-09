using UnityEngine;
using UnityEngine.U2D;
using ZeroHero;

public class ResourceMgr
{

    #region Method
    public static Texture LoadTexture(int textureID)
    {
        Texture texture = null;
        var cfg = IconCfgMgr.GetById(textureID);
        if (cfg == null) return null;

        string path = cfg.AssetPath + cfg.SpriteName;
#if UNITY_EDITOR
        texture = Resources.Load(path) as Texture;
#else
        texture = ABMgr.Instance.LoadRes("ui", cfg.SpriteName) as Texture;
#endif
        if (texture == null)
        {
            Debug.LogError("Load Texture Failed!  不存在Texture: " + path);
        }
        return texture;
    }
    public static Sprite LoadSprite(int iconID)
    {
        SpriteAtlas spriteAtlas = null;
        Sprite sprite = null;
        var cfg = IconCfgMgr.GetById(iconID);
        if (cfg == null) return null;

        string atlasPath = cfg.AssetPath + cfg.AtlasName;
#if UNITY_EDITOR
        spriteAtlas = Resources.Load(atlasPath) as SpriteAtlas;
#else
        spriteAtlas = ABMgr.Instance.LoadRes("ui", cfg.AtlasName) as SpriteAtlas;
#endif
        if (spriteAtlas == null)
        {
            Debug.LogError("LoadSprite Failed!  不存在SpriteAtlas: " + atlasPath);
        }
        else
        {
            sprite = spriteAtlas.GetSprite(cfg.SpriteName);
        }
        return sprite;
    }
    public static GameObject LoadCamera(string camName)
    {
        GameObject prefab = null;
        var cfg = CameraCfgMgr.GetByCameraName(camName);
#if UNITY_EDITOR
        prefab = Resources.Load(cfg.AssetPath + cfg.AssetName) as GameObject;
        if (prefab == null) Debug.LogError("CameraCfgMgr: 不存在路径 " + cfg.AssetPath + cfg.AssetName);
#else
        prefab = ABMgr.Instance.LoadRes("camera", cfg.AssetName.ToLower()) as GameObject;
#endif

        return prefab;
    }
    public static GameObject LoadUI(string uiName)
    {
        GameObject prefab = null;
        var uiCfg = UICfgMgr.GetByUIName(uiName);
#if UNITY_EDITOR
        prefab = Resources.Load(uiCfg.AssetPath + uiCfg.AssetName) as GameObject;
        if (prefab == null) Debug.LogError("UICfgMgr: 不存在路径 " + uiCfg.AssetPath + uiCfg.AssetName);
#else
        prefab = ABMgr.Instance.LoadRes("ui", uiCfg.AssetName.ToLower()) as GameObject;
#endif

        return prefab;
    }
    public static GameObject LoadCharacter(CharacterType characterName)
    {
        GameObject prefab = null;
        var rolecfg = RoleCfgMgr.GetByCharacterType(characterName);
        var cfg = ModleCfgMgr.GetById(rolecfg.ResourceId);
#if UNITY_EDITOR
        prefab = Resources.Load(cfg.AssetPath + cfg.AssetName) as GameObject;
        if (prefab == null) Debug.LogError("ModleCfgMgr: 不存在路径 " + cfg.AssetPath + cfg.AssetName);
#else
        prefab = ABMgr.Instance.LoadRes("character", cfg.AssetName.ToLower()) as GameObject;
#endif

        return prefab;
    }
    public static SkillObject LoadSkillObject(int skillId)
    {
       SkillObject skillObject = null;
        var skillCfg = SkillCfgMgr.GetBySkillID(skillId);
#if UNITY_EDITOR
        skillObject = Resources.Load(skillCfg.AssetPath + skillCfg.AssetName) as SkillObject;
        if (skillObject == null) Debug.LogError("SkillCfgMgr: 不存在路径 " + skillCfg.AssetPath + skillCfg.AssetName);
#else
        skillObject = ABMgr.Instance.LoadRes("skill", skillCfg.AssetName.ToLower()) as SkillObject;
#endif

        return skillObject;
    }
    public static GameObject LoadEffect(int effectId)
    {
        GameObject prefab = null;
        var effectCfg = EffectCfgMgr.GetById(effectId);
#if UNITY_EDITOR
        prefab = Resources.Load(effectCfg.AssetPath + effectCfg.AssetName) as GameObject;
        if (prefab == null) Debug.LogError("EffectCfgMgr: 不存在路径 " + effectCfg.AssetPath + effectCfg.AssetName);
#else
        prefab = ABMgr.Instance.LoadRes("effect", effectCfg.AssetName.ToLower()) as GameObject;
#endif

        return prefab;
    }
    public static GameObject LoadResource(int resourceId)
    {
        GameObject prefab = null;
        var resourceCfg = ResourceCfgMgr.GetById(resourceId);
#if UNITY_EDITOR
        prefab = Resources.Load(resourceCfg.AssetPath + resourceCfg.AssetName) as GameObject;
        if (prefab == null) Debug.LogError("ResourceCfgMgr: 不存在路径 " + resourceCfg.AssetPath + resourceCfg.AssetName);
#else
        prefab = ABMgr.Instance.LoadRes("ui", resourceCfg.AssetName.ToLower()) as GameObject;
#endif

        return prefab;
    }
    #endregion

}
