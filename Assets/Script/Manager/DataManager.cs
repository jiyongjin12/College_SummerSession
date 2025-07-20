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

    // public List<float> GetCurGunLVData() //int curGunIndex, int curLV
    // {
    //     int curGunIndex = curPlayerData.gunID;
    //     int curLV = curPlayerData.gunLV;
    //     List<float> returnList = new();
    //     string curLVGunTextData = gunUpgradeData[curGunIndex].LV[curLV - 1];
    //     string[] columns = curLVGunTextData.Split(',');
    //     Debug.Log(curLVGunTextData);
    //     for (int i = 2; i < columns.Length; i++)
    //     {
    //         if (float.TryParse(columns[i], out var temp)) returnList.Add(temp);
    //     }
    //     return returnList;
    // }

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
                if (int.TryParse(columns[j], out var temp)) curStatusList.LV.Add(temp);
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
                    //나중에 저장데이터 가져올때 초기화된 GunLV 리스트에 레벨저장값 넣어두기
                    curGunLVData = new();
                }
                continue;
            }
            if (columns[1] == string.Empty) continue;
            GunData curGunData = new();
            for (int j = 2; j < columns.Length; j++)
            {
                curGunData.status.Add(float.Parse(columns[j]));
            }
            curGunLVData.LV.Add(curGunData);
        }
        gunUpgradeData = newData;
    }
}

[Serializable]
public class PlayerData
{
    public List<int> LV = new();
}

[Serializable]
public class GunDataList
{
    public List<GunData> LV = new();
}

[Serializable]
public class GunData
{
    public List<float> status = new();
}