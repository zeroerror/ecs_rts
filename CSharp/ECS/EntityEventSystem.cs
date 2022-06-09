using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
namespace ZeroHero
{
    public class EntityEventSystem : UnitySingleton<EntityEventSystem>
    {
        #region [游戏事件(外界触发)]
        public event Callback onGoldChange;
        public void OnGoldChange(params object[] args)
        {
            onGoldChange?.Invoke(args);
        }
        public event Callback onGameInfoUpdate;
        public void OnGameInfoUpdate(params object[] args)
        {
            onGameInfoUpdate?.Invoke(args);
        }
        public event Callback onClickCharacter;
        public void OnClickCharacter(params object[] args)
        {
            onClickCharacter?.Invoke(args);
        }
        public event Callback onChosenCharacterUpdate;
        public void OnChosenCharacterUpdate(params object[] args)
        {
            onChosenCharacterUpdate?.Invoke(args);
        }
        public event Callback onCameraSetFollowTarget;
        public void OnCameraSetFollowTarget(params object[] args)
        {
            onCameraSetFollowTarget?.Invoke(args);
        }
        public event Callback onSetTeamSuccess;
        public void OnSetTeamSuccess(int teamIndex)
        {
            onSetTeamSuccess?.Invoke(teamIndex);
        }
        #region 游戏状态
        public event Callback onGameStateChanged;
        public void OnGameStateChanged(params object[] args)
        {
            onGameStateChanged?.Invoke(args);
        }
        #endregion

        #endregion

        #region [实体类事件]
        public EntityArchetype memberDieEvent { get; private set; }
        public EntityArchetype buffArchetype { get; private set; }
        public EntityArchetype releaseSkillArchetype { get; private set; }
        #endregion

        #region [Field]
        private float clickStartTime;
        private float clickEndTime;
        private float doubleClickInterval = 0.2f;
        #endregion

        private void Start()
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            memberDieEvent = entityManager.CreateArchetype(
                    typeof(MemberDieEvent)
                 );
            buffArchetype = entityManager.CreateArchetype(
                typeof(Buff)
             );
            releaseSkillArchetype = entityManager.CreateArchetype(
              typeof(ReleaseSkill)
           );

        }
    }

}


