﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using GameNetcodeStuff;
using NightmareFreddy.Freddles;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static StartOfRound;

namespace NightmareFreddy.NightmareFreddy;

public class NightmareFreddyAi : EnemyAI
{
    [Header("Roar Global Info [ Modifiable ]")]
    public float radiusRoar = 10f; // Radius of the roar effect
    public float forceRoar = 50f; // Force of the roar effect
    [Header("Material")]
    public Material endoSkeleton;
    public Material exoSkeleton;
    public Material DistortionMaterial;
    [Header("Audio")]
    public AudioSource scream;
    public AudioSource footstep;
    public AudioSource spawning;
    public AudioSource hitting;
    public AudioClip[] gigles;
    public BoxCollider EnemyCollider;
    [Header("Attack")]
    public Transform attackArea;
    [Header("Rendering!")]
    public SkinnedMeshRenderer FreddyRenderer;
    public MeshRenderer Sphere;
    [NonSerialized] 
    public bool enoughFreddles;

    private float timeSinceHittingLocalPlayer;

    private bool didRoar;
    private Coroutine spawningMaterialChanges;
    private float animationSpeedAttack;
    enum State {
        Hidden,
        Spawning,
        Attacking,
        Walking,
        Running,
        Screaming,
        WaitingOnCoroutine
        
    }

