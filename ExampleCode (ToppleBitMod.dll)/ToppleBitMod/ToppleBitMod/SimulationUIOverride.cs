using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using ToppleBitMod;
using ToppleBitModding;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace ToppleBitModding;

[Patch(typeof(SimulationUI), 2)]
public class SimulationUIOverride
{

    public static void Awake(SimulationUI __instance)
    {
        var map_list = FieldAccess.Get<List<MapObject>>(__instance, "selectableMapObjects");
        map_list.Clear();
        foreach (MapObject mapObject in Singleton<Map>.I.SampleMapObjects)
        {
            if (!(mapObject is Domino { FallState: not FallState.Standing }))
            {
                map_list.Add(mapObject);
            }
        }

        Loader.Log("Adding new button");
        FieldAccess.Get<Button>(__instance, "mapObjectButtonTemplate").gameObject.SetActive(value: true);
        int i = 0;
        foreach (MapObject mapObject in map_list)
        {
            if (i < 7)
            {
                i++;
                continue;
            }
            Loader.Log(mapObject.GetType().ToString());
            Button button = UnityEngine.Object.Instantiate(FieldAccess.Get<Button>(__instance, "mapObjectButtonTemplate"), FieldAccess.Get<Button>(__instance, "mapObjectButtonTemplate").transform.parent);
            Sprite sprite = ((Tile)mapObject.GetTile()).sprite;
            button.image.sprite = sprite;
            button.onClick.AddListener(delegate
            {
                Singleton<MapEditor>.I.SetSelectedMapObject(mapObject);
            });
        }
        FieldAccess.Get<Button>(__instance, "mapObjectButtonTemplate").gameObject.SetActive(value: false);
        /* DO NOT ADD THE LISTENER UNTIL I CAN MAKE THAT RUN IN PLACE OF UNITY'S AWAKE AND NOT AFTER
        FieldAccess.Get<Button>(__instance, "playButton").onClick.AddListener(Singleton<Simulation>.I.Toggle);
        FieldAccess.Get<Button>(__instance, "resetButton").onClick.AddListener(Singleton<Map>.I.ResetMapObjects);
        FieldAccess.Get<Button>(__instance, "stepButton").onClick.AddListener(Singleton<Simulation>.I.Step);
        FieldAccess.Get<TMP_InputField>(__instance, "tickRateInput").onValueChanged.AddListener(FieldAccess.Get<UnityEngine.Events.UnityAction<string>>(__instance, "UpdateTickRate"));
        FieldAccess.Get<Button>(__instance, "undoButton").onClick.AddListener(Singleton<MapEditor>.I.Undo);
        FieldAccess.Get<Button>(__instance, "redoButton").onClick.AddListener(Singleton<MapEditor>.I.Redo);
        FieldAccess.Get<Button>(__instance, "quitButton").onClick.AddListener(Application.Quit);
        Singleton<Simulation>.I.OnSimulationToggled += FieldAccess.Get<Action>(__instance, "OnSimulationToggled");
        */
    }
}
