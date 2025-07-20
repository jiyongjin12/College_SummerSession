using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatusUpgradeData", menuName = "Data/StatusUpgradeData")]
public class StatusUpgradeData : ScriptableObject
{
    [Header("0 : HP / 1 : O2 / 2 : Speed / 3 : Capacity")]
    public List<int> playerLV = new();
    public int gunID;
    public int gunLV;
}
