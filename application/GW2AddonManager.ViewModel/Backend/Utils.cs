using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Threading;
using System.Windows;

namespace GW2AddonManager
{
    public delegate void UpdateMessageChangedEventHandler(object sender, string message);
    public delegate void UpdateProgressChangedEventHandler(object sender, int pct);

    public interface IUpdateChangedEvents
    {
        public event UpdateMessageChangedEventHandler MessageChanged;
        public event UpdateProgressChangedEventHandler ProgressChanged;
    }

    public abstract class UpdateChangedEvents : IUpdateChangedEvents, IProgress<float>
    {
        public event UpdateMessageChangedEventHandler MessageChanged;
        public event UpdateProgressChangedEventHandler ProgressChanged;

        protected void OnProgressChanged(int i, int n)
        {
            ProgressChanged?.Invoke(this, i * 100 / n);
        }

        protected void OnMessageChanged(string msg)
        {
            MessageChanged?.Invoke(this, msg);
        }

        public void Report(float value) => OnProgressChanged((int)(value * 100), 100);
    }

    public static class Constants
    {
        public const string AddonFolder = "resources\\addons";
    }

    public interface IHyperlinkHandler
    {

    }

    public static class Utils
    {
        public static void RemoveFiles(IFileSystem fs, IEnumerable<string> files, string basePath = "")
        {
            foreach (var f in files) {
                var fp = fs.Path.Combine(basePath, f);
                if (fs.File.Exists(fp))
                    fs.File.Delete(fp);
            }
        }

        public static void DeleteIfExists(this IFile f, string path)
        {
            if(f.Exists(path))
                f.Delete(path);
        }

        public static bool IsDirectory(this ZipArchiveEntry entry)
        {
            return (entry.FullName[^1] == Path.DirectorySeparatorChar || entry.FullName[^1] == Path.AltDirectorySeparatorChar) && string.IsNullOrEmpty(entry.Name);
        }

        public static List<string> ExtractArchiveWithFilesList(string archiveFilePath, string destFolder, IFileSystem fs)
        {
            using Stream source = fs.File.OpenRead(archiveFilePath);
            return ExtractArchiveWithFilesList(source, destFolder, fs);
        }

        public static List<string> ExtractArchiveWithFilesList(Stream source, string destFolder, IFileSystem fs)
        {
            destFolder = fs.Path.GetFullPath(destFolder);

            using ZipArchive archive = new ZipArchive(source);
            List<string> paths = new List<string>(archive.Entries.Count);

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!entry.IsDirectory())
                {
                    paths.Add(entry.FullName);
                }
            }
            
            string destFolderName = fs.Path.GetFileName(destFolder);
            if (paths.All(path =>
                path.StartsWith($"{destFolderName}{fs.Path.DirectorySeparatorChar}") ||
                path.StartsWith($"{destFolderName}{fs.Path.AltDirectorySeparatorChar}")))
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    paths[i] = paths[i][(destFolderName.Length + 1)..];
                }

                destFolder = fs.Path.GetDirectoryName(destFolder);
            }

            archive.ExtractToDirectory(destFolder);

            return paths;
        }

        public static string GetRelativePath(this IPath _, string relativeTo, string path)
        {
            return Path.GetRelativePath(relativeTo, path);
        }
    }

    //public class ArrayMultiValueConverter : IMultiValueConverter
    //{
    //    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return values.Clone();
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    //    {
    //        return (object[])value;
    //    }
    //}
}
