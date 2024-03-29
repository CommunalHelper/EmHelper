﻿using Celeste.Mod.EmHelper.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.ModInterop;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;

namespace Celeste.Mod.EmHelper.Module {
    public class EmHelper : EverestModule {
        public static EmHelper Instance;
        private ILHook PlayerOrig_updateHook; //IL hook, used to modify the player's code so walkelines can interact with triggers
        public bool LevelHasTriggerHappyWalkeline = false;

        public EmHelper() {
            Instance = this;
        }

        public override void Load() {
            On.Celeste.LevelLoader.ctor += LevelLoader_ctor;
            On.Celeste.CassetteBlock.BlockedCheck += Walkeline_BlockedCheck;
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
            PlayerOrig_updateHook = new ILHook(typeof(Player).GetMethod("orig_Update"), Walkeline_triggerupdate); //i want to swallow burning coals, used to let walkelines interact with triggers, have no idea how it works anymore

            typeof(GravityHelperImports).ModInterop();
        }

        // Resets walkeline trigger check whenever we change levels. Set to "true" in Walkeline.Awake() if trigger-enabled
        // We can't just check the tracker for Walkelines here since it can contain Walkelines from the previous screen
        private void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            LevelHasTriggerHappyWalkeline = false;
            orig(self, playerIntro, isFromLoader);
        }

        private void Walkeline_triggerupdate(ILContext il) {
            ILCursor cursor = new(il);
            //go to (base.CollideCheck(trigger) instr => instr.MatchLdloc(0), instr => instr.MatchLdloc(10), 
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdarg(0), instr => instr.MatchLdloc(10), instr => instr.MatchCall<Entity>("CollideCheck"))) {
                cursor.Emit(OpCodes.Ldarg_0); //adds the player to the stack, thanks max!
                cursor.Emit(OpCodes.Ldloc_S, il.Body.Variables[10]); //adds the trigger to the stack, thanks again max!
                cursor.EmitDelegate<Func<bool, Player, Trigger, bool>>((collided, player, trigger) => {
                    return collided || (LevelHasTriggerHappyWalkeline && CheckWalkelinesInsideTrigger(player, trigger));
                });
            }
        }

        private bool CheckWalkelinesInsideTrigger(Player player, Trigger trigger) {
            if (trigger != null) { //never too sure
                foreach (Walkeline walkeline in player.Scene.Tracker.GetEntities<Walkeline>()) { //i could swap the player with the trigger, but im not sure that will work
                    if (walkeline != null && trigger.CollideCheck(walkeline) && walkeline.TriggerHappy) { //triggerhappy is just a flag in the walke's code to check if it should collide with triggers
                        return true; //there's at least a walkeline inside the trigger
                    }
                }
            }

            return false; //no walkelines inside the trigger
        }

        private void LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startPosition) {
            orig(self, session, startPosition);
            PlayerSprite.CreateFramesMetadata("walkeline");
            PlayerSprite.CreateFramesMetadata("windupwalkeline");
        }

        private static readonly MethodInfo TryActorWiggleUp = typeof(CassetteBlock).GetMethod("TryActorWiggleUp", BindingFlags.Instance | BindingFlags.NonPublic);
        private bool Walkeline_BlockedCheck(On.Celeste.CassetteBlock.orig_BlockedCheck orig, CassetteBlock self) {
            Walkeline walkeline = self.CollideFirst<Walkeline>();
            return (walkeline != null && !(bool)TryActorWiggleUp.Invoke(self, new object[] { walkeline })) || orig(self);
        }

        public override void Unload() {
            On.Celeste.LevelLoader.ctor -= LevelLoader_ctor;
            On.Celeste.CassetteBlock.BlockedCheck -= Walkeline_BlockedCheck;
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
            PlayerOrig_updateHook?.Dispose();
            PlayerOrig_updateHook = null;
        }
    }
}
