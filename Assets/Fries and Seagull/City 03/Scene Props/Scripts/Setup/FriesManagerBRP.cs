using System;
using System.Collections.Generic;
# if UNITY_EDITOR
using Seagull.City_03.Inspector;
using UnityEditor;
# endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace Seagull.City_03.SceneProps.Setup {
    public class FriesManagerBRP : MonoBehaviour {

        public static void setupLight() { }
        public static void unsetLight() { }

        public GameObject postProcessVolumePrefab;
        
        [Tooltip("Post Process effect including glowing will only show to these cameras")]
        public List<Camera> gameCameras = new();
        
        [Tooltip("Which layer should Post Processor Volume use")]
        public int postProcessLayer = -1;
        
# if UNITY_EDITOR
        [AButton("Initialize")] [IgnoreInInspector]
        public Action initialize;

        private void Reset() {
            initialize = init;
        }

        private void init() {
            if (gameCameras == null || gameCameras.Count == 0) {
                Debug.LogError("Please provide at least 1 valid camera to Game Cameras field.");
                return;
            }
            
            if (string.IsNullOrEmpty(LayerMask.LayerToName(postProcessLayer))) {
                Debug.LogError("Please provide a valid layer ID in Post Process Layer field.");
                return;
            }
            
            setupPostProcessorVlume();
            
            foreach (var camera in gameCameras) {
                PostProcessLayer ppl = camera.GetComponent<PostProcessLayer>();
                if (ppl) {
                    ppl.volumeLayer |= LayerMask.GetMask(LayerMask.LayerToName(postProcessLayer));
                    continue;
                }
                
                ppl = camera.gameObject.AddComponent<PostProcessLayer>();
                ppl.volumeLayer = LayerMask.GetMask(LayerMask.LayerToName(postProcessLayer));
            }
            Debug.Log($"Init post-processor settings for Built-in Rendering Pipeline successfully.");
        }

        private void setupPostProcessorVlume() {
            // 检查现在有没有 Post Processor
            bool hasValidPostProcessor = false;
            GameObject globalPPVGobj = GameObject.Find("Post Process Volume (Fries)");
            PostProcessVolume globalPPV = null;
            if (globalPPVGobj != null) {
                globalPPV = globalPPVGobj.GetComponent<PostProcessVolume>();
                FriesPostProcessorIdentifier yppi = globalPPVGobj.GetComponent<FriesPostProcessorIdentifier>();
                if (yppi != null) hasValidPostProcessor = true;
            }

            // 如果没有，则创建 Yurei Post Process Volume
            if (!hasValidPostProcessor) {
                GameObject postProcessor = GameObject.Instantiate(postProcessVolumePrefab);
                postProcessor.layer = postProcessLayer;
                postProcessor.name = "Post Process Volume (Fries)";
            }
            // 如果有 则检查它的完整性
            else {
                globalPPVGobj.layer = postProcessLayer;
                
                if (globalPPV.sharedProfile == null) {
                    globalPPV.sharedProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
                    // 保存资产到指定路径
                    #if UNITY_EDITOR
                    AssetDatabase.CreateAsset(globalPPV.sharedProfile, "Assets/Post Process Volume (Fries).asset");
                    AssetDatabase.SaveAssets();
                    #endif
                }

                if (globalPPV.sharedProfile.GetSetting<Bloom>() == null)
                    globalPPV.sharedProfile.AddSettings<Bloom>();
                
                Bloom b = globalPPV.sharedProfile.GetSetting<Bloom>();
                if (!b.active) b.active = true;
                if (!b.enabled) b.enabled.value = true;
                
                b.intensity.value = 14.5f;
                b.intensity.overrideState = true;
                b.threshold.value = 2f;
                b.threshold.overrideState = true;
            }
        }
# endif
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(FriesManagerBRP))]
    public class FriesInitializerInspector : AnInspector { }
    #endif
}