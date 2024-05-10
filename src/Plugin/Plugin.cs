using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using System.IO;
using LethalBestiary.Modules;
using HarmonyLib;
using NightmareFreddy.Configurations;

namespace NightmareFreddy.Plugin {
    [BepInPlugin(ModGUID, PluginInformation.PLUGIN_NAME, PluginInformation.PLUGIN_VERSION)]
    [BepInDependency(LethalBestiary.Plugin.ModGUID)] 
    public class Plugin : BaseUnityPlugin {
        // It is a good idea for our GUID to be more unique than only the plugin name. Notice that it is used in the BepInPlugin attribute.
        // The GUID is also used for the config file name by default.
        public const string ModGUID = "Xilef992." + PluginInformation.PLUGIN_NAME;
        internal static new ManualLogSource Logger;
    
        public static AssetBundle ModAssetsFreddles;
        public static AssetBundle ModAssetsFreddy;
        private readonly Harmony harmony = new Harmony(PluginInformation.PLUGIN_GUID);
        public static new Config FreddyConfig { get; internal set; }
        private void Awake() {
            Logger = base.Logger;

            FreddyConfig = new(base.Config);

            // This should be ran before Network Prefabs are registered.
            

            // We load the asset bundle that should be next to our DLL file, with the specified name.
            // You may want to rename your asset bundle from the AssetBundle Browser in order to avoid an issue with
            // asset bundle identifiers being the same between multiple bundles, allowing the loading of only one bundle from one mod.
            // In that case also remember to change the asset bundle copying code in the csproj.user file.
            var bundleFreddles = "freddlesmodassets";
            var bundleFreddy = "freddymodassets";
            ModAssetsFreddles = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), bundleFreddles));
            ModAssetsFreddy = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Info.Location), bundleFreddy));
            if (ModAssetsFreddles == null) {
                Debug.Log("Error Loading Asset [ Freddles ]");
                return;
            }
            if (ModAssetsFreddy == null) {
                Debug.Log("Error Loading Asset [ Freddy Nightmare ]");
                return;
            }
            // We load our assets from our asset bundle. Remember to rename them both here and in our Unity project.
            var FreddlesEnemy = ModAssetsFreddles.LoadAsset<EnemyType>("FreddlesEnemy");
            var FreddlesTN = ModAssetsFreddles.LoadAsset<TerminalNode>("FreddlesTN");
            var FreddlesTK = ModAssetsFreddles.LoadAsset<TerminalKeyword>("FreddlesTK");
            
            // Network Prefabs need to be registered. See https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
            // LethalLib registers prefabs on GameNetworkManager.Start.
            NetworkPrefabs.RegisterNetworkPrefab(FreddlesEnemy.enemyPrefab);
            Enemies.RegisterEnemy(FreddlesEnemy,FreddyConfig.RARITY_SPAWN.Value , Levels.LevelTypes.All, Enemies.SpawnType.Outside, FreddlesTN, FreddlesTK);
            
            
            
            
            // We load our assets from our asset bundle. Remember to rename them both here and in our Unity project.
            var FreddyEnemy = ModAssetsFreddy.LoadAsset<EnemyType>("NightmareFreddy");
            var FreddyTN = ModAssetsFreddy.LoadAsset<TerminalNode>("FreddyTN");
            var FreddyTK = ModAssetsFreddy.LoadAsset<TerminalKeyword>("FreddyTK");
            
            // Network Prefabs need to be registered. See https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
            // LethalLib registers prefabs on GameNetworkManager.Start.
            NetworkPrefabs.RegisterNetworkPrefab(FreddyEnemy.enemyPrefab);
            Enemies.RegisterEnemy(FreddyEnemy, FreddyConfig.RARITY_SPAWN.Value, Levels.LevelTypes.All, Enemies.SpawnType.Outside, FreddyTN, FreddyTK);
            InitializeNetworkBehaviours();
            harmony.PatchAll(typeof(ConfigurationsPatch));
            Debug.Log("Nightmare Freddy Loaded Successfully!");
            Debug.Log("                                                                                       \n                                  %%######%%%%                                         \n                                ###%%%%#{}{#%%%                                        \n                                ###%%%%%###%%%%                                        \n                                %##%%%%%####%%%             %##%                       \n                                 ##%%%%%####%%%          %%########%                   \n                                 #{#%%%%##{#%%%         ##%%%%%%%%#{#                  \n                                  ##%%%%##{#%%%%      %##%#{{{{{#%%##%                 \n                 %#######%        ##%%%%####%%%%%%%  %##%{{{{{{{{#%##%                 \n               %###%%%%%%#{#%   %%%%%%%%##{}{{#%%%%%%###}}}{{{{{{#%##%                 \n              %##%#{{{{#%%%#{#%%%%%%%%%%%%%%%%##%%%%%%%%{}}}}{{{#%%##                  \n              %%#{}{{{{{##%%%##%%###################%%%%%%#{{{{#%%##%                  \n              %%##}}}{{{{{{%%%######################%%##%%%%%%#%###%                   \n               %%#{}{{{{{#%%%########################%%%%##%%%###%                     \n               %%###{{#%%%%###%%%####################%%###%%%%%%%                      \n                 %####%%%%##%%%%%#####{#{{{{{{{{{{{#%########%%%%%                     \n                  %%#%%###%%###%%%#######{{##{{{#%%%%%%######%%%%%                     \n                    %%%##%%%#%%#%%%%%%%%#{{{{{%%%####%%%%###%%%%%%                     \n                     #%#%%%%%%##%%#{##%%%%{{{%%%%%#}{{#%%###%#%%%%                     \n                     #%%%%%%%#{%%%%%#{}{#%%{{%%%%#+:={#%%%%%%#%%%%                     \n                     ####%%%%##%%%%[::}{#%%#{%%%%%%%%%%%%%%###%%%%                     \n                      ####%%%#{%%%%%#%%%%%%#{{%%%%%%%%%%%%###%%%%%%%                   \n                      #####%%%%{%%%%%%%%%%##{{{%%%%%%%%#%%%%%%%%%%%%%%                 \n                      ######%#####%%%%%%#%####{{#%%#####%%%%%%%%%%%%%%%%               \n                      %%######%%%%#######%%%##{#%#{}}}{}}{###%%%%%%%%%%%               \n                    %%%%###%%%###{{{{{}{{{{#%%%{[}}}}}}}{{{##%%%%%%%%%%                \n                   ########{{{###########{{{{{{{{{{{}}}}{{###%%%%%%%%%%                \n                   ########{{##%%%%%%######{{{{{{{{{{}}{{##%%%%%%^-{%%                 \n                   %%%%%%##{{{#%%%%%%%%#####{{{{#{}}[}{{{{{()<^-^=~##%                 \n                    %%%%%%#####%%%%%%%%##{########{{}][[=~)~-~~-)(>##%                 \n                      %%%][]{{}({{#{{##{{#}]#}))#^-[]-(%>*#<*>>^(%###%%                \n                       ##{[[(]]]^*{(-]#*+#(~}%><%}^{#>[%{]%#[##%%{-{#%%                \n                        #%{{#(][]={#*[%](%#]#%##%%%%%%%%%%%%%%%>(}>##%%                \n                        #%%#%{#}%{%%%%%%%%%%%%%%%%%%%%%%%%%%%({^-]}%%%%                \n                        ##%[}##%%%%%%%%%%%%%%%%%%%%%%%%%%%%]{>:*:)%%%%                 \n                         ##]]]+[%%%%%%%%%%%%%%%%%%%%%[%%)(%>=^:~{%%%%%                 \n                         ###]<-=+(##{%%}{%#}%%(%%[<%#~(%>-[>:^#%##%%%                  \n                         ####(~*:-[}~[%<+#[=}#=)%(:]{~-#[<[#{{##%%%%                   \n                          ######(=)]~-%)-[[-+#*~{}<}%###{{###%%%%%                     \n                           %###%%%%###%%%%%%%############%%%%%%                        \n                             %%%%%%%%%%%%%%%%%%%%%%%%%%%%%{{%                          \n                               %%%%%%%%%%%%%%%%%%%%%%%%%%###%                          \n                              #%%%%%%%%%%%%%%%%%%%%####{{{}{%                          \n                              %%%%%%%%%%%%%%%  %%%%%%%#####%%                          \n                               %%%%%%%%%%%       %%%%%%%%%%%                           \n                                  %%%%%              %%%%                              \n                                                                                       ");
        }

        private static void InitializeNetworkBehaviours() {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        } 
    }
}