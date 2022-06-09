using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using ZeroHero;
/// <summary>
/// 用于绑定GameObject对应的Entity
/// </summary>
public class EntityGameObject : MonoBehaviour
{
    #region [Public_Field]
    public Entity entity = Entity.Null;
    public EntityManager entityManager;
    public bool isNeedInitByConfig = false;
    public bool isNeedInitByAttributeBase = false;
    public AttributeBase savedAttributeBase;
    public CampType campType;
    [Header("目标1")]
    public float3 place_1;
    [Header("目标2")]
    public float3 place_2;
    [Header("目标3")]
    public float3 place_3;
    [Header("目标4")]
    public float3 place_4;
    public Entity pathPositionBufferEntity = Entity.Null;
    #endregion
    #region [Property]
    public int level { get; private set; }
    public float exp { get; private set; }
    #endregion
    #region [Private_Field]      
    [SerializeField] private ConvertedEntityHolder entityHolder;
    private Animator _anim;
    private GameObject _atkEffect = null;
    private GameObject _flameEffect = null;
    private GameObject _healingEffect = null;
    private GameObject _acceleratedEffect = null;
    private Rotation rotation;
    private float3 lastFramePos;
    private float3 curFramePos;
    private float lastFrameHealth = 0;
    private float curFrameHealth;
    private float coefficient;
    private Attribute _attribute;
    private AttributeBase _attributeBase;
    private SkillsTimer _skillsTimer;

    private bool isDead = false;
    private bool isAttacking = false;
    private bool isWalking = false;
    private bool isHealing = false;
    private bool isAccelerated = false;
    private float maxHealth;
    private string _shootEffName = "";
    #endregion

    public static GameObject _curSelectedGameObj { get; private set; }
    private void Start()
    {
        _anim = GetComponent<Animator>();
        coefficient = 3f * transform.localScale.z;
    }
    private void OnDestroy()
    {
        Destroy(gameObject);
        RemoveConnectedEffect();
        if (entity != Entity.Null && GameController._gameState != GameState.退出)
        {
            entityManager.DestroyEntity(entity);
        }

    }
    private void Update()
    {
        #region 游戏状态检测和初始化实体
        if (GameController._gameState == GameState.结束)
        {
            OnDestroy();
            return;
        }
        if (entity == Entity.Null)
        {
            Init();
            return;
        }
        #endregion
        #region 判断是否死亡
        if (!entityManager.HasComponent<Attribute>(entity))
        {
            if (!isDead)
            {
                isDead = true;
                RemoveConnectedEffect();
                if (_anim != null) Invoke("DestroyGameObj", 2);
                else DestroyGameObj();
                if (_anim != null)
                {
                    _anim.speed = 1f;
                    _anim.SetBool("attack", false);
                    _anim.SetBool("walk", false);
                    _anim.SetBool("dead", true);
                }
            }
            if (_anim != null && _anim.GetCurrentAnimatorStateInfo(0).IsName("Dead"))
            {
                _anim.SetBool("dead", false);
            }
            return;
        }
        #endregion
        #region entity组件信息获取
        _attribute = entityManager.GetComponentData<Attribute>(entity);
        _attributeBase = entityManager.GetComponentData<AttributeBase>(entity);
        level = _attributeBase.level;
        exp = _attributeBase.exp;
        if (_attribute.characterType != CharacterType.中立障碍物)
        {
            _skillsTimer = entityManager.GetComponentData<SkillsTimer>(entity);
        }

        rotation = entityManager.GetComponentData<Rotation>(entity);
        curFramePos = entityManager.GetComponentData<Translation>(entity).Value;
        lastFramePos = transform.position;
        Common.FormatFloat3(ref curFramePos, 2);
        Common.FormatFloat3(ref lastFramePos, 2);
        if (UnityEngine.Time.frameCount % 10 == 0)
        {
            isWalking = !lastFramePos.Equals(curFramePos);
        }
        curFrameHealth = _attribute.health;
        isAttacking = entityManager.HasComponent<AttackComponent>(entity);
        isHealing = (lastFrameHealth != 0 && curFrameHealth > lastFrameHealth) ? true : false;
        isAccelerated = _attribute.moveSpeed > _attributeBase.moveSpeed;
        #endregion
        #region [Transform移动]
        transform.position = curFramePos;
        transform.rotation = rotation.Value;
        #endregion
        #region 射击特效、动画状态机
        if (_shootEffName != "") SetShootEffect(_shootEffName);
        if (_anim) SetAnim();
        #endregion
        #region 其他特效
        if (isHealing)
        {
            SetEffect("回血", curFramePos + new float3(0, 1f, 0), ref _healingEffect);
        }
        if (isAccelerated)
        {
            SetEffect("加速", curFramePos + new float3(0, 1f, 0), ref _acceleratedEffect);
            _acceleratedEffect.transform.forward = -transform.forward;
            float3 axis = new float3(-1, 0, 0);
            float3 curForward = transform.forward;
            float doc = curForward.x * axis.x + curForward.z * axis.z;
            float l1 = Mathf.Sqrt(curForward.x * curForward.x + curForward.z * curForward.z);
            float l2 = Mathf.Sqrt(axis.x * axis.x + axis.z * axis.z);
            float cos = doc / (l1 * l2);
            float angle = Mathf.Acos(cos) * (curForward.z < 0 ? -1 : 1);
            var main = _acceleratedEffect.GetComponent<ParticleSystem>().main;
            main.startRotation = new ParticleSystem.MinMaxCurve(angle);
        }
        else
        {
            if (_acceleratedEffect) _acceleratedEffect.SetActive(false);
        }
        lastFrameHealth = curFrameHealth;
        switch (_attribute.characterType)
        {
            case CharacterType.步兵:
                break;
            case CharacterType.枪手:
                break;
            case CharacterType.战士:
                break;
            case CharacterType.坦克:
                if (_attribute.health < (maxHealth / 2))
                    SetEffect("载具冒火", curFramePos + new float3(0, 1f, 0), ref _flameEffect);
                else
                    StopEffect(ref _flameEffect);
                break;
            case CharacterType.机枪人:
                if (_attribute.health < (maxHealth / 2))
                    SetEffect("载具冒火", curFramePos + new float3(0, 2f, 0), ref _flameEffect);
                else
                    StopEffect(ref _flameEffect);
                break;
            case CharacterType.防御塔:
                if (_attribute.health < (maxHealth / 2))
                    SetEffect("载具冒火", curFramePos + new float3(0, 2f, 0), ref _flameEffect);
                else
                    StopEffect(ref _flameEffect);
                break;
            default:
                break;
        }
        #endregion

        if (entityManager.HasComponent<AllySelected>(entity))
        {
            _curSelectedGameObj = gameObject;
        }
        else if (_curSelectedGameObj == gameObject)
        {
            _curSelectedGameObj = null;
        }
    }

