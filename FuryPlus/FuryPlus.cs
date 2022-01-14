using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using JetBrains.Annotations;
using Modding;
using Mono.Cecil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using On;
using UnityEngine;
using Vasi;
using ReflectionHelper = Modding.ReflectionHelper;

namespace FuryPlus
{
    //This class removes the original Fury of the Fallen functionality and instead makes it a charm that doubles
    //your damage while also doubling the damage you receive. The removal of each functionality is based on 
    //telling the game you do not have Fury equipped when you actually do. If perchance another method (be it 
    //native or from a mod) read this value at the same time, this could cause problems, though it seems unlikely.
    //In the occurence of such or other problems, the much more convoluted FuryPlusAlt class can be used, available
    //in this same project, although it only features the first two methods of this class.
    [UsedImplicitly]
    public class FuryPlus : Mod, ITogglableMod
    {
        public FuryPlus() : base("Fury Plus") { }


        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        
        public override void Initialize()
        {
            Log("Fury Plus Initializing");

            ModHooks.TakeHealthHook += OnHealthTaken;
            ModHooks.HitInstanceHook += OnHit;
            On.HeroAnimationController.PlayIdle += OnPlayIdle;
            On.KnightHatchling.OnEnable += KnightHatchling_OnEnable;


        }

        //Removes the flashing sprite and extra damage of the Hatchlings (Glowing Womb minions)
        private void KnightHatchling_OnEnable(On.KnightHatchling.orig_OnEnable orig, KnightHatchling self)
        {

            bool orig_equippedCharm_6 = PlayerData.instance.equippedCharm_6;

            PlayerData.instance.equippedCharm_6 = false;

            orig(self);

            PlayerData.instance.equippedCharm_6 = orig_equippedCharm_6;

        }

        //Adds the Hurt Animation when the player is at 1 health that would otherwise not play with Fury
        private void OnPlayIdle(On.HeroAnimationController.orig_PlayIdle orig, HeroAnimationController self)
        {

            bool orig_equippedCharm_6 = PlayerData.instance.equippedCharm_6;

            PlayerData.instance.equippedCharm_6 = false;

            orig(self);

            PlayerData.instance.equippedCharm_6 = orig_equippedCharm_6;
        }

        //Doubles damage dealt
        private HitInstance OnHit(Fsm owner, HitInstance hit)
        {
            if (PlayerData.instance.equippedCharm_6) { 
            
                hit.DamageDealt *= 2;
            }

            return hit;
        }

        //Doubles damage taken
        private int OnHealthTaken(int damage)
        {
            if (PlayerData.instance.equippedCharm_6)
            {
                return damage * 2;
            }

            return damage;
        }


        public void Unload()
        {
            ModHooks.TakeHealthHook -= OnHealthTaken;
            ModHooks.HitInstanceHook -= OnHit;
            On.HeroAnimationController.PlayIdle -= OnPlayIdle;
            On.KnightHatchling.OnEnable -= KnightHatchling_OnEnable;
        }
    }
}