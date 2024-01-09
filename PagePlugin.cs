using System;
using System.IO;
using System.Collections;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using UnityEngine;

using SimpleJSON;
using YamlDotNet.Serialization;
using DG.Tweening;
using LSFunctions;

using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

using PageCreator.Functions;
using PageCreator.Patchers;

namespace PageCreator
{
    [BepInPlugin("com.mecha.pagecreator", "Page Creator", "2.1.4")]
    public class PagePlugin : BaseUnityPlugin
    {
        // Updates:
        //-Added PageCreator
        //-

        public static PagePlugin inst;
        public static string className = "[<color=#0E36FD>PageCreator</color>] " + PluginInfo.PLUGIN_VERSION + "\n";
        readonly Harmony harmony = new Harmony("Pages");
        public static ConfigFile ModConfig
        {
            get
            {
                if (inst != null)
                    return inst.Config;

                return null;
            }
        }

        public static ConfigEntry<bool> PlayCustomMusic { get; set; }
        public static ConfigEntry<LoadMode> MusicLoadMode { get; set; }
        public static ConfigEntry<int> MusicIndex { get; set; }

        public static ConfigEntry<string> MusicGlobalPath { get; set; }

        public static ConfigEntry<KeyCode> ReloadMainMenu { get; set; }

        public static string prevScene = "Main Menu";
        public static string prevBranch;
        public static string prevInterface = "beatmaps/menus/menu.lsm";
        public static bool fromPageLevel = false;

        public enum LoadMode
        {
            Settings,
            StoryFolder,
            EditorFolder,
            GlobalFolder
        }

        public static bool prevPlayCustomMusic;
        public static LoadMode prevMusicLoadMode;
        public static int prevMusicIndex;

        public static int randomIndex = -1;

        void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo("Plugin Page Creator is loaded!");

            PlayCustomMusic = Config.Bind("Music", "Play Custom Music", true, "Allows you to load any number of songs from settings/menus.");
            MusicLoadMode = Config.Bind("Music", "Load Directory", LoadMode.Settings, "Where the music loads from. Settings path: Project Arrhythmia/settings/menus.");
            MusicIndex = Config.Bind("Music", "File Index", -1, "If number is less than 0 or higher than the song file count, it will play a random song. Otherwise it will use the specified index.");
            MusicGlobalPath = Config.Bind("Music", "Global Path", "C:/", "Set this path to whatever path you want if you're using Global Load Directory.");

            ReloadMainMenu = Config.Bind("Menu", "Reload Main Menu key", KeyCode.F5, "The key to reload the main menu for easy reloading of modified menu file.");

            prevPlayCustomMusic = PlayCustomMusic.Value;
            prevMusicLoadMode = MusicLoadMode.Value;
            prevMusicIndex = MusicIndex.Value;

            Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(UpdateSettings);

            harmony.PatchAll(typeof(PagePlugin));
            harmony.PatchAll(typeof(InterfaceControllerPatch));

            if (!ModCompatibility.mods.ContainsKey("PageCreator"))
            {
                var mod = new ModCompatibility.Mod(this, GetType());

                mod.Methods.Add("SetupPageEditor", (Action)SetupPageEditor);

                ModCompatibility.mods.Add("PageCreator", mod);
            }
        }

        void Update()
        {
            if (ArcadeManager.inst.ic != null && ArcadeManager.inst.ic.gameObject.scene.name == "Main Menu" && Input.GetKeyDown(ReloadMainMenu.Value))
            {
                SceneManager.inst.LoadScene("Main Menu");
            }
        }

        public static void SetupPageEditor()
        {
            PageEditor.Init();
        }

        static void UpdateSettings(object sender, EventArgs e)
        {
            if (EditorManager.inst == null && ArcadeManager.inst != null && ArcadeManager.inst.ic != null && (prevPlayCustomMusic != PlayCustomMusic.Value || prevMusicLoadMode != MusicLoadMode.Value || prevMusicIndex != MusicIndex.Value))
            {
                prevPlayCustomMusic = PlayCustomMusic.Value;
                prevMusicLoadMode = MusicLoadMode.Value;
                prevMusicIndex = MusicIndex.Value;

                PlayMusic(ArcadeManager.inst.ic);
            }
        }

