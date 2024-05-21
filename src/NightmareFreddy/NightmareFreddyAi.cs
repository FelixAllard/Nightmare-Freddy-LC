using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using GameNetcodeStuff;
using NightmareFreddy.Configurations;
using NightmareFreddy.Freddles;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements.UIR;
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
    public BoxCollider ScanNode;

    private float timeSinceHittingLocalPlayer;
    private Coroutine spawningMaterialChanges;
    private float animationSpeedAttack;
    private bool wasRunning;
    private int lastBeforeAttack;
    [Header("Head rotation stuff")]
    public Transform freddyStare; 

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


        SetDestinationToPosition(StartOfRound.Instance.shipInnerRoomBounds.transform.position);
        
        int attempts = 0;
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
                    SetDestinationToPosition(destination);
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

        ScanNode.enabled = false;
        StartCoroutine(TransitionMaterial(false, 0));
        SwitchToBehaviourStateClientRpc(0);
        
    }

    private void LateUpdate()
    {
        if (targetPlayer != null)
        {
            freddyStare.position = targetPlayer.transform.position;
        }
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
        if (Vector3.Distance(GameObject.FindObjectOfType<HangarShipDoor>().transform.position, transform.position) < 5)
        {
            OpenShipDoorsClientRpc();
        }
        //Making sure Scream is right
        switch(currentBehaviourStateIndex) {
            case (int)State.Hidden ://0
                Debug.Log(GetNumberOfFreddles(true));
                //CORE LOGIC
                
                if (GetNumberOfFreddles(true) >=  Plugin.Plugin.FreddyConfiguration.NUMBER_FREDDLES_PHASE_2.Value)
                {

                    spawningMaterialChanges = StartCoroutine(TransitionMaterial(true,10f));
                    SwitchToBehaviourStateServerRpc((int)State.Spawning);
                    PlayEuhEuhClientRpc();

                }
                else
                {
                    if (RandomNumberGenerator.GetInt32(Plugin.Plugin.FreddyConfiguration.POURCENTAGE_SPAWN.Value) <= 2)
                    {
                        SpawnNewFreddle();
                    }
                }
                break;
            case (int)State.Spawning ://1
                
                
                if (GetNumberOfFreddles(true) <= 3 )
                {
                    if (spawningMaterialChanges == null)
                    {
                        spawningMaterialChanges = StartCoroutine(TransitionMaterial(false,15f));
                    }
                }
                if (endoSkeleton.GetFloat("_Strenght") == 5f)
                {
                    SwitchToBehaviourStateServerRpc((int)State.Screaming);
                }
                //CORE LOGIC
                
                break;
            case (int)State.Attacking ://2
                
                
                break;
            case (int)State.Walking : //3
                SetTargetPlayerClientRpc(FindPlayerToTarget().actualClientId);

                CheckIfPlayerHittableServerRpc();
                
                SetDestinationToPosition(targetPlayer.transform.position);
                if (RandomNumberGenerator.GetInt32(100) <= 2)
                {
                    SpawnNewFreddle();
                }
                if (RandomNumberGenerator.GetInt32(125) == 1)
                {
                    SwitchToBehaviourStateServerRpc((int)State.Running);
                    PlayEuhEuhClientRpc();
                }
                //CORE LOGIC
                
                break;
            case (int)State.Running ://4
                SetTargetPlayerClientRpc(FindPlayerToTarget().actualClientId);
                if (RandomNumberGenerator.GetInt32(100) <= 1)
                {
                    SpawnNewFreddle();
                }
                if (RandomNumberGenerator.GetInt32(100) == 1)
                {
                    SwitchToBehaviourStateServerRpc((int)State.Walking);
                    PlayEuhEuhClientRpc();
                }
                SetDestinationToPosition(targetPlayer.transform.position);
                CheckIfPlayerHittableServerRpc();
                //SetMovingTowardsTargetPlayer(targetPlayer);
                    
                
                //CORE LOGIC
                break;
            case (int)State.Screaming ://5
                
                break;
            case (int)State.WaitingOnCoroutine:
                
                break;
            default:
                break;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void SwitchToBehaviourStateServerRpc(int x)
    {
        SwitchToBehaviourStateClientRpc(x);
    }
    //TODO Fix no animation when spawning...
    [ClientRpc]
    public void SwitchToBehaviourStateClientRpc(int x)
    {
        this.SwitchToBehaviourStateOnLocalClient(x);
        switch(currentBehaviourStateIndex) {
            case (int)State.Hidden ://0
                agent.speed = 0f;
                wasRunning = false;
                break;
            case (int)State.Spawning ://1
                agent.speed = 0f;
                ScanNode.enabled = true;
                EnemyCollider.enabled = true;
                wasRunning = false;
                PlayLullabyClientRpc();
                break;
            case (int)State.Attacking ://2
                ActivateAllFreddlesClientRpc();
                if (wasRunning)
                {
                    agent.speed = 5f;
                }
                else
                {
                    agent.speed = 0f; 
                }
                PlayAnimationServerRpc("Attack");
                break;
            case (int)State.Walking : //3
                agent.speed = 4f;
                PlayAnimationServerRpc("Walking");
                ActivateAllFreddlesClientRpc();
                wasRunning = false;
                lastBeforeAttack = (int)State.Walking;
                break;
            case (int)State.Running ://4
                ActivateAllFreddlesClientRpc();
                agent.speed = 7f;
                PlayAnimationServerRpc("Running");
                wasRunning = true;
                lastBeforeAttack = (int)State.Running;
                break;
            case (int)State.Screaming ://5
                ActivateAllFreddlesClientRpc();
                agent.speed = 0f;
                PlayAnimationServerRpc("Roar");
                PerformRoarClientRpc();
                wasRunning = false;
                break;
            case (int)State.WaitingOnCoroutine: //6
                
                
                break;
            default:
                break;
        }
    }
    [ClientRpc]
    private void PlayLullabyClientRpc()
    {
        spawning.Play();
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
            ModifyMaterialClientRpc(newValue); 
        
            // Increment elapsed time using unscaled time
            elapsedTime += Time.unscaledDeltaTime;

            // Wait for the next frame
            yield return null;
        }

        // Ensure the material ends at the target value
        ModifyMaterialClientRpc(targetValue);
    }
    [ClientRpc]
    public void ModifyMaterialClientRpc(float x)
    {
        if (x < 1.01f && x >= 0.0f)
        {
            endoSkeleton.SetFloat("_Dissolve", 1-x);
            endoSkeleton.SetFloat("_Strenght",0);
            FreddyRenderer.enabled = true;
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
    [ServerRpc(RequireOwnership = false)]
    public void CheckIfPlayerHittableServerRpc() {
        CheckIfPlayerHittableClientRpc();
    }
    [ClientRpc]
    public void CheckIfPlayerHittableClientRpc()
    {
        int playerLayer = 1 << 3; // This can be found from the game's Asset Ripper output in Unity
        Collider[] hitColliders = Physics.OverlapBox(attackArea.position, attackArea.localScale, Quaternion.identity, playerLayer);
        if(hitColliders.Length > 0){
            foreach (var player in hitColliders){
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(player);
                if (playerControllerB != null)
                {
                    SwitchToBehaviourStateServerRpc((int)State.Attacking);
                }
            }
        }

    }

    public void SwingAttack()
    {
        SwingAttackHitServerRpc(true);
    }
    [ServerRpc(RequireOwnership = false)]
    public void SwingAttackHitServerRpc(bool switchState) {
        /*hitting.Play();
        int playerLayer = 1 << 3; // This can be found from the game's Asset Ripper output in Unity
        Collider[] hitColliders = Physics.OverlapBox(attackArea.position, attackArea.localScale, Quaternion.identity, playerLayer);
        if(hitColliders.Length > 0){
            foreach (var player in hitColliders){
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(player);
                if (playerControllerB != null)
                {
                    timeSinceHittingLocalPlayer = 0f;
                    playerControllerB.DamagePlayer(
                        40
                    );
                    PushingPlayer(playerControllerB);
                    PlayEuhEuhClientRpc();
                }
            }
        }
        SwitchToBehaviourStateServerRpc(lastBeforeAttack);
        */
        
        SwingAttackHitClientRpc(switchState);
    }
    //TODO Fix endless damage
    [ClientRpc]
    public void SwingAttackHitClientRpc(bool switchState) {
        hitting.Play();
        int playerLayer = 1 << 3; // This can be found from the game's Asset Ripper output in Unity
        Collider[] hitColliders = Physics.OverlapBox(attackArea.position, attackArea.localScale, Quaternion.identity, playerLayer);
        if(hitColliders.Length > 0){
            foreach (var player in hitColliders){
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(player);
                if (playerControllerB != null)
                {
                    if (playerControllerB == RoundManager.Instance.playersManager.localPlayerController)
                    {
                        playerControllerB.DamagePlayer(
                            40
                        );
                        PushingPlayer(playerControllerB);
                        PlayEuhEuhClientRpc();
                        break;
                    }
                }
            }
        }
        SwitchToBehaviourStateServerRpc(lastBeforeAttack);
    }
    [ClientRpc]
    public void ActivateAllFreddlesClientRpc()
    {
        FreddlesAi[] freddles = GetAllFreddles(false);
        foreach (var freddle in freddles)
        {
            freddle.ActivateDangerousMod();
        }
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
        SwitchToBehaviourStateServerRpc((int)State.Walking);
        ActivateAllFreddlesClientRpc();
    }
    public void Logger(String log)
    {
        Debug.Log("[NightmareFreddy][Freddy] ~ " + log);
    }


    [ClientRpc]
    public void PlayEuhEuhClientRpc()
    {
        creatureVoice.PlayOneShot(gigles[RandomNumberGenerator.GetInt32(3)]);
    }
    [ClientRpc]
    public void ScreamClientRpc()
    {
        scream.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlayAnimationServerRpc(String x)
    {
        PlayAnimationClientRpc(x);
    }
    [ClientRpc]

    public void PlayAnimationClientRpc(String x)
    {
        creatureAnimator.SetTrigger(x);
    }

    public void StartRoar()
    {
        Sphere.enabled = true;
        ScreamClientRpc();
    }

    [ClientRpc]
    public void SetTargetPlayerClientRpc(ulong clientId)
    {
        PlayerControllerB[] players=  RoundManager.Instance.playersManager.allPlayerScripts;
        foreach (var player in players)
        {
            if (player.actualClientId == clientId)
            {
                targetPlayer = player;
                return;
            }
        }

    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        endoSkeleton.SetFloat("_Dissolve", 1);
        endoSkeleton.SetFloat("_Strenght",0);
        exoSkeleton.SetFloat("_Dissolve", 1);
    }
}