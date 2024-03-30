﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

namespace NightmareFreddy.Freddles;

public class FreddlesAi : EnemyAI
{
    public Material endoMaterial;
    public Material coverMaterial;


    
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

        destination = GetRandomPointInCollider(StartOfRound.Instance.shipBounds);
        
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
        SwitchCurrentbehaviourClientRpc((int)State.Running);
        
        burnTick = RandomNumberGenerator.GetInt32(5, 15);
        currentBurnProgress = 0;
        //TODO get the sitting position
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
        Debug.Log("We have an Ai Intervale");
        switch(currentBehaviourStateIndex) {
            case (int)State.Running :
                Debug.Log("Running!");
                creatureAnimator.SetBool("Idle",false);
                SetDestinationToPosition(destination);
                
                if (Vector3.Distance(transform.position, destination) <1f)
                {
                    SwitchCurrentbehaviourClientRpc((int)State.Idle);
                }

                if (CheckIfLookedAt())
                {  
                    SwitchCurrentbehaviourClientRpc((int)State.LookedAt);
                }
                break;
            case (int)State.LookedAt :
                agent.ResetPath();
                creatureAnimator.SetBool("Idle", true);
                if (currentBurnProgress > 0)
                {
                    currentBurnProgress -= 1;
                    ModifyMaterialBasedOnBurnProgress();
                }
                if (!CheckIfLookedAt())
                {  
                    SwitchCurrentbehaviourClientRpc((int)State.Running);
                }
                break;
            case (int)State.Idle :
                creatureAnimator.SetBool("Idle",true);
                arrived = true;
                if (currentBurnProgress > 0)
                {
                    currentBurnProgress -= 1;
                    ModifyMaterialBasedOnBurnProgress();
                }
                if (CheckIfFlashedAt())
                {
                    SwitchCurrentbehaviourClientRpc((int)State.Burning);
                }
                

                break;
            case (int)State.Burning :
                if (CheckIfFlashedAt())
                {
                    creatureAnimator.SetBool("Burning", true);
                    if (currentBurnProgress < burnTick)
                    {
                        currentBurnProgress += 1;
                        ModifyMaterialBasedOnBurnProgress();
                    }
                    else
                    {
                        //TODO kill Freddles
                        SwitchToBehaviourClientRpc(1000);
                        StartCoroutine(DestroySequence());
                    }
                }
                else
                {
                    SwitchCurrentbehaviourClientRpc((int)State.Idle);
                }
                break;
            default:
                Debug.Log("I AM DYING");
                break;
        }
    }

    public bool CheckIfLookedAt()
    {
        foreach (var player in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if (player.HasLineOfSightToPosition(transform.position))
            {
                Debug.Log("We got seen ....");
                return true;
            }
        }
        return false;
    }
    public bool CheckIfFlashedAt()
    {
        foreach (var player in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if (!player.HasLineOfSightToPosition(transform.position))
            {
                foreach (var item in player.ItemSlots)
                {
                    if (item.gameObject.GetComponent<FlashlightItem>() != null)
                    {
                        if (item.gameObject.GetComponent<FlashlightItem>().isBeingUsed)
                        {
                            if (Vector3.Distance(player.transform.position, transform.position) < 10f)
                            {
                                Debug.Log("FLASHING!!!!");
                                return true;
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
    }
    public void ModifyMaterial(float x)
    {
        if (x < 1.01f && x >= 0.0f)
        {
            endoMaterial.SetFloat("_Dissolve",x);;
            coverMaterial.SetFloat("Vector1_FEFF47F1",x);;
        }
        
    }

    [ClientRpc]
    public void SwitchCurrentbehaviourClientRpc(int x)
    {
        currentBehaviourStateIndex = x;
    }

    IEnumerator DestroySequence()
    {
        
        
        yield return new WaitForSeconds(3);
    }
}