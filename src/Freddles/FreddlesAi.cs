﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using GameNetcodeStuff;
using NightmareFreddy.Configurations;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace NightmareFreddy.Freddles;

public class FreddlesAi : EnemyAI
{
    
    [Header("Material Handler!")]
    public Material endoMaterial;
    private Material copiedEndoMaterial;
    public Material coverMaterial;
    private Material copiedCoverMaterial;
    public SkinnedMeshRenderer FreddlesRenderer;
    [Header("Audio")] 
    public AudioClip glitchProj;
    public AudioClip paranoidStep;
    [NonSerialized] 
    public Vector3 destination;

    [NonSerialized] 
    public bool arrived;
    private bool sittingOrLying;
    private int burnTick;
    private int currentBurnProgress;
    private bool canCollide = false;
    enum State {
        Running,
        LookedAt,
        Idle,
        Burning,
        Aggressive,
        TrueBurn,
        None
    }

    public void Awake()
    {
        if (endoMaterial != null)
        {
            copiedEndoMaterial = new Material(endoMaterial);
        }

        if (coverMaterial != null)
        {
            copiedCoverMaterial = new Material(coverMaterial);

        }
        FreddlesRenderer.materials =
        [
            copiedCoverMaterial,
            copiedEndoMaterial
        ];
    }

