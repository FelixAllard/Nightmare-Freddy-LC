using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using NightmareFreddy.Freddles;
using Unity.Netcode;
using UnityEngine;

namespace NightmareFreddy.NightmareFreddy;

public class NightmareFreddyAi : EnemyAI
{
    
    public float radiusRoar = 5f; // Radius of the roar effect
    public float forceRoar = 10f; // Force of the roar effect
    public Material endoSkeleton;
    public Material exoSkeleton;
    public AudioSource scream;
    public AudioSource footstep;
    public AudioSource spawning;
    public AudioSource hitting;
    public Transform attackArea;
    public SkinnedMeshRenderer FreddyRenderer;
    [NonSerialized] 
    public bool enoughFreddles;

    private float timeSinceHittingLocalPlayer;

    private bool didRoar;
    private Coroutine spawningMaterialChanges;
    enum State {
        Hidden,
        Spawning,
        Attacking,
        Walking,
        Running,
        Screaming
        
    }

    public override void Start()
    {
        base.Start();
        StartCoroutine(TransitionMaterial(false, 0));
    }
    
    public override void DoAIInterval()
    {
        base.DoAIInterval();
        //Making sure Scream is right
        if (currentBehaviourStateIndex != (int)State.Screaming)
        {
            didRoar = false;
        }
        switch(currentBehaviourStateIndex) {
            case (int)State.Hidden :
                //CORE LOGIC
                agent.speed = 0f;
                if (GetNumberOfFreddles(true) >= 5)
                {
                    if (spawningMaterialChanges == null)
                    {
                        spawningMaterialChanges = StartCoroutine(TransitionMaterial(true,10f));
                    }
                    
                }
                break;
            case (int)State.Spawning :
                agent.speed = 0f;
                if (GetNumberOfFreddles(true) <= 3 )
                {
                    if (spawningMaterialChanges == null)
                    {
                        spawningMaterialChanges = StartCoroutine(TransitionMaterial(false,15f));
                    }
                }
                //CORE LOGIC
                
                break;
            case (int)State.Attacking :

                //CORE LOGIC
                creatureAnimator.SetTrigger("AttackAnimation");
                agent.speed = 0f;
                break;
            case (int)State.Walking :

                //CORE LOGIC
                agent.speed = 4f;
                break;
            case (int)State.Running :
                
                //CORE LOGIC
                agent.speed = 7f;
                
                break;
            case (int)State.Screaming :
                didRoar = true;
                PerformRoarClientRpc();

                agent.speed = 0f;
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
            RoundManager.Instance.GetRandomPositionInRadius(
                StartOfRound.Instance.middleOfShipNode.position,
                15f,
                30f
                ), 
            0f,
            RoundManager.Instance.currentLevel.OutsideEnemies.IndexOf(enemyToSpawn),
            enemyToSpawn.enemyType
        );
    }
    /// <summary>
    /// Function to call for Freddy to find his target. He aims to find someone that is the closest to him and the ship
    /// </summary>
    /// <returns></returns>
    private PlayerControllerB FindPlayerToTarget()
    {
        Vector3 doorPosition =  StartOfRound.Instance.shipDoorNode.position;
        PlayerControllerB highest = RoundManager.Instance.playersManager.allPlayerScripts[0];
        float highestDistance = 0f;
        var position = transform.position;
        foreach (var playerObject in RoundManager.Instance.playersManager.allPlayerObjects)
        {
            float distance = Vector3.Distance(playerObject.transform.position, position) +
                             Vector3.Distance(position, doorPosition);
            if (distance > highestDistance)
            {
                highest = playerObject.GetComponent<PlayerControllerB>();
            }

        }

        return highest;
    }
    
    /// <summary>
    /// Will make Nightmare Freddy Roar and push back everything
    /// </summary>
    [ClientRpc]
    private void PerformRoarClientRpc()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radiusRoar); // Find all colliders in the radius

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
        }
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

        float startValue = endoSkeleton.GetFloat("Vector1_FEFF47F1");

        float targetValue = manifest ? 0f : 2f;
        if (manifest)
        {
            yield return new WaitForSeconds(1);
        }
        while (elapsedTime < time)
        {
            ModifyMaterial(Mathf.Lerp(startValue, targetValue, elapsedTime / time)); 
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        ModifyMaterial(targetValue);
    }
    public void ModifyMaterial(float x)
    {
        if (x < 1.01f && x >= 0.0f)
        {
            endoSkeleton.SetFloat("NAMENOTREADY", x);
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
            exoSkeleton.SetFloat("NAMENOTREADY", x);
        }
        
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
                }
            }
        }

        if (switchState = true)
        {
            SwitchToBehaviourClientRpc((int)State.Running);
        }
    }

    [ClientRpc]
    public void OpenShipDoors()
    {
        HangarShipDoor door = GameObject.FindObjectOfType<HangarShipDoor>();
        door.doorPower = 0f;
    }




    [ClientRpc]
    public void StopRoarClientRpc()
    {
        
    }
}