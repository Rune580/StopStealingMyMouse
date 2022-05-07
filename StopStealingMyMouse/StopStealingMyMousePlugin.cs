using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using RoR2;
using UnityEngine;
using Monomod.RuntimeDetour;

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
        private Hook hook;
        
        private void Awake()
        {
            var targetMethod = typeof(RoR2.MPEventSystemManager).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var overridingMethod = GetType().GetMethod(nameof(MPEventSystemManagerOnUpdate), BindingFlags.NonPublic | BindingFlags.Instance);
            hook = new Hook(targetMethod, overridingMethod);
            
            HGButton.OnClickCustom += HGButtonOnOnClickCustom;
        }

        private void HGButtonOnOnClickCustom(HGButton.orig_OnClickCustom orig, RoR2.UI.HGButton self)
        {
            if (_arrestRoR2)
                _arrestRoR2 = false;

            orig(self);
        }

        private void MPEventSystemManagerOnUpdate(MPEventSystemManager.orig_Update orig, RoR2.MPEventSystemManager self)
        {
            if (_arrestRoR2)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = false;
                return;
            }
            
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
