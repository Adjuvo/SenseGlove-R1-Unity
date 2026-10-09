using R1;
using System.Collections;
using UnityEngine;

public class R1_UnitTests : MonoBehaviour
{
    private bool running = false;


    private IEnumerator RunTests()
    {
        Debug.Log("Starting R1-Unity Unit Tests...");
        yield return null; //wait 1 frame

        yield return null; //wait 1 frame
        Debug.Log("Init. This might freeze unless you have the Python API running!");
        R1_API.Init();
        Debug.Log("Initialized!");
        yield return new WaitForSeconds(2.0f);

        yield return null; //wait 1 frame
        int[] deviceIds = R1_API.GetDeviceIds();
        Debug.Log($"Found {deviceIds.Length} deviceId(s). Running tests for each:");

        yield return null; //wait 1 frame
        int leftID = R1_API.GetLeftHandDeviceId();
        int rightId = R1_API.GetRightHandDeviceId();
        Debug.Log($"Specific DeviceIds: L={leftID} R={rightId}");

        foreach (int deviceId in deviceIds)
        {

            //Reading (Python -> unity)
            yield return null; //wait 1 frame
            SGDeviceInfo deviceInfo = R1_API.GetDeviceInfo(deviceId);
            Debug.Log($"R1_API.GetDeviceInfo({deviceId}) -> {deviceInfo.ToString()}");

            yield return null; //wait 1 frame
            float[][] exo_deg = R1_API.GetExoAngles_Deg(deviceId);
            Debug.Log($"R1_API.GetExoAngles_Deg({deviceId}) -> {ToString(exo_deg)}");

            yield return null; //wait 1 frame
            float[][] exo_rad = R1_API.GetExoAngles_Rad(deviceId);
            Debug.Log($"R1_API.GetExoAngles_Rad({deviceId}) -> {ToString(exo_rad)}");

            yield return null; //wait 1 frame
            bool exo_joints = R1_API.GetExoJoints_Pos_Rot(deviceId, out Vector3[][] positions, out Quaternion[][] rotations);
            Debug.Log($"R1_API.GetExoJoints_Pos_Rot({deviceId}) -> {exo_joints} / {ToString(positions)} / {ToString(rotations)}");

            yield return null; //wait 1 frame
            float[][] exo_lengths = R1_API.GetExoLinkageLengths(deviceId);
            Debug.Log($"R1_API.GetExoLinkageLengths({deviceId}) -> {ToString(exo_lengths)}");

            yield return null; //wait 1 frame
            float[] distances = R1_API.GetFingerTipDistances(deviceId);
            Debug.Log($"R1_API.GetFingerTipDistances({deviceId}) -> {ToString(distances)}");

            yield return null; //wait 1 frame
            bool gotTips = R1_API.GetFingertips_Pos_Rot(deviceId, out Vector3[] tipPos, out Quaternion[] tipRot);
            Debug.Log($"R1_API.GetFingertips_Pos_Rot({deviceId}) -> {gotTips} / {ToString(tipPos)} / {ToString(tipRot)}");

            yield return null; //wait 1 frame
            float[] forcesSensed = R1_API.GetForcesSensed(deviceId);
            Debug.Log($"R1_API.GetForcesSensed({deviceId}) -> {ToString(forcesSensed)}");

            yield return null; //wait 1 frame
            Hand handed = R1_API.GetHandedness(deviceId);
            Debug.Log($"R1_API.GetHandedness({deviceId}) -> {handed}");

            yield return null; //wait 1 frame
            bool percs = R1_API.GetPercentageBents(deviceId, out int[] flex_per, out int[] abd_perc);
            Debug.Log($"R1_API.GetPercentageBents({deviceId}) -> {percs} / {ToString(flex_per)} / {ToString(abd_perc)}");

            yield return null; //wait 1 frame
            bool rawPerc = R1_API.GetRawPercentageBentAngles(deviceId, out float[] flexion_angles, out float[] abd_angles);
            Debug.Log($"R1_API.GetRawPercentageBentAngles({deviceId}) -> {rawPerc}");

            yield return null; //wait 1 frame
            bool active = R1_API.IsDeviceActive(deviceId);
            Debug.Log($"R1_API.IsDeviceActive({deviceId}) -> {active}");

            yield return null; //wait 1 frame
            bool leftHand = R1_API.IsLeftHand(deviceId);
            Debug.Log($"R1_API.IsLeftHand({deviceId}) -> {leftHand}");

            yield return null; //wait 1 frame
            int force = R1_API.NrOfFingersForce(deviceId);
            Debug.Log($"R1_API.NrOfFingersForce({deviceId}) -> {force}");

            yield return null; //wait 1 frame
            int tracking = R1_API.NrOfFingersTracking(deviceId);
            Debug.Log($"R1_API.NrOfFingersTracking({deviceId}) -> {tracking}");

            yield return new WaitForSeconds(0.2f);
            int register = R1_FastCom.Writer_RegisterDevice(deviceId);
            Debug.Log($"R1_API.Writer_RegisterDevice({deviceId}) -> {register}");

            if (register > 0)
            {
                float testPulse = 1.0f;
                Debug.Log($"Set Forces for all fingers for {testPulse} second");
                float startTime = Time.time;
                float elapsed = 0;
                do
                {
                    yield return null; //wait 1 frame
                    R1_API.SetForceGoals(deviceId, new int[5] { 4000, 4000, 4000, 4000, 4000 });
                    elapsed = Time.time - startTime;
                }
                while (elapsed < testPulse);

                yield return null; //wait 1 frame
                R1_API.SetForceGoals(deviceId, new int[5]);
                Debug.Log("Forced de-activated");
            }
            else
            {
                Debug.LogError($"Could not register glove with id {deviceId}, return code = {register}" );
            }

            yield return null; //wait 1 frame
            int unregister = R1_FastCom.Writer_UnregisterDevice(deviceId);
            Debug.Log($"R1_API.Writer_RegisterDevice({deviceId}) -> {unregister}");

            //yield return null; //wait 1 frame
            //Debug.LogError("TODO: Implememnt Vibration Commands - Separate Task");
        }

        yield return new WaitForSeconds(1.0f);
        Debug.Log("Disposing of API resources");
        R1_API.Dispose();
        Debug.Log("Disposed");

        Debug.Log("Unit Tests Complete");
        running = false;
    }

    private static string ToString<T>(T[][] array)
    {
        string val = "[";
        for (int i=0; i<array.Length; i++)
        {
            if (i > 0)
                val += ", ";
            val += ToString(array[i]);
        }
        return val + "]";
    }

    private static string ToString<T>(T[] array)
    {
        string val = "[";
        for (int i = 0; i < array.Length; i++)
        {
            if (i > 0)
                val += ", ";
            val += array[i].ToString();
        }
        return val + "]";
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (running)
                return;
            running = true;
            StartCoroutine(RunTests());
        }
    }

}
