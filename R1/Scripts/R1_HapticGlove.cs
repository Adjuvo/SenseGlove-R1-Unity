using SG;
using SGCore.Kinematics;
using UnityEngine;

/*
 * Represents a instance of an R1 Glove that can provide data to our Unity API (SG_HandPose)
 */

namespace R1
{

    public class R1_HapticGlove : MonoBehaviour, SG.IHandPoseProvider
    {
        //-------------------------------------------------------------------------------------------------------------
        // R1 Parameters / Tuning

        /// <summary> sets whether or not this instance connects to the left or right hand. </summary>
        public Hand handSide = Hand.RIGHT;


        protected SGCore.Kinematics.BasicHandModel internalHandModel = null;

        protected SG_HandPose lastHandPose = null;
        protected volatile bool updatePose = true; //if true, we update the pose when requested. Otherwise we 

        protected volatile int deviceID = -1;

        //-------------------------------------------------------------------------------------------------------------
        // IHandPoseProvider Implementation

        // Static stuff

        public bool TracksRightHand()
        {
            return this.handSide == Hand.RIGHT;
        }
        public HandTrackingDevice TrackingType()
        {
            return HandTrackingDevice.HapticGlove;
        }

        // Setup / control

        public BasicHandModel GetKinematics()
        {
            if (internalHandModel == null)
                internalHandModel = SGCore.Kinematics.BasicHandModel.Default(this.TracksRightHand());
            return internalHandModel;
        }

        public void SetKinematics(BasicHandModel handModel)
        {
            this.internalHandModel = handModel;
        }




        public bool IsConnected() //Check the R1 API for any updates to data?
        {
            return this.deviceID > -1;
        }

        // Hand Tracking

        public bool GetHandPose(out SG_HandPose handPose, bool forcedUpdate = false)
        {
            if (updatePose || forcedUpdate)
            {
                //TODO: grab from the latest Flexion . TODO: Check which one you belong to...
                if (R1_API.GetPercentageBents(this.deviceID, out int[] flexionValues, out int[] abductionValues))
                {
                    //TODO: Calculate an SG_HandPose from these things.
                    SG_HandPose pose = CalculateSimplePose(flexionValues, abductionValues, this.GetKinematics());

                    //TODO: Add proper wirst tracking interface
                    pose.wristPosition = this.transform.position;
                    pose.wristRotation = this.transform.rotation;

                    lastHandPose = pose;
                }
                updatePose = false;
            }
            handPose = lastHandPose;
            return handPose != null;
        }


        public bool GetNormalizedFlexion(out float[] flexions)
        {
            flexions = lastHandPose != null ? lastHandPose.normalizedFlexion : new float[0];
            return lastHandPose != null;
        }


        //Unused

        public bool TryGetBatteryLevel(out float value01)
        {
            value01 = 0.0f;
            return false;
        }

        public float OverrideGrab()
        {
            return 0.0f;
        }

        public float OverrideUse()
        {
            return 0.0f;
        }

        //-------------------------------------------------------------------------------------------------------------
        // R1_HapticGlove Management

        private void OnConnectionUpdate()
        {
            this.deviceID = this.TracksRightHand() ? 
                R1_API.GetRightHandDeviceId() 
                : R1_API.GetLeftHandDeviceId();

            if (this.deviceID > -1)
                R1_API.GetDeviceInfo(this.deviceID);
        }

        //-------------------------------------------------------------------------------------------------------------
        // Monobehaviour

        void Start()
        {
            R1_API.Init();
        }


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void OnEnable()
        {
            OnConnectionUpdate(); //because it's possible we are enabled / spawned in a while after the first glove has connected.
            R1_API.OnConnectionChange.AddListener(OnConnectionUpdate);
        }

        void OnDisable()
        {
            R1_API.OnConnectionChange.RemoveListener(OnConnectionUpdate);
            this.deviceID = -1;
        }

        // Update is called once per frame
        void LateUpdate()
        {
            updatePose = true; //re-allow calculations for next frame.
        }


        //-------------------------------------------------------------------------------------------------------------
        // Forward Kinematics

