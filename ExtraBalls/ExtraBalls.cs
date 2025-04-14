using System.Collections.Generic;
using System;
using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using EntityStates;
using EntityStates.Bell.BellWeapon;
using RoR2.Skills;

namespace ExtraBalls
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class ExtraBalls : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Skeletogne";
        public const string PluginName = "ExtraBalls";
        public const string PluginVersion = "0.0.0";

        public void Awake()
        {
            Log.Init(Logger);
            ModifySkillDef();
            ApplyILHook();
        }
        private static void ModifySkillDef()
        {
            SkillDef skillDef = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Bell/BellBodyBellBlast.asset").WaitForCompletion();
            skillDef.activationState = new SerializableEntityStateType(typeof(ChargeMultiBomb));
        }

        private static Transform GetCurrentBombTransform(ChargeMultiBomb newState, ChargeTrioBomb baseState)
        {
            int currentBombIndex = baseState.currentBombIndex;
            return newState.bombTransforms[currentBombIndex - 1];
        }

        private static void ApplyILHook()
        {
            IL.EntityStates.Bell.BellWeapon.ChargeTrioBomb.FixedUpdate += (il) =>
            {
                ILCursor c1 = new ILCursor(il);
                if (c1.TryGotoNext(
                    x => x.MatchLdcI4(3)
                ))
                {
                    c1.Index++;
                    c1.Emit(OpCodes.Ldarg_0);
                    c1.EmitDelegate<Func<int, ChargeTrioBomb, int>>((originalValue, baseState) =>
                    {
                        if (baseState is ChargeMultiBomb newState && newState != null)
                        {
                            return newState.bombTransforms.Count;
                        }
                        return originalValue;
                    });
                }
                else
                {
                    Log.Error($"IL.EntityStates.Bell.BellWeapon.ChargeTrioBomb.FixedUpdate cursor c1 failed to match!");
                }
                ILCursor c2 = new ILCursor(il);
                if (c2.TryGotoNext(
                    x => x.MatchStloc(0)
                ))
                {
                    c2.Emit(OpCodes.Ldarg_0);
                    c2.EmitDelegate<Func<Transform, ChargeTrioBomb, Transform>>((originalValue, baseState) =>
                    {
                        if (baseState is ChargeMultiBomb newState && newState != null)
                        {
                            return GetCurrentBombTransform(newState, baseState);
                        }
                        return originalValue;
                    });
                }
                else
                {
                    Log.Error($"IL.EntityStates.Bell.BellWeapon.ChargeTrioBomb.FixedUpdate cursor c2 failed to match!");
                }
                ILCursor c3 = new ILCursor(il);
                if (c3.TryGotoNext(
                    x => x.MatchStloc(3)
                ))
                {
                    c3.Emit(OpCodes.Ldarg_0);
                    c3.EmitDelegate<Func<Transform, ChargeTrioBomb, Transform>>((originalValue, baseState) =>
                    {
                        if (baseState is ChargeMultiBomb newState && newState != null)
                        {
                            return GetCurrentBombTransform(newState, baseState);
                        }
                        return originalValue;
                    });
                }
                else
                {
                    Log.Error($"IL.EntityStates.Bell.BellWeapon.ChargeTrioBomb.FixedUpdate cursor c3 failed to match!");
                }
                ILCursor c4 = new ILCursor(il);
                if (c4.TryGotoNext(
                    x => x.MatchLdsfld<ChargeTrioBomb>(nameof(ChargeTrioBomb.muzzleflashPrefab))
                ))
                {
                    c4.Emit(OpCodes.Ldarg_0);
                    c4.EmitDelegate<Action<ChargeTrioBomb>>((baseState) =>
                    {
                        if (baseState is ChargeMultiBomb newState && newState != null)
                        {
                            GameObject muzzleFlashPrefab = ChargeTrioBomb.muzzleflashPrefab;
                            GameObject baseObject = baseState.gameObject;
                            Transform transform = GetCurrentBombTransform(newState, baseState);
                            bool transmit = false;
                            if (!baseObject)
                                return;
                            EffectData effectData = new EffectData
                            {
                                origin = transform.position,
                            };
                            EffectManager.SpawnEffect(muzzleFlashPrefab, effectData, transmit);
                        }
                    });
                    Instruction target = c4.Clone().GotoNext(
                        x => x.MatchLdarg(0),
                        x => x.MatchLdarg(0),
                        x => x.MatchLdfld<ChargeTrioBomb>(nameof(ChargeTrioBomb.currentBombIndex)),
                        x => x.MatchLdcI4(1),
                        x => x.MatchSub()).Next;
                    c4.Emit(OpCodes.Br, target);
                }
                else
                {
                    Log.Error($"IL.EntityStates.Bell.BellWeapon.ChargeTrioBomb.FixedUpdate cursor c4 failed to match!");
                }
            };
        }
    }
    public class ChargeMultiBomb : ChargeTrioBomb
    {
        public List<Transform> bombTransforms = new List<Transform>();
        public int bombCount;
        public override void OnEnter()
        {
            base.OnEnter();
            //bomb count set here
            bombCount = 5;
            //time between skill state start and first bomb getting fired (all of these are dependent on attack speed!)
            base.prepDuration *= 2f;
            //time between individual bomb preps
            base.timeBetweenPreps *= 2f;
            //time between first bomb getting fired and returning to main state (skill is considered finished)
            base.barrageDuration *= 2f;
            //time between individual bomb fires
            base.timeBetweenBarrages *= 2f;
            GenerateTransforms();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
        }
        public override void OnExit()
        {
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            //prevents brass contraptions from interrupting their own attacks
            return InterruptPriority.Skill;
        }
        public void GenerateTransforms()
        {
            if (bombCount == 0)
            {
                return;
            }
            var baseRadius = 3.8f;
            float startingPos;
            int bombsToDistribute = bombCount;
            int currentRingSize = 8;
            float radius = baseRadius;
            while (bombsToDistribute > 0)
            {
                float currentStepSize = 2 * Mathf.PI / currentRingSize;
                if (bombsToDistribute >= currentRingSize)
                {
                    startingPos = 0f;
                }
                else
                {
                    startingPos = 0f - (currentStepSize / 2) * (bombsToDistribute - 1);
                }
                float currentPos = startingPos;
                int remainingBombs = bombsToDistribute;
                for (int i = 0; i < Mathf.Min(currentRingSize, remainingBombs); i++)

                {
                    float x = Mathf.Sin(currentPos) * radius;
                    float y = Mathf.Cos(currentPos) * radius;
                    float z = 0f;
                    Vector3 vector = new Vector3(x, y, z);
                    GameObject newGameObject = new GameObject();
                    newGameObject.transform.parent = base.transform;
                    newGameObject.transform.localPosition = vector;
                    newGameObject.transform.localScale = Vector3.one;
                    newGameObject.transform.rotation = Quaternion.identity;
                    Transform newTransform = newGameObject.transform;
                    bombTransforms.Add(newTransform);
                    currentPos += currentStepSize;
                    bombsToDistribute--;
                }
                currentRingSize += 4;
                radius += 3.8f;
            }
        }
    }
}

