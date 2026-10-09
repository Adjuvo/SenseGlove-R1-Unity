using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
 * Static Entry point for the R1 API inside Unity. This is where you grab data / subscribe to events.
 * May spawn additional GameObjects / Components to manage connection lifetimes.
 * 
 * author:
 * max@senseglove.com
 */

namespace R1
{

    public class R1_API
    {
        private static UnityCommunications s_UnityCommunications = UnityCommunications.FastCom;

        private const int test_leftID = 69, test_rightID = 420;



        private static Dictionary<int, SGDeviceInfo> deviceInfoCache = new Dictionary<int, SGDeviceInfo>();

        private static bool isInitialized = false;







        /// <summary> Initialize the back-end instance(s) and runtime variables </summary>
        public static void Init() //TODO: Communication Type.
        {
            if (isInitialized)
                return;

            isInitialized = true;
            Debug.Log("Initialized R1 Runtime");
            R1_Runtime.CommunicationType = s_UnityCommunications;
            R1_Runtime.CheckInitialize(); //initialize if if hasn't done so already
            R1_Runtime.OnDataReceived.AddListener(OnData);
            R1_Runtime.OnConnectionChange.AddListener(OnConnection);
            //The Runtime is what will initialize the actual back-end and gets destroyed when the game ends. It also generates events, since Shared Memory / Dummy Data cannot.
        }


        /// <summary> Clean up the back-end instance(s) and runtime variables. </summary>
        public static void Dispose()
        {
            isInitialized = false;
            //Todo: Check if an instance of our runtime exists, if is does; dispose.
            //Instance should contain all required data, and shut its;ef down when the Gameobject is destroyed / the simulation is shut down.
            Debug.Log("Disposed of R1 Runtime");

            R1_Runtime.CheckDispose();
            R1_Runtime.OnDataReceived.RemoveListener(OnData);
            R1_Runtime.OnConnectionChange.RemoveListener(OnConnection);
        }



        // Side-implementation via other API function(s)
        public static bool IsDeviceActive(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
                return R1_UnitTestData.GetTestCount() > 0;
            else if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                //Todo: Check if a proper C++ implementation is required.
                //However, for now, the API crashes whenever you unplug a glove - so if a glove appears in de list it's active and sending data
                int[] ids = GetDeviceIds();
                foreach (int id in ids)
                {
                    if (id == deviceId)
                        return true;
                }
                return false;
            } 
            throw new System.NotImplementedException();
        }


        // Full implementation via FastCom Function.
        public static int[] GetDeviceIds()
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
                return R1_UnitTestData.GetTestCount() > 0 ? new int[] { test_rightID, test_leftID } : new int[0];
            else if (s_UnityCommunications == UnityCommunications.FastCom)
                return R1_FastCom.GetDeviceIds();

