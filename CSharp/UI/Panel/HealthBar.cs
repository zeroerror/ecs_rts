using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.UI;
using Unity.Mathematics;
using Unity.Transforms;
using ZeroHero;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private ConvertedEntityHolder convertedEntityHolder;
    public GameObject healthBarPrefab;
    public Transform healthBarPoint;
    private GameObject _bloodEffect;
    private Transform _uiBar;
    private Transform _mainCamera;
    private Slider _slider;
    private Slider _energySlider;
    private float _maxValue;
    private float _eachHealth = 250;
    private float _eachWidth = 2;
    private Entity entity = Entity.Null;
    private EntityManager entityManager;
    private Attribute _attribute;
    private AttributeBase _attributeBase;
    private SkillsTimer skillsTimer;
    [Header("始终可见")]
    public bool alwaysVisable;
    [Header("持续时间")]
    public float duration;
    private float cd;
    private float lastFrameHealth;
    private float curFrameHealth;
    private float lastEnergy;
    private float curEnergy;
    private float _maxHealth;

    private void Init()
    {
        entity = convertedEntityHolder.GetEntity();
        entityManager = convertedEntityHolder.GetEntityManager();
        if (entity != Entity.Null)
        {
            _attribute = entityManager.GetComponentData<Attribute>(entity);
            _attributeBase = entityManager.GetComponentData<AttributeBase>(entity);
            CreateHealthBar();
            _maxHealth = _attribute.health;
            _maxValue = _maxHealth / _eachHealth;
            _slider.maxValue = _maxValue;
            _energySlider.maxValue = _maxValue;
            _slider.SetValueWithoutNotify(_slider.maxValue);
            RectTransform rectTrans = _slider.GetComponent<RectTransform>();
            float2 sizeDelta = rectTrans.sizeDelta;
            sizeDelta.x = _eachWidth * _maxHealth / 1000;
            rectTrans.sizeDelta = sizeDelta;

        }
    }
    private void LateUpdate()
    {
        //初始化实体
        if (entity == Entity.Null)
        {
            Init();
            return;
        }
        if (!entityManager.HasComponent<Attribute>(entity))
        {
            Destroy(this);
            return;
        }
        UpdateHealthBar();
    }
    private void CreateHealthBar()
    {
        _mainCamera = CameraMgr._worldCamera;
        foreach (Canvas canvas in FindObjectsOfType<Canvas>())
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.transform.tag.Equals("血条"))
            {
                EntityGameObject ego = GetComponent<EntityGameObject>();
                if (ego)
                {
                    CampType campType = ego.campType;
                    var cfg = ResourceCfgMgr.GetById(CampCfgMgr.GetByCampType(campType).HealthBar);
                    healthBarPrefab = ResourceMgr.LoadResource(cfg.Id);
                }
                _uiBar = Instantiate(healthBarPrefab, canvas.transform).transform;
                _slider = _uiBar.GetComponent(typeof(Slider)) as Slider;
                _energySlider = _uiBar.GetComponentsInChildren(typeof(Slider))[1] as Slider;
                _uiBar.gameObject.SetActive(alwaysVisable);
                break;
            }
        }
    }
    private void OnDestroy()
    {
        if (_uiBar) Destroy(_uiBar.gameObject);
    }
    public void UpdateHealthBar()
    {
        if (_slider != null)
        {
            //更新基础属性信息
            if (entityManager.GetComponentData<AttributeBase>(entity).health != _attributeBase.health)
            {
                _attributeBase = entityManager.GetComponentData<AttributeBase>(entity);
                _maxHealth = _attributeBase.health;
                _maxValue = _maxHealth / _eachHealth;
                _slider.maxValue = _maxValue;
                _energySlider.maxValue = _maxValue;
                //RectTransform rectTrans = _slider.GetComponent<RectTransform>();
                //float2 sizeDelta = rectTrans.sizeDelta;
                //sizeDelta.x = _eachWidth * _maxHealth / 1000;
                //rectTrans.sizeDelta = sizeDelta;
            }

            _attribute = entityManager.GetComponentData<Attribute>(entity);
            skillsTimer = entityManager.GetComponentData<SkillsTimer>(entity);
            curFrameHealth = _attribute.health;
            curEnergy = _attribute.energy;
            if (lastFrameHealth != curFrameHealth)
            {
                float value1 = _maxValue * curFrameHealth / _maxHealth;
                _slider.SetValueWithoutNotify(value1);
                cd = duration;
                lastFrameHealth = curFrameHealth;
            }

            if (_attributeBase.isHero)
            {
                if (lastEnergy != curEnergy)
                {
                    float value2 = _maxValue * curEnergy / _attributeBase.energy;
                    _energySlider.SetValueWithoutNotify(value2);
                    lastEnergy = curEnergy;
                }
            }
            _energySlider.gameObject.SetActive(_attributeBase.isHero);


            _slider.transform.position = healthBarPoint.position;
            _slider.transform.forward = _mainCamera.forward;
            bool isActive = true;
            if (!alwaysVisable)
            {
                if (cd <= 0) isActive = false;
                else cd -= Time.deltaTime;
            }
            _uiBar.gameObject.SetActive(isActive);
        }

    }
}
