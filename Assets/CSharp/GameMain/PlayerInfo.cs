using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class PlayerInfo
{
    [XmlAttribute("Gold")]
    public int Gold;
    [XmlAttribute("Level")]
    public int Level;
    [XmlAttribute("BestRecord")]
    public int ShortestSecond;
    [XmlAttribute("CurrentRecord")]
    public int ThisSecond;
}
