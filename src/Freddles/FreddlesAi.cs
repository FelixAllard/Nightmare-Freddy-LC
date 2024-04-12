using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace NightmareFreddy.Freddles;

public class FreddlesAi : EnemyAI
{
    public Material endoMaterial;
    public Material coverMaterial;
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
    enum State {
        Running,
        LookedAt,
        Idle,
        Burning
    }

    public override void Start()
    {
        base.Start();

        arrived = false;
        //TEMPORARY MUST BE MODIFIED
        destination = Vector3.zero;
        while (destination==Vector3.zero)
        {
            destination = GetRandomPointInCollider(StartOfRound.Instance.shipInnerRoomBounds);
            if (destination != null)
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
                }
            }
            else
            {
                Debug.LogWarning("Destination is not set!");
            }
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
        creatureAnimator.SetBool("Sit", sittingOrLying);
        SwitchCurrentBehaviourClientRpc((int)State.Running);
        
        burnTick = RandomNumberGenerator.GetInt32(10,20);
        currentBurnProgress = 0;
        //TODO get the sitting position
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
                if (creatureAnimator.GetBool("Idle"))
                {
                    creatureAnimator.SetBool("Idle",false);
                }
                SetDestinationToPosition(destination);
                
                if (Vector3.Distance(transform.position, destination) <1f)
                {
                    ModifyMaterial(0);
                    SwitchCurrentBehaviourClientRpc((int)State.Idle);
                }

                if (CheckIfLookedAt())
                {  
                    SwitchCurrentBehaviourClientRpc((int)State.LookedAt);
                }
                break;
            case (int)State.LookedAt :
                agent.ResetPath();
                if (!creatureAnimator.GetBool("Idle"))
                {
                    creatureAnimator.SetBool("Idle",true);
                }
                if (currentBurnProgress > 0)
                {
                    currentBurnProgress -= 1;
                    ModifyMaterialBasedOnBurnProgress();
                }
                if (!CheckIfLookedAt())
                {  
                    SwitchCurrentBehaviourClientRpc((int)State.Running);
                }
                break;
            case (int)State.Idle :
                arrived = true;
                if (IsHost)
                {
                    if (RandomNumberGenerator.GetInt32(100) <= 2)
                    {
                        PlayFootStepNoiseClientRpc();
                    }
                }
                if (!creatureAnimator.GetBool("Idle"))
                {
                    creatureAnimator.SetBool("Idle",true);
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
                if (endoMaterial.GetFloat("_Dissolve") == 0)
                {
                    ModifyMaterial(0);
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
                if (CheckIfFlashedAt())
                {
                    if (!creatureAnimator.GetBool("Burning"))
                    {
                        creatureAnimator.SetBool("Burning", true);
                    }
                    if (currentBurnProgress < burnTick)
                    {
                        currentBurnProgress += 1;
                        ModifyMaterialBasedOnBurnProgress();
                    }
                    else
                    {
                        //TODO kill Freddles
                        SwitchToBehaviourClientRpc(1000);
                        StartCoroutine(DestroySequence(3));
                    }
                }
                else
                {
                    
                    SwitchCurrentBehaviourClientRpc((int)State.Idle);
                }
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
            if (player.HasLineOfSightToPosition(transform.position))
            {
                foreach (var item in player.ItemSlots)
                {
                    if (item != null)
                    {
                        if (item.gameObject.GetComponent<FlashlightItem>() != null)
                        {
                            if (item.gameObject.GetComponent<FlashlightItem>().isBeingUsed)
                            {
                                if ( Vector3.Distance(player.transform.position, transform.position) < 3f)
                                {
                                    if (player.LineOfSightToPositionAngle(transform.position) < 20)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return false;
    }
    //Material Vlaue modifier
    public void ModifyMaterialBasedOnBurnProgress()
    {
        float x = Mathf.Clamp01((float)currentBurnProgress / burnTick);
        ModifyMaterial(x);
        if (currentBurnProgress == 0)
        {
            creatureAnimator.SetBool("Burning", false);
        }
    }
    public void ModifyMaterial(float x)
    {
        if (x < 1.01f && x >= 0.0f)
        {
            endoMaterial.SetFloat("_Dissolve",x);
            coverMaterial.SetFloat("Vector1_FEFF47F1",x);
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
}