﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AtomSearch
{
    public static class AppHelper
    {
        #region Fields

        private static string AppIndexLocation = "C:/ProgramData/Microsoft/Windows/Start Menu/Programs";

        private static List<Result> InstalledApps = new List<Result>();

        private static bool cached = false;

        #endregion Fields

        #region Methods

        public static void CacheInstalledPrograms(string appIndexLocation = null)
        {
            if (appIndexLocation != null && AppIndexLocation != appIndexLocation)
            {
                AppIndexLocation = appIndexLocation;
                InstalledApps = new List<Result>();
                cached = false;
            }

            if (!cached)
            {
                foreach (string shortcutPath in Directory.GetFiles(AppIndexLocation, "*.*", SearchOption.AllDirectories))
                {
                    if (ShortcutHelper.IsAccessibleLink(shortcutPath, out var target))
                        InstalledApps.Add(new Result(Path.GetFileNameWithoutExtension(shortcutPath), Icon.ExtractAssociatedIcon(target), target, target));
                }
                cached = true;
            }
        }

        public static IEnumerable<Result> GetResults(string provided)
        {
            foreach (var installed in InstalledApps)
            {
                var sDiff = StringDifferenceHelper.LevenshteinDistance(installed.DisplayText, provided);
                installed.MatchRankStringDifference = (double)4000 - (double)sDiff / (double)installed.DisplayText.Length * (double)4000;
            }

            return InstalledApps.OrderByDescending(x => x.MatchRank);
        }

        //TEMPORARY
        public static IEnumerable<Result> GetApps()
            => InstalledApps;

        #endregion Methods
    }
}
