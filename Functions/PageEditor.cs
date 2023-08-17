using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using InControl;
using SimpleJSON;
using LSFunctions;

using RTFunctions.Functions;

using Element = InterfaceController.InterfaceElement;
using ButtonSetting = InterfaceController.ButtonSetting;
using Branch = InterfaceController.InterfaceBranch;

using ElementType = InterfaceController.InterfaceElement.Type;
using ButtonType = InterfaceController.ButtonSetting.Type;
using BranchType = InterfaceController.InterfaceBranch.Type;

namespace PageCreator.Functions
{
    public class PageEditor : MonoBehaviour
    {
        public static PageEditor inst;

		public static void Init()
        {
			Debug.LogFormat("{0}Init() => PageEditor", PagePlugin.className);
			var gameObject = new GameObject("PageEditor");
			gameObject.AddComponent<PageEditor>();
        }

        void Awake()
        {
            if (!inst)
                inst = this;
            else if (inst != this)
                Destroy(gameObject);

			inst.StartCoroutine(CreateUI());
        }

        public IEnumerator DeleteComponents()
        {
            Destroy(GameObject.Find("Interface"));
            Destroy(GameObject.Find("EventSystem").GetComponent<InControlInputModule>());
            Destroy(GameObject.Find("EventSystem").GetComponent<BaseInput>());
            GameObject.Find("EventSystem").AddComponent<StandaloneInputModule>();
            Destroy(GameObject.Find("Main Camera").GetComponent<InterfaceLoader>());
            Destroy(GameObject.Find("Main Camera").GetComponent<ArcadeController>());
            Destroy(GameObject.Find("Main Camera").GetComponent<FlareLayer>());
            Destroy(GameObject.Find("Main Camera").GetComponent<GUILayer>());
            yield break;
        }

        public IEnumerator CreateUI()
        {
			yield return LoadInterfaces();

            yield return inst.StartCoroutine(DeleteComponents());

			yield break;
        }

        public IEnumerator LoadInterfaces()
        {
            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + "beatmaps/menus"))
            {
                var files = Directory.GetFiles(RTFile.ApplicationDirectory + "beatmaps/menus", "*.lsm", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var json = FileManager.inst.LoadJSONFileRaw(file);
                    var inter = new Interface(file, new List<Branch>());

					ParseLilScript(inter, json);

                    interfaces.Add(inter);
                }
            }