    public override void Start()
    {
        base.Start();
        EnemyCollider.enabled = false;
        endoSkeleton.SetFloat("_Strenght",0f);
        endoSkeleton.SetFloat("_Dissolve",1);
        exoSkeleton.SetFloat("_Dissolve", 1);
        Sphere.enabled = false;
        /*Vector3 hangarShipDoorPosition = GameObject.FindObjectOfType<HangarShipDoor>().transform.position;

        // Perform a raycast to find the nearest point on the navmesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(hangarShipDoorPosition, out hit, 10f, NavMesh.AllAreas))
        {
            // If a valid point on the navmesh is found, warp the agent to that position
            agent.Warp(hit.position);
        }
        else
        {
            Debug.LogError("Could not find a valid position on the NavMesh.");
        }*/
        StartCoroutine(TransitionMaterial(false, 0)); //TODO should be false but true for testing
        SwitchToBehaviourClientRpc(0);
    }
    
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (Vector3.Distance(GameObject.FindObjectOfType<HangarShipDoor>().transform.position, transform.position) < 5)
        {
            OpenShipDoorsClientRpc();
        }
        //Making sure Scream is right
        if (currentBehaviourStateIndex != (int)State.Screaming)
        {
            didRoar = false;
        }
        Debug.Log(currentBehaviourStateIndex);
        switch(currentBehaviourStateIndex) {
            case (int)State.Hidden ://0
                Debug.Log(GetNumberOfFreddles(true));
                //CORE LOGIC
                
                if (GetNumberOfFreddles(true) >= 1)
                {
                    Debug.Log("World is beauty!!!");
                    spawningMaterialChanges = StartCoroutine(TransitionMaterial(true,10f));
                    SwitchToBehaviourStateClientRpc((int)State.Spawning);
                    creatureVoice.PlayOneShot(gigles[RandomNumberGenerator.GetInt32(3)]);
                    
                }
                else
                {
                    if (RandomNumberGenerator.GetInt32(100) <= 2)
                    {
                        Debug.Log("Let's spawn a freddle!");
                        SpawnNewFreddle();
                    }
                }
                break;
            case (int)State.Spawning ://1
                if (!spawning.isPlaying)
                {
                    spawning.Play();
                }
                
                if (GetNumberOfFreddles(true) <= 3 )
                {
                    if (spawningMaterialChanges == null)
                    {
                        spawningMaterialChanges = StartCoroutine(TransitionMaterial(false,15f));
                    }
                }

                if (endoSkeleton.GetFloat("_Strenght") == 5f)
                {
                    SwitchToBehaviourStateClientRpc((int)State.Screaming);
                }
                //CORE LOGIC
                
                break;
            case (int)State.Attacking ://2
                
                
                break;
            case (int)State.Walking : //3
                targetPlayer = FindPlayerToTarget();
                SetDestinationToPosition(targetPlayer.transform.position);
                if (CheckIfPlayerHittable())
                {
                    SwitchToBehaviourStateClientRpc((int)State.Attacking);
                    
                }
                if (RandomNumberGenerator.GetInt32(200) == 1)
                {
                    SwitchToBehaviourStateClientRpc((int)State.Running);
                    creatureVoice.PlayOneShot(gigles[RandomNumberGenerator.GetInt32(3)]);
                }
                //CORE LOGIC
                
                break;
            case (int)State.Running ://4
                targetPlayer =  FindPlayerToTarget();
                SetDestinationToPosition(targetPlayer.transform.position);
                //SetMovingTowardsTargetPlayer(targetPlayer);
                if (CheckIfPlayerHittable())
                {
                    SwitchToBehaviourStateClientRpc((int)State.Attacking);
                }
                //CORE LOGIC
                

                break;
            case (int)State.Screaming ://5
                didRoar = true;
                
                break;
            case (int)State.WaitingOnCoroutine:
                
                break;
            default:
                break;
        }
    }
    [ClientRpc]
    public void SwitchToBehaviourStateClientRpc(int x)
    {
        this.SwitchToBehaviourStateOnLocalClient(x);
        switch(currentBehaviourStateIndex) {
            case (int)State.Hidden ://0
                agent.speed = 0f;
                break;
            case (int)State.Spawning ://1
                agent.speed = 0f;
                EnemyCollider.enabled = true;

                break;
            case (int)State.Attacking ://2
                if (previousBehaviourStateIndex == (int)State.Running)
                {
                    agent.speed = 5f;
                }
                else
                {
                    agent.speed = 0f; 
                }
                creatureAnimator.SetTrigger("Attack");
                break;
            case (int)State.Walking : //3
                agent.speed = 4f;
                creatureAnimator.SetTrigger("Walking");
                break;
            case (int)State.Running ://4
                agent.speed = 7f;
                creatureAnimator.SetTrigger("Running");
                break;
            case (int)State.Screaming ://5
                agent.speed = 0f;
                creatureAnimator.SetTrigger("Roar");
                PerformRoarClientRpc(); ;

                break;
            case (int)State.WaitingOnCoroutine: //6
                
                
                break;
            default:
                break;
        }
    }
    /// <summary>
    /// Pretty  Self Explanatory
    /// </summary>
    /// <param name="onlyArrived"></param>
    /// <returns></returns>
    public int GetNumberOfFreddles(bool onlyArrived)
    {
        FreddlesAi[] freddlesAiScripts = FindObjectsOfType<FreddlesAi>();
        if (!onlyArrived)
        {
            return freddlesAiScripts.Length;
        }
        else
        {
            int numberOfFreddles = 0;
            foreach (var freddle in freddlesAiScripts)
            {
                if (freddle.arrived)
                {
                    numberOfFreddles += 1;
                }
            }

            return numberOfFreddles;
        }
    }
    /// <summary>
    /// Get all the Freddles on the map
    /// </summary>
    /// <param name="onlyArrived"></param>
    /// <returns></returns>
    public FreddlesAi[] GetAllFreddles(bool onlyArrived)
    {
        FreddlesAi[] freddlesAiScripts = FindObjectsOfType<FreddlesAi>();
        if (!onlyArrived)
        {
            return freddlesAiScripts;
        }
        else
        {
            List<FreddlesAi> freddles = new List<FreddlesAi>();
            foreach (var freddle in freddlesAiScripts)
            {
                if (freddle.arrived)
                {
                    freddles.Add(freddle);
                }
            }

            return freddles.ToArray();
        }
    }
    
    /// <summary>
    /// This function will spawn a new Freddles in a radius around the ship!
    /// </summary>
    public void SpawnNewFreddle()
    {
        var allEnemiesList = new List<SpawnableEnemyWithRarity>();
        allEnemiesList.AddRange(RoundManager.Instance.currentLevel.Enemies);
        allEnemiesList.AddRange(RoundManager.Instance.currentLevel.OutsideEnemies);
        var enemyToSpawn = allEnemiesList.Find(x => x.enemyType.enemyName.Equals("Freddles"));
        RoundManager.Instance.SpawnEnemyGameObject(
            RoundManager.Instance.GetRandomNavMeshPositionInRadius(
                StartOfRound.Instance.shipBounds.ClosestPointOnBounds(transform.position),
                30f
                ),
            0f,
            RoundManager.Instance.currentLevel.OutsideEnemies.IndexOf(enemyToSpawn),
            enemyToSpawn.enemyType
        );
        
        Debug.Log("Finished spawning enemy!");
    }
    /// <summary>
    /// Function to call for Freddy to find his target. He aims to find someone that is the closest to him and the ship
    /// </summary>
    /// <returns></returns>
    private PlayerControllerB FindPlayerToTarget()
    {
        Vector3 doorPosition = StartOfRound.Instance.shipBounds.transform.position;
        PlayerControllerB highest = RoundManager.Instance.playersManager.allPlayerScripts[0];
        float highestDistance = 0f;
        var position = transform.position;
        foreach (var playerObject in RoundManager.Instance.playersManager.allPlayerScripts)
        {
            if (playerObject.isPlayerControlled && !playerObject.isPlayerDead)
            {
                float distance = Vector3.Distance(playerObject.transform.position, position) +
                                 Vector3.Distance(position, doorPosition);
                if (distance > highestDistance)
                {
                    highest = playerObject.GetComponent<PlayerControllerB>();
                }
            }
        }
        Debug.Log(highest.name);
        return highest;
    }
    
    /// <summary>
    /// Will make Nightmare Freddy Roar and push back everything
    /// </summary>
    [ClientRpc]
    private void PerformRoarClientRpc()
    {
        
        PlayerControllerB[] players = RoundManager.Instance.playersManager.allPlayerScripts;

        foreach (var player in players)
        {
            if (player.isPlayerControlled && !player.isPlayerDead)
            {
                if (Vector3.Distance(transform.position, player.transform.position) < radiusRoar)
                {
                    PushingPlayer(player);                    
                }
            }
            
        }
        /*Collider[] colliders = Physics.OverlapSphere(transform.position, radiusRoar); // Find all colliders in the radius

        foreach (Collider col in colliders)
        {
            if (col.gameObject.GetComponent<PlayerControllerB>())
            {
                Rigidbody rb = col.GetComponent<Rigidbody>(); // Get the rigidbody of the collider
                if (rb != null)
                {
                    Vector3 direction = col.transform.position - transform.position; // Calculate the direction away from the roar
                    direction.Normalize();
                    rb.AddForce(direction * forceRoar, ForceMode.Impulse); // Apply force to push the object away
                }
            }
        }*/
    }
    /// <summary>
    /// Responsible for pushing back a single player!
    /// </summary>
    /// <param name="player"></param>
    public void PushingPlayer(PlayerControllerB player) {
        // Apply damage to the player
        player.DamagePlayer(5);

        // Calculate direction away from the roar
        Vector3 direction = player.transform.position - transform.position;
        direction.y = Mathf.Max(0, direction.y); // Ensure the y-component is non-negative
        direction.Normalize();

        // Get the player's rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();

        // Disable kinematic temporarily to allow applying force
        rb.isKinematic = false;

        // Calculate the force vector in the opposite direction
        Vector3 forceVector = -direction * forceRoar;

        // Add force to push the player in the opposite direction
        rb.AddForce(forceVector, ForceMode.Impulse);

        // Add upward force to make the player go upward a bit
        rb.AddForce(Vector3.up * forceRoar/2, ForceMode.Impulse);

        // Re-enable kinematic to prevent further physics interactions
        StartCoroutine(EnableKninematicPlayer(2, rb));
    }

    private IEnumerator EnableKninematicPlayer(int i, Rigidbody rb)
    {
        yield return new WaitForSeconds(3);
        rb.isKinematic = true;
    }


    /// <summary>
    /// For the transition from visible to invisible, must be called with Coroutine
    /// </summary>
    /// <param name="manifest"></param>
    /// <param name="time"></param>
    /// <returns>Coroutine</returns>
    private IEnumerator TransitionMaterial(bool manifest, float time)
    {
        float elapsedTime = 0;

        // Get the initial value of "_Dissolve" property
        float startValue = endoSkeleton.GetFloat("_Dissolve");

        // Calculate the target value based on the manifest parameter
        float targetValue = manifest ? 2f : 0f;

        // Wait for one frame before starting the transition
        yield return null;

        // Transition loop
        while (elapsedTime < time)
        {
            // Calculate the new value using interpolation
            float newValue = Mathf.Lerp(startValue, targetValue, elapsedTime / time);
        
            // Update the material property
            ModifyMaterial(newValue); 
        
            // Increment elapsed time using unscaled time
            elapsedTime += Time.unscaledDeltaTime;

            // Wait for the next frame
            yield return null;
        }

        // Ensure the material ends at the target value
        ModifyMaterial(targetValue);
    }
    public void ModifyMaterial(float x)
    {
        if (x < 1.01f && x >= 0.0f)
        {
            endoSkeleton.SetFloat("_Dissolve", 1-x);
            endoSkeleton.SetFloat("_Strenght",0);
            
            if (x==0f)
            {
                
                FreddyRenderer.enabled = false;
            }
            else
            {
                FreddyRenderer.enabled = true;
            }
        }
        if (x > 1f && x <= 2.0f)
        {
            float y = x - 1f;
            exoSkeleton.SetFloat("_Dissolve", 1-y);
            if (x == 2.0f)
            {
                endoSkeleton.SetFloat("_Strenght",5);
            }
        }
        
    }
    public bool CheckIfPlayerHittable() {
        int playerLayer = 1 << 3; // This can be found from the game's Asset Ripper output in Unity
        Collider[] hitColliders = Physics.OverlapBox(attackArea.position, attackArea.localScale, Quaternion.identity, playerLayer);
        if(hitColliders.Length > 0){
            foreach (var player in hitColliders){
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(player);
                if (playerControllerB != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void SwingAttack()
    {
        hitting.Play();
        SwingAttackHitClientRpc(true);
    }
    [ClientRpc]
    public void SwingAttackHitClientRpc(bool switchState) {
        int playerLayer = 1 << 3; // This can be found from the game's Asset Ripper output in Unity
        Collider[] hitColliders = Physics.OverlapBox(attackArea.position, attackArea.localScale, Quaternion.identity, playerLayer);
        if(hitColliders.Length > 0){
            foreach (var player in hitColliders){
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(player);
                if (playerControllerB != null)
                {
                    timeSinceHittingLocalPlayer = 0f;
                    playerControllerB.DamagePlayer(40);
                    PushingPlayer(playerControllerB);
                    creatureVoice.PlayOneShot(gigles[RandomNumberGenerator.GetInt32(3)]);
                }
            }
        }
        SwitchToBehaviourStateClientRpc(previousBehaviourStateIndex);
    }

    [ClientRpc]
    public void OpenShipDoorsClientRpc()
    {
        HangarShipDoor door = GameObject.FindObjectOfType<HangarShipDoor>();
        door.doorPower = 0f;
    }

    public void PlayFootStepSounds()
    {
        footstep.Play();
    }

    [ClientRpc]
    public void PlayRandomGigglesClientRpc(int x)
    {
        scream.PlayOneShot(gigles[x]);
    }

    public void AnimationScreamEnd()
    {
        Sphere.enabled = false;
        SwitchToBehaviourStateClientRpc((int)State.Walking);
    }

    public void Logger(String log)
    {
        Debug.Log("[NightmareFreddy][Freddy] ~ " + log);
        
    }

    public void StartRoar()
    {
        Sphere.enabled = true;
        scream.Play();
    }
}