using System.Linq;
using UnityEngine;

namespace ProceduralTools;

public class DragCubeTool : MonoBehaviour
{
    public Part part;

    public static DragCubeTool UpdateDragCubes(Part p, bool immediate = false)
    {
        var tool = p.GetComponent<DragCubeTool>();
        if (tool == null)
        {
            tool = p.gameObject.AddComponent<DragCubeTool>();
            tool.part = p;
            if (immediate && tool.Ready())
                tool.UpdateCubes();
        }

        return tool;
    }

    public void FixedUpdate()
    {
        if (Ready())
            UpdateCubes();
    }

    public bool Ready()
    {
        if (HighLogic.LoadedSceneIsFlight)
            return FlightGlobals.ready; //&& !part.packed && part.vessel.loaded;
        if (HighLogic.LoadedSceneIsEditor)
            return part.localRoot == EditorLogic.RootPart &&
                   part.gameObject.layer != LayerMask.NameToLayer("TransparentFX");
        return true;
    }

    private void UpdateCubes()
    {
        if (FARInstalled)
            part.SendMessage("GeometryPartModuleRebuildMeshData");
        var dragCube = DragCubeSystem.Instance.RenderProceduralDragCube(part);
        part.DragCubes.ClearCubes();
        part.DragCubes.Cubes.Add(dragCube);
        part.DragCubes.ResetCubeWeights();
        part.DragCubes.ForceUpdate(true, true, false);
        part.DragCubes.SetDragWeights();
        Destroy(this);
    }

    private static bool? _FARInstalled;

    public static bool FARInstalled
    {
        get
        {
            _FARInstalled ??= AssemblyLoader.loadedAssemblies.Any(a =>
                a.assembly.GetName().Name == "FerramAerospaceResearch");
            return _FARInstalled.Value;
        }
    }
}