        public static void PlayMusic(InterfaceController __instance)
        {
            var directory = RTFile.ApplicationDirectory + "settings/menus/";
            switch (MusicLoadMode.Value)
            {
                case LoadMode.StoryFolder:
                    {
                        directory = RTFile.ApplicationDirectory + "beatmaps/story";
                        break;
                    }
                case LoadMode.EditorFolder:
                    {
                        directory = RTFile.ApplicationDirectory + "beatmaps/editor";
                        break;
                    }
                case LoadMode.GlobalFolder:
                    {
                        directory = MusicGlobalPath.Value;
                        break;
                    }
            }

            if (PlayCustomMusic.Value && RTFile.DirectoryExists(directory))
            {
                string oggSearchPattern = "*.ogg";
                string wavSearchPattern = "*.wav";
                if (MusicLoadMode.Value == LoadMode.StoryFolder || MusicLoadMode.Value == LoadMode.EditorFolder)
                {
                    oggSearchPattern = "level.ogg";
                    wavSearchPattern = "level.wav";
                }

                var oggFiles = Directory.GetFiles(directory, oggSearchPattern, SearchOption.AllDirectories);
                var wavFiles = Directory.GetFiles(directory, wavSearchPattern, SearchOption.AllDirectories);

                var songFiles = new string[oggFiles.Length + wavFiles.Length];

                for (int i = 0; i < oggFiles.Length; i++)
                {
                    songFiles[i] = oggFiles[i];
                }
                for (int i = oggFiles.Length; i < songFiles.Length; i++)
                {
                    songFiles[i] = wavFiles[i - oggFiles.Length];
                }

                if (songFiles.Length > 0)
                {
                    songs = songFiles;

                    if (MusicIndex.Value >= 0 && MusicIndex.Value < songFiles.Length)
                    {
                        randomIndex = MusicIndex.Value;
                    }

                    if (MusicIndex.Value < 0 || MusicIndex.Value > songFiles.Length - 1)
                    {
                        if (randomIndex < 0 || randomIndex >= songFiles.Length)
                            randomIndex = UnityEngine.Random.Range(0, songFiles.Length - 1);
                    }

                    var songFileCurrent = songFiles[Mathf.Clamp(randomIndex, 0, songFiles.Length - 1)];
                    if (!string.IsNullOrEmpty(songFileCurrent))
                    {
                        __instance.StartCoroutine(FileManager.inst.LoadMusicFileRaw(songFileCurrent, false, delegate (AudioClip clip)
                        {
                            AudioManager.inst.PlayMusic(Path.GetFileName(songFileCurrent), clip);
                        }));
                    }
                    else
                    {
                        PlayDefaultMusic(__instance);
                    }
                }
                else
                {
                    PlayDefaultMusic(__instance);
                }
            }
            else
            {
                PlayDefaultMusic(__instance);
            }
        }

        public static string[] songs;

        public static void PlayDefaultMusic(InterfaceController __instance)
        {
            if (__instance.interfaceSettings.music == "menu")
            {
                AudioManager.inst.PlayMusic(DataManager.inst.GetSettingEnumValues("MenuMusic", 0), 0f);
                return;
            }
            if (__instance.interfaceSettings.music != null && __instance.interfaceSettings.music != "")
            {
                AudioManager.inst.PlayMusic(__instance.interfaceSettings.music, 0f);
            }
        }

