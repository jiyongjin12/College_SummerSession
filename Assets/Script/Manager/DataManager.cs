using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DataManager : MonoBehaviour
{
    public static DataManager instance { get; private set; }

    public StatusUpgradeData curPlayerData;
    public List<PlayerData> upgradeData;
    public List<GunDataList> gunUpgradeData;

    [Header("Data")]
    [SerializeField] private TextAsset StatusUpgradeData;
    [SerializeField] private TextAsset GunUpgradeData;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(gameObject);

        ReadStatusUpgradeData(StatusUpgradeData.text);
        ReadGunUpgradeData(GunUpgradeData.text);
    }

    public List<float> GetCurGunLVData() //int curGunIndex, int curLV
    {
        //나중에 특수 스탯 확인용으로 돌릴 예정, string으로 저장해두고 총기에 따라 불러오는 스탯이 다를테니 그부분 신경쓰며 할것
        // int curGunIndex = curPlayerData.gunID;
        // int curLV = curPlayerData.gunLV;
        List<float> returnList = new();
        // string curLVGunTextData = gunUpgradeData[curGunIndex][curLV - 1];
        // string[] columns = curLVGunTextData.Split(',');
        // foreach (var data in columns)
        // {
        //     if (float.TryParse(data, out var temp)) returnList.Add(temp);
        // }
        return returnList;
    }

    public void ReadStatusUpgradeData(string data)
    {
        string[] rows = data.Split('\n');
        List<PlayerData> newData = new();
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');
            PlayerData curStatusList = new();
            for (int j = 1; j < columns.Length; j++)
            {
                if (int.TryParse(columns[j], out var temp)) curStatusList.data.Add(temp);
            }

            newData.Add(curStatusList);
        }
        upgradeData = newData;
    }
    public void ReadGunUpgradeData(string data)
    {
        string[] rows = data.Split('\n');
        List<GunDataList> newData = new() { };
        GunDataList curGunLVData = new();
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');

            if (columns[0] != string.Empty || columns.Length <= 1)
            {
                if (i != 1)
                {
                    newData.Add(curGunLVData);
                    curGunLVData = new();
                }
                continue;
            }
            if (columns[1] == string.Empty) continue;
            GunData curGunData = new()
            {
                curLV = int.Parse(columns[1]),
                damage = float.Parse(columns[2]),
                bulletDelay = float.Parse(columns[3]),
                rerodeDelay = float.Parse(columns[4]),
                magazine = int.Parse(columns[5]),
                maxAmmo = int.Parse(columns[6]),
                dirRanMin = float.Parse(columns[7]),
                dirRanMax = float.Parse(columns[8])
            };
            curGunLVData.LV.Add(curGunData);
        }
        gunUpgradeData = newData;
    }
}

[Serializable]
public class PlayerData
{
    public List<int> data = new();
}

[Serializable]
public class GunDataList
{
    public List<GunData> LV = new();
}

[Serializable]
public class GunData
{
    public int curLV;

    public float damage;
    public float bulletDelay;
    public float rerodeDelay;
    public int magazine;
    public int maxAmmo;
    public float dirRanMin;
    public float dirRanMax;
}
