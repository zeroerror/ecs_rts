/// <summary>
/// Date: 2022/4/20 15:21. ---Created By ZeroHero. 
/// Date: 2022/4/20 15:21. ---LastUpdated By ZeroHero.
/// </summary>
namespace ZeroHero
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    #region [委托]
    #region 回调委托
    public delegate void Callback(params object[] args);
    #endregion
    #endregion

    #region [通用]
    public struct m_float3
    {
        public float x;
        public float y;
        public float z;
        public m_float3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public static explicit operator m_float3(string v)
        {
            //example:v="0,0,0"
            m_float3 f3 = new m_float3();
            string[] colSplited = v.Split(',');
            if (colSplited.Length >= 3)
            {
                float x = float.Parse(colSplited[0]);
                float y = float.Parse(colSplited[1]);
                float z = float.Parse(colSplited[2]);
                f3 = new m_float3(x, y, z);
            }
            return f3;
        }
    }
    #endregion

    public class utils
    {
        public static Dictionary<string, System.Type> TYPE_REFLECTION = new Dictionary<string, System.Type>()
        {
            ["int"] = typeof(int),
            ["float"] = typeof(float),
            ["bool"] = typeof(bool),
            ["m_float3"] = typeof(m_float3),
            ["string"] = typeof(string),
            ["EntityType"] = typeof(EntityType),
            ["RoleType"] = typeof(RoleType),
            ["CampType"] = typeof(CampType),
            ["CharacterType"] = typeof(CharacterType),
            ["BuffType"] = typeof(BuffType),
            ["DamageType"] = typeof(DamageType),
            ["BulletType"] = typeof(BulletType),
        };
        public static Vector3 GetMouseWorldPosition()
        {
            Vector3 worldPosition = Vector3.zero;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 1000))
            {
                return raycastHit.point;
            }
            return worldPosition;
        }
        public static IEnumerator WaitForSecondsRealtime(float sec, Action action = null)
        {
            yield return new WaitForSecondsRealtime(sec);
            action?.Invoke();
        }
        public static IEnumerator WaitForSecondsRealtimeLoop(float interval, float delaySec, Action action = null)
        {
            yield return new WaitForSecondsRealtime(delaySec);
            while (true)
            {
                action?.Invoke();
                yield return new WaitForSecondsRealtime(interval);
            }
        }
    }



}


