using System.Collections.Generic;
using System;
using BepInEx;
using RoR2;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using EntityStates.Bell.BellWeapon;

namespace ExtraBalls
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class ExtraBalls : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Skeletogne";
        public const string PluginName = "ExtraBalls";
        public const string PluginVersion = "0.0.0";

        public void OnEnable()
        {
            On.EntityStates.Bell.BellWeapon.ChargeTrioBomb.OnExit += ClearCachedInformation;
        }
        public void OnDisable()
        {
            On.EntityStates.Bell.BellWeapon.ChargeTrioBomb.OnExit -= ClearCachedInformation;
        }
        private void ClearCachedInformation(On.EntityStates.Bell.BellWeapon.ChargeTrioBomb.orig_OnExit orig, ChargeTrioBomb self)
        {
            if (cachedBellInformation.TryGetValue(self, out _) == true)
            {
                cachedBellInformation.Remove(self);
            }
            orig(self);
        }
        public void Awake()
        {
            Log.Init(Logger);
            ApplyILHook();
        }
        private static Dictionary<ChargeTrioBomb, List<Transform>> cachedBellInformation = new Dictionary<ChargeTrioBomb, List<Transform>>();
        private static List<Transform> GenerateTransformList(int count, ChargeTrioBomb baseState)
        {
            if (count == 0)
            {
                return null;
            }
            var list = new List<Transform>();
            var baseRadius = 3.8f;
            float startingPos;
            int bombsToDistribute = count;
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
                    newGameObject.transform.parent = baseState.transform;
                    newGameObject.transform.localPosition = vector;
                    newGameObject.transform.localScale = Vector3.one;
                    newGameObject.transform.rotation = Quaternion.identity;
                    Transform newTransform = newGameObject.transform;
                    list.Add(newTransform);
                    currentPos += currentStepSize;
                    bombsToDistribute--;
                }
                currentRingSize += 4;
                radius += 3.8f;
            }
            return list;
        }
        private static void ApplyILHook()
        {
            IL.EntityStates.Bell.BellWeapon.ChargeTrioBomb.FixedUpdate += (il) =>
            {
                ILCursor c0 = new ILCursor(il);
                c0.Emit(OpCodes.Ldarg_0);
                c0.EmitDelegate<Action<ChargeTrioBomb>>((baseState) =>
                {
                    if (baseState != null && !cachedBellInformation.TryGetValue(baseState, out _))
                    {
                        //this is where you input how many bombs this instance of the attack will fire. set high at your own peril.
                        int localBombCount = 5;
                        List<Transform> list = GenerateTransformList(localBombCount, baseState);
                        float actionSpeedModifier = Mathf.Max(localBombCount / 3, 1);
                        baseState.timeBetweenPreps /= actionSpeedModifier;
                        baseState.timeBetweenBarrages /= actionSpeedModifier;
                        cachedBellInformation.Add(baseState, list);
                    }
                });
                ILCursor c1 = new ILCursor(il);
                if (c1.TryGotoNext(
                    x => x.MatchLdcI4(3)
                ))
                {
                    c1.Index++;
                    c1.Emit(OpCodes.Ldarg_0);
                    c1.EmitDelegate<Func<int, ChargeTrioBomb, int>>((originalValue, baseState) =>
                    {
                        if (baseState != null)
                        {
                            List<Transform> list = new List<Transform>();
                            cachedBellInformation.TryGetValue(baseState, out list);
                            return list.Count;
                        }
                        return originalValue;
                    });
                }
                else
                {
                    Log.Error($"c1 failed!");
                }
                ILCursor c2 = new ILCursor(il);
                if (c2.TryGotoNext(
                    x => x.MatchStloc(0)
                ))
                {
                    c2.Emit(OpCodes.Ldarg_0);
                    c2.EmitDelegate<Func<Transform, ChargeTrioBomb, Transform>>((originalValue, baseState) =>
                    {
                        if (baseState != null)
                        {
                            int currentBombIndex = baseState.currentBombIndex;
                            List<Transform> list = new List<Transform>();
                            cachedBellInformation.TryGetValue(baseState, out list);
                            //currentBombIndex starts from 1! 
                            return list[currentBombIndex - 1];
                        }
                        return originalValue;
                    });
                }
                else
                {
                    Log.Error($"c2 failed!");
                }
                ILCursor c3 = new ILCursor(il);
                if (c3.TryGotoNext(
                    x => x.MatchStloc(3)
                ))
                {
                    c3.Emit(OpCodes.Ldarg_0);
                    c3.EmitDelegate<Func<Transform, ChargeTrioBomb, Transform>>((originalValue, baseState) =>
                    {
                        int currentBombIndex = baseState.currentBombIndex;
                        List<Transform> list = new List<Transform>();
                        cachedBellInformation.TryGetValue(baseState, out list);
                        return list[currentBombIndex-1];
                    });
                }
                else
                {
                    Log.Error($"c3 failed!");
                }
                ILCursor c4 = new ILCursor(il);
                if (c4.TryGotoNext(
                    x => x.MatchLdsfld<ChargeTrioBomb>(nameof(ChargeTrioBomb.muzzleflashPrefab))
                ))
                {
                    c4.Emit(OpCodes.Ldarg_0);
                    c4.EmitDelegate<Action<ChargeTrioBomb>>((baseState) =>
                    {
                        if (baseState != null)
                        {
                            GameObject muzzleFlashPrefab = ChargeTrioBomb.muzzleflashPrefab;
                            GameObject baseObject = baseState.gameObject;
                            List<Transform> list = new List<Transform>();
                            cachedBellInformation.TryGetValue(baseState, out list);
                            Transform transform = list[baseState.currentBombIndex - 1];
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
                    Log.Error($"c4 failed!");
                }
            };
        }
    }
}

