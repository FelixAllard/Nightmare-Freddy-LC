using System;
using BepInEx.Configuration;
using Unity.Collections;
using Unity.Netcode;

namespace NightmareFreddy.Configurations;

public class Config :SyncedInstance<Config>
{
    
    public ConfigEntry<int> RARITY_SPAWN { get; private set; } 
    public ConfigEntry<int> NUMBER_FREDDLES_PHASE_2 { get; private set; } 
    public ConfigEntry<int> POURCENTAGE_SPAWN { get; private set; } 
    public ConfigEntry<int> DAMAGE_FREDDLES { get; private set; } 
    
    public Config(ConfigFile cfg)
    {

        RARITY_SPAWN = cfg.Bind("Rarity", "How rare do you want Freddy to spawn?", 40,
            "This is the usual Rarity Value that is present in every single monster mod"
        );
        NUMBER_FREDDLES_PHASE_2 = cfg.Bind("Difficulty", "Freddles before Freddy spaw", 7,
            "The number of unmoving Freddles before Freddy spawns"
        );
        POURCENTAGE_SPAWN = cfg.Bind("Difficulty", "Change for Freddles to spawn", 170,
            "THe higher the number, the less change for the Freddles to spawn every 0.2 seconds"
        ); 
        DAMAGE_FREDDLES = cfg.Bind("Difficulty", "Damage Freddles Deal to you", 4,
            "When the Freddles are aggressive, what is the ammount of damage they would deal?"
        );
    }
    public static void RequestSync() {
        if (!IsClient) return;

        using FastBufferWriter stream = new(IntSize, Allocator.Temp);
        MessageManager.SendNamedMessage("Xilef992NightmareFreddy_OnRequestConfigSync", 0uL, stream);
    }
    public static void OnRequestSync(ulong clientId, FastBufferReader _) {
        if (!IsHost) return;
    
        /*Debug.Log($"Config sync request received from client: {clientId}");*/

        byte[] array = SerializeToBytes(Instance);
        int value = array.Length;

        using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

        try {
            stream.WriteValueSafe(in value, default);
            stream.WriteBytesSafe(array);

            MessageManager.SendNamedMessage("Xilef992NightmareFreddy_OnReceiveConfigSync", clientId, stream);
        } catch(Exception e) {
            /*Debug.Log($"Error occurred syncing config with client: {clientId}\n{e}");*/
        }
    }
    public static void OnReceiveSync(ulong _, FastBufferReader reader) {
        if (!reader.TryBeginRead(IntSize)) {
            /*Debug.Log("Config sync error: Could not begin reading buffer.");*/
            return;
        }

        reader.ReadValueSafe(out int val, default);
        if (!reader.TryBeginRead(val)) {
            /*Debug.Log("Config sync error: Host could not sync.");*/
            return;
        }

        byte[] data = new byte[val];
        reader.ReadBytesSafe(ref data, val);

        SyncInstance(data);

        /*Debug.Log("Successful synced Config");*/
    }
}