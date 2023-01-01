using System.Collection.Generic;
using System.IO;
using MelonLoader;
using UnityEngine;

namespace NeonWhiteQoL
{
  internal class SettingsHandler
  {
  public static int[] storedConfig = new int[12];
  
    public static int[] ReadConfig()
    {
      List<int> settings = new List<int>();
      
      string path = string.Concat(MelonHandler.ModsDirectory, "/RMMBY/Mods/NeonLite/config.ini")
      
      if (!File.Exists(path))
      {
        File.Create(path);
        CreateConfig(12);
      }
      
      StreamReader r = new StreamReader(path);
      
      int j = 0;
      string line;
      using(r)
      {
        do
        {
          line = r.ReadLine();
          if(line != null) settings.Add(int.Parse(line));
          
          j++;
        }
        while (line != null);
        
        r.Close();
        
        if (j != 12)
        {
          CreateConfig(12);
          return new int[12];
        }
        
        return settings.ToArray();
      }
    }
    
    public static void CreateConfig(int lineCount)
    {
      string path = string.Concat(MelonHandler.ModsDirectory, "/RMMBY/Mods/NeonLite/config.ini")
      StreamWriter sw = new StreamWriter(path, false);
        
      for (int i = 0; i < lineCount; i++)
      {
        sw.WriteLine("0");
      }
    }
    
    public static void CheckUpdateConfig()
    {
      int[] newConfig = ReadConfig();
      
      for (int i = 0; i < 12; i++)
      {
        if (storedConfig[i] != newConfig[i]
        {
          SetNewSetting(i, newConfig[i]);
        }
      }
    }
    
    public static void GetFirstSettings()
    {
      int[] newConfig = ReadConfig();
      
      for (int i = 0; i < 12; i++)
      {
        SetNewSetting(i, newConfig[i];
      }
    }
    
    public static void SetNewSetting(int settingID, int value)
    {
      switch (settingID)
      {
        case 0:
          BegoneApocalypse.ToggleMod(value);
          break;
        case 1:
          GreenHP.ToggleMod(value);
          break;
        case 2:
          BossfightGhost.ToggleMod(value);
          break;
        case 3:
          GameObject.FindObjectOfType<CheaterBanlist>().ToggleMod(value);
          break;
        case 4:
          CommunityMedals.ToggleMod(value);
          break;
        case 5:
          LeaderboardFix.ToggleMod(value);
          break;
        case 6:
          IGTimer.ToggleMod(value);
          break;
        case 7:
          PBTracker.ToggleMod(value);
          break;
        case 8:
          RemoveMission.ToggleMod(value);
          break;
        case 9:
          if (value == 0) NeonLite.useSessionTimer = true;
          else NeonLite.useSessionTimer = false;
          break;
        case 10:
          ShowcaseBypass.ToggleMod(value);
          break;
        case 11:
          //What kind of psycho would turn this off?
          SkipIntro.ToggleMod(value);
          break;
      }
    }
  }
}