    public override void Start()
    {
        base.Start();
        int attempts = 0;
        arrived = false;
        //TEMPORARY MUST BE MODIFIED
        destination = Vector3.zero;
        while (destination==Vector3.zero && attempts<10)
        {
            attempts++;
            destination = GetRandomPointInCollider(StartOfRound.Instance.shipInnerRoomBounds);
            if (destination != null )
            {
                // Create a new NavMeshPath instance
                NavMeshPath path = new NavMeshPath();

                // Calculate the path to the destination
                NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);

                // Check if the path is valid
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    
                }
                else
                {
                    destination = Vector3.zero;
                    break;
                }
            }
            else
            {
                
            }
            
        }

        if (attempts == 10)
        {
            StartCoroutine(DestroySequence(0));
        }
        //Setting if it is a sitting frebeard
        if (RandomNumberGenerator.GetInt32(2)==0)
        {
            sittingOrLying = true;
        }
        else
        {
            sittingOrLying = false;
        }
        PlayAnimationClientRpc("Sit",sittingOrLying);
        SwitchCurrentBehaviourClientRpc((int)State.Running);
        
        burnTick = RandomNumberGenerator.GetInt32(10,20);
        currentBurnProgress = 0;
        StartCoroutine(DestroySequence(30));
    }
    public static Vector3 GetRandomPointInCollider(Collider collider)
    {
        // Get bounds of the collider
        Bounds bounds = collider.bounds;

        // Generate a random point within the bounds
        Vector3 randomPoint = new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );

        // Sample the position on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 5.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        else
        {
            // If no valid position found, return the center of the collider
            return bounds.center;
        }
    }
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        switch(currentBehaviourStateIndex) {
            case (int)State.Running :
                agent.isStopped = false;
                if (creatureAnimator.GetBool("Idle"))
                {
                    PlayAnimationClientRpc("Idle",false);

                }


                SetDestinationToPosition(destination);
                if (Vector3.Distance(transform.position, destination) <1f)
                {
                    ModifyMaterialClientRpc(0);
                    SwitchCurrentBehaviourClientRpc((int)State.Idle);
                }

                if (CheckIfLookedAt())
                {  
                    SwitchCurrentBehaviourClientRpc((int)State.LookedAt);
                }
                break;
            case (int)State.LookedAt :
                agent.ResetPath();
                agent.isStopped = true;
                if (!creatureAnimator.GetBool("Idle"))
                {
                    PlayAnimationClientRpc("Idle",true);

                }

                if (copiedEndoMaterial.GetFloat("_Dissolve") == 0)
                {
                    ModifyMaterialClientRpc(0);
                    if (IsHost)
                    {
                        PlayGlitchClientRpc(false);
                    }
                }
                if (currentBurnProgress > 0)
                {
                    currentBurnProgress -= 1;
                    ModifyMaterialBasedOnBurnProgress();
                }
                if (CheckIfFlashedAt())
                {
                    if (IsHost)
                    {
                        PlayGlitchClientRpc(true);
                    }
                    SwitchCurrentBehaviourClientRpc((int)State.TrueBurn);
                    agent.ResetPath();
                }
                
                if (!CheckIfLookedAt())
                {  
                    SwitchCurrentBehaviourClientRpc((int)State.Running);
                }
                
                break;
            case (int)State.Idle :
                arrived = true;
                agent.isStopped = true;
                if (IsHost)
                {
                    if (RandomNumberGenerator.GetInt32(100) <= 2)
                    {
                        PlayFootStepNoiseClientRpc();
                    }
                }
                if (!creatureAnimator.GetBool("Idle"))
                {
                    PlayAnimationClientRpc("Idle",true);
                }

                if (IsHost)
                {
                    if (RandomNumberGenerator.GetInt32(100) == 0)
                    {
                        if (!CheckIfLookedAt())
                        {
                            PlayFootStepNoiseClientRpc();
                        }
                    }
                }
                if (copiedEndoMaterial.GetFloat("_Dissolve") == 0)
                {
                    ModifyMaterialClientRpc(0);
                    if (IsHost)
                    {
                        PlayGlitchClientRpc(false);
                    }
                }
                if (currentBurnProgress > 0)
                {
                    currentBurnProgress -= 1;
                    ModifyMaterialBasedOnBurnProgress();
                }
                if (CheckIfFlashedAt())
                {
                    if (IsHost)
                    {
                        PlayGlitchClientRpc(true);
                    }
                    SwitchCurrentBehaviourClientRpc((int)State.Burning);
                }
                break;
            case (int)State.Burning :
                agent.isStopped = true;
                if (CheckIfFlashedAt())
                {
                    if (!creatureAnimator.GetBool("Burning"))
                    {
                        PlayAnimationClientRpc("Burning",true);
                    }
                    if (currentBurnProgress < burnTick)
                    {
                        currentBurnProgress += 1;
                        ModifyMaterialBasedOnBurnProgress();
                    }
                    else
                    {
                        SwitchCurrentBehaviourClientRpc((int)State.None);
                        StartCoroutine(DestroySequence(3));
                    }
                }
                else
                {
                    if (arrived)
                    {
                        SwitchCurrentBehaviourClientRpc((int)State.Idle);
                    }
                    else
                    {
                        SwitchCurrentBehaviourClientRpc((int)State.LookedAt);
                    }
                }
                break;
            case (int)State.Aggressive :
                agent.isStopped = false;
                if (CheckIfFlashedAt())
                {
                    if (IsHost)
                    {
                        PlayGlitchClientRpc(true);
                    }
                    PlayAnimationClientRpc("Idle",true);
                    SwitchCurrentBehaviourClientRpc((int)State.TrueBurn);
                    agent.ResetPath();
                    break;
                }
                if (creatureAnimator.GetBool("Idle"))
                {
                    PlayAnimationClientRpc("Idle",false);
                }

                TargetClosestPlayer();
                if (targetPlayer != null)
                {
                    SetDestinationToPosition(targetPlayer.transform.position);
                    SwingAttackHitClientRpc();
                }
                else
                {
                    SetDestinationToPosition(destination);
                }
                ModifyMaterialBasedOnBurnProgress();
                
                break;
            case(int)State.TrueBurn :
                agent.isStopped = true;
                if (!creatureAnimator.GetBool("Burning"))
                {
                    PlayAnimationClientRpc("Burning",true);

                }
                if (currentBurnProgress < burnTick)
                {
                    currentBurnProgress += 1;
                    ModifyMaterialBasedOnBurnProgress();
                }
                else
                {
                    SwitchCurrentBehaviourClientRpc((int)State.None);
                    StartCoroutine(DestroySequence(3));
                }
                break;
            case(int)State.None :
                
                break;
            default:
                break;
        }
    }

    public bool CheckIfLookedAt()
    {
        foreach (var player in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if (player.HasLineOfSightToPosition(transform.position))
            { ;
                return true;
            }
        }
        return false;
    }
    
    public bool CheckIfFlashedAt()
    {
        foreach (var player in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if (!player.HasLineOfSightToPosition(transform.position)) continue;
            foreach (var item in player.ItemSlots)
            {
                if (item == null) continue;
                if (item.gameObject.GetComponent<FlashlightItem>() == null) continue;
                if (!item.gameObject.GetComponent<FlashlightItem>().isBeingUsed) continue;
                if ( !(Vector3.Distance(player.transform.position, transform.position) < 6f)) continue;
                if (!(player.LineOfSightToPositionAngle(transform.position) < 20)) continue;
                return true;
            }
        }
        return false;
    }
    //Material Vlaue modifier
    public void ModifyMaterialBasedOnBurnProgress()
    {
        float x = Mathf.Clamp01((float)currentBurnProgress / burnTick);
        ModifyMaterialClientRpc(x);
        if (currentBurnProgress == 0)
        {
            PlayAnimationClientRpc("Burning",false);
        }
    }
    [ClientRpc]
    public void ModifyMaterialClientRpc(float x)
    {
        if (x < 1.01f && x >= 0.0f)
        {
            copiedEndoMaterial.SetFloat("_Dissolve",x);
            copiedCoverMaterial.SetFloat("Vector1_FEFF47F1",x);
        }
        
    }

    [ClientRpc]
    public void SwitchCurrentBehaviourClientRpc(int x)
    {
        currentBehaviourStateIndex = x;
    }
    //AUDIO RPC
    public void PlayFootStep()
    {
        creatureSFX.Play();
    }
    
    [ClientRpc]
    public void PlayFootStepNoiseClientRpc()
    {
        creatureSFX.PlayOneShot(paranoidStep);
        
    }
    [ClientRpc]
    public void PlayGlitchClientRpc(bool x)
    {
        if (x)
        {
            if (!creatureVoice.isPlaying)
            {
                creatureVoice.Play();
            }
        }
        else
        {
            creatureVoice.Stop();
        }
    }
    

    IEnumerator DestroySequence( int x)
    {

        yield return new WaitForSeconds(x);
        if (currentBehaviourStateIndex == 2)
        {
            
        }
        else
        {
            KillEnemyClientRpc(true);
        }
        
    }

    public void ActivateDangerousMod()
    {
        SwitchCurrentBehaviourClientRpc((int)State.Aggressive);
        canCollide = true;
        StopCoroutine(DestroySequence(30));
    }
    [ClientRpc]
    public void SwingAttackHitClientRpc() {
        if (canCollide)
        {
            int playerLayer = 1 << 3; // This can be found from the game's Asset Ripper output in Unity
            var transform1 = transform;
            Collider[] hitColliders = Physics.OverlapBox(transform1.position, transform1.localScale, Quaternion.identity, playerLayer);
            if(hitColliders.Length > 0){
                foreach (var player in hitColliders){
                    PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(player);
                    if (playerControllerB != null)
                    {
                        playerControllerB.DamagePlayer(Plugin.Plugin.FreddyConfiguration.DAMAGE_FREDDLES.Value);
                        PlayAnimationClientRpc("Idle",true);
                        SwitchCurrentBehaviourClientRpc((int)State.TrueBurn);
                    }
                }
            }
        }
    }
    /// <summary>
    /// When he gets hit, will be called
    /// </summary>
    /// <param name="force"></param>
    /// <param name="playerWhoHit"></param>
    /// <param name="playHitSFX"></param>
    /// <param name="hitID"></param>
    public override void HitEnemy(int force = 1, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1) {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
        if(isEnemyDead){
            return;
        }
        enemyHP -= force;
        if (IsOwner) {
            if (enemyHP <= 0 && !isEnemyDead) {
                // Our death sound will be played through creatureVoice when KillEnemy() is called.
                // KillEnemy() will also attempt to call creatureAnimator.SetTrigger("KillEnemy"),
                // so we don't need to call a death animation ourselves.
                //StopCoroutine(SwingAttack());
                // We need to stop our search coroutine, because the game does not do that by default.
                SwitchCurrentBehaviourClientRpc((int)State.TrueBurn);
            }
        }
    }
    [ClientRpc]
    public void PlayAnimationClientRpc(string name, bool value)
    {
        creatureAnimator.SetBool(name, value);
    }
}