using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using MonoMod.RuntimeDetour;
using RoR2.UI;
using Object = UnityEngine.Object;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace StopStealingMyMouse;

[BepInPlugin(Guid, ModName, Version)]
public sealed class StopStealingMyMousePlugin : BaseUnityPlugin
{
    private const string
        ModName = "Stop stealing my mouse!",
        Author = "rune580",
        Guid = "com." + Author + "." + "stopstealingmymouse",
        Version = "1.3.0";
        
    private static bool _arrestRoR2 = true;
    private ILHook _mpEventSystemManagerHook;
    private Hook _hgButtonHook;

    private void Awake()
    {
        ArrestRoR2();
    }

    private void ArrestRoR2()
    {
        var targetMethod = typeof(MPEventSystemManager).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        _mpEventSystemManagerHook = new ILHook(targetMethod, MPEventSystemManagerOnUpdate);

        targetMethod = typeof(HGButton).GetMethod("OnClickCustom", BindingFlags.Public | BindingFlags.Instance);
        var overridingMethod = GetType().GetMethod(nameof(HGButtonOnOnClickCustom), BindingFlags.NonPublic | BindingFlags.Instance);

        _hgButtonHook = new Hook(targetMethod, overridingMethod, this);
    }

    private void ReleaseRoR2()
    {
        _mpEventSystemManagerHook.Undo();
        _hgButtonHook.Undo();
    }

    private void MPEventSystemManagerOnUpdate(ILContext il)
    {
        var cursor = new ILCursor(il)
            .Goto(0);
        
        if (cursor.TryGotoNext(MoveType.After,
                i => i.MatchCall<Application>("get_isBatchMode"),
                i => i.MatchBrtrue(out _),
                i => i.MatchCall<MPEventSystemManager>("get_kbmEventSystem"),
                i => i.MatchLdnull(),
                i => i.MatchCall<Object>("op_Inequality"),
                i => i.MatchBrfalse(out _)
            ))
        {
            cursor.Emit(OpCodes.Call, typeof(StopStealingMyMousePlugin).GetMethod(nameof(PreventTheft), BindingFlags.NonPublic | BindingFlags.Static));
            cursor.Emit(OpCodes.Ret);
        }
    }

    private static void PreventTheft()
    {
        if (!_arrestRoR2)
            return;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;
    }
        
    private void HGButtonOnOnClickCustom(Action<HGButton> orig, HGButton self)
    {
        if (_arrestRoR2)
            ReleaseRoR2();

        orig(self);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (_arrestRoR2 && RoR2Application.loadFinished && hasFocus)
        {
            _arrestRoR2 = false;
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }
    }
}