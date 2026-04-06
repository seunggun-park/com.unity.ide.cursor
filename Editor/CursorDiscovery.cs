using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.CodeEditor;

namespace CursorEditor
{
    public interface IDiscovery
    {
        CodeEditor.Installation[] PathCallback();
    }

    public class CursorDiscovery : IDiscovery
    {
        List<CodeEditor.Installation> m_Installations;

        public CodeEditor.Installation[] PathCallback()
        {
            if (m_Installations == null)
            {
                m_Installations = new List<CodeEditor.Installation>();
                FindInstallationPaths();
            }

            return m_Installations.ToArray();
        }

        void FindInstallationPaths()
        {
            string[] possiblePaths =
#if UNITY_EDITOR_OSX
            {
                "/Applications/Cursor.app"
            };
#elif UNITY_EDITOR_WIN
            {
                GetProgramFiles() + @"/cursor/Cursor.exe",
                GetLocalAppData() + @"/Programs/cursor/Cursor.exe",
            };
#else
            {
                "/usr/bin/cursor",
                "/bin/cursor",
                "/usr/local/bin/cursor"
            };
#endif
            var existingPaths = possiblePaths.Where(CursorExists).ToList();
            if (!existingPaths.Any())
            {
                return;
            }

            var lcp = GetLongestCommonPrefix(existingPaths);
            switch (existingPaths.Count)
            {
                case 1:
                {
                    var path = existingPaths.First();
                    m_Installations = new List<CodeEditor.Installation>
                    {
                        new CodeEditor.Installation
                        {
                            Path = path,
                            Name = "Cursor"
                        }
                    };
                    break;
                }
                case 2 when existingPaths.Any(path => !(path.Substring(lcp.Length).Contains("/") || path.Substring(lcp.Length).Contains("\\"))):
                {
                    goto case 1;
                }
                default:
                {
                    m_Installations = existingPaths.Select(path => new CodeEditor.Installation
                    {
                        Name = $"Cursor ({path.Substring(lcp.Length)})",
                        Path = path
                    }).ToList();

                    break;
                }
            }
        }

#if UNITY_EDITOR_WIN
        static string GetProgramFiles()
        {
            return Environment.GetEnvironmentVariable("ProgramFiles")?.Replace("\\", "/");
        }

        static string GetLocalAppData()
        {
            return Environment.GetEnvironmentVariable("LOCALAPPDATA")?.Replace("\\", "/");
        }
#endif

        static string GetLongestCommonPrefix(List<string> paths)
        {
            var baseLength = paths.First().Length;
            for (var pathIndex = 1; pathIndex < paths.Count; pathIndex++)
            {
                baseLength = Math.Min(baseLength, paths[pathIndex].Length);
                for (var i = 0; i < baseLength; i++)
                {
                    if (paths[pathIndex][i] == paths[0][i]) continue;

                    baseLength = i;
                    break;
                }
            }

            return paths[0].Substring(0, baseLength);
        }

        static bool CursorExists(string path)
        {
#if UNITY_EDITOR_OSX
            return System.IO.Directory.Exists(path);
#else
            return new FileInfo(path).Exists;
#endif
        }
    }
}
