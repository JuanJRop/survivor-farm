using System;
using System.Collections.Generic;
using System.IO;
using SurvivorFarm.Runtime.Core;

public static class PersistenceFileChecks
{
    public sealed class Document
    {
        public int version;
        public Position playerPosition;
        public Inventory inventory;
        public Survival survival;
        public List<Position> plots;
    }
    public sealed class Position { public float x, y, z; }
    public sealed class Inventory { public int wood, stone, coins; }
    public sealed class Survival { public int health; public float hungerPercent; }

    private const string First = "{\"version\":20,\"playerPosition\":{\"x\":1,\"y\":2,\"z\":0},\"inventory\":{\"wood\":10,\"stone\":2,\"coins\":7},\"survival\":{\"health\":5,\"hungerPercent\":1},\"day\":1,\"hour\":8,\"toolUpgrades\":{},\"crafting\":{},\"tutorialQuest\":{},\"plots\":[],\"resources\":[],\"resourceSpawns\":[],\"unlockZones\":[],\"dungeonChests\":[],\"groundLoot\":[],\"house\":null,\"adventure\":null,\"valley\":null,\"buildings\":null}";
    private static readonly string Second = First.Replace("\"coins\":7", "\"coins\":8");

    private static bool Validate(string json, out string error) => SaveJsonValidation.TryValidate(json, typeof(Document), out error);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }

    public static string[] Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var passed = new List<string>();
        string path = Path.Combine(directory, "normal.json");
        var store = new SaveFileStore(path, Validate);
        Check(store.TryLoad(out _) == SaveLoadSource.Missing && !store.IsWriteBlocked, "Missing file should allow saving");
        passed.Add("Missing slot");
        Check(store.TryWrite(First), store.LastError);
        Check(store.TryWrite(Second), store.LastError);
        Check(File.ReadAllText(path) == Second && File.ReadAllText(path + ".bak") == First && !File.Exists(path + ".tmp"), "Backup rotation");
        passed.Add("Durable write and repeated atomic replacement");

        File.WriteAllText(path, "{broken");
        store = new SaveFileStore(path, Validate);
        Check(store.TryLoad(out string recovered) == SaveLoadSource.Backup && recovered == First, "Backup recovery");
        Check(File.ReadAllText(path) == "{broken", "Loading must leave evidence untouched");
        Check(store.TryWrite(Second), store.LastError);
        Check(File.ReadAllText(path + ".bak") == First, "Recovered backup must survive the first save");
        Check(Directory.GetFiles(directory, "normal.json.rejected-*").Length == 1, "Damaged primary must be retained");
        passed.Add("Corrupt primary recovery and healthy backup retention");

        path = Path.Combine(directory, "failed.json");
        File.WriteAllText(path, "bad primary");
        File.WriteAllText(path + ".bak", "bad backup");
        File.WriteAllText(path + ".tmp", "bad temp");
        store = new SaveFileStore(path, Validate);
        Check(store.TryLoad(out _) == SaveLoadSource.Failed && store.IsWriteBlocked, "Total failure must block saves");
        Check(!store.TryWrite(First) && File.ReadAllText(path) == "bad primary", "Autosave after failed load");
        File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp");
        Check(store.TryLoad(out _) == SaveLoadSource.Failed && !store.TryWrite(First), "Deleting evidence must not unblock initial state");
        File.WriteAllText(path + ".bak", First);
        Check(store.TryLoad(out _) == SaveLoadSource.Backup && !store.IsWriteBlocked && store.TryWrite(Second), "Explicit successful retry");
        passed.Add("Total failure, persistent write lock, successful retry");

        path = Path.Combine(directory, "temp.json");
        File.WriteAllText(path + ".tmp", First);
        store = new SaveFileStore(path, Validate);
        Check(store.TryLoad(out recovered) == SaveLoadSource.Temporary && recovered == First, "Temporary recovery");
        Check(store.TryWrite(Second) && File.ReadAllText(path + ".bak") == First, "Recovered temporary must survive reuse");
        passed.Add("Interrupted first save recovered from temporary");

        path = Path.Combine(directory, "precedence.json");
        File.WriteAllText(path, First); File.WriteAllText(path + ".tmp", Second);
        store = new SaveFileStore(path, Validate);
        Check(store.TryLoad(out recovered) == SaveLoadSource.Primary && recovered == First, "Committed primary takes precedence");
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(!store.TryWrite(Second), "Locked primary must fail safely");
        Check(File.ReadAllText(path) == First && store.TryWrite(Second), "Retry after file lock");
        passed.Add("Committed precedence and controlled IO lock/retry");

        path = Path.Combine(directory, "blocked-temp.json");
        store = new SaveFileStore(path, Validate);
        Check(store.TryWrite(First), store.LastError);
        Directory.CreateDirectory(path + ".tmp");
        Check(!store.TryWrite(Second) && File.ReadAllText(path) == First, "Temporary path error must preserve primary");
        passed.Add("Temporary path failure preserves previous save");

        path = Path.Combine(directory, "encoding.json");
        File.WriteAllBytes(path, new byte[] { 0xc3, 0x28 });
        File.WriteAllText(path + ".bak", First);
        store = new SaveFileStore(path, Validate);
        Check(store.TryLoad(out recovered) == SaveLoadSource.Backup && recovered == First, "Invalid UTF8 recovery");
        Check(store.TryWrite(Second) && File.ReadAllText(path + ".bak") == First, "Save after invalid UTF8 recovery");
        Check(File.ReadAllBytes(Directory.GetFiles(directory, "encoding.json.rejected-*")[0])[0] == 0xc3, "Invalid bytes must be preserved");
        passed.Add("Invalid UTF8 recovery preserves original bytes");

        path = Path.Combine(directory, "oversize.json");
        using (var oversized = new FileStream(path, FileMode.Create, FileAccess.Write)) oversized.SetLength(SaveFileStore.MaximumBytes + 1L);
        File.WriteAllText(path + ".bak", First);
        store = new SaveFileStore(path, Validate);
        Check(store.TryLoad(out recovered) == SaveLoadSource.Backup && recovered == First, "Oversized primary recovery");
        Check(store.TryWrite(Second) && File.ReadAllText(path + ".bak") == First, "Save after oversized primary recovery");
        Check(new FileInfo(Directory.GetFiles(directory, "oversize.json.rejected-*")[0]).Length == SaveFileStore.MaximumBytes + 1L, "Oversized evidence retained");
        passed.Add("Oversized file recovery and evidence retention");

        var invalid = new[] { "", "{}", "null", "[]", First.Substring(0, First.Length - 1), First + "garbage", First + First,
            First.Replace("\"coins\":7", "\"coins\":2147483648"), First.Replace("\"x\":1", "\"x\":1e999"),
            First.Replace("\"x\":1", "\"x\":NaN"), First.Replace("\"x\":1", "\"x\":\"oops\""),
            First.Replace("\"version\":20", "\"version\":20,\"version\":20"),
            First.Replace("\"plots\":[]", "\"plots\":[null]"),
            First.Replace("\"coins\":7", "\"coins\":7,\"coins\":8"), First.Replace("\"health\":5,", ""),
            First.Replace("\"plots\":[],", ""), First.Replace("\"plots\":[]", "\"plots\":{}") };
        foreach (string json in invalid) Check(!Validate(json, out _), "Accepted invalid JSON: " + json);
        for (int length = 0; length < First.Length; length++) Check(!Validate(First.Substring(0, length), out _), "Accepted truncation at " + length);
        for (int version = 5; version <= 20; version++) Check(Validate(First.Replace("\"version\":20", "\"version\":" + version), out _), "Legacy structural validation " + version);
        passed.Add("Structural validation: 17 invalid inputs, every truncation offset, versions 5..20");

        path = Path.Combine(directory, "separate-slot.json");
        Check(new SaveFileStore(path, Validate).TryWrite(First), "An isolated new slot should remain writable");
        passed.Add("Slot isolation");
        return passed.ToArray();
    }
}