        public static SG_HandPose CalculateSimplePose(int[] perc_flexions, int[] perc_abd, SGCore.Kinematics.BasicHandModel kinematics)
        {
            // perc_flexions / perc_abd are values between... 0 and 10000
            // differs between the left / right gloves?
            SGCore.Kinematics.Vect3D[][] handAngles = new Vect3D[5][];
            float[] normalizedFlex01 = new float[5];

            for (int f=0; f<5; f++)
            {
                normalizedFlex01[f] = perc_flexions.Length > f ? perc_flexions[f] / 10000f : 0.0f; // Flexion is 'easy' since its between 0.0 (fingers straight) and 1.0 (full flexion)
                float abd01 = perc_abd.Length > f ? perc_abd[f] / 10000f : 0.0f; // Flexion is 'easy' since its between 0.0 (fingers straight) and 1.0 (full flexion)
                        
                if (f == 0)
                    handAngles[f] = CalculateThumbAngles(normalizedFlex01[f], abd01, kinematics.IsRight);
                else
                    handAngles[f] = CalculateFingerAngles(normalizedFlex01[f], abd01, kinematics.IsRight);
            }

            SGCore.HandPose iPose = SGCore.HandPose.FromHandAngles(handAngles, kinematics.IsRight, kinematics);
            //SGCore.HandPose iPose = SGCore.HandPose.Fist(kinematics.IsRight, kinematics);

            SG.SG_HandPose res = new SG.SG_HandPose(iPose);
            res.normalizedFlexion = normalizedFlex01; //input the acutal value rather than that from iPose which is calculated off handAngles.
            return res;
        }


        private static Vect3D[] thumb0Values_R = new Vect3D[3]
        {
            new Vect3D(0.0f, -40.0f*Mathf.Deg2Rad, 10.0f*Mathf.Deg2Rad),
            new Vect3D(0.0f, 0.0f, 0.0f),
            new Vect3D(0.0f, 0.0f, 0.0f),
        };

        private static Vect3D[] thumb1Values_R = new Vect3D[3]
        {
            new Vect3D(0, -20*Mathf.Deg2Rad, 60.0f*Mathf.Deg2Rad),
            new Vect3D(0, 45*Mathf.Deg2Rad, 0),
            new Vect3D(0, 60*Mathf.Deg2Rad, 0),
        };


        private static Vect3D[] finger0Values_R = new Vect3D[3]
        {
            new Vect3D(0, 0.0f, -20*Mathf.Deg2Rad),
            new Vect3D(0, 0.0f, 0),
            new Vect3D(0, 0.0f, 0)
        };


        //TODO: A nicer way to declare these.
        private static Vect3D[] finger1Values_R = new Vect3D[3]
        {
            new Vect3D(0, 80*Mathf.Deg2Rad, 20*Mathf.Deg2Rad), 
            new Vect3D(0, 100*Mathf.Deg2Rad, 0), 
            new Vect3D(0, 90*Mathf.Deg2Rad, 0)
        };


        public static Vect3D[] CalculateFingerAngles(float perc_flexions, float perc_abduction, bool rightHand)
        {
            float mcpFlex = SGCore.Kinematics.Values.Map(perc_flexions, 0.0f, 1.0f, finger0Values_R[0].y, finger1Values_R[0].y);
            float pipFlex = SGCore.Kinematics.Values.Map(perc_flexions, 0.0f, 1.0f, finger0Values_R[1].y, finger1Values_R[1].y);
            float dipFlex = SGCore.Kinematics.Values.Map(perc_flexions, 0.0f, 1.0f, finger0Values_R[2].y, finger1Values_R[2].y);

            float mcpAbd = SGCore.Kinematics.Values.Map(perc_abduction, 0.0f, 1.0f, finger0Values_R[0].z, finger1Values_R[0].z);

            return new Vect3D[3]
            {
                new Vect3D(0.0f, mcpFlex, rightHand ? mcpAbd : -mcpAbd), //MCP Twist, Flex, Anb
                new Vect3D(0.0f, pipFlex, 0.0f),
                new Vect3D(0.0f, dipFlex, 0.0f)
            };
        }

        public static Vect3D[] CalculateThumbAngles(float perc_flexions, float perc_abduction, bool rightHand)
        {
            float cmcFlex = SGCore.Kinematics.Values.Map(perc_flexions, 0.0f, 1.0f, thumb0Values_R[0].y, thumb1Values_R[0].y);
            float mcpFlex = SGCore.Kinematics.Values.Map(perc_flexions, 0.0f, 1.0f, thumb0Values_R[1].y, thumb1Values_R[1].y);
            float ipFlex  = SGCore.Kinematics.Values.Map(perc_flexions, 0.0f, 1.0f, thumb0Values_R[2].y, thumb1Values_R[2].y);

            float cmcAbd = SGCore.Kinematics.Values.Map(perc_abduction, 0.0f, 1.0f, thumb0Values_R[0].z, thumb1Values_R[0].z);
            //Debug.Log($"CMC Flexion {perc_flexions} -> {cmcFlex}:");
            return new Vect3D[3]
            {
                new Vect3D(0.0f, cmcFlex, rightHand ? cmcAbd : -cmcAbd), //CMC Twist, Flex, Anb
                new Vect3D(0.0f, mcpFlex, 0.0f),
                new Vect3D(0.0f, ipFlex, 0.0f)
            };
        }


    }








}