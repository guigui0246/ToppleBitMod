using System.Collections.Generic;
using ToppleBitModding;
using UnityEngine;
using System;

namespace ToppleBitMod
{
    [Patch(typeof(MapEditor), 1)]
    public class MapEditorOverride
    {
        public void Constructor(MapEditor __instance, MapEditorInitializationData initializationData, InputSystem inputSystem, Map map, Simulator simulator, ProjectMenuManager projectMenuManager, MapDrawerData mapDrawerData, ApplicationSystem applicationSystem)
        {
            var mapObjectTilemap = initializationData.GetMapObjectTilemap();
            FieldAccess.Set(__instance, "mapObjectTilemap", mapObjectTilemap);
            var selectionTilemap = initializationData.GetSelectionTilemap();
            FieldAccess.Set(__instance, "selectionTilemap", selectionTilemap);
            var selectionTile = initializationData.GetSelectionTile();
            FieldAccess.Set(__instance, "selectionTile", selectionTile);
            var maxEditHistory = initializationData.GetMaxEditHistory();
            FieldAccess.Set(__instance, "maxEditHistory", maxEditHistory);
            FieldAccess.Set(__instance, "rotateLeftKey", initializationData.GetRotateLeftKey());
            FieldAccess.Set(__instance, "rotateRightKey", initializationData.GetRotateRightKey());
            FieldAccess.Set(__instance, "mirrorXKey", initializationData.GetMirrorXKey());
            FieldAccess.Set(__instance, "mirrorYKey", initializationData.GetMirrorYKey());
            FieldAccess.Set(__instance, "undoKey", initializationData.GetUndoKey());
            FieldAccess.Set(__instance, "redoKey", initializationData.GetRedoKey());
            FieldAccess.Set(__instance, "clearKey", initializationData.GetClearKey());
            FieldAccess.Set(__instance, "inputSystem", inputSystem);
            FieldAccess.Set(__instance, "map", map);
            var selection = Selection.Create(map);
            FieldAccess.Set(__instance, "selection", selection);
            FieldAccess.Set(__instance, "selectionDrawer", SelectionDrawer.Create(selection, mapObjectTilemap, selectionTilemap, selectionTile, mapDrawerData));
            FieldAccess.Set(__instance, "editHistory", EditHistory.Create(maxEditHistory));
            FieldAccess.Set(__instance, "sampleMapObjects", new MapObject[8]
            {
                OrthogonalDomino.Create(map, simulator, Vector2Int.zero, Rotation.Right, FallState.Standing, RotationSet.None, Rotation.Right),
                DiagonalDomino.Create(map, simulator, Vector2Int.zero, Rotation.Right, FallState.Standing, RotationSet.None),
                ForkDomino.Create(map, simulator, Vector2Int.zero, Rotation.Right, FallState.Standing, RotationSet.None),
                Crossover.Create(map, simulator, Vector2Int.zero, Rotation.Right, RotationFallStateSet.None, RotationFallStateSet.None),
                Unfaller.Create(map, simulator, Vector2Int.zero, Rotation.Right, RotationFallStateSet.None, RotationFallStateSet.None),
                Trigger.Create(map, simulator, Vector2Int.zero, Rotation.Right, hasTriggered: false),
                ClickTrigger.Create(map, simulator, Vector2Int.zero, Rotation.Right, hitByClickTrigger: false, needsToTrigger: false),
                ResetDomino.Create(map, simulator, Vector2Int.zero, Rotation.Right, FallState.Standing, RotationSet.None, Rotation.Right)
            });
            FieldAccess.Get<Action>(__instance, "InitStateMachine")();
            applicationSystem.AddQuitAborter(__instance);
            selection.OnMapChanged += FieldAccess.Get<Action>(__instance, "OnMapChanged");
            projectMenuManager.OnProjectSelected += FieldAccess.Get<Action<List<MapObject>>>(__instance, "OnProjectSelected");
            projectMenuManager.OnProjectSaved += FieldAccess.Get<Action>(__instance, "OnProjectSaved");
            projectMenuManager.OnProjectLoaded += FieldAccess.Get<Action>(__instance, "OnProjectLoaded");
            Loader.Log("Added Reset Domino");
        }
    }
}