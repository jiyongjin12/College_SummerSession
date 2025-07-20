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
        int curGunIndex = curPlayerData.gunID;
        int curLV = curPlayerData.gunLV;
        List<float> returnList = new();
        string curLVGunTextData = gunUpgradeData[curGunIndex].LV[curLV - 1];
        string[] columns = curLVGunTextData.Split(',');
        Debug.Log(curLVGunTextData);
        for (int i = 2; i < columns.Length; i++)
        {
            if (float.TryParse(columns[i], out var temp)) returnList.Add(temp);
        }
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
            curGunLVData.LV.Add(rows[i]);
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
    public List<string> LV = new();
}