            yield break;
        }

		public void ParseLilScript(Interface inter, string _json)
		{
			var jn = JSON.Parse(_json);

			if (jn["settings"] != null)
			{
				inter.settings.times = new Vector2(jn["settings"]["times"]["min"].AsFloat, jn["settings"]["times"]["max"].AsFloat);
				inter.settings.language = DataManager.inst.GetSettingInt("Language_i", 0);
				inter.settings.initialBranch = jn["settings"]["initial_branch"];
				if (jn["settings"]["text_color"] != null)
				{
					inter.settings.textColor = LSColors.HexToColor(jn["settings"]["text_color"]);
				}
				if (jn["settings"]["bg_color"] != null)
				{
					inter.settings.bgColor = LSColors.HexToColor(jn["settings"]["bg_color"]);
				}
				inter.settings.music = jn["settings"]["music"];
				inter.settings.returnBranch = jn["settings"]["return_branch"];
			}
			//StartCoroutine(handleEvent(null, "apply_ui_theme", true));
			for (int i = 0; i < jn["branches"].Count; i++)
			{
				JSONNode jnbranch = jn["branches"];
				inter.branches.Add(new Branch(jnbranch[i]["name"]));
				inter.branches[i].clear_screen = jnbranch[i]["settings"]["clear_screen"].AsBool;
				if (jnbranch[i]["settings"]["back_branch"] != null)
				{
					inter.branches[i].BackBranch = jnbranch[i]["settings"]["back_branch"];
				}
				else
				{
					inter.branches[i].BackBranch = "";
				}
				inter.branches[i].type = convertInterfaceBranchToEnum(jnbranch[i]["settings"]["type"]);
				for (int j = 0; j < jnbranch[i]["elements"].Count; j++)
				{
					var type = ElementType.Text;
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					List<string> list = new List<string>();
					if (jnbranch[i]["elements"][j]["type"] != null)
					{
						type = convertInterfaceElementToEnum(jnbranch[i]["elements"][j]["type"]);
					}
					if (jnbranch[i]["elements"][j]["settings"] != null)
					{
						foreach (JSONNode child in jnbranch[i]["elements"][j]["settings"].Children)
						{
							string[] array = ((string)child).Split(new char[1] { ':' }, 2);
							dictionary.Add(array[0], array[1]);
						}
					}
					int num = 1;
					if (dictionary.ContainsKey("loop"))
					{
						num = int.Parse(dictionary["loop"]);
					}
					if (jnbranch[i]["elements"][j]["data"] != null)
					{
						foreach (JSONNode child2 in jnbranch[i]["elements"][j]["data"].Children)
						{
							string item = child2;
							list.Add(item);
						}
					}
					else
					{
						Debug.LogErrorFormat("{0}Couldn't load data for branch [{1}] element [{2}]", PagePlugin.className, i, j);
					}
					if (dictionary.Count > 0)
					{
						for (int k = 0; k < num; k++)
						{
							inter.branches[i].elements.Add(new Element(jnbranch[i]["name"], type, dictionary, list));
						}
					}
					else
					{
						for (int l = 0; l < num; l++)
						{
							inter.branches[i].elements.Add(new Element(jnbranch[i]["name"], type, list));
						}
					}
				}
			}
			Debug.LogFormat("{0}Parsed interface with [{1}] branches", PagePlugin.className, jn["branches"].Count);
		}

		public static ElementType convertInterfaceElementToEnum(string _type)
		{
			_type = _type.ToLower();
			switch (_type)
			{
				case "text": return ElementType.Text;
				case "divider": return ElementType.Divider;
				case "buttons": return ElementType.Buttons;
				case "media": return ElementType.Media;
				case "event": return ElementType.Event;
				default:
					Debug.LogFormat("{0}Couldn't convert type [{1}]", PagePlugin.className, _type);
					return ElementType.Text;
			}
		}

		public static BranchType convertInterfaceBranchToEnum(string _type)
		{
			if (string.IsNullOrEmpty(_type))
			{
				return BranchType.Normal;
			}
			_type = _type.ToLower();
			switch (_type)
			{
				case "normal":
					return BranchType.Normal;
				case "menu":
					return BranchType.Menu;
				case "main_menu":
					return BranchType.MainMenu;
				case "skipable":
					return BranchType.Skipable;
				default:
					Debug.LogFormat("{0}Couldn't convert type [{1}]", PagePlugin.className, _type);
					return BranchType.Normal;
			}
		}

		public void Exit()
        {
            interfaces.Clear();
            SceneManager.inst.LoadScene("Main Menu");
        }

        public List<Interface> interfaces = new List<Interface>();

        public class Interface
        {
            public Interface(string filePath, List<Branch> branches)
            {
                this.filePath = filePath;
                this.branches = branches;
				settings = new Settings();
            }

            public string filePath;
			public Settings settings;
            public List<Branch> branches = new List<Branch>();

			public class Settings
			{
				public int language;

				public Vector2 times = new Vector2(0.01f, 0.05f);

				public string initialBranch = "name";

				public Color textColor = LSColors.gray900;

				public Color textHighlightColor = Color.black;

				public Color borderColor = Color.black;

				public Color borderHighlightColor = new Color32(66, 66, 66, byte.MaxValue);

				public Color bgColor = LSColors.gray100;

				public string music;

				public string returnBranch = "";
			}
        }
    }
}
