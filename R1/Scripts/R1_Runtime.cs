using R1;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

/*
 * R1 Monobehaviour object that is created at runtime to manage the lifetime of the R1 API and its objects.
 * Since, for example, callbacks are not possible with Shared Memory / FastCom, we invoke events here?
 * 
 * author:
 * max@senseglove.com
 * 
 */



public class R1_Runtime : MonoBehaviour
{
    //--------------------------------------------------------------------------------------------------------------
    // Member Variables

    public KeyCode NextDummyData = KeyCode.D;
    public KeyCode PreviousDummyData = KeyCode.A;

    //--------------------------------------------------------------------------------------------------------------
    // Events

    public static UnityEvent OnDataReceived = new UnityEvent();
    public static UnityEvent OnConnectionChange = new UnityEvent();

    //--------------------------------------------------------------------------------------------------------------
    // Singleton Pattern

    /// <summary> Default </summary>
    private static UnityCommunications s_communicationType = UnityCommunications.FastCom;

    /// <summary> Actual instance currenty in the scene. </summary>
    private static R1_Runtime activeInstance = null;



    private Coroutine updateLoop = null;
    

    public static R1_Runtime Instance
    {
        get 
        {
            CheckInitialize();
            return activeInstance; 
        }
    }

    public static UnityCommunications CommunicationType
    {
        get { return s_communicationType; }
        set
        {
            //TODO: if value is different, swap at runtime.
            s_communicationType = UnityCommunications.FastCom;
        }
    }


    /// <summary> Instantiates a copy of this Runtime inside the Scene if one doesn't yet exist. </summary>
    public static void CheckInitialize()
    {
        if (activeInstance != null)
            return;

        activeInstance = GameObject.FindFirstObjectByType<R1_Runtime>();
        if (activeInstance == null)
        {
            //one does not exist in the scene (yet)
            GameObject r1Obj = new GameObject("R1 Runtime");
            activeInstance = r1Obj.AddComponent<R1_Runtime>();
        } 
        //else it's not NULL so we use it
        GameObject.DontDestroyOnLoad(activeInstance.gameObject);
    }


    public static void CheckDispose()
    {
        if (activeInstance != null)
        {
            GameObject.Destroy(activeInstance.gameObject);
        }
    }



    //--------------------------------------------------------------------------------------------------------------
    // API Wrapper


    private void StartRoutine()
    {
        if (updateLoop != null)
        {
            StopCoroutine(updateLoop);
            updateLoop = null;
        }
        if (s_communicationType == UnityCommunications.UnitTest)
            updateLoop = StartCoroutine(StartUnitTestRoutine());
        else if (s_communicationType == UnityCommunications.FastCom)
            updateLoop = StartCoroutine(StartFastComRoutine());
        else
            Debug.LogError($"R1 Runtime Implementation for {s_communicationType.ToString()} not found");
    }

    private IEnumerator StartUnitTestRoutine()
    {
        while (Application.isPlaying && this.enabled)
        {
            //Update data events
            if (Input.GetKeyDown(this.NextDummyData))
            {
                R1_UnitTestData.NextActiveTest();
                OnConnectionChange?.Invoke();
                OnDataReceived?.Invoke();
            }
            else if (Input.GetKeyDown(this.PreviousDummyData))
            {
                R1_UnitTestData.PreviousActiveTest();
                OnConnectionChange?.Invoke();
                OnDataReceived?.Invoke();
            }
            yield return null;
        }
    }





    private int leftHandId = -1, rightHandId = -1;
    //private int[] leftForceGoals = new int[5];
    //private int[] rightForceGoals = new int[5];
    private R1_MessageLog msgLog = null;

    public static string FindFileInStreamingAssets(string fileName)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;

        string[] streamingAssetFolders = Directory.GetDirectories(
            projectRoot,
            "StreamingAssets",
            SearchOption.AllDirectories
        );
        foreach (string folder in streamingAssetFolders)
        {
            string[] files = Directory.GetFiles(
                folder,
                fileName,
                SearchOption.AllDirectories
            );
            if (files.Length > 0)
            {
                return Path.GetFullPath(files[0]);
            }
        }
        Debug.LogWarning($"Could not find '{fileName}' in any StreamingAssets folder.");
        return null;
    }




    private const string configFileName = "r1_force_feedback_config.json";

    private static string ConfirmInStreamingAssets(bool overrideExiting)
    {
#if UNITY_EDITOR
        try
        {
            string destinationDirectory = Path.Combine(Application.dataPath, "StreamingAssets");
            string destinationPath = Path.Combine(destinationDirectory, configFileName);

            if (System.IO.File.Exists(destinationPath) && !overrideExiting)
                return destinationPath;

            string sourcePath = FindFileInProjects(configFileName);
            if (sourcePath == null || sourcePath.Length == 0)
            {
                Debug.LogError($"The Config file {configFileName} cannot be found!");
                return "";
            }
            Directory.CreateDirectory(destinationDirectory);
            File.Copy(
                sourcePath,
                destinationPath,
                overwrite: true);
            AssetDatabase.Refresh();
            return destinationPath;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message);
        }