        [HarmonyPatch(typeof(DataManager), "Start")]
        [HarmonyPostfix]
        static void DataStart()
        {
            if (RTFile.FileExists(RTFile.ApplicationDirectory + "settings/menu.lss"))
            {
                string rawProfileJSON = FileManager.inst.LoadJSONFile("settings/menu.lss");

                JSONNode jn = JSON.Parse(rawProfileJSON);
                string note = "JSON code based on what exists in the files";
                if (note.Contains("note"))
                {
                }

                jn["TransRights"][0]["name"] = "<sprite name=trans_pa_logo> Yes";
                jn["TransRights"][0]["values"] = "<sprite name=trans_pa_logo>";
                jn["TransRights"][1]["name"] = "<sprite name=pa_logo> No";
                jn["TransRights"][1]["values"] = "<sprite name=pa_logo>";

                jn["MenuMusic"][0]["name"] = "shuffle";
                jn["MenuMusic"][0]["values"] = "menu";
                jn["MenuMusic"][0]["function_call"] = "apply_menu_music";

                jn["MenuMusic"][1]["name"] = "barrels";
                jn["MenuMusic"][1]["values"] = "barrels";
                jn["MenuMusic"][1]["function_call"] = "apply_menu_music";

                jn["MenuMusic"][2]["name"] = "nostalgia";
                jn["MenuMusic"][2]["values"] = "nostalgia";
                jn["MenuMusic"][2]["function_call"] = "apply_menu_music";

                jn["MenuMusic"][3]["name"] = "arcade dream";
                jn["MenuMusic"][3]["values"] = "arcade_dream";
                jn["MenuMusic"][3]["function_call"] = "apply_menu_music";

                jn["MenuMusic"][4]["name"] = "distance";
                jn["MenuMusic"][4]["values"] = "distance";
                jn["MenuMusic"][4]["function_call"] = "apply_menu_music";


                jn["ArcadeDifficulty"][0]["name"] = "zen (invincible)";
                jn["ArcadeDifficulty"][0]["values"] = "zen";

                jn["ArcadeDifficulty"][1]["name"] = "normal";
                jn["ArcadeDifficulty"][1]["values"] = "normal";

                jn["ArcadeDifficulty"][2]["name"] = "1 life";
                jn["ArcadeDifficulty"][2]["values"] = "1_life";

                jn["ArcadeDifficulty"][3]["name"] = "1 hit";
                jn["ArcadeDifficulty"][3]["values"] = "1_hit";


                jn["ArcadeGameSpeed"][0]["name"] = "x0.5";
                jn["ArcadeGameSpeed"][0]["values"] = "0.5";

                jn["ArcadeGameSpeed"][1]["name"] = "x0.8";
                jn["ArcadeGameSpeed"][1]["values"] = "0.8";

                jn["ArcadeGameSpeed"][2]["name"] = "x1.0";
                jn["ArcadeGameSpeed"][2]["values"] = "1.0";

                jn["ArcadeGameSpeed"][3]["name"] = "x1.2";
                jn["ArcadeGameSpeed"][3]["values"] = "1.2";

                jn["ArcadeGameSpeed"][4]["name"] = "x1.5";
                jn["ArcadeGameSpeed"][4]["values"] = "1.5";


                jn["QualityLevel"][0]["name"] = "Low";
                jn["QualityLevel"][0]["values"] = "low";

                jn["QualityLevel"][1]["name"] = "Normal";
                jn["QualityLevel"][1]["values"] = "normal";


                jn["AntiAliasing"][0]["name"] = "None";
                jn["AntiAliasing"][0]["values"] = "0";

                jn["AntiAliasing"][1]["name"] = "x2";
                jn["AntiAliasing"][1]["values"] = "2";


                jn["SortingHuman"][0]["values"]["desc"] = "NEW";
                jn["SortingHuman"][0]["values"]["asc"] = "OLD";

                jn["SortingHuman"][0]["name"] = "date_downloaded";


                jn["SortingHuman"][1]["values"]["desc"] = "Z-A";
                jn["SortingHuman"][1]["values"]["asc"] = "A-Z";

                jn["SortingHuman"][1]["name"] = "song_name";


                jn["SortingHuman"][2]["values"]["desc"] = "Z-A";
                jn["SortingHuman"][2]["values"]["asc"] = "A-Z";

                jn["SortingHuman"][2]["name"] = "artist_name";


                jn["SortingHuman"][3]["values"]["desc"] = "Z-A";
                jn["SortingHuman"][3]["values"]["asc"] = "A-Z";

                jn["SortingHuman"][3]["name"] = "creator_name";


                jn["SortingHuman"][4]["values"]["desc"] = "HARD";
                jn["SortingHuman"][4]["values"]["asc"] = "EASY";

                jn["SortingHuman"][4]["name"] = "difficulty";

                DataManager.inst.interfaceSettings = jn;
            }
            else
            {
                JSONNode jn = JSON.Parse("{}");
                jn["UITheme"][0]["name"] = "Light";
                jn["UITheme"][0]["values"]["bg"] = "#E0E0E0";
                jn["UITheme"][0]["values"]["text"] = "#212121";
                jn["UITheme"][0]["values"]["highlight"] = "#424242";
                jn["UITheme"][0]["values"]["text-highlight"] = "#E0E0E0";
                jn["UITheme"][0]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][0]["function_call"] = "apply_ui_theme";

                jn["UITheme"][1]["name"] = "Dark";
                jn["UITheme"][1]["values"]["bg"] = "#212121";
                jn["UITheme"][1]["values"]["text"] = "#E0E0E0";
                jn["UITheme"][1]["values"]["highlight"] = "#E0E0E0";
                jn["UITheme"][1]["values"]["text-highlight"] = "#212121";
                jn["UITheme"][1]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][1]["function_call"] = "apply_ui_theme";

