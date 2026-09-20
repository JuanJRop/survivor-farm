using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    public sealed class PortfolioSaveEntry
    {
        public string Slot,Label;public DateTime Updated;
    }
    /// <summary>Read-only catalogue of this demo's existing slots. Loading never invents a filename.</summary>
    public static class PortfolioSaveCatalog
    {
        private const string Prefix="survivor_farm_save_portfolio_";
        [Serializable] private sealed class Header {public SliceSnapshot portfolio;}
        public static List<PortfolioSaveEntry> List(string folder)
        {
            var result=new List<PortfolioSaveEntry>();if(!Directory.Exists(folder))return result;
            foreach(var file in Directory.EnumerateFiles(folder,Prefix+"*.json").Take(300))
            {
                try
                {
                    var info=new FileInfo(file);if(info.Length>4000000)continue;
                    var json=File.ReadAllText(file);if(!GameSaveSystem.ValidateSaveJson(json,out _))continue;
                    var header=JsonUtility.FromJson<Header>(json);if(header?.portfolio==null)continue;
                    var slot=info.Name.Substring(Prefix.Length,info.Name.Length-Prefix.Length-5);
                    result.Add(new PortfolioSaveEntry{Slot=slot,Updated=info.LastWriteTime,
                        Label=$"Día {header.portfolio.day} · {info.LastWriteTime:dd/MM HH:mm}"+(header.portfolio.completed?" · completada":"")});
                }
                catch(IOException) { }
                catch(UnauthorizedAccessException) { }
            }
            return result.OrderByDescending(x=>x.Updated).ToList();
        }
        public static bool Load(PortfolioSession session,string slot)
        {
            if(session==null||session.HasBegun||string.IsNullOrEmpty(slot)||slot.IndexOfAny(Path.GetInvalidFileNameChars())>=0||slot.Contains("/")||slot.Contains("\\"))return false;
            var save=UnityEngine.Object.FindFirstObjectByType<GameSaveSystem>();
            if(save==null||!List(Path.GetDirectoryName(save.SaveFile)).Any(e=>e.Slot==slot))return false;
            if(GameSaveSystem.IsQa)session.SelectQaSave(slot);
            else {PlayerPrefs.SetString("SurvivorFarmPortfolioSlot",slot);PlayerPrefs.Save();}
            return session.ContinueGame();
        }
    }
}