    private void LateUpdate()
    {
        if (_curSelectedGameObj == null)
        {
            EntityEventSystem.Instance.OnCameraSetFollowTarget();
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (_curSelectedGameObj == gameObject)
            {
                if (CameraFollower.followTarget != _curSelectedGameObj)
                {
                    EntityEventSystem.Instance.OnCameraSetFollowTarget(_curSelectedGameObj);
                    Debug.Log("设置相机对象 : " + _curSelectedGameObj);
                }
                else
                {
                    EntityEventSystem.Instance.OnCameraSetFollowTarget();
                }
            }
        }
    }
    /// <summary>
    /// 摧毁GameObject
    /// </summary>
    private void DestroyGameObj()
    {
        switch (_attribute.characterType)
        {
            case CharacterType.步兵:
                break;
            case CharacterType.枪手:
                if (_atkEffect) _atkEffect.SetActive(false);
                break;
            case CharacterType.战士:
                break;
            case CharacterType.坦克:
                EffectMgr.Instance.PlayEffect("载具爆炸", curFramePos);
                break;
            case CharacterType.机枪人:
                break;
            case CharacterType.防御塔:
                EffectMgr.Instance.PlayEffect("载具爆炸", curFramePos);
                break;
            default:
                break;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 移除循环链接着的特效
    /// </summary>
    private void RemoveConnectedEffect()
    {
        if (_flameEffect) _flameEffect.SetActive(false);
        if (_healingEffect) _healingEffect.SetActive(false);
        if (_acceleratedEffect) _acceleratedEffect.SetActive(false);
    }

    /// <summary>
    /// 设置动画状态机
    /// </summary>
    private void SetAnim()
    {
        if (_anim == null) return;
        if (_anim.GetBool("walk") != isWalking) _anim.SetBool("walk", isWalking);
        _anim.SetBool("attack", isAttacking && !_anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"));
        if (_anim.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            _anim.speed = 1 / (1f / _attribute.atkSpeed);
        }
        else if (_anim.GetCurrentAnimatorStateInfo(0).IsName("Walk"))
        {
            _anim.speed = 1f * (_attribute.moveSpeed / coefficient);
        }
        else
        {
            _anim.speed = 1f;
        }
    }

    /// <summary>
    /// 设置射击特效
    /// </summary>
    /// <param name="effectName"></param>
    private void SetShootEffect(string effectName)
    {
        if (isWalking && _atkEffect)
        {
            _atkEffect.SetActive(false);
            return;
        }
        if (!isAttacking)
        {
            return;
        }

        float3 atkPoint = entityManager.GetComponentData<LocalToWorld>(_attribute.atkPoint).Position;
        if (_atkEffect == null)
        {
            _atkEffect = EffectMgr.Instance.PlayEffect(effectName, atkPoint);
        }
        else
        {
            _atkEffect.GetComponent<ParticleSystem>().Play();
            _atkEffect.transform.position = atkPoint;
            _atkEffect.transform.forward = transform.forward;
            _atkEffect.SetActive(true);
        }
    }

    /// <summary>
    /// 设置特效
    /// </summary>
    /// <param name="effectName">特效名称</param>
    /// <param name="pos"></param>
    /// <param name="effect"></param>
    private void SetEffect(string effectName, float3 pos, ref GameObject effect)
    {
        if (effect == null)
        {
            effect = EffectMgr.Instance.PlayEffect(effectName, pos);
        }
        else
        {
            effect.SetActive(true);
            effect.transform.position = pos;
            effect.transform.forward = transform.forward;
        }
        effect.transform.localScale = transform.localScale; //根据体型大小变化
    }

    /// <summary>
    /// 停止特效
    /// </summary>
    /// <param name="effect"></param>
    private void StopEffect(ref GameObject effect)
    {
        if (effect == null) return;
        effect.SetActive(false);
    }

    /// <summary>
    /// 初始化参数
    /// </summary>
    private void Init()
    {
        entity = entityHolder.GetEntity();
        entityManager = entityHolder.GetEntityManager();
        if (entity != Entity.Null)
        {
            _attribute = entityManager.GetComponentData<Attribute>(entity);
            switch (_attribute.characterType)
            {
                case CharacterType.步兵:
                    break;
                case CharacterType.枪手:
                    _shootEffName = "开枪";
                    break;
                case CharacterType.炮车:
                    _shootEffName = "开炮";
                    break;
                case CharacterType.战士:
                    break;
                case CharacterType.坦克:
                    break;
                case CharacterType.机枪人:
                    _shootEffName = "开炮";
                    break;
                default:
                    break;
            }

            if (_attribute.characterType != CharacterType.中立障碍物)
            {
                entityManager.SetComponentData<Translation>(entity, new Translation { Value = transform.position });
                if (isNeedInitByAttributeBase) GameController.RespawnCharacter(gameObject, savedAttributeBase);//重生角色
                else if (isNeedInitByConfig) GameController.InitCharacter(gameObject, _attribute.characterType);//根据配置初始化人物参数
                maxHealth = _attributeBase.health;
            }
            else
            {
                maxHealth = _attribute.health;
            }


            //给小兵添加目标FinalGoal
            entityManager.AddComponentData<FinalGoal>(entity, new FinalGoal
            {
                place_1 = place_1,
                place_2 = place_2,
                place_3 = place_3,
                place_4 = place_4
            });

            if (pathPositionBufferEntity != Entity.Null)
            {
                DynamicBuffer<PathPosition> selfBuffer = entityManager.GetBuffer<PathPosition>(entity);
                DynamicBuffer<PathPosition> buffer = entityManager.GetBuffer<PathPosition>(pathPositionBufferEntity);
                for (int i = 0; i < buffer.Length; i++)
                {
                    PathPosition pathPosition = buffer[i];
                    selfBuffer.Add(pathPosition);
                }

                entityManager.SetComponentData<PathFollow>(entity, new PathFollow { pathIndex = buffer.Length - 1 });
            }

        }
    }
}