                jn["UITheme"][2]["name"] = "Alpha";
                jn["UITheme"][2]["values"]["bg"] = "#1E1E1E";
                jn["UITheme"][2]["values"]["text"] = "#ECECEC";
                jn["UITheme"][2]["values"]["highlight"] = "#F2762A";
                jn["UITheme"][2]["values"]["text-highlight"] = "#ECECEC";
                jn["UITheme"][2]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][2]["function_call"] = "apply_ui_theme";

                jn["UITheme"][3]["name"] = "Beta";
                jn["UITheme"][3]["values"]["bg"] = "#F2F2F2";
                jn["UITheme"][3]["values"]["text"] = "#333333";
                jn["UITheme"][3]["values"]["highlight"] = "#F05355";
                jn["UITheme"][3]["values"]["text-highlight"] = "#F2F2F2";
                jn["UITheme"][3]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][3]["function_call"] = "apply_ui_theme";

                jn["UITheme"][4]["name"] = "Neir";
                jn["UITheme"][4]["values"]["bg"] = "#D1CDB7";
                jn["UITheme"][4]["values"]["text"] = "#454138";
                jn["UITheme"][4]["values"]["highlight"] = "#454138";
                jn["UITheme"][4]["values"]["text-highlight"] = "#D1CDB7";
                jn["UITheme"][4]["values"]["buttonbg"] = "transparent";
                jn["UITheme"][4]["function_call"] = "apply_ui_theme";

                RTFile.WriteToFile("settings/menu.lss", jn.ToString(3));
            }
        }

        [HarmonyPatch(typeof(InterfaceLoader), "Start")]
        [HarmonyPrefix]
        static bool InterfaceLoaderPrefix(InterfaceLoader __instance)
        {
            string text = "";
            if (string.IsNullOrEmpty(__instance.file))
            {
                text = SaveManager.inst.CurrentStoryLevel.BeatmapJson.text;
                DiscordController.inst.OnDetailsChange("Playing Story");
                DiscordController.inst.OnStateChange("Level: " + SaveManager.inst.CurrentStoryLevel.SongName);
                DiscordController.inst.OnIconChange("arcade");
            }
            else
            {
                if (__instance.isYAML)
                {
                    string text2 = (Resources.Load("terminal/" + __instance.location + "/" + __instance.file) as TextAsset).text;
                    object graph = new DeserializerBuilder().Build().Deserialize(new StringReader(text2));
                    text = new SerializerBuilder().JsonCompatible().Build().Serialize(graph);
                    LSText.CopyToClipboard(text);
                }
                else if (__instance.gameObject.scene.name == "Main Menu" && (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main/menu.lsm") || RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main.lsm")))
                {
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/main.lsm");
                        prevInterface = "beatmaps/menus/main.lsm";
                    }
                    else if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/main/menu.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/main/menu.lsm");
                        prevInterface = "beatmaps/menus/main/menu.lsm";
                    }
                }
                else if (__instance.gameObject.scene.name == "Game" && (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause/menu.lsm") || RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause.lsm")))
                {
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/pause.lsm");
                        prevInterface = "beatmaps/menus/pause.lsm";
                    }
                    else if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/pause/menu.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/pause/menu.lsm");
                        prevInterface = "beatmaps/menus/pause/menu.lsm";
                    }
                }
                else if (__instance.gameObject.scene.name == "Interface" && RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/story_mode.lsm"))
                {
                    if (RTFile.FileExists(RTFile.ApplicationDirectory + "beatmaps/menus/story_mode.lsm"))
                    {
                        text = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + "beatmaps/menus/story_mode.lsm");
                        prevInterface = "beatmaps/menus/story_mode.lsm";
                    }
                }
                else
                {
                    text = (Resources.Load("terminal/" + __instance.location + "/" + __instance.file) as TextAsset).text;
                }

                DiscordController.inst.OnDetailsChange("In Menu");
                DiscordController.inst.OnStateChange("");
                DiscordController.inst.OnIconChange("");
            }

            __instance.terminal.GetComponent<InterfaceController>().ParseLilScript(text);
            InputDataManager.inst.playersCanJoin = __instance.playersCanJoin;
            return false;
        }

        public static IEnumerator ReturnToMenu()
        {
            SceneManager.inst.LoadScene(prevScene);

            while (!ArcadeManager.inst.ic)
                yield return null;

            if (!string.IsNullOrEmpty(prevBranch))
            {
                InterfaceControllerPatch.LoadInterface(ArcadeManager.inst.ic, prevInterface, false);
                ArcadeManager.inst.ic.SwitchBranch(prevBranch);
            }
        }

        [HarmonyPatch(typeof(ArcadeManager), "Update")]
        [HarmonyPostfix]
        static void ArcadeManagerUpdatePostfix(ArcadeManager __instance)
        {
            if (Input.GetKeyDown(KeyCode.G) && __instance.ic != null && __instance.ic.buttons != null && __instance.ic.buttons.Count > 0)
            {
                __instance.ic.currHoveredButton = __instance.ic.buttons[0];
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(ArcadeManager.inst.ic.buttons[0]);
            }
        }
    }
}
