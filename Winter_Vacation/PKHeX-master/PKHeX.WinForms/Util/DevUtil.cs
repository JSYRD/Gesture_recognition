﻿#if DEBUG
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHeX.WinForms
{
    public static class DevUtil
    {
        public static void AddControl(ToolStripDropDownItem t)
        {
            t.DropDownItems.Add(GetTranslationUpdater());
        }

        private static readonly string[] Languages = {"ja", "fr", "it", "de", "es", "ko", "zh"};
        private const string DefaultLanguage = GameLanguage.DefaultLanguage;

        public static bool IsUpdatingTranslations { get; private set; }

        /// <summary>
        /// Call this to update all translatable resources (Program Messages, Legality Text, Program GUI)
        /// </summary>
        private static void UpdateAll()
        {
            if (DialogResult.Yes != WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Update translation files with current values?"))
                return;
            IsUpdatingTranslations = true;
            DumpStringsLegality();
            DumpStringsMessage();
            UpdateTranslations();
            IsUpdatingTranslations = false;
        }

        private static ToolStripMenuItem GetTranslationUpdater()
        {
            var ti = new ToolStripMenuItem
            {
                ShortcutKeys = Keys.Control | Keys.Alt | Keys.D,
                Visible = false,
            };
            ti.Click += (s, e) => UpdateAll();
            return ti;
        }

        private static void UpdateTranslations()
        {
            // add mode
            WinFormsTranslator.SetRemovalMode(false);
            WinFormsTranslator.LoadSettings<PKHeXSettings>(DefaultLanguage);
            WinFormsTranslator.LoadAllForms(LoadBanlist); // populate with every possible control
            WinFormsTranslator.UpdateAll(DefaultLanguage, Languages); // propagate to others
            WinFormsTranslator.DumpAll(Banlist); // dump current to file

            // de-populate
            WinFormsTranslator.SetRemovalMode(); // remove used keys, don't add any
            WinFormsTranslator.LoadSettings<PKHeXSettings>(DefaultLanguage, false);
            WinFormsTranslator.LoadAllForms(LoadBanlist);
            WinFormsTranslator.RemoveAll(DefaultLanguage, PurgeBanlist); // remove all lines from above generated files that still remain

            // Move translated files from the debug exe loc to their project location
            var files = Directory.GetFiles(Application.StartupPath);
            var dir = GetResourcePath("PKHeX.WinForms", "Resources", "text");
            foreach (var f in files)
            {
                var fn = Path.GetFileName(f);
                if (!fn.EndsWith(".txt"))
                    continue;
                if (!fn.StartsWith("lang_"))
                    continue;

                var loc = Path.Combine(dir, fn);
                if (File.Exists(loc))
                    File.Delete(loc);
                File.Move(f, loc);
                // if net framework support is ever removed, use the new overload instead of the stuff above:
                // File.Move(f, loc, true);
            }

            Application.Exit();
        }

        private static readonly string[] LoadBanlist =
        {
            nameof(SplashScreen),
        };

        private static readonly string[] Banlist =
        {
            nameof(SplashScreen),
            "Gender=", // editor gender labels
            "BTN_Shinytize", // ☆
            "Main.L_SizeH", // height rating
            "Main.L_SizeW", // weight rating
            "Main.B_Box", // << and >> arrows
            "Main.L_Characteristic=", // Characterstic (dynamic)
            "Main.L_Potential", // ★☆☆☆ IV judge evaluation
            "SAV_HoneyTree.L_Tree0", // dynamic, don't bother
            "SAV_Misc3.BTN_Symbol", // symbols should stay as their current character
            "SAV_GameSelect.L_Prompt", // prompt text (dynamic)
            "SAV_BlockDump8.L_BlockName", // Block name (dynamic)
        };

        private static readonly string[] PurgeBanlist =
        {
            nameof(SuperTrainingEditor),
            nameof(ErrorWindow),
            nameof(SettingsEditor),
        };

        private static void DumpStringsMessage() => DumpStrings(typeof(MessageStrings), false, "PKHeX.Core", "Resources", "text", "program");
        private static void DumpStringsLegality() => DumpStrings(typeof(LegalityCheckStrings), true, "PKHeX.Core", "Resources", "legality", "checks");

        private static void DumpStrings(Type t, bool sorted, params string[] rel)
        {
            var dir = GetResourcePath(rel);
            var langs = new[] {DefaultLanguage}.Concat(Languages);
            foreach (var lang in langs)
            {
                LocalizationUtil.SetLocalization(t, lang);
                var entries = LocalizationUtil.GetLocalization(t);
                var export = entries.Select(z => new {Variable = z.Split('=')[0], Line = z})
                    .OrderBy(z => z.Variable) // sort by length (V1 = 2, V100 = 4)
                    .Select(z => z.Line); // sorted lines

                if (!sorted)
                    export = entries;

                var location = GetFileLocationInText(t.Name, dir, lang);
                File.WriteAllLines(location, export);
                LocalizationUtil.SetLocalization(t, DefaultLanguage);
            }
        }

        private static string GetFileLocationInText(string fileType, string dir, string lang)
        {
            var fn = $"{fileType}_{lang}.txt";
            return Path.Combine(dir, fn);
        }

        private static string GetResourcePath(params string[] subdir)
        {
            var path = Application.StartupPath;
            const string projname = "PKHeX\\";
            var pos = path.LastIndexOf(projname, StringComparison.Ordinal);
            var str = path[..(pos + projname.Length)];
            var coreFolder = Path.Combine(str, Path.Combine(subdir));

            return coreFolder;
        }
    }
}
#endif