            throw new System.NotImplementedException();
        }

        // Full implementation via FastCom Function.
        public static int GetRightHandDeviceId()
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
                return R1_UnitTestData.GetTestCount() > 0 ? test_rightID : 0;
            else if (s_UnityCommunications == UnityCommunications.FastCom)
                return R1_FastCom.GetRightHandDeviceId();

            throw new System.NotImplementedException();
        }

        // Full implementation via FastCom Function.
        public static int GetLeftHandDeviceId()
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
                return R1_UnitTestData.GetTestCount() > 0 ? test_leftID : 0;
            else if (s_UnityCommunications == UnityCommunications.FastCom)
                return R1_FastCom.GetLeftHandDeviceId();
            throw new System.NotImplementedException();
        }


        // Side-implementation via other API Function.
        public static bool IsLeftHand(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
                return deviceId == test_leftID;
            if (s_UnityCommunications == UnityCommunications.FastCom)
                return GetHandedness(deviceId) == Hand.LEFT;
            throw new System.NotImplementedException();
        }

        // Side-implementation via other API Function.
        public static Hand GetHandedness(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
                return deviceId == test_leftID ? Hand.LEFT : Hand.RIGHT;
            else if (s_UnityCommunications == UnityCommunications.FastCom)
                return GetDeviceInfo(deviceId).Hand;
            throw new System.NotImplementedException();
        }

        // Side-implementation via other API Function.
        public static int NrOfFingersTracking(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
                return 5;
            else if(s_UnityCommunications == UnityCommunications.FastCom)
                return GetDeviceInfo(deviceId).NrFingersTracking;
            throw new System.NotImplementedException();
        }

        // Side-implementation via other API Function.
        public static int NrOfFingersForce(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
                return 4;
            else if(s_UnityCommunications == UnityCommunications.FastCom)
                return GetDeviceInfo(deviceId).NrFingersForce;
            throw new System.NotImplementedException();
        }

        /// <summary> Fake implementation fired once per frame </summary>
        public static UnityEvent OnNewData = new UnityEvent();

        /// <summary>  </summary>
        public static UnityEvent OnConnectionChange = new UnityEvent();


        // Yet to be implemented
        public static float[][] GetExoAngles_Rad(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                if (R1_UnitTestData.LoadCurrentTest(out TestCase tCase))
                    return tCase.exo_angles_rad;
                return new float[0][];
            }
            if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                if (R1_FastCom.ReadFast_Float2DArray(deviceId, "exo_angles_rad_filtered", 40, out float[][] angles) > 0)
                    return angles;
                return new float[0][];
            }
            throw new System.NotImplementedException();
        }

        // Side-implementation via other API Function.
        public static float[][] GetExoAngles_Deg(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                if (R1_UnitTestData.LoadCurrentTest(out TestCase tCase))
                    return tCase.exo_angles_deg;
                return new float[0][];
            }
            if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                if (R1_FastCom.ReadFast_FlatFloat2DArray(deviceId, "exo_angles_rad_filtered", 40, out float[] values, out int rows, out int columns) > 0)
                {   //Might as well convert the (quicker) flat array AND convert it to degrees for now.
                    float[][] angles = new float[rows][];
                    for (int r = 0; r < rows; r++)
                    {
                        angles[r] = new float[columns];
                        for (int c = 0; c < columns; c++)
                        {
                            angles[r][c] = values[r * columns + c] * Mathf.Rad2Deg; //convert these. TODO: Invert them too?
                        }
                    }
                    return angles;
                }    
                return new float[0][];
            }
            throw new System.NotImplementedException();
        }



        // Full implementation via FastCom Function.
        public static SGDeviceInfo GetDeviceInfo(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                SGDeviceInfo res = new SGDeviceInfo();
                res.ComType = CommunicationType.UNIT_TEST;
                res.DataOrigin = DataOrigin.LIVE_TEST_SIM;
                res.DeviceType = DeviceType.Rembrandt;
                res.DeviceID = deviceId;
                res.FirmwareVersion = "0.0.0-unit";
                res.Hand = GetHandedness(deviceId);
                res.ExoLinkageType = ExoLinkageType.REMBRANDT_PROTO_05;
                res.EncodingType = EncodingType.REMBRANDT_v01;
                res.NrFingersTracking = NrOfFingersTracking(deviceId);
                return res;
            }
            else if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                if (deviceInfoCache.TryGetValue(deviceId, out SGDeviceInfo cachedData))
                    return cachedData; //already been retrieved once
                if (IsDeviceActive(deviceId)) //only store / chache it if it exists in active devices.
                {
                    int res = FastCom_GetDeviceInfo(deviceId, out SGDeviceInfo info);
                    deviceInfoCache[deviceId] = info;
                    return res < 0 ? new SGDeviceInfo() : info;
                }
                else
                    return new SGDeviceInfo();
                
            }
            throw new System.NotImplementedException();
        }



        /// <summary> Reads DeviceInfo form a particular field. If said field does not exist, return fallback </summary>
        /// <param name="deviceId"></param>
        /// <param name="fieldName"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        public static int FastCom_GetDeviceInfo_Int32_WithFallBack(int deviceId, string fieldName, int fallback)
        {
            int code = R1_FastCom.GetDeviceInfo_Int32(deviceId, fieldName);
            if (code > -1)
                return code;
            return fallback;
        }

        /// <summary> Generate Local SG DeviceInfo out of a number of DeviceInfo calls </summary>
        /// <param name="deviceId"></param>
        /// <param name="deviceInfo"></param>
        /// <returns></returns>
        public static int FastCom_GetDeviceInfo(int deviceId, out SGDeviceInfo deviceInfo)
        {
            deviceInfo = new SGDeviceInfo();
            deviceInfo.DeviceID = deviceId;

            //if there is a FW version and one other thing, it's a success. The others I grab if needed.
            int fwCode = R1_FastCom.GetFirmwareVersion(deviceId, out string fwString);
            if (fwCode > 0) //iif we get here, I know the device exists.
            {
                deviceInfo.FirmwareVersion = fwString;

                deviceInfo.ComType = (CommunicationType)FastCom_GetDeviceInfo_Int32_WithFallBack(deviceId, "communication_type", 0);
                deviceInfo.DataOrigin = (DataOrigin)FastCom_GetDeviceInfo_Int32_WithFallBack(deviceId, "data_origin", 0);
                deviceInfo.DeviceType = (DeviceType)FastCom_GetDeviceInfo_Int32_WithFallBack(deviceId, "device_type", 0);
                deviceInfo.EncodingType = (EncodingType)FastCom_GetDeviceInfo_Int32_WithFallBack(deviceId, "encoding_type", 0);
                deviceInfo.ExoLinkageType = (ExoLinkageType)FastCom_GetDeviceInfo_Int32_WithFallBack(deviceId, "exo_linkage_type", 0);
                deviceInfo.Hand = FastCom_GetDeviceInfo_Int32_WithFallBack(deviceId, "hand", 0) == 0 ? Hand.LEFT : Hand.RIGHT;
                deviceInfo.NrFingersForce = FastCom_GetDeviceInfo_Int32_WithFallBack(deviceId, "nr_fingers_force", 0);
                deviceInfo.NrFingersTracking = FastCom_GetDeviceInfo_Int32_WithFallBack(deviceId, "nr_fingers_tracking", 0);
            }
            return fwCode;
        }



        private static float[][] ut_exoLinks = new float[5][]
        {
            new float[8] { 11.62f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 10.0f },
            new float[8] { 11.62f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 10.0f },
            new float[8] { 11.62f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 10.0f },
            new float[8] { 11.62f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 10.0f },
            new float[8] { 11.62f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 10.0f }
        };


        // Fake implementation using Dummy Data. -could- be re-calculated from Sensor Data.
        public static float[][] GetExoLinkageLengths(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                return ut_exoLinks;
                //TODO: Calculate these for the exoJointPositions if need be
            }
            if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                Debug.Log("TODO: Implement ExoLinkageLengths through FastCom or DataBase");
                return ut_exoLinks;
            }
            throw new System.NotImplementedException();
        }


        // Yet to be implemented
        public static bool GetFingertips_Pos_Rot(int deviceId, out Vector3[] positions, out Quaternion[] rotations)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                if (R1_UnitTestData.LoadCurrentTest(out TestCase tCase))
                {
                    positions = tCase.fingerip_poss;
                    rotations = tCase.fingerip_rots;
                    return true;
                }
                positions = new Vector3[5];
                rotations = new Quaternion[5];
                return false;
            }
            if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                //Grab the deviceInd
                SGDeviceInfo info = GetDeviceInfo(deviceId);
                if (FastCom_GetFingertips_Pos_Rot(deviceId, info.NrFingersTracking, out positions, out rotations) > 0)
                {
                    //TODO: Conditioning / fixing?
                    return true;
                }
                positions = new Vector3[5];
                rotations = new Quaternion[5];
                return false;
            }

            throw new System.NotImplementedException();
        }


        /// <summary> Return the fingertip Positions / Rotations </summary>
        /// <param name="deviceId"></param>
        /// <param name="fingersWithTracking"></param>
        /// <param name="positions"></param>
        /// <param name="rotations"></param>
        /// <returns></returns>
        public static int FastCom_GetFingertips_Pos_Rot(int deviceId, int fingersWithTracking, out UnityEngine.Vector3[] positions, out UnityEngine.Quaternion[] rotations)
        {
            positions = null; //since I want to avoid re-assingning them unless I have to
            rotations = null;

            //Positions
            int buffSize = fingersWithTracking * 3; //xyz
            int rows = 0, columns = 0;
            float[] values;
            int res = R1_FastCom.ReadFast_FlatFloat2DArray(deviceId, "fingertips_pos", buffSize, out values, out rows, out columns);
            if (res < 0)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: Error code {res}!");
                return -9000;
            }
            if (rows != fingersWithTracking)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: FingersWithTracking is set to {fingersWithTracking}, but this does not match {rows} rows of data!");
                return -9001;
            }
            if (columns != 3)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: Expecting 3 column values for XYZ but got {columns}");
                return -9002;
            }
            //Now we can actually do zee parsing.
            positions = new UnityEngine.Vector3[rows];
            for (int f = 0; f < rows; f++) //foreach finger
            {
                int fI = f * 3; //start index for this particular fingers
                positions[f] = ToUnityPosition(values[fI], values[fI+1], values[fI+2]);
            }

            //Rotations- re using some of the variables...
            buffSize = fingersWithTracking * 4; //xyzw
            res = R1_FastCom.ReadFast_FlatFloat2DArray(deviceId, "fingertips_rot", buffSize, out values, out rows, out columns);
            if (res < 0)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: Error code {res}!");
                return -9003;
            }
            if (rows != fingersWithTracking)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: FingersWithTracking is set to {fingersWithTracking}, but this does not match {rows} rows of data!");
                return -9004;
            }
            if (columns != 4)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: Expecting 4 column values for XYZW but got {columns}");
                return -9005;
            }
            //Now we can actually do zee parsing.
            rotations = new UnityEngine.Quaternion[rows];
            for (int f = 0; f < rows; f++) //foreach finger
            {
                int fI = f * 3; //start index for this particular fingers
                rotations[f] = ToUnityRotation(values[fI], values[fI + 1], values[fI + 2], values[fI + 3]);
            }
            return 1;
        }





        // Side-implementation via other API Function.
        public static float[] GetFingerTipDistances(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                if (R1_UnitTestData.LoadCurrentTest(out TestCase tCase))
                    return tCase.fingertip_distances;
            }
            else if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                if (GetFingertips_Pos_Rot(deviceId, out Vector3[] pos, out Quaternion[] rots))
                {
                    float[] dists = new float[pos.Length - 1];
                    for (int f=0; f<pos.Length - 1; f++)
                    {
                        dists[f] = (pos[f + 1] - pos[0]).magnitude;
                    }
                    return dists;
                }
                return new float[4];
            }
            throw new System.NotImplementedException();
        }


        //Not implemented yet due to missing 3D array implementation [f][i][x,y,z]
        public static bool GetExoJoints_Pos_Rot(int deviceId, out Vector3[][] positions, out Quaternion[][] rotations)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                if (R1_UnitTestData.LoadCurrentTest(out TestCase tCase))
                {
                    positions = tCase.exo_poss;
                    rotations = tCase.exo_rots;
                    return true;
                }
                positions = new Vector3[0][];
                rotations = new Quaternion[0][];
                return false;
            }
            else if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                SGDeviceInfo info = GetDeviceInfo(deviceId);
                int exoPos = 9; //8 positions + tip?
                int posCode = FastCom_GetExoJointPositions(deviceId, info.NrFingersTracking, exoPos, out positions);
                if (posCode > 0)
                {
                    int rotCode = FastCom_GetExoJointRotations(deviceId, info.NrFingersTracking, exoPos, out rotations);
                    if (rotCode > 0)
                        return true;
                }

                positions = new Vector3[0][];
                rotations = new Quaternion[0][];
                return false;
            }
            throw new System.NotImplementedException();
        }


        private static Vector3 ToUnityPosition(float sgX, float sgY, float sgZ)
        {
            //Y up [mm] -> Z up [m] 
            return new Vector3
            (
                sgX / 1000.0f,
                sgZ / 1000.0f,
                sgY / 1000.0f
            );
        }

        private static Quaternion ToUnityRotation(float sgW, float sgX, float sgY, float sgZ)
        {
            //python Quat [w x y z]
            //-quat.x, -quat.z, -quat.y, quat.w
            return new Quaternion
            (
                -sgX,
                -sgZ,
                -sgY,
                sgW
            );
        }

        private static int FastCom_GetExoJointPositions(int deviceId, int fingersWithTracking, int exoPositionCount, out Vector3[][] positions)
        {
            positions = new Vector3[0][];
            int bufferSize = fingersWithTracking * exoPositionCount * 3; //x3 because xyz

            int result = R1_FastCom.ReadFast_FlatFloat3DArray(deviceId, "exo_joint_poss", bufferSize, out float[] values,
                out int rows, out int columns, out int depth);
            
            if (result < 0)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: Error code {result}!");
                return -9000;
            }
            if (rows != fingersWithTracking)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: FingersWithTracking is set to {fingersWithTracking}, but this does not match {rows} rows of data!");
                return -9001;
            }
            if (columns != exoPositionCount)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: FingersWithTracking is set to {exoPositionCount}, but this does not match {columns} columns of data!");
                return -9002;
            }
            if (depth != 3)
            {
                UnityEngine.Debug.LogError($"fingertips_pos for {deviceId}: Expecting 3 column values for XYZ but got {depth}");
                return -9003;
            }

            //we have the correct setup...
            positions = new UnityEngine.Vector3[rows][];
            for (int f = 0; f < rows; f++) //foreach finger
            {
                positions[f] = new Vector3[columns];
                for (int j=0; j<columns; j++)
                {
                    int index = (f * columns + j) * depth;
                    positions[f][j] = ToUnityPosition(values[index], values[index + 1], values[index + 2]);
                }
                
            }
            return result;
        }

        private static int FastCom_GetExoJointRotations(int deviceId, int fingersWithTracking, int exoPositionCount, out Quaternion[][] rotations)
        {
            rotations = new Quaternion[0][];
            int bufferSize = fingersWithTracking * exoPositionCount * 4; //x3 because xyzw

            int result = R1_FastCom.ReadFast_FlatFloat3DArray(deviceId, "exo_joint_rots", bufferSize, out float[] values,
                out int rows, out int columns, out int depth);

            if (result < 0)
            {
                UnityEngine.Debug.LogError($"exo_joint_rots for {deviceId}: Error code {result}!");
                return -9000;
            }
            if (rows != fingersWithTracking)
            {
                UnityEngine.Debug.LogError($"exo_joint_rots for {deviceId}: FingersWithTracking is set to {fingersWithTracking}, but this does not match {rows} rows of data!");
                return -9001;
            }
            if (columns != exoPositionCount)
            {
                UnityEngine.Debug.LogError($"exo_joint_rots for {deviceId}: FingersWithTracking is set to {exoPositionCount}, but this does not match {columns} columns of data!");
                return -9002;
            }
            if (depth != 4)
            {
                UnityEngine.Debug.LogError($"exo_joint_rots for {deviceId}: Expecting 4 column values for XYZW but got {depth}");
                return -9003;
            }

            //we have the correct setup...
            rotations = new UnityEngine.Quaternion[rows][];
            for (int f = 0; f < rows; f++) //foreach finger
            {
                rotations[f] = new Quaternion[columns];
                for (int j = 0; j < columns; j++)
                {
                    int index = (f * columns + j) * depth;
                    rotations[f][j] = ToUnityRotation(values[index], values[index + 1], values[index + 2], values[index + 3]);
                }

            }
            return result;
        }


        // Full implementation via FastCom Function.
        public static bool GetPercentageBents(int deviceId, out int[] flexion_perc, out int[] abduction_perc)
        {
            flexion_perc = new int[5];
            abduction_perc = new int[5];
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                if (R1_UnitTestData.LoadCurrentTest(out TestCase tCase))
                {
                    flexion_perc = tCase.flex_perc_bents;
                    abduction_perc = tCase.abd_perc_bents;
                    return true;
                }
                return false;
            }
            else if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                int fInt = R1_FastCom.ReadFast_Int32Array(deviceId, "flex_perc_bents", 5, out flexion_perc);
                if (fInt > 0)
                {
                    int aInt = R1_FastCom.ReadFast_Int32Array(deviceId, "abd_perc_bents", 5, out abduction_perc);
                    if (aInt > 0)
                        return true;
                }

                flexion_perc = new int[5];
                abduction_perc = new int[5];
                return false;
            }
            throw new System.NotImplementedException();
        }

        // Not yet implemented
        public static bool GetRawPercentageBentAngles(int deviceId, out float[] flexion_angles, out float[] abduction_angles)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                if (R1_UnitTestData.LoadCurrentTest(out TestCase tCase))
                {
                    flexion_angles = tCase.flex_perc_bent_angles;
                    abduction_angles = tCase.abd_perc_bent_angles;
                    return true;
                }
            }
            if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                Debug.Log("TODO: Calculate / Get GetRawPercentageBentAngles over FastCom?");
                flexion_angles = new float[0];
                abduction_angles = new float[0];
                return false;
            }
            throw new System.NotImplementedException();
        }


        // Not yet implemented
        public static void SetPercentageBentVars(int deviceId,
            float[] min_thetas_flexion,
            float[] max_thetas_flexion,
            float[] min_thetas_abduction,
            float[] max_thetas_abduction,
            int out_max_perc_bent = 10000)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                return; //TODO: Recalculate Perc_bents ?
            }
            throw new System.NotImplementedException();
        }


        // Not yet implemented
        public static float[] GetForcesSensed(int deviceId)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                return new float[4]; //0.0f
            }
            if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                int res = R1_FastCom.ReadFast_FloatArray(deviceId, "forces_sensed", 5, out float[] values);
                if (res > 0)
                    return values;
                return new float[0];
            }
            throw new System.NotImplementedException();
        }



        // Not yet implemented
        public static void SetForceGoals(int deviceId, int[] force_goals)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                return;
            }
            if (s_UnityCommunications == UnityCommunications.FastCom)
            {
                R1_FastCom.WriteFast_Int32Array(deviceId, "force_goals", force_goals);
                return;
            }
            throw new System.NotImplementedException();
        }


        // Not yet implemented
        public static void SetForceGoalsWithControlMode(int deviceId, float[] force_goals, ControlMode[] control_modes)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                return;
            }
            throw new System.NotImplementedException();
        }


        // Not yet implemented
        public static void SetRawVibroData(int deviceId, int[][] vibro_data)
        {
            if (s_UnityCommunications == UnityCommunications.UnitTest)
            {
                return;
            }
            throw new System.NotImplementedException();
        }




        //Invoked from Runtime since SharedMem does not implement events. Later on, could be implemented through callback in the C++ back-end.
        private static void OnData()
        {
            OnNewData?.Invoke();
        }

        private static void OnConnection()
        {
            OnConnectionChange?.Invoke();
        }


    }


    public enum Hand
    {
        UNKNOWN = -1,
        LEFT = 0,
        RIGHT = 1
    }

    public enum ControlMode
    {
        FORCE_GOAL_DEFAULT = 0,
        //"""This is the default mode. The motors will try to reach the force goal set. Set the force goal to 0 to move freely."""
        
        OFF = 9999,
        //"""Motors will fully turn off, so also not move with your finger"""
    
        //CALIBRATE_TRACKING_NEVER_EVER_USE = 42666 
        //"""DON'T EVER SEND THIS TO THE GLOVE, it will reset tracking offset angles. It will ruin it the glove for good and requires customer support to fix it."""
    }

    public enum DeviceType
    {
        Rembrandt,
    }

    public enum UnityCommunications
    {
        UnitTest,
        FastCom
    }

    public enum ExoLinkageType
    {
        REMBRANDT_PROTO_02,
        REMBRANDT_PROTO_03,
        REMBRANDT_PROTO_04,
        REMBRANDT_PROTO_05
    }
    
    public enum EncodingType
    {
        REMBRANDT_v01
    }
    
    public enum DataOrigin
    {
        CPP_SDK, // = "DATA_BUFFER" # also for CPP API
        LIVE_TEST_SIM // = "LIVE_TEST_SIM"
    }

    public enum CommunicationType
    {
        UNIT_TEST = 0,
        SIMULATED_GLOVE = 1,
        REAL_GLOVE_USB = 2
    }



    public class SGDeviceInfo
    {
        public int DeviceID { get; set; }
        public Hand Hand { get; set; }

        public int NrFingersTracking { get; set; }

        public int NrFingersForce { get; set; }

        public string FirmwareVersion { get; set; }

        public DeviceType DeviceType { get; set; }

        public CommunicationType ComType { get; set; }

        public ExoLinkageType ExoLinkageType { get; set; }

        public EncodingType EncodingType { get; set; }

        public DataOrigin DataOrigin { get; set; }

        public SGDeviceInfo()
        {

        }

        public override string ToString()
        {
            return $"DeviceID: {DeviceID}, " +
              $"Hand: {Hand}, " +
              $"NrFingersTracking: {NrFingersTracking}, " +
              $"NrFingersForce: {NrFingersForce}, " +
              $"FirmwareVersion: {FirmwareVersion}, " +
              $"DeviceType: {DeviceType}, " +
              $"ComType: {ComType}, " +
              $"ExoLinkageType: {ExoLinkageType}, " +
              $"EncodingType: {EncodingType}, " +
              $"DataOrigin: {DataOrigin}";
        }
    }

    public class ThimbleDimensions
    {
        public Vector3 SphereCenterPos { get; set; }

        public Quaternion Rotation { get; set; }

        public float Radius { get; set; }

        public Vector3 CylinderCenterPos { get; set; }

        public float CylinderLength { get; set; }

    }
}