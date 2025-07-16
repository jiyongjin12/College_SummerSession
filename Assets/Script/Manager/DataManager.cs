using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class DataManager : MonoBehaviour
{
    public static DataManager instance { get; private set; }

    public StatusUpgradeData curPlayerData;
    public UpgradeData upgradeData;
    public List<List<string>> gunUpgradeData;
    public List<string> testData;

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
        string curLVGunTextData = gunUpgradeData[curGunIndex][curLV - 1];
        string[] columns = curLVGunTextData.Split(',');
        foreach (var data in columns)
        {
            if (float.TryParse(data, out var temp)) returnList.Add(temp);
        }
        return returnList;
    }

    public void ReadStatusUpgradeData(string data)
    {
        string[] rows = data.Split('\n');
        var newData = new UpgradeData();
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');
            List<int> curStatusList = new();
            for (int j = 1; j < columns.Length; j++)
            {
                if(int.TryParse(columns[j], out var temp)) curStatusList.Add(temp);
            }

            switch (i)
            {
                case 1: newData.hpLVList = curStatusList; break;
                case 2: newData.O2LVList = curStatusList; break;
                case 3: newData.speedLVList = curStatusList; break;
                case 4: newData.capacityLVList = curStatusList; break;
            }
        }
        upgradeData = newData;
    }
    public void ReadGunUpgradeData(string data)
    {
        string[] rows = data.Split('\n');
        List<List<string>> newData = new();
        List<string> curGunData = new();
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = rows[i].Split(',');

            if (columns[0] != string.Empty)
            {
                if (i != 1)
                {
                    newData.Add(curGunData);
                    curGunData = new();
                }
                continue;
            }
            curGunData.Add(rows[i]);
        }
        testData = curGunData;
        newData.Add(curGunData);
        gunUpgradeData = newData;
    }
}

[System.Serializable]
public class UpgradeData
{
    public List<int> hpLVList;
    public List<int> O2LVList;
    public List<int> speedLVList;
    public List<int> capacityLVList;
}
