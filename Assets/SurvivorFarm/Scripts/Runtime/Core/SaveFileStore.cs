using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;

namespace SurvivorFarm.Runtime.Core
{
    public delegate bool SaveFileValidator(string json, out string error);

    public enum SaveLoadSource { Missing, Primary, Backup, Temporary, Failed }

    // Owns only one slot. Loading never modifies the on-disk recovery evidence.
    public sealed class SaveFileStore
    {
        public const int MaximumBytes = 16 * 1024 * 1024;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly SaveFileValidator validator;
        private bool inspected;
        private string recoveredJson;

        public string Path { get; }
        public string BackupPath => Path + ".bak";
        public string TemporaryPath => Path + ".tmp";
        public bool IsWriteBlocked { get; private set; }
        public string LastError { get; private set; }

        public SaveFileStore(string path, SaveFileValidator validator)
        {
            Path = System.IO.Path.GetFullPath(path);
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public void BlockWrites(string error)
        {
            IsWriteBlocked = true;
            LastError = error;
        }

        public SaveLoadSource TryLoad(out string json)
        {
            inspected = true;
            json = null;
            var errors = new List<string>();
            string[] candidates = { Path, BackupPath, TemporaryPath };
            for (int i = 0; i < candidates.Length; i++)
            {
                try
                {
                    if (!TryRead(candidates[i], out string candidate)) continue;
                    if (!validator(candidate, out string error))
                    {
                        errors.Add(System.IO.Path.GetFileName(candidates[i]) + ": " + error);
                        continue;
                    }
                    json = candidate;
                    recoveredJson = i == 0 ? null : candidate;
                    IsWriteBlocked = false;
                    LastError = errors.Count == 0 ? null : string.Join("; ", errors);
                    return i == 0 ? SaveLoadSource.Primary : i == 1 ? SaveLoadSource.Backup : SaveLoadSource.Temporary;
                }
                catch (Exception exception) when (IsFileError(exception))
                {
                    errors.Add(System.IO.Path.GetFileName(candidates[i]) + ": " + exception.Message);
                }
            }
            if (errors.Count == 0 && !IsWriteBlocked)
            {
                LastError = null;
                return SaveLoadSource.Missing;
            }
            BlockWrites(errors.Count == 0 ? LastError : string.Join("; ", errors));
            return SaveLoadSource.Failed;
        }

        public bool TryWrite(string json)
        {
            if (IsWriteBlocked) return false;
            if (!inspected) TryLoad(out _);
            if (IsWriteBlocked) return false;
            try
            {
                if (!validator(json, out string error))
                {
                    LastError = "Invalid save snapshot: " + error;
                    return false;
                }
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
                bool primaryExists = TryInspect(Path, out _, out bool primaryValid);

                // A recovered temporary must survive reuse of .tmp. Do not rotate a
                // damaged primary over the only healthy backup on the next autosave.
                if (recoveredJson != null && !primaryValid)
                    PreserveRecoveryBackup();
                else if (primaryExists && !primaryValid)
                {
                    BlockWrites("The primary save changed or became invalid. Reload before saving.");
                    return false;
                }

                WriteDurable(TemporaryPath, json);
                if (primaryExists)
                    File.Replace(TemporaryPath, Path, primaryValid ? BackupPath : RejectedPath(Path));
                else
                    File.Move(TemporaryPath, Path);

                recoveredJson = null;
                LastError = null;
                return true;
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                LastError = exception.Message;
                return false;
            }
        }

        private void PreserveRecoveryBackup()
        {
            bool exists = TryInspect(BackupPath, out string backup, out _);
            if (exists && backup == recoveredJson) return;
            string temporary = BackupPath + ".tmp";
            WriteDurable(temporary, recoveredJson);
            if (exists) File.Replace(temporary, BackupPath, RejectedPath(BackupPath));
            else File.Move(temporary, BackupPath);
        }

        private static string RejectedPath(string path) => path + ".rejected-" + Guid.NewGuid().ToString("N");

        private bool TryInspect(string path, out string json, out bool valid)
        {
            valid = false;
            try
            {
                bool exists = TryRead(path, out json);
                valid = exists && validator(json, out _);
                return exists;
            }
            catch (Exception exception) when (exception is InvalidDataException || exception is DecoderFallbackException)
            {
                // Bad bytes/size are corrupt content, not permission or sharing errors.
                // File.Replace can preserve their exact bytes in a rejected copy.
                json = null;
                return true;
            }
        }

        private static bool TryRead(string path, out string json)
        {
            json = null;
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > MaximumBytes) throw new InvalidDataException("Save exceeds the size limit.");
                    using (var reader = new StreamReader(stream, Utf8, true)) json = reader.ReadToEnd();
                    return true;
                }
            }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }

        private static void WriteDurable(string path, string json)
        {
            byte[] bytes = Utf8.GetBytes(json);
            if (bytes.Length > MaximumBytes) throw new IOException("Save exceeds the size limit.");
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static bool IsFileError(Exception exception) => exception is IOException || exception is InvalidDataException ||
            exception is UnauthorizedAccessException || exception is SecurityException ||
            exception is ArgumentException || exception is NotSupportedException;
    }
}
