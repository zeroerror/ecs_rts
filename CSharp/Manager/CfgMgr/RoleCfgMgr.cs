//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ZeroHero {
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using UnityEngine;
    using Unity.Mathematics;
    using UnityEngine.Networking;
    using System.Text;
    
    
    [Serializable()]
    public class RoleCfg {
        
        [XmlAttribute("CharacterType")]
        public CharacterType CharacterType{ get; set; }//;
        
        [XmlAttribute("IsHero")]
        public bool IsHero{ get; set; }//;
        
        [XmlAttribute("Health")]
        public float Health{ get; set; }//;
        
        [XmlAttribute("Energy")]
        public float Energy{ get; set; }//;
        
        [XmlAttribute("SmallSkillID")]
        public int SmallSkillID{ get; set; }//;
        
        [XmlAttribute("BigSkillID")]
        public int BigSkillID{ get; set; }//;
        
        [XmlAttribute("BulletType")]
        public BulletType BulletType{ get; set; }//;
        
        [XmlAttribute("RoleType")]
        public RoleType RoleType{ get; set; }//;
        
        [XmlAttribute("DamageType")]
        public DamageType DamageType{ get; set; }//;
        
        [XmlAttribute("AtkStrength")]
        public float AtkStrength{ get; set; }//;
        
        [XmlAttribute("AtkSpeed")]
        public float AtkSpeed{ get; set; }//;
        
        [XmlAttribute("DamageRange")]
        public float DamageRange{ get; set; }//;
        
        [XmlAttribute("AtkRange")]
        public float AtkRange{ get; set; }//;
        
        [XmlAttribute("SearchRange")]
        public float SearchRange{ get; set; }//;
        
        [XmlAttribute("WalkSpeed")]
        public float WalkSpeed{ get; set; }//;
        
        [XmlAttribute("BulletInitSpeed")]
        public string BulletInitSpeed{ get; set; }//;
        
        [XmlAttribute("BulletSpeed")]
        public float BulletSpeed{ get; set; }//;
        
        [XmlAttribute("ResourceId")]
        public int ResourceId{ get; set; }//;
        
        [XmlAttribute("IconId")]
        public int IconId{ get; set; }//;
    }
    
    public struct RoleStruct {
        
        public CharacterType CharacterType;
        
        public bool IsHero;
        
        public float Health;
        
        public float Energy;
        
        public int SmallSkillID;
        
        public int BigSkillID;
        
        public BulletType BulletType;
        
        public RoleType RoleType;
        
        public DamageType DamageType;
        
        public float AtkStrength;
        
        public float AtkSpeed;
        
        public float DamageRange;
        
        public float AtkRange;
        
        public float SearchRange;
        
        public float WalkSpeed;
        
        public string BulletInitSpeed;
        
        public float BulletSpeed;
        
        public int ResourceId;
        
        public int IconId;
    }
    
    public sealed class RoleCfgMgr {
        
        private static SerializableDictionary<CharacterType, RoleCfg> roleCfgDic;
        
        public static void Init() {
            var uri = new System.Uri(Path.Combine(Application.streamingAssetsPath, "xml","角色表.xml"));
            var request = UnityWebRequest.Get(uri.AbsoluteUri);
            request.SendWebRequest();
            while (!request.isDone){ if (request.isNetworkError) { Debug.Log(request.error); return;}};
            byte[] bytes = Encoding.UTF8.GetBytes(request.downloadHandler.text);
            MemoryStream stream = new MemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            XmlSerializer xmlFormatter = new XmlSerializer(typeof(SerializableDictionary<CharacterType, RoleCfg>));;
            roleCfgDic = (SerializableDictionary<CharacterType, RoleCfg>)xmlFormatter.Deserialize(stream);;
            stream.Close();;
        }
        
        public static RoleCfg GetByCharacterType(CharacterType charactertype) {
            if (!roleCfgDic.TryGetValue(charactertype, out RoleCfg cfg)) {
              Debug.LogError("角色表: 配置表出错, 不存在id: "+charactertype );    
              return null;
            }
            return cfg;
        }
        
        public static List<RoleCfg> GetConfigList() {
            List<RoleCfg> list = new List<RoleCfg>();
            foreach (var item in roleCfgDic){list.Add(item.Value);};
            return list;
        }
    }
}
