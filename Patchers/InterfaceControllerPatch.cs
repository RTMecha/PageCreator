﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using HarmonyLib;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;
using TMPro;
using Steamworks;
using InControl;
using DG.Tweening;
using SimpleJSON;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Managers.Networking;

using PageCreator.Functions;

using Element = InterfaceController.InterfaceElement;
using ButtonSetting = InterfaceController.ButtonSetting;
using Branch = InterfaceController.InterfaceBranch;

using ElementType = InterfaceController.InterfaceElement.Type;
using ButtonType = InterfaceController.ButtonSetting.Type;
using BranchType = InterfaceController.InterfaceBranch.Type;

namespace PageCreator.Patchers
{
    [HarmonyPatch(typeof(InterfaceController))]
    public class InterfaceControllerPatch : MonoBehaviour
    {
		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		static bool StartPrefix(InterfaceController __instance)
		{
			if (EditorManager.inst != null)
				__instance.gameObject.SetActive(false);

			//if (Resources.LoadAll<QuickElement>("terminal/quick-elements") != null)
			//{
			//	foreach (QuickElement quickElement in Resources.LoadAll<QuickElement>("terminal/quick-elements"))
			//	{
			//		__instance.quickElements.Add(quickElement.name, quickElement);
			//	}
			//}

			foreach (var quickElement in QuickElementManager.AllQuickElements)
            {
				if (!__instance.quickElements.ContainsKey(quickElement.Key))
					__instance.quickElements.Add(quickElement.Key, quickElement.Value);
            }

			DataManager.inst.UpdateSettingString("colon", ":");
			LSHelpers.HideCursor();
			if (!DataManager.inst.HasKey("MasterVolume"))
			{
				__instance.ResetAudioSettings();
			}
			if (!DataManager.inst.HasKey("Resolution_i"))
			{
				__instance.ResetVideoSettings();
			}
			InputDataManager.inst.BindMenuKeys();
			__instance.MainPanel = __instance.transform.Find("Panel");

			Debug.LogFormat("{0}Load On Start: {1}", PagePlugin.className, __instance.loadOnStart);
			if (__instance.loadOnStart)
			{
				Debug.LogFormat("{0}Loading null...", PagePlugin.className);
				LoadInterface(__instance, null);
			}

			if (GameManager.inst == null)
			{
				PagePlugin.PlayMusic(__instance);

                //Camera.main.gameObject.layer = 5;

                //var postprocess = Camera.main.gameObject.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();

                //var menuEffects = new GameObject("MenuEffects");
                //menuEffects.layer = 5;
                //menuEffects.AddComponent<MenuEffects>();
            }

            //Destroy(GameObject.Find("EventSystem").GetComponent<InControlInputModule>());
            //Destroy(GameObject.Find("EventSystem").GetComponent<BaseInput>());
            //GameObject.Find("EventSystem").AddComponent<StandaloneInputModule>();

            return false;
		}