#endif
        return "";
    }

    private static string FindFileInProjects(string fileName)
    {
#if UNITY_EDITOR
        string fileName_noExtension = Path.GetFileNameWithoutExtension(fileName);
        string[] guids = AssetDatabase.FindAssets(fileName_noExtension);

        if (guids.Length == 0)
        {
            Debug.LogError(
                $"Could not find {fileName_noExtension}.");
            return null;
        }
        if (guids.Length > 0)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string foundName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (foundName.Equals(fileName_noExtension, System.StringComparison.OrdinalIgnoreCase))
                {
                    //and filename cannot contain stramingassets
                    Debug.Log($"  {path}");
                    return path;
                }
            }
            return "";
        }
#endif
        return "";
    }


    public static string GetFFBConfigPath()
    {
        //Debug.LogError("TODO: Grab Configuration Filepath from streamingassets at runtime");
#if UNITY_EDITOR
            return ConfirmInStreamingAssets(true);
#else
        //Runtime Implementation
        string path = Path.Combine(Application.streamingAssetsPath, configFileName);
        if (File.Exists(path))
        {
            return Path.GetFullPath(path);
        }
        Debug.LogError($"JSON file not found: {path}");
        return "";
#endif
    }

    private IEnumerator StartFastComRoutine()
    {
        //Initialization
        if (msgLog == null)
            msgLog = GameObject.FindFirstObjectByType<R1_MessageLog>();


        R1_FastCom.InitializeDebugger();

        int read_iniCode = R1_FastCom.Init_Reader(false);

        int write_iniCode = -9000;
        string configPath = GetFFBConfigPath();
        if (configPath != null && configPath.Length > 0)
        {
            Debug.Log($"Initializing FastCom Writer with config located at {configPath}");
            write_iniCode = R1_FastCom.Init_Writer(configPath);
        }
        else
        {
            Debug.LogError("Could not locate the Config File! Haptics Writer will not be intialized, and therefore Force-Feedback will not work.");
        }

        Debug.Log($"Completed FastCom Initialization with code {read_iniCode} / {write_iniCode}");
        yield return null; //wait 1 frame

        while (Application.isPlaying && this.enabled)
        {
            //For now, we can do it like this, since unplugging the glove kills the Python process. Assumedly they will never change.
            if (leftHandId < 0)
            {
                int lhID = R1_FastCom.GetLeftHandDeviceId();
                if (lhID > -1)
                {
                    leftHandId = lhID;
                    R1_FastCom.Writer_RegisterDevice(leftHandId);
                    Debug.Log($"Registered Left Hand ID {leftHandId}");
                    OnConnectionChange?.Invoke();
                }
            }
            //if (leftHandId > -1)
            //    R1_FastCom.WriteFast_Int32Array(leftHandId, "force_goals", leftForceGoals);

            if (rightHandId < 0)
            {
                int rhId = R1_FastCom.GetRightHandDeviceId();
                if (rhId > -1)
                {
                    rightHandId = rhId;
                    R1_FastCom.Writer_RegisterDevice(rightHandId);
                    Debug.Log($"Registered Right Hand ID {rightHandId}");
                    OnConnectionChange?.Invoke();
                }
            }
            //if (rightHandId > -1)
            //    R1_FastCom.WriteFast_Int32Array(rightHandId, "force_goals", rightForceGoals);

            if (msgLog != null) //we have a message log in the scene.
            {
                List<string> msg = R1_FastCom.GetQueuedMessages();
                foreach (string message in msg)
                    msgLog.AddMessage(message);
            }
            else
                R1_FastCom.PrintDebugMessages();

            if (leftHandId > 0 || rightHandId > 0)
                OnDataReceived?.Invoke();
            yield return null;
        }
    }

    //--------------------------------------------------------------------------------------------------------------
    // Monobehaviour


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CheckInitialize();
        if (activeInstance != this) //if I've been assigned, otherwise we jsut turn ourselves off.
        {
            this.enabled = false;
            return;
            //To Initialization. Otherwise turn yourself off
        }
        OnConnectionChange?.Invoke();
        OnDataReceived?.Invoke();
        StartRoutine();
    }

    private void OnDestroy()
    {
        if (updateLoop != null)
        {
            StopCoroutine(updateLoop);
            updateLoop = null;
        }
        if (CommunicationType == UnityCommunications.FastCom)
        {
            if (leftHandId > -1)
                R1_FastCom.Writer_UnregisterDevice(leftHandId);
            if (rightHandId > -1)
                R1_FastCom.Writer_UnregisterDevice(rightHandId);

            //TOOD: Offload Initialization to a worker thread so it doesn't freeze our Unity Project
            Debug.Log("Disposing FastCom.");
            int dispCode = R1_FastCom.Dispose();
            Debug.Log($"Completed FastCom Initialization with code {dispCode}");

            R1_FastCom.DisposeDebugger();
        }
        if (activeInstance == this)
        {
            activeInstance = null; //clear reference
        }
    }
}
