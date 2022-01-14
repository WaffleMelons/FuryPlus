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
    //This class is an alternate implementation of of the FuryPlus class. While the FuryPlus class focuses on
    //accomplishing the desired results in a short and easy manner, this class focuses instead on the use of
    //Reflection, for the sake of learning and experimenting. If additional changes are needed in the hooked
    //methods of FuryPlus, the existing implementation might not suffice, and thus an implemention as seen
    //in this class may be necessary. Initialize and Unload methods are commented to prevent this class from
    //being used in-game.
    [UsedImplicitly]
    public class FuryPlusAlt : Mod, ITogglableMod
    {
        public FuryPlusAlt() : base("Fury Plus Alt") { }


        public override string GetVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        
        public override void Initialize()
        {
            //LoadHooks
        }

        public void LoadHooks()
        {
            Log("Fury Plus Alt Initializing");

            ModHooks.TakeHealthHook += OnHealthTaken;
            ModHooks.HitInstanceHook += OnHit;
            On.HeroAnimationController.PlayIdle += OnPlayIdle;
            On.KnightHatchling.OnEnable += KnightHatchling_OnEnable;
        }

        //Removes the flashing sprite and extra damage of the Hatchlings (Glowing Womb minions)
        private void KnightHatchling_OnEnable(On.KnightHatchling.orig_OnEnable orig, KnightHatchling self)
        {
            //My first time using Reflection!

            //Vasi method 1
            //Mirror.SetField<KnightHatchling, bool>("dreamSpawn", true);

            //Vasi method 2
            ref bool dreamSpawn = ref Mirror.GetFieldRef<KnightHatchling, bool>(self, "dreamSpawn");

            //Non-Vasi method 1 (uses GetFieldInfo instead of GetField as seen in docs since the docs seemed to be wrong)
            //FieldInfo fi = ReflectionHelper.GetFieldInfo(typeof(KnightHatchling), "dreamSpawn");
            //fi.SetValue(khInstance, true);

            //Non-Vasi method 2 (uses SetField instead of SetAttr because the latter seems to not exist anymore)
            //ReflectionHelper.SetField<KnightHatchling, bool>(self, "dreamSpawn", true);

            if (GameManager.instance.entryGateName == "dreamGate")
            {
                dreamSpawn = true;
            }

            PlayerData playerData = GameManager.instance.playerData;

            ref KnightHatchling.TypeDetails details = ref Mirror.GetFieldRef<KnightHatchling, KnightHatchling.TypeDetails>(self, "details");
            details = ((!playerData.equippedCharm_10) ? self.normalDetails : self.dungDetails);

            ref AudioSource audioSource = ref Mirror.GetFieldRef<KnightHatchling, AudioSource>(self, "audioSource");
            if (audioSource)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
                if (self.loopClips.Length > 0)
                {
                    audioSource.clip = self.loopClips[UnityEngine.Random.Range(0, self.loopClips.Length)];
                }
            }

            //Fury-related section in the original method

            //if (playerData.equippedCharm_6 && playerData.health == 1 && (!playerData.equippedCharm_27 || playerData.healthBlue <= 0))
            //{
            //    ref SpriteFlash spriteFlash = ref Mirror.GetFieldRef<KnightHatchling, SpriteFlash>(self, "spriteFlash");
            //    if (spriteFlash)
            //    {
            //        spriteFlash.FlashingFury();
            //    }
            //    details.damage = details.damage + 5;
            //}


            if (self.dungPt)
            {
                if (details.dung && !dreamSpawn)
                {
                    self.dungPt.Play();
                }
                else
                {
                    self.dungPt.Stop();
                }
            }
            if (self.enemyRange)
            {
                self.enemyRange.gameObject.SetActive(false);
            }
            if (self.groundRange)
            {
                self.groundRange.gameObject.SetActive(true);
            }

            ref Collider2D col = ref Mirror.GetFieldRef<KnightHatchling, Collider2D>(self, "col");

            if (col)
            {
                col.enabled = false;
            }

            ref List<Collider2D> groundColliders = ref Mirror.GetFieldRef<KnightHatchling, List<Collider2D>>(self, "groundColliders");
            groundColliders.Clear();

            ref GameObject target = ref Mirror.GetFieldRef<KnightHatchling, GameObject>(self, "target");
            target = null;

            //The previously shown Vasi and Non-Vasi methods of reflection did not seem to work on the LastFrameState and CurrentState variables.
            //This could perhaps be due to the fact that these variables are not considered fields (at least according to dnSpy). It could also
            //be because they are public variables with private Setter methods, which may be a particularly unexpected behaviour to the
            //Reflection Helpers. The working solution below uses Microsoft's BindingFlags, which I believe the Reflection Helpers are based
            //on (https://docs.microsoft.com/en-us/dotnet/api/system.reflection.bindingflags?view=netframework-3.5)
            Type t = typeof(KnightHatchling);
            t.InvokeMember("LastFrameState", BindingFlags.SetProperty, null, self, new object[] { KnightHatchling.State.None });
            t.InvokeMember("CurrentState", BindingFlags.SetProperty, null, self, new object[] { KnightHatchling.State.None });

            if (self.terrainCollider)
            {
                self.terrainCollider.enabled = true;
            }

            ref MeshRenderer meshRenderer = ref Mirror.GetFieldRef<KnightHatchling, MeshRenderer>(self, "meshRenderer");
            if (meshRenderer)
            {
                meshRenderer.enabled = false;
            }

            self.StartCoroutine("Spawn");
        }

        //Adds the Hurt Animation when the player is at 1 health that would otherwise not play with Fury
        private void OnPlayIdle(On.HeroAnimationController.orig_PlayIdle orig, HeroAnimationController self)
        {

            if (PlayerData.instance.health == 1 && PlayerData.instance.healthBlue < 1)
            {
                //Fury-related section in the original method

                //if (PlayerData.instance.equippedCharm_6)
                //{
                //    self.animator.Play("Idle");
                //}
                //else
                //{
                self.animator.Play("Idle Hurt");
                //}
            }
            else if (self.animator.IsPlaying("LookUp"))
            {
                self.animator.Play("LookUpEnd");
            }
            else if (self.animator.IsPlaying("LookDown"))
            {
                self.animator.Play("LookDownEnd");
            }
            else if (HeroController.instance.wieldingLantern)
            {
                self.animator.Play("Lantern Idle");
            }
            else
            {
                self.animator.Play("Idle");
            }
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
            //UnloadHooks();
        }

        public void UnloadHooks()
        {
            ModHooks.TakeHealthHook -= OnHealthTaken;
            ModHooks.HitInstanceHook -= OnHit;
            On.HeroAnimationController.PlayIdle -= OnPlayIdle;
            On.KnightHatchling.OnEnable -= KnightHatchling_OnEnable;
        }
    }
}