		[HarmonyPatch("LoadInterface", new Type[] { typeof(string) })]
		[HarmonyPrefix]
		static bool LoadInterfacePrefix(InterfaceController __instance, string _filename)
		{
			LoadInterface(__instance, _filename);
			return false;
        }

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		static bool UpdatePrefix(InterfaceController __instance, ref GameObject ___lastSelectedObj)
		{
			if (InputDataManager.inst.menuActions.Cancel.WasPressed && __instance.screenDone && __instance.currentBranch != "main_menu" && __instance.interfaceBranches[__instance.CurrentBranchIndex].type == BranchType.Menu)
			{
				if (__instance.branchChain.Count > 1)
				{
					if (!string.IsNullOrEmpty(__instance.interfaceBranches[__instance.CurrentBranchIndex].BackBranch))
					{
						__instance.SwitchBranch(__instance.interfaceBranches[__instance.CurrentBranchIndex].BackBranch);
					}
					else
					{
						__instance.SwitchBranch(__instance.branchChain[__instance.branchChain.Count - 2]);
					}
				}
				else
				{
					AudioManager.inst.PlaySound("Block");
				}
			}
			else if (InputDataManager.inst.menuActions.Cancel.WasPressed && __instance.screenDone && __instance.interfaceBranches[__instance.CurrentBranchIndex].type == BranchType.MainMenu && !string.IsNullOrEmpty(__instance.interfaceSettings.returnBranch))
			{
				__instance.SwitchBranch(__instance.interfaceSettings.returnBranch);
			}
			int num = 0;
			foreach (GameObject gameObject in __instance.buttons)
			{
				if (__instance.buttonSettings.Count > num && __instance.buttonSettings[num] != null && gameObject == EventSystem.current.currentSelectedGameObject)
				{
					if (__instance.buttonSettings[num].type == ButtonType.Int)
					{
						int num2 = DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting);
						if (InputDataManager.inst.menuActions.Left.WasPressed)
						{
							num2 -= __instance.buttonSettings[num].value;
							if (num2 < __instance.buttonSettings[num].min)
							{
								AudioManager.inst.PlaySound("Block");
								num2 = __instance.buttonSettings[num].min;
							}
							else
							{
								AudioManager.inst.PlaySound("LeftRight");
								Debug.Log(string.Concat(new object[]
								{
									"Subtract : ",
									num2,
									" : ",
									__instance.buttonSettings[num].setting
								}));
								DataManager.inst.UpdateSettingInt(__instance.buttonSettings[num].setting, num2);
							}
						}
						if (InputDataManager.inst.menuActions.Right.WasPressed)
						{
							num2 += __instance.buttonSettings[num].value;
							if (num2 > __instance.buttonSettings[num].max)
							{
								AudioManager.inst.PlaySound("Block");
								num2 = __instance.buttonSettings[num].max;
							}
							else
							{
								AudioManager.inst.PlaySound("LeftRight");
								Debug.Log(string.Concat(new object[]
								{
									"Add : ",
									num2,
									" : ",
									__instance.buttonSettings[num].setting
								}));
								DataManager.inst.UpdateSettingInt(__instance.buttonSettings[num].setting, num2);
							}
						}
					}
					else if (__instance.buttonSettings[num].type == ButtonType.Bool)
					{
						bool flag = DataManager.inst.GetSettingBool(__instance.buttonSettings[num].setting);
						if (InputDataManager.inst.menuActions.Left.WasPressed || InputDataManager.inst.menuActions.Right.WasPressed)
						{
							flag = !flag;
							AudioManager.inst.PlaySound("LeftRight");
							DataManager.inst.UpdateSettingBool(__instance.buttonSettings[num].setting, flag);
						}
					}
					else if (__instance.buttonSettings[num].type == ButtonType.Vector2)
					{
						int num3 = DataManager.inst.GetSettingVector2DIndex(__instance.buttonSettings[num].setting);
						if (InputDataManager.inst.menuActions.Left.WasPressed)
						{
							num3 -= __instance.buttonSettings[num].value;
							if (num3 < __instance.buttonSettings[num].min)
							{
								AudioManager.inst.PlaySound("Block");
								num3 = __instance.buttonSettings[num].min;
							}
							else
							{
								AudioManager.inst.PlaySound("LeftRight");
								DataManager.inst.UpdateSettingVector2D(__instance.buttonSettings[num].setting, num3, DataManager.inst.resolutions.ToArray());
							}
						}
						if (InputDataManager.inst.menuActions.Right.WasPressed)
						{
							num3 += __instance.buttonSettings[num].value;
							if (num3 > __instance.buttonSettings[num].max)
							{
								AudioManager.inst.PlaySound("Block");
								num3 = __instance.buttonSettings[num].max;
							}
							else
							{
								AudioManager.inst.PlaySound("LeftRight");
								DataManager.inst.UpdateSettingVector2D(__instance.buttonSettings[num].setting, num3, DataManager.inst.resolutions.ToArray());
							}
						}
					}
					else if (__instance.buttonSettings[num].type == ButtonType.String)
					{
						DataManager.inst.GetSettingEnumName(__instance.buttonSettings[num].setting, 0);
						int num4 = DataManager.inst.GetSettingEnum(__instance.buttonSettings[num].setting, 0);
						if (__instance.buttonSettings[num].setting == "Language")
						{
							num4 = DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting + "_i");
							DataManager.inst.GetSettingString(__instance.buttonSettings[num].setting);
						}
						if (InputDataManager.inst.menuActions.Left.WasPressed)
						{
							if (__instance.buttonSettings[num].setting == "Language")
							{
								num4 -= __instance.buttonSettings[num].value;
								if (num4 < __instance.buttonSettings[num].min)
								{
									AudioManager.inst.PlaySound("Block");
									num4 = __instance.buttonSettings[num].min;
								}
								else
								{
									AudioManager.inst.PlaySound("LeftRight");
									DataManager.inst.UpdateSettingInt(__instance.buttonSettings[num].setting + "_i", num4);
								}
							}
							else
							{
								num4--;
								if (num4 < 0)
								{
									AudioManager.inst.PlaySound("Block");
								}
								else
								{
									AudioManager.inst.PlaySound("LeftRight");
									DataManager.inst.UpdateSettingEnum(__instance.buttonSettings[num].setting, num4);
									string settingEnumFunctionCall = DataManager.inst.GetSettingEnumFunctionCall(__instance.buttonSettings[num].setting, num4);
									if (!string.IsNullOrEmpty(settingEnumFunctionCall))
									{
										__instance.StartCoroutine(handleEvent(__instance, null, settingEnumFunctionCall, true));
									}
								}
							}
						}
						if (InputDataManager.inst.menuActions.Right.WasPressed)
						{
							if (__instance.buttonSettings[num].setting == "Language")
							{
								num4 += __instance.buttonSettings[num].value;
								if (num4 > __instance.buttonSettings[num].max)
								{
									AudioManager.inst.PlaySound("Block");
									num4 = __instance.buttonSettings[num].max;
								}
								else
								{
									AudioManager.inst.PlaySound("LeftRight");
									DataManager.inst.UpdateSettingInt(__instance.buttonSettings[num].setting + "_i", num4);
								}
							}
							else
							{
								num4++;
								if (num4 >= DataManager.inst.GetSettingEnumCount(__instance.buttonSettings[num].setting))
								{
									AudioManager.inst.PlaySound("Block");
								}
								else
								{
									AudioManager.inst.PlaySound("LeftRight");
									DataManager.inst.UpdateSettingEnum(__instance.buttonSettings[num].setting, num4);
									string settingEnumFunctionCall2 = DataManager.inst.GetSettingEnumFunctionCall(__instance.buttonSettings[num].setting, num4);
									if (!string.IsNullOrEmpty(settingEnumFunctionCall2))
									{
										__instance.StartCoroutine(handleEvent(__instance, null, settingEnumFunctionCall2, true));
									}
								}
							}
						}
					}
				}
				if (gameObject == EventSystem.current.currentSelectedGameObject)
				{
					gameObject.transform.Find("bg").GetComponent<Image>().color = __instance.interfaceSettings.borderHighlightColor;
					if (gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>())
						gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
					if (gameObject.transform.Find("text").GetComponent<TextMeshPro>())
						gameObject.transform.Find("text").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textHighlightColor;
					if (gameObject.transform.Find("float"))
					{
						if (gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
						if (gameObject.transform.Find("float").GetComponent<TextMeshPro>())
							gameObject.transform.Find("float").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textHighlightColor;
					}
					if (gameObject.transform.Find("bool"))
					{
						if (gameObject.transform.Find("bool").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("bool").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
						if (gameObject.transform.Find("bool").GetComponent<TextMeshPro>())
							gameObject.transform.Find("bool").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textHighlightColor;
					}
					if (gameObject.transform.Find("vector2"))
					{
						if (gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
						if (gameObject.transform.Find("vector2").GetComponent<TextMeshPro>())
							gameObject.transform.Find("vector2").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textHighlightColor;
					}
					if (gameObject.transform.Find("string"))
					{
						if (gameObject.transform.Find("string").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("string").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textHighlightColor;
						if (gameObject.transform.Find("string").GetComponent<TextMeshPro>())
							gameObject.transform.Find("string").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textHighlightColor;
					}
					__instance.currHoveredButton = gameObject;
				}
				else
				{
					gameObject.transform.Find("bg").GetComponent<Image>().color = __instance.interfaceSettings.borderColor;
					if (gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>())
						gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
					if (gameObject.transform.Find("text").GetComponent<TextMeshPro>())
						gameObject.transform.Find("text").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textColor;
					if (gameObject.transform.Find("float"))
					{
						if (gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
						if (gameObject.transform.Find("float").GetComponent<TextMeshPro>())
							gameObject.transform.Find("float").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textColor;
					}
					if (gameObject.transform.Find("bool"))
					{
						if (gameObject.transform.Find("bool").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("bool").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
						if (gameObject.transform.Find("bool").GetComponent<TextMeshPro>())
							gameObject.transform.Find("bool").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textColor;
					}
					if (gameObject.transform.Find("vector2"))
					{
						if (gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
						if (gameObject.transform.Find("vector2").GetComponent<TextMeshPro>())
							gameObject.transform.Find("vector2").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textColor;
					}
					if (gameObject.transform.Find("string"))
					{
						if (gameObject.transform.Find("string").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("string").GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
						if (gameObject.transform.Find("string").GetComponent<TextMeshPro>())
							gameObject.transform.Find("string").GetComponent<TextMeshPro>().color = __instance.interfaceSettings.textColor;
					}
				}
				if (!__instance.screenGlitch)
				{
					if (__instance.buttonSettings[num].type == ButtonType.Int)
					{
						int num5 = DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting);
						num5 = Mathf.Clamp(num5, 0, 9);
						if (gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>())
						{
							gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text = "< [         ] >";
							gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text = gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text.Insert(num5 + 3, "■");
						}
						if (gameObject.transform.Find("float").GetComponent<TextMeshPro>())
						{
							gameObject.transform.Find("float").GetComponent<TextMeshPro>().text = "< [         ] >";
							gameObject.transform.Find("float").GetComponent<TextMeshPro>().text = gameObject.transform.Find("float").GetComponent<TextMeshPro>().text.Insert(num5 + 3, "■");
						}
					}
					else if (__instance.buttonSettings[num].type == ButtonType.Bool)
					{
						bool settingBool = DataManager.inst.GetSettingBool(__instance.buttonSettings[num].setting);
						if (gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text = "< [ " + (settingBool ? "true" : "false") + " ] >";
						if (gameObject.transform.Find("float").GetComponent<TextMeshPro>())
							gameObject.transform.Find("float").GetComponent<TextMeshPro>().text = "< [ " + (settingBool ? "true" : "false") + " ] >";
					}
					else if (__instance.buttonSettings[num].type == ButtonType.Vector2)
					{
						Vector2 settingVector2D = DataManager.inst.GetSettingVector2D(__instance.buttonSettings[num].setting);
						if (gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>().text = string.Concat(new object[]
							{
								"< [ ",
								settingVector2D.x,
								", ",
								settingVector2D.y,
								" ] >"
							});
						if (gameObject.transform.Find("vector2").GetComponent<TextMeshPro>())
							gameObject.transform.Find("vector2").GetComponent<TextMeshPro>().text = string.Concat(new object[]
							{
								"< [ ",
								settingVector2D.x,
								", ",
								settingVector2D.y,
								" ] >"
							});
					}
					else if (__instance.buttonSettings[num].type == ButtonType.String)
					{
						string str;
						if (__instance.buttonSettings[num].setting == "Language")
						{
							str = DataManager.inst.GetLanguage(DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting + "_i", 0));
						}
						else
						{
							str = DataManager.inst.GetSettingEnumName(__instance.buttonSettings[num].setting, 0);
						}
						if (gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>())
							gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>().text = "< [ " + str + " ] >";
						if (gameObject.transform.Find("float").GetComponent<TextMeshPro>())
							gameObject.transform.Find("float").GetComponent<TextMeshPro>().text = "< [ " + str + " ] >";
					}
				}
				num++;
			}
			__instance.SpeedUp = InputDataManager.inst.menuActions.Submit.IsPressed;
			if (EventSystem.current.currentSelectedGameObject == null && __instance.buttonsActive)
			{
				EventSystem.current.SetSelectedGameObject(___lastSelectedObj);
			}
			if (___lastSelectedObj != EventSystem.current.currentSelectedGameObject && __instance.screenDone)
			{
				AudioManager.inst.PlaySound("UpDown");
			}
			___lastSelectedObj = EventSystem.current.currentSelectedGameObject;
			return false;
		}

		public static void LoadInterface(InterfaceController __instance, string path, bool switchBranch = true)
		{
			string text;
			if (string.IsNullOrEmpty(path))
			{
				text = SaveManager.inst.CurrentStoryLevel.BeatmapJson.text;
			}
			else
			{
				text = FileManager.inst.LoadJSONFile(path);
			}
			PagePlugin.prevInterface = path;
			Debug.LogFormat("{0}Loaded interface [{1}]", PagePlugin.className, path);
			ParseLilScript(__instance, text, switchBranch);
		}

		[HarmonyPatch("handleEvent")]
		[HarmonyPrefix]
		static bool handleEventPrefix(InterfaceController __instance, ref IEnumerator __result, ref string __0, string __1, bool __2 = false)
        {
			__result = handleEvent(__instance, __0, __1, __2);
			return false;
        }

		public static IEnumerator handleEvent(InterfaceController __instance, string _branch, string _data, bool _override = false)
		{
			if (!(__instance.currentBranch == _branch || _override))
			{
				yield break;
			}

			string dataEvent = _data;

			if (!dataEvent.Contains("::"))
			{
				dataEvent = dataEvent.Replace("|", "::");
			}
			string[] data = dataEvent.Split(new string[1] { "::" }, 5, StringSplitOptions.None);
			switch (data[0].ToLower())
			{
				case "if":
					if (DataManager.inst.GetSettingBool(data[1]))
					{
						__instance.SwitchBranch(data[2]);
					}
					break;
				case "setting":
					switch (data[1].ToLower())
					{
						case "bool":
							DataManager.inst.UpdateSettingBool(data[2], bool.Parse(data[3]));
							break;
						case "enum":
							DataManager.inst.UpdateSettingEnum(data[2], int.Parse(data[3]));
							break;
						case "string":
						case "str":
							DataManager.inst.UpdateSettingString(data[2], data[3]);
							break;
						case "achievement":
						case "achieve":
							SteamWrapper.inst.achievements.SetAchievement(data[2]);
							break;
						case "clearachievement":
						case "clearachieve":
							SteamWrapper.inst.achievements.ClearAchievement(data[2]);
							break;
						case "int":
							if (data[3] == "add")
							{
								DataManager.inst.UpdateSettingInt(data[2], DataManager.inst.GetSettingInt(data[2]) + 1);
							}
							else if (data[3] == "sub")
							{
								DataManager.inst.UpdateSettingInt(data[2], DataManager.inst.GetSettingInt(data[2]) - 1);
							}
							else
							{
								DataManager.inst.UpdateSettingInt(data[2], int.Parse(data[3]));
							}
							break;
						default:
							Debug.LogError("Kind not found for setting [" + dataEvent + "]");
							break;
					}
					break;
				case "apply_ui_theme_with_reload":
					{
						Color textColor3 = __instance.interfaceSettings.textColor;
						__instance.interfaceSettings.textHighlightColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["text-highlight"]);
						__instance.interfaceSettings.bgColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["bg"]);
						__instance.interfaceSettings.borderHighlightColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["highlight"]);
						__instance.interfaceSettings.textColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["text"]);
						__instance.interfaceSettings.borderColor = LSColors.HexToColorAlpha(DataManager.inst.GetSettingEnumValues("UITheme", 0)["buttonbg"]);
						__instance.SwitchBranch(__instance.currentBranch);
						__instance.cam.GetComponent<Camera>().backgroundColor = __instance.interfaceSettings.bgColor;

						var tmpUGUI = __instance.MainPanel.transform.GetComponentsInChildren<TextMeshProUGUI>();
						foreach (var textMeshProUGUI3 in tmpUGUI)
						{
							if (textMeshProUGUI3.color == textColor3)
							{
								textMeshProUGUI3.color = __instance.interfaceSettings.textColor;
							}
						}

						var tmp = __instance.MainPanel.transform.GetComponentsInChildren<TextMeshPro>();
						foreach (var textMeshProUGUI3 in tmp)
						{
							if (textMeshProUGUI3.color == textColor3)
							{
								textMeshProUGUI3.color = __instance.interfaceSettings.textColor;
							}
						}

						SaveManager.inst.UpdateSettingsFile(false);
						break;
					}
				case "apply_ui_theme":
					{
						Color textColor2 = __instance.interfaceSettings.textColor;
						__instance.interfaceSettings.textHighlightColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["text-highlight"]);
						__instance.interfaceSettings.bgColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["bg"]);
						__instance.interfaceSettings.borderHighlightColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["highlight"]);
						__instance.interfaceSettings.textColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["text"]);
						__instance.interfaceSettings.borderColor = LSColors.HexToColorAlpha(DataManager.inst.GetSettingEnumValues("UITheme", 0)["buttonbg"]);
						__instance.cam.GetComponent<Camera>().backgroundColor = __instance.interfaceSettings.bgColor;

						var tmpUGUI = __instance.MainPanel.transform.GetComponentsInChildren<TextMeshProUGUI>();
						foreach (var textMeshProUGUI2 in tmpUGUI)
						{
							if (textMeshProUGUI2.color == textColor2)
							{
								textMeshProUGUI2.color = __instance.interfaceSettings.textColor;
							}
						}

						var tmp = __instance.MainPanel.transform.GetComponentsInChildren<TextMeshPro>();
						foreach (var textMeshProUGUI2 in tmp)
						{
							if (textMeshProUGUI2.color == textColor2)
							{
								textMeshProUGUI2.color = __instance.interfaceSettings.textColor;
							}
						}
						SaveManager.inst.UpdateSettingsFile(false);
						break;
					}
				case "apply_level_ui_theme":
					if (GameManager.inst != null)
					{
						Color color = LSColors.ContrastColor(LSColors.InvertColor(GameManager.inst.LiveTheme.backgroundColor));
						Color backgroundColor = GameManager.inst.LiveTheme.backgroundColor;
						__instance.interfaceSettings.textHighlightColor = backgroundColor;
						__instance.interfaceSettings.bgColor = new Color(0f, 0f, 0f, 0.3f);
						__instance.interfaceSettings.borderHighlightColor = color;
						__instance.interfaceSettings.textColor = color;
						__instance.interfaceSettings.borderColor = (((data.Length > 1 && data[1].ToLower() == "true") || data.Length == 1) ? LSColors.fadeColor(color, 0.3f) : LSColors.transparent);
					}
					break;
				case "apply_menu_music":
					AudioManager.inst.PlayMusic(DataManager.inst.GetSettingEnumValues("MenuMusic", 0), 1f);
					break;
				case "apply_video_settings_with_reload":
					__instance.SwitchBranch(__instance.currentBranch);
					__instance.ApplyVideoSettings();
					SaveManager.inst.UpdateSettingsFile(false);
					break;
				case "apply_video_settings":
					__instance.ApplyVideoSettings();
					break;
				case "save_settings":
					SaveManager.inst.UpdateSettingsFile(false);
					break;
				case "wait":
					if (data.Length >= 2)
					{
						float result = 0.5f;
						float.TryParse(data[1], out result);
						if (__instance.SpeedUp && __instance.FastSpeed > 0f)
						{
							yield return new WaitForSeconds(result / __instance.FastSpeed);
						}
						else
						{
							yield return new WaitForSeconds(result);
						}
					}
					break;
				case "branch":
					__instance.SwitchBranch(data[1]);
					break;
				case "exit":
					Application.Quit();
					break;
				case "setsavedlevel":
					Debug.LogFormat("setsavedlevel: {0} - {1}", int.Parse(data[1]), int.Parse(data[2]));
					SaveManager.inst.SetSaveStoryLevel(int.Parse(data[1]), int.Parse(data[2]));
					break;
				case "setcurrentlevel":
					SaveManager.inst.SetCurrentStoryLevel(int.Parse(data[1]), int.Parse(data[2]));
					break;
				case "loadscene":
					Debug.Log("Try to load [" + data[1] + "]");
					if (data.Length >= 3)
					{
						Debug.Log("Loading Scene with Loading Display off?");
						SceneManager.inst.LoadScene(data[1], bool.Parse(data[2]));
					}
					else
					{
						SceneManager.inst.LoadScene(data[1]);
					}
					break;
				case "loadnextlevel":
					SceneManager.inst.LoadNextLevel();
					break;
				case "parse":
					{
						if (data.Length >= 3)
						{
							if (bool.Parse(data[2]))
								__instance.interfaceBranches.Clear();
						}
						LoadInterface(__instance, data[1]);
						break;
					}
				case "loadlevel":
					{
						if (!data[1].Contains("level.lsb") && RTFile.FileExists(RTFile.ApplicationDirectory + data[1] + "/level.lsb"))
						{
							Debug.LogFormat("{0}Loading level from {1}", PagePlugin.className, data[1]);

							if (RTFile.FileExists(RTFile.ApplicationDirectory + data[1] + "/level.lsb"))
							{
								PagePlugin.prevBranch = data.Length > 2 ? data[2] : __instance.interfaceBranches.Find(x => x.name == __instance.currentBranch) != null ? __instance.interfaceBranches.Find(x => x.name == __instance.currentBranch).BackBranch : __instance.currentBranch;

								PagePlugin.prevScene = __instance.gameObject.scene.name;

								LevelManager.OnLevelEnd = delegate ()
								{
									PagePlugin.inst.StartCoroutine(PagePlugin.ReturnToMenu());
								};

								LevelManager.Load(RTFile.ApplicationDirectory + data[1] + "/level.lsb", false);
                            }
						}

						break;
					}
				case "loadlevelonline":
					{
						try
                        {
							__instance.StartCoroutine(AlephNetworkManager.DownloadJSONFile(data[1], delegate (string x)
							{

							}));
                        }
						catch
                        {

                        }

						if (!data[1].Contains("level.lsb") && RTFile.FileExists(RTFile.ApplicationDirectory + data[1] + "/level.lsb"))
						{
							Debug.LogFormat("{0}Loading level from {1}", PagePlugin.className, data[1]);
							if (RTFile.FileExists(RTFile.ApplicationDirectory + data[1] + "/metadata.lsb"))
							{
								Debug.LogFormat("{0}Loading metadata...", PagePlugin.className);
								string metadataStr = FileManager.inst.LoadJSONFileRaw(RTFile.ApplicationDirectory + data[1] + "/metadata.lsb");

								var metadata = DataManager.inst.ParseMetadata(metadataStr);

								ulong range = (ulong)metadata.beatmap.workshop_id;

								PublishedFileId_t publishedFileId_T = new PublishedFileId_t(range);

								SteamWorkshop.SteamItem steamItem = new SteamWorkshop.SteamItem(publishedFileId_T);

								steamItem.metaData = metadata;

								Debug.LogFormat("{0}Setting steamItem...", PagePlugin.className);
								steamItem.itemID = (int)range;
								steamItem.id = publishedFileId_T;
								steamItem.size = metadataStr.Length;
								steamItem.folder = RTFile.ApplicationDirectory + data[1];
								steamItem.musicID = Path.GetFileName(steamItem.folder);

								if (RTFile.FileExists(RTFile.ApplicationDirectory + data[1] + "/level.ogg"))
								{
									__instance.StartCoroutine(FileManager.inst.LoadMusicFileRaw(steamItem.folder + "/level.ogg", true, delegate (AudioClip clip)
									{
										PagePlugin.fromPageLevel = true;
										if (data.Length > 2)
											PagePlugin.prevBranch = data[2];
										else
											PagePlugin.prevBranch = __instance.currentBranch;

										PagePlugin.prevScene = __instance.gameObject.scene.name;

										Debug.LogFormat("{0}Setting ArcadeQueue...", PagePlugin.className);
										SaveManager.ArcadeLevel arcadeLevel = new SaveManager.ArcadeLevel("", FileManager.inst.LoadJSONFileRaw(steamItem.folder + "/level.lsb"), steamItem.metaData, clip);
										arcadeLevel.AudioFileStr = steamItem.folder + "/level.ogg";

										SaveManager.inst.ArcadeQueue = arcadeLevel;

										DataManager.inst.UpdateSettingBool("IsArcade", true);

										SceneManager.inst.LoadScene("Game");
									}));
								}
							}
						}

						break;
                    }
				case "deleteline":
					if (data.Length > 2)
					{
						Destroy(__instance.MainPanel.GetChild(__instance.MainPanel.childCount - 1 + int.Parse(data[1])).gameObject);
					}
					else
					{
						Destroy(__instance.MainPanel.GetChild(int.Parse(data[1])).gameObject);
					}
					break;
				case "replaceline":
					{
						AudioManager.inst.PlaySound("Click");
						string dataText = data[2];
						int childCount = ((data.Length > 3) ? (__instance.MainPanel.childCount - 1 + int.Parse(data[1])) : int.Parse(data[1]));
						dataText = RunTextTransformations(__instance, dataText, childCount);
						if (data.Length > 3)
						{
							//Debug.Log(__instance.MainPanel.GetChild(__instance.MainPanel.childCount - 1 + int.Parse(data[1])));
							//Debug.Log(__instance.MainPanel.GetChild(__instance.MainPanel.childCount - 1 + int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshProUGUI>().text);
							if (__instance.MainPanel.GetChild(__instance.MainPanel.childCount - 1 + int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshProUGUI>())
								__instance.MainPanel.GetChild(__instance.MainPanel.childCount - 1 + int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshProUGUI>().text = dataText;
							if (__instance.MainPanel.GetChild(__instance.MainPanel.childCount - 1 + int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshPro>())
								__instance.MainPanel.GetChild(__instance.MainPanel.childCount - 1 + int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshPro>().text = dataText;
						}
						else
						{
							if (__instance.MainPanel.GetChild(int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshProUGUI>())
								__instance.MainPanel.GetChild(int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshProUGUI>().text = dataText;
							if (__instance.MainPanel.GetChild(int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshPro>())
								__instance.MainPanel.GetChild(int.Parse(data[1])).Find("text").gameObject.GetComponent<TextMeshPro>().text = dataText;
						}
						break;
					}
				case "replacelineinbranch":
					{
						int index = __instance.interfaceBranches.FindIndex(x => x.name == data[1]);
						__instance.interfaceBranches[index].elements[int.Parse(data[2])].data = new List<string> { data[3] };
						break;
					}
				case "playsound":
					{
						if (!RTFile.FileExists(RTFile.ApplicationDirectory + data[1]))
						{
							AudioManager.inst.PlaySound(data[1]);
						}
						else
						{
							__instance.StartCoroutine(FileManager.inst.LoadMusicFile(data[1], delegate (AudioClip clip)
							{
								AudioManager.inst.PlaySound(clip);
							}));
						}
						break;
					}
				case "playsoundonline":
                    {
						try
						{
							if (data[1].ToLower().Substring(data[1].ToLower().Length - 4, 4) == ".ogg")
								__instance.StartCoroutine(AlephNetworkManager.DownloadAudioClip(data[1], AudioType.OGGVORBIS, delegate (AudioClip audioClip)
								{
									AudioManager.inst.PlaySound(audioClip);
								}));
						}
						catch
						{

						}
						break;
					}
				case "playmusic":
					{
						if (!RTFile.FileExists(RTFile.ApplicationDirectory + data[1]))
						{
							AudioManager.inst.PlayMusic(data[1], 0.5f);
						}
						else
						{
							__instance.StartCoroutine(FileManager.inst.LoadMusicFile(data[1], delegate (AudioClip clip)
							{
								AudioManager.inst.PlayMusic(data[1], clip, false, 0.5f);
							}));
						}
						break;
					}
				case "pausemusic":
					AudioManager.inst.CurrentAudioSource.Pause();
					break;
				case "resumemusic":
					AudioManager.inst.CurrentAudioSource.Play();
					break;
				case "setmusicvol":
					if (data[1] == "back")
					{
						AudioManager.inst.CurrentAudioSource.volume = AudioManager.inst.musicVol;
					}
					else
					{
						AudioManager.inst.CurrentAudioSource.volume = float.Parse(data[1]);
					}
					break;
				case "clearplayers":
					if (data.Length > 1)
					{
						InputDataManager.inst.ClearInputs((data[1] == "true") ? true : false);
					}
					else
					{
						InputDataManager.inst.ClearInputs();
					}
					break;
				case "loadarcadelevels":
					{
						__instance.StartCoroutine(ArcadeManager.inst.GetFiles());
						break;
					}
				case "openlink":
					{
						if (data[1].Contains("https://www.youtube.com") || data[1].Contains("https://www.discord.com/") || data[1].Contains(".newgrounds.com/"))
							Application.OpenURL(data[1]);
						break;
					}
				case "setbg":
					__instance.interfaceSettings.bgColor = LSColors.HexToColor(data[1].Replace("#", ""));
					__instance.cam.GetComponent<Camera>().backgroundColor = __instance.interfaceSettings.bgColor;
					break;
				case "sethighlight":
					__instance.interfaceSettings.borderHighlightColor = LSColors.HexToColor(data[1].Replace("#", ""));
					break;
				case "settext":
					{
						Color textColor = __instance.interfaceSettings.textColor;
						__instance.interfaceSettings.textColor = LSColors.HexToColor(data[1].Replace("#", ""));

						var tmpUGUI = __instance.MainPanel.transform.GetComponentsInChildren<TextMeshProUGUI>();
						foreach (var textMeshProUGUI in tmpUGUI)
						{
							if (textMeshProUGUI.color == textColor)
							{
								textMeshProUGUI.color = __instance.interfaceSettings.textColor;
							}
						}

						var tmp = __instance.MainPanel.transform.GetComponentsInChildren<TextMeshPro>();
						foreach (var textMeshProUGUI in tmp)
						{
							if (textMeshProUGUI.color == textColor)
							{
								textMeshProUGUI.color = __instance.interfaceSettings.textColor;
							}
						}
						break;
					}
				case "setbuttonbg":
					{
						string text = data[1].Replace("#", "");
						Color borderColor = __instance.interfaceSettings.borderColor;
						if (text == "none")
						{
							__instance.interfaceSettings.borderColor = new Color(0f, 0f, 0f, 0f);
						}
						else
						{
							__instance.interfaceSettings.borderColor = LSColors.HexToColorAlpha(text);
						}
						break;
					}
				case "unpauselevel":
					if (GameManager.inst)
					{
						GameManager.inst.UnPause();
					}
					break;
				case "restartlevel":
                    {
						if (GameManager.inst)
                        {
							AudioManager.inst.SetMusicTime(0f);
							GameManager.inst.hits.Clear();
							GameManager.inst.deaths.Clear();
							GameManager.inst.UnPause();
                        }
						break;
                    }
				case "quittoarcade":
					{
						if (GameManager.inst != null)
						{
							GameManager.inst.QuitToArcade();
						}
						break;
					}
				case "subscribe_official_arcade_levels":
					{
						SteamWorkshop.inst.Subscribe(new PublishedFileId_t(1753879306uL));
						SteamWorkshop.inst.Subscribe(new PublishedFileId_t(1754882933uL));
						SteamWorkshop.inst.Subscribe(new PublishedFileId_t(1754881252uL));
						SteamWorkshop.inst.Subscribe(new PublishedFileId_t(1754881974uL));
						break;
					}
				case "pageeditor":
                    {
						PageEditor.Init();
						break;
                    }
			}
			yield return null;
		}

		[HarmonyPatch("AddElement")]
        [HarmonyPrefix]
        static bool AddElementPrefix(InterfaceController __instance, ref IEnumerator __result, Element __0, bool __1)
        {
			__result = AddElement(__instance, __0, __1);
			return false;
        }

		public static IEnumerator AddElement(InterfaceController __instance, Element _element, bool _immediate)
		{
			if (!(_element.branch == __instance.currentBranch))
			{
				yield break;
			}
			__instance.StartCoroutine(ScrollBottom(__instance));
			float totalTime = ((!__instance.SpeedUp) ? UnityEngine.Random.Range(__instance.interfaceSettings.times.x, __instance.interfaceSettings.times.y) : (UnityEngine.Random.Range(__instance.interfaceSettings.times.x, __instance.interfaceSettings.times.y) / __instance.FastSpeed));
			if (!_immediate)
			{
				AudioManager.inst.PlaySound("Click");
			}
			int childCount = __instance.MainPanel.childCount;
			switch (_element.type)
			{
				case ElementType.Text:
					{
						string text5;
						if (_element.data.Count > 0)
						{
							text5 = _element.data[0];
						}
						else
						{
							text5 = " ";
							Debug.Log(_element.branch + " - " + childCount);
						}
						string dataText2 = ((_element.data.Count > __instance.interfaceSettings.language) ? _element.data[__instance.interfaceSettings.language] : text5);
						var gameObject = Instantiate(__instance.TextPrefab, Vector3.zero, Quaternion.identity);
						gameObject.name = "button";
						gameObject.transform.SetParent(__instance.MainPanel);
						gameObject.transform.localScale = Vector3.one;
						gameObject.name = string.Format("[{0}] Text", childCount);

						var gameObject3 = gameObject.transform.Find("bg").gameObject;
						var text = gameObject.transform.Find("text").gameObject;

						if (_element.settings.ContainsKey("reactiveScale"))
						{
							var audio = gameObject.AddComponent<ReactiveAudio>();
							audio.intensity = new float[2]
							{
								1f,
								1f
							};
							audio.channels = new int[2]
							{
								0,
								0
							};

							if (_element.settings.ContainsKey("reativeScaleIntensityX"))
								audio.intensity[0] = float.Parse(_element.settings["reactiveScaleIntensityX"]);
							if (_element.settings.ContainsKey("reactiveScaleIntensityY"))
								audio.intensity[1] = float.Parse(_element.settings["reactiveScaleIntensityY"]);
							if (_element.settings.ContainsKey("reactiveScaleChannelX"))
								audio.channels[0] = int.Parse(_element.settings["reactiveScaleChannelX"]);
							if (_element.settings.ContainsKey("reactiveScaleChannelY"))
								audio.channels[1] = int.Parse(_element.settings["reactiveScaleChannelY"]);
						}

						if (_element.settings.ContainsKey("bg-color"))
						{
							if (_element.settings["bg-color"] == "text-color")
							{
								gameObject3.GetComponent<Image>().color = __instance.interfaceSettings.textColor;
							}
							else
							{
								gameObject3.GetComponent<Image>().color = LSColors.HexToColor(_element.settings["bg-color"]);
							}
						}
						else
						{
							gameObject3.GetComponent<Image>().color = LSColors.transparent;
						}
						if (_element.settings.ContainsKey("text-color"))
						{
							if (_element.settings["text-color"] == "bg-color")
							{
								text.GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.bgColor;
							}
							else
							{
								text.GetComponent<TextMeshProUGUI>().color = LSColors.HexToColor(_element.settings["text-color"]);
							}
						}
						else
						{
							text.GetComponent<TextMeshProUGUI>().color = __instance.interfaceSettings.textColor;
						}

						if (string.IsNullOrEmpty(dataText2))
						{
							break;
						}

						if (_element.settings.ContainsKey("alignment"))
						{
							switch (_element.settings["alignment"])
							{
								case "left":
									if (!_element.settings.ContainsKey("valignment"))
									{
										text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
										break;
									}
									switch (_element.settings["valignment"])
									{
										case "top":
											text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
											break;
										case "center":
											text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
											break;
										case "bottom":
											text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
											break;
									}
									break;
								case "center":
									if (!_element.settings.ContainsKey("valignment"))
									{
										text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Midline;
										break;
									}
									switch (_element.settings["valignment"])
									{
										case "top":
											text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Top;
											break;
										case "center":
											text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Midline;
											break;
										case "bottom":
											text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Bottom;
											break;
									}
									break;
								case "right":
									if (!_element.settings.ContainsKey("valignment"))
									{
										text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
										break;
									}
									switch (_element.settings["valignment"])
									{
										case "top":
											text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopRight;
											break;
										case "center":
											text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
											break;
										case "bottom":
											text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomRight;
											break;
									}
									break;
							}
						}
						else if (_element.settings.ContainsKey("valignment"))
						{
							switch (_element.settings["valignment"])
							{
								case "top":
									text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
									break;
								case "center":
									text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
									break;
								case "bottom":
									text.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
									break;
							}
						}

						dataText2 = RunTextTransformations(__instance, dataText2, childCount);
						if (dataText2.Contains("[[") && dataText2.Contains("]]"))
						{
							foreach (Match item in Regex.Matches(dataText2, "\\[\\[([^\\]]*)\\]\\]"))
							{
								Debug.Log(item.Groups[0].Value);
								string value = item.Groups[0].Value;
								string value2 = item.Groups[1].Value;
								dataText2 = dataText2.Replace(value, LSText.FormatString(value2));
							}
						}
						string[] words = dataText2.Split(new string[1] { " " }, StringSplitOptions.RemoveEmptyEntries);
						string tempText = "";
						for (int i = 0; i < words.Length; i++)
						{
							float seconds = totalTime / (float)words.Length;
							if (text != null)
							{
								tempText = tempText + words[i] + " ";
								text.GetComponent<TextMeshProUGUI>().text = tempText + ((i % 2 == 0) ? "▓▒░" : "▒░░");
							}
							yield return new WaitForSeconds(seconds);
						}
						if (_element.settings.ContainsKey("font-style") && text != null)
						{
							switch (_element.settings["font-style"])
							{
								case "light":
									text.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
									break;
								case "normal":
									text.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
									break;
								case "bold":
									text.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
									break;
							}
						}
						else if (text != null)
						{
							text.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
						}
						if (text != null)
						{
							text.GetComponent<TextMeshProUGUI>().text = dataText2;
						}
						break;
					}
				case ElementType.Buttons:
					{
						GameObject gameObject = Instantiate(__instance.ButtonElementPrefab, Vector3.zero, Quaternion.identity);
						gameObject.name = "button";
						gameObject.transform.SetParent(__instance.MainPanel);
						gameObject.transform.localScale = Vector3.one;
						gameObject.name = string.Format("[{0}] Button Holder", childCount);
						if (_element.settings.ContainsKey("width"))
						{
							float result = 0.5f;
							float.TryParse(_element.settings["width"], out result);
							gameObject.GetComponent<LayoutElement>().preferredWidth = result * 1792f;
						}
						if (_element.settings.ContainsKey("orientation"))
						{
							if (_element.settings["orientation"] == "horizontal")
							{
								gameObject.GetComponent<VerticalLayoutGroup>().enabled = false;
							}
							else if (_element.settings["orientation"] == "vertical")
							{
								gameObject.GetComponent<HorizontalLayoutGroup>().enabled = false;
							}
							else if (_element.settings["orientation"] == "grid")
							{
								DestroyImmediate(gameObject.GetComponent<HorizontalLayoutGroup>());
								DestroyImmediate(gameObject.GetComponent<VerticalLayoutGroup>());
								var gridLayoutGroup = gameObject.AddComponent<GridLayoutGroup>();
								gridLayoutGroup.spacing = new Vector2(16f, 16f);
								float result2 = 1f;
								if (_element.settings.ContainsKey("grid_h"))
								{
									float.TryParse(_element.settings["grid_h"], out result2);
								}
								int result3 = 0;
								if (_element.settings.ContainsKey("grid_corner"))
								{
									int.TryParse(_element.settings["grid_corner"], out result3);
								}
								float result4 = 1f;
								if (_element.settings.ContainsKey("grid_v"))
								{
									float.TryParse(_element.settings["grid_v"], out result4);
								}
								gridLayoutGroup.cellSize = new Vector2((1792f - 16f * (result2 - 1f)) / result2, result4 * 54f);
								gridLayoutGroup.childAlignment = (TextAnchor)result3;
							}
						}
						else
						{
							gameObject.GetComponent<HorizontalLayoutGroup>().enabled = false;
						}
						string[] array = ((_element.data.Count > __instance.interfaceSettings.language) ? _element.data[__instance.interfaceSettings.language] : _element.data[0]).Split(new string[1] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
						__instance.buttonSettings.Clear();
						if (_element.settings.ContainsKey("buttons"))
						{
							string[] array2 = _element.settings["buttons"].Split(new string[1] { ":" }, StringSplitOptions.None);
							int num = 0;
							string[] array3 = array2;
							foreach (string text2 in array3)
							{
								if (!string.IsNullOrEmpty(text2))
								{
									string[] array4 = text2.Split(new string[1] { "|" }, StringSplitOptions.None);
									var buttonSetting = new ButtonSetting(ConvertStringToButtonType(__instance, array4[0]));
									if (buttonSetting.type == ButtonType.Event)
									{
										int num2 = 0;
										string[] array5 = array4;
										foreach (string text3 in array5)
										{
											if (num2 != 0)
											{
												buttonSetting.setting += text3;
											}
											if (num2 != 0 && num2 < array4.Length - 1)
											{
												buttonSetting.setting += "|";
											}
											num2++;
										}
									}
									else
									{
										buttonSetting.setting = array4[1];
										buttonSetting.value = int.Parse(array4[2]);
										buttonSetting.min = int.Parse(array4[3]);
										buttonSetting.max = int.Parse(array4[4]);
									}
									__instance.buttonSettings.Add(buttonSetting);
								}
								else if (num != 0)
								{
									__instance.buttonSettings.Add(new ButtonSetting(ButtonType.Empty));
								}
								num++;
							}
						}
						else
						{
							for (int l = 0; l < array.Length; l++)
							{
								__instance.buttonSettings.Add(new ButtonSetting(ButtonType.Empty));
							}
						}
						for (int m = 0; m < array.Length; m++)
						{
							string[] array6 = array[m].Split(':');
							GameObject gameObject2;
							if (__instance.buttonSettings.Count > m && __instance.buttonSettings[m].setting != null)
							{
								switch (__instance.buttonSettings[m].type)
								{
									case ButtonType.Int:
										gameObject2 = Instantiate(__instance.IntButtonPrefab, Vector3.zero, Quaternion.identity);
										gameObject2.name = "button";
										break;
									case ButtonType.Vector2:
										gameObject2 = Instantiate(__instance.Vector2ButtonPrefab, Vector3.zero, Quaternion.identity);
										gameObject2.name = "button";
										break;
									case ButtonType.Bool:
										gameObject2 = Instantiate(__instance.BoolButtonPrefab, Vector3.zero, Quaternion.identity);
										gameObject2.name = "button";
										break;
									case ButtonType.String:
										gameObject2 = Instantiate(__instance.StringButtonPrefab, Vector3.zero, Quaternion.identity);
										gameObject2.name = "button";
										break;
									default:
										gameObject2 = Instantiate(__instance.ButtonPrefab, Vector3.zero, Quaternion.identity);
										gameObject2.name = "button";
										break;
								}
							}
							else
							{
								gameObject2 = Instantiate(__instance.ButtonPrefab, Vector3.zero, Quaternion.identity);
								gameObject2.name = "button";
							}
							gameObject2.transform.SetParent(gameObject.transform);
							gameObject2.transform.localScale = Vector3.one;
							gameObject2.name = string.Format("[{0}][{1}] Button", childCount, m);

							if (_element.settings.ContainsKey("reactiveScale"))
							{
								var audio = gameObject2.AddComponent<ReactiveAudio>();
								audio.intensity = new float[2]
								{
									1f,
									1f
								};
								audio.channels = new int[2]
								{
									0,
									0
								};

								if (_element.settings.ContainsKey("reactiveScaleIntensityX"))
									audio.intensity[0] = float.Parse(_element.settings["reactiveScaleIntensityX"]);
								if (_element.settings.ContainsKey("reactiveScaleIntensityY"))
									audio.intensity[1] = float.Parse(_element.settings["reactiveScaleIntensityY"]);
								if (_element.settings.ContainsKey("reactiveScaleChannelX"))
									audio.channels[0] = int.Parse(_element.settings["reactiveScaleChannelX"]);
								if (_element.settings.ContainsKey("reactiveScaleChannelY"))
									audio.channels[1] = int.Parse(_element.settings["reactiveScaleChannelY"]);
							}

							if (_element.settings.ContainsKey("alignment"))
							{
								switch (_element.settings["alignment"])
								{
									case "left":
										if (!_element.settings.ContainsKey("valignment"))
										{
											gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
											break;
										}
										switch (_element.settings["valignment"])
										{
											case "top":
												gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
												break;
											case "center":
												gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
												break;
											case "bottom":
												gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
												break;
										}
										break;
									case "center":
										if (!_element.settings.ContainsKey("valignment"))
										{
											gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Midline;
											break;
										}
										switch (_element.settings["valignment"])
										{
											case "top":
												gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Top;
												break;
											case "center":
												gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Midline;
												break;
											case "bottom":
												gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Bottom;
												break;
										}
										break;
									case "right":
										if (!_element.settings.ContainsKey("valignment"))
										{
											gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
											break;
										}
										switch (_element.settings["valignment"])
										{
											case "top":
												gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopRight;
												break;
											case "center":
												gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
												break;
											case "bottom":
												gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomRight;
												break;
										}
										break;
								}
							}
							else if (_element.settings.ContainsKey("valignment"))
							{
								switch (_element.settings["valignment"])
								{
									case "top":
										gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
										break;
									case "center":
										gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
										break;
									case "bottom":
										gameObject2.transform.GetChild(1).GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomLeft;
										break;
								}
							}
							__instance.buttons.Add(gameObject2);
							if (m == 0 && __instance.buttonsActive)
							{
								EventSystem.current.SetSelectedGameObject(gameObject2);
							}
							gameObject2.transform.Find("text").GetComponent<TextMeshProUGUI>().text = __instance.ParseText(array6[0]);
							if (array6[0] == "")
							{
								Navigation navigation = default(Navigation);
								navigation.mode = Navigation.Mode.None;
								gameObject2.GetComponent<Button>().navigation = navigation;
								gameObject2.transform.Find("bg").GetComponent<Image>().enabled = false;
								continue;
							}
							gameObject2.GetComponent<EventTrigger>().triggers.Add(CreateButtonHoverTrigger(__instance, EventTriggerType.PointerEnter, gameObject2));
							if (_element.settings.ContainsKey("buttons") && __instance.buttonSettings[m].type == ButtonType.Event)
							{
								gameObject2.GetComponent<EventTrigger>().triggers.Add(CreateButtonTriggerForEvent(__instance, EventTriggerType.Submit, _element.branch, __instance.buttonSettings[m].setting));
								gameObject2.GetComponent<EventTrigger>().triggers.Add(CreateButtonTriggerForEvent(__instance, EventTriggerType.PointerClick, _element.branch, __instance.buttonSettings[m].setting));
							}
							else if (array6.Length == 2)
							{
								gameObject2.GetComponent<EventTrigger>().triggers.Add(CreateButtonTrigger(__instance, EventTriggerType.Submit, gameObject, array6[1]));
								gameObject2.GetComponent<EventTrigger>().triggers.Add(CreateButtonTrigger(__instance, EventTriggerType.PointerClick, gameObject, array6[1]));
							}
							else
							{
								string text4 = array6[1];
								if (text4 == "setting_str")
								{
									DataManager.inst.UpdateSettingString(array6[2], array6[3]);
								}
							}
							if (_element.settings.ContainsKey("default_button") && __instance.buttons.Count > int.Parse(_element.settings["default_button"]) && __instance.buttonsActive)
							{
								EventSystem.current.SetSelectedGameObject(__instance.buttons[int.Parse(_element.settings["default_button"])]);
							}
						}
						break;
					}
				case ElementType.Event:
					foreach (string datum in _element.data)
					{
						yield return __instance.StartCoroutine(handleEvent(__instance, _element.branch, datum));
					}
					break;
				case ElementType.Media:
                    {
						var gameObject = new GameObject("Media");
						gameObject.transform.SetParent(__instance.MainPanel);
						gameObject.transform.localScale = Vector3.one;

						var gameObjectRT = gameObject.AddComponent<RectTransform>();
						gameObjectRT.anchoredPosition = Vector3.zero;

						var gameObjectImage = gameObject.AddComponent<Image>();

						if (_element.data.Count > 2 && float.TryParse(_element.data[1], out float sizeX) && float.TryParse(_element.data[2], out float sizeY))
							gameObjectRT.sizeDelta = new Vector2(sizeX, sizeY);
						
						if (RTFile.FileExists(RTFile.ApplicationDirectory + _element))
							gameObjectImage.sprite = SpriteManager.LoadSprite(RTFile.ApplicationDirectory + _element);

						break;
                    }
			}
		}

		[HarmonyPatch("ParseLilScript")]
		[HarmonyPrefix]
		static bool ParseLilScriptPrefix(InterfaceController __instance, string __0)
		{
			ParseLilScript(__instance, __0);
			return false;
		}

		public static void ParseLilScript(InterfaceController __instance, string _json, bool switchBranch = true)
		{
			DOTween.KillAll();
			DOTween.Clear(true);
			JSONNode jn = JSON.Parse(_json);
			if (jn["settings"] != null)
			{
				__instance.interfaceSettings.times = new Vector2(jn["settings"]["times"]["min"].AsFloat, jn["settings"]["times"]["max"].AsFloat);
				__instance.interfaceSettings.language = DataManager.inst.GetSettingInt("Language_i", 0);
				__instance.interfaceSettings.initialBranch = jn["settings"]["initial_branch"];
				if (jn["settings"]["text_color"] != null)
				{
					__instance.interfaceSettings.textColor = LSColors.HexToColor(jn["settings"]["text_color"]);
				}
				if (jn["settings"]["bg_color"] != null)
				{
					__instance.interfaceSettings.bgColor = LSColors.HexToColor(jn["settings"]["bg_color"]);
				}
				__instance.interfaceSettings.music = jn["settings"]["music"];
				__instance.interfaceSettings.returnBranch = jn["settings"]["return_branch"];
			}
			__instance.StartCoroutine(handleEvent(__instance, null, "apply_ui_theme", true));
			for (int i = 0; i < jn["branches"].Count; i++)
			{
				var jnbranch = jn["branches"];
				__instance.interfaceBranches.Add(new Branch(jnbranch[i]["name"]));
				__instance.interfaceBranches[i].clear_screen = jnbranch[i]["settings"]["clear_screen"].AsBool;
				if (jnbranch[i]["settings"]["back_branch"] != null)
				{
					__instance.interfaceBranches[i].BackBranch = jnbranch[i]["settings"]["back_branch"];
				}
				else
				{
					__instance.interfaceBranches[i].BackBranch = "";
				}
				__instance.interfaceBranches[i].type = convertInterfaceBranchToEnum(__instance, jnbranch[i]["settings"]["type"]);
				for (int j = 0; j < jnbranch[i]["elements"].Count; j++)
				{
					var type = ElementType.Text;
					var settings = new Dictionary<string, string>();
					var list = new List<string>();
					if (jnbranch[i]["elements"][j]["type"] != null)
					{
						type = convertInterfaceElementToEnum(__instance, jnbranch[i]["elements"][j]["type"]);
					}
					if (jnbranch[i]["elements"][j]["settings"] != null)
					{
						foreach (var child in jnbranch[i]["elements"][j]["settings"].Children)
						{
							string[] array = ((string)child).Split(new char[1] { ':' }, 2);
							settings.Add(array[0], array[1]);
						}
					}
					int num = 1;
					if (settings.ContainsKey("loop"))
					{
						num = int.Parse(settings["loop"]);
					}

					//Add a foreach loop for parsing menus / loading levels

					if (jnbranch[i]["elements"][j]["data"] != null)
					{
						foreach (var child2 in jnbranch[i]["elements"][j]["data"].Children)
						{
							string item = child2;
							list.Add(item);
						}
					}
					else
					{
						Debug.LogErrorFormat("{0}Couldn't load data for branch [{1}] element [{2}]", PagePlugin.className, i, j);
					}
					if (settings.Count > 0)
					{
						for (int k = 0; k < num; k++)
						{
							__instance.interfaceBranches[i].elements.Add(new Element(jnbranch[i]["name"], type, settings, list));
						}
					}
					else
					{
						for (int l = 0; l < num; l++)
						{
							__instance.interfaceBranches[i].elements.Add(new Element(jnbranch[i]["name"], type, list));
						}
					}
				}
			}
			Debug.LogFormat("{0}Parsed interface with [{1}] branches", PagePlugin.className, jn["branches"].Count);

			if (switchBranch)
				__instance.SwitchBranch(__instance.interfaceSettings.initialBranch);
		}

		public static BranchType convertInterfaceBranchToEnum(InterfaceController __instance, string _type) => (BranchType)__instance.GetType().GetMethod("convertInterfaceBranchToEnum", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { _type });
		
		public static ElementType convertInterfaceElementToEnum(InterfaceController __instance, string _type) => (ElementType)__instance.GetType().GetMethod("convertInterfaceElementToEnum", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { _type });

		public static string RunTextTransformations(InterfaceController __instance, string dataText, int childCount) => (string)__instance.GetType().GetMethod("RunTextTransformations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { dataText, childCount });

		public static IEnumerator ScrollBottom(InterfaceController __instance) => (IEnumerator)__instance.GetType().GetMethod("ScrollBottom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { });

		public static ButtonType ConvertStringToButtonType(InterfaceController __instance, string _type) => (ButtonType)__instance.GetType().GetMethod("ConvertStringToButtonType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { _type });

		public static EventTrigger.Entry CreateButtonHoverTrigger(InterfaceController __instance, EventTriggerType _type, GameObject _element) => (EventTrigger.Entry)__instance.GetType().GetMethod("CreateButtonHoverTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { _type, _element });

		public static EventTrigger.Entry CreateButtonTriggerForEvent(InterfaceController __instance, EventTriggerType _type, string _branch, string _eventData) => (EventTrigger.Entry)__instance.GetType().GetMethod("CreateButtonTriggerForEvent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { _type, _branch, _eventData });

		public static EventTrigger.Entry CreateButtonTrigger(InterfaceController __instance, EventTriggerType _type, GameObject element, string _link) => (EventTrigger.Entry)__instance.GetType().GetMethod("CreateButtonTrigger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(__instance, new object[] { _type, element, _link });
	}
}
