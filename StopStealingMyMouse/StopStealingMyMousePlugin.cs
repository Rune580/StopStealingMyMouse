using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using RoR2;
using UnityEngine;
using MonoMod.RuntimeDetour;
using RoR2.UI;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

namespace StopStealingMyMouse
{
    [BepInPlugin(Guid, ModName, Version)]
    public sealed class StopStealingMyMousePlugin : BaseUnityPlugin
    {
        private const string
            ModName = "Stop stealing my mouse!",
            Author = "rune580",
            Guid = "com." + Author + "." + "stopstealingmymouse",
            Version = "1.1.0";
        
        private bool _arrestRoR2 = true;
        private Hook _mpEventSystemManagerHook;
        private Hook _hgButtonHook;

        private void Awake()
        {
            ArrestRoR2();
        }

        private void ArrestRoR2()
        {
            var targetMethod = typeof(MPEventSystemManager).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var overridingMethod = GetType().GetMethod(nameof(MPEventSystemManagerOnUpdate), BindingFlags.NonPublic | BindingFlags.Instance);

            _mpEventSystemManagerHook = new Hook(targetMethod, overridingMethod, this);

            targetMethod = typeof(HGButton).GetMethod("OnClickCustom", BindingFlags.Public | BindingFlags.Instance);
            overridingMethod = GetType().GetMethod(nameof(HGButtonOnOnClickCustom), BindingFlags.NonPublic | BindingFlags.Instance);

            _hgButtonHook = new Hook(targetMethod, overridingMethod, this);
        }

        private void ReleaseRoR2()
        {
            _mpEventSystemManagerHook.Undo();
            _hgButtonHook.Undo();
        }

        private void MPEventSystemManagerOnUpdate(Action<MPEventSystemManager> orig, MPEventSystemManager self)
        {
            if (_arrestRoR2)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = false;
                return;
            }
            
            orig(self);
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
}
