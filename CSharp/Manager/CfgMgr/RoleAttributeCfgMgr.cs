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
    public class RoleAttributeCfg {
        
        [XmlAttribute("ID")]
        public int ID{ get; set; }//;
        
        [XmlAttribute("Health")]
        public float Health{ get; set; }//;
        
        [XmlAttribute("AtkStrength")]
        public float AtkStrength{ get; set; }//;
        
        [XmlAttribute("AtkSpeed")]
        public float AtkSpeed{ get; set; }//;
        
        [XmlAttribute("MoveSpeed")]
        public float MoveSpeed{ get; set; }//;
    }
    
    public struct RoleAttributeStruct {
        
        public int ID;
        
        public float Health;
        
        public float AtkStrength;
        
        public float AtkSpeed;
        
        public float MoveSpeed;
    }
    
    public sealed class RoleAttributeCfgMgr {
        
        private static SerializableDictionary<int, RoleAttributeCfg> roleattributeCfgDic;
        
        public static void Init() {
            var uri = new System.Uri(Path.Combine(Application.streamingAssetsPath, "xml","角色属性表.xml"));
            var request = UnityWebRequest.Get(uri.AbsoluteUri);
            request.SendWebRequest();
            while (!request.isDone){ if (request.isNetworkError) { Debug.Log(request.error); return;}};
            byte[] bytes = Encoding.UTF8.GetBytes(request.downloadHandler.text);
            MemoryStream stream = new MemoryStream();
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            XmlSerializer xmlFormatter = new XmlSerializer(typeof(SerializableDictionary<int, RoleAttributeCfg>));;
            roleattributeCfgDic = (SerializableDictionary<int, RoleAttributeCfg>)xmlFormatter.Deserialize(stream);;
            stream.Close();;
        }
        
        public static RoleAttributeCfg GetByID(int id) {
            if (!roleattributeCfgDic.TryGetValue(id, out RoleAttributeCfg cfg)) {
              Debug.LogError("角色属性表: 配置表出错, 不存在id: "+id );    
              return null;
            }
            return cfg;
        }
        
        public static List<RoleAttributeCfg> GetConfigList() {
            List<RoleAttributeCfg> list = new List<RoleAttributeCfg>();
            foreach (var item in roleattributeCfgDic){list.Add(item.Value);};
            return list;
        }
    }
}
