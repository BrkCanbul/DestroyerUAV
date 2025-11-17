

# if UNITY_EDITOR
namespace Seagull.City_03.SceneProps.Setup {
    using UnityEngine.Rendering;
    using System.Threading;
    using UnityEditor;
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.Requests;
    using UnityEngine;
    

// 保证脚本在Editor编译后自动执行
    public static class SetupScriptURP {
        
        private const string PostProcessPackage = "com.unity.postprocessing";
        private const string UniversalRpPackage = "com.unity.render-pipelines.universal";
        private const string MathematicsPackage = "com.unity.mathematics";

        // 当脚本重新编译完成后，自动执行以下方法
        private static void checkPackagesOnLoad() {
            // 同步方式列出已安装的 package
            ListRequest listRequest = Client.List(true);
            while (!listRequest.IsCompleted) {
                Thread.Sleep(100);
            }

            if (listRequest.Status == StatusCode.Success) {
                bool hasPostProcess = false;
                bool hasUniversalRP = false;
                bool hasMathematics = false;

                // 检查当前已安装的所有包
                foreach (var package in listRequest.Result) {
                    if (package.name == PostProcessPackage) hasPostProcess = true;
                    if (package.name == UniversalRpPackage) hasUniversalRP = true;
                    if (package.name == MathematicsPackage) hasMathematics = true;
                }

                // 如果有 postprocessing，就卸载
                if (hasPostProcess) {
                    Debug.Log("Postprocessing package found. Removing...");
                    RemoveRequest removeRequest = Client.Remove(PostProcessPackage);
                    while (!removeRequest.IsCompleted) {
                        Thread.Sleep(100);
                    }

                    if (removeRequest.Status == StatusCode.Success)
                        Debug.Log("Postprocessing package removed successfully.");
                    else
                        Debug.LogError($"Failed to remove {PostProcessPackage}. Error: {removeRequest.Error.message}");
                }
                // 如果有 数学包，就卸载
                if (hasMathematics) {
                    Debug.Log("Mathematics package found. Removing...");
                    RemoveRequest removeRequest = Client.Remove(MathematicsPackage);
                    while (!removeRequest.IsCompleted) Thread.Sleep(100);

                    if (removeRequest.Status == StatusCode.Success) Debug.Log("Mathematics package removed successfully.");
                    else Debug.LogError($"Failed to remove {MathematicsPackage}. Error: {removeRequest.Error.message}");
                }

                // 如果没有 Universal RP，就安装
                if (!hasUniversalRP) {
                    Debug.Log("Universal Render Pipeline not found. Installing...");
                    AddRequest addRequest = Client.Add(UniversalRpPackage);
                    while (!addRequest.IsCompleted) {
                        Thread.Sleep(100);
                    }

                    if (addRequest.Status == StatusCode.Success)
                        Debug.Log("Universal Render Pipeline installed successfully.");
                    else
                        Debug.LogError($"Failed to install {UniversalRpPackage}. Error: {addRequest.Error.message}");
                }
            }
            else {
                Debug.LogError($"Failed to list packages. Error: {listRequest.Error.message}");
            }
        }
        
        [MenuItem("Tools/Fries/City 03/Setup Universal Rendering Pipeline", priority = 2)]
        public static void setup1() {
            RenderPipelineAsset currentRP = GraphicsSettings.defaultRenderPipeline;
            if (currentRP == null) {
                Debug.LogError("Current Render Pipeline is not URP");
                return;
            }
            if (currentRP.GetType().Name != "UniversalRenderPipelineAsset") {
                Debug.LogError("Current Render Pipeline is not URP");
                return;
            }

            checkPackagesOnLoad();
            Debug.Log("Import of dependencies complete!");

            string packagePath = "Assets/Fries and Seagull/City 03/Pipelines/Universal Pipeline.unitypackage";
            AssetDatabase.ImportPackage(packagePath, false);
            Debug.Log("Import of .unitypackage complete!");
            
            // 创建 Fries Initializer
            GameObject friesManager = GameObject.Find("Fries Props Manager");
            if (friesManager == null) {
                friesManager = new GameObject("Fries Props Manager");
                friesManager.AddComponent<FriesManagerURP>();
            }

            if (!friesManager.GetComponent<FriesManagerURP>())
                Debug.LogError("You have an invalid Fries Props Manager in the scene. Please delete it and try again.");

            // 清除当前的选择
            Selection.activeGameObject = null;
            // 设置当前对象为选中状态
            Selection.activeGameObject = friesManager;
            // 也可以将视图聚焦到该对象上
            EditorGUIUtility.PingObject(friesManager);
        }
        
        
        [MenuItem("Tools/Fries/City 03/Setup for URP Lighting Example (Optional)", priority = 53)]
        public static void light() {
            FriesManagerURP.setupLight();
        }
        
        [MenuItem("Tools/Fries/City 03/Revert URP Lighting Example Setting (Optional)", priority = 54)]
        public static void delight() {
            FriesManagerURP.unsetLight();
        }
    }
    
}
# endif