using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SupplyData", menuName = "Data/SupplyData")]
public class SupplyData : ScriptableObject
{
    public int hp;
    public int ammo;
    public int O2;
    public int capacity;
    public List<int> curFishList = new();
}
