using UnityEngine;
using UnityEngine.UIElements;

namespace R1
{

    public class R1_ExoVisualization : MonoBehaviour
    {
        public Hand handSide = Hand.RIGHT;

        public GameObject exoJointTemplate;
        public GameObject fingerJointTemplate;

        public bool drawDebugLines = true;

        public bool drawExoLines = true;
        public LineRenderer[] exoLines = new LineRenderer[0];
        

        //TODO: Toggle Finger and Exo visualization
        private Transform[][] exoTransforms = null;

        private int deviceID = -1;

        public void CheckCreateExo(Vector3[][] sizeRef)
        {
            if (exoTransforms == null)
            {
                exoTransforms = new Transform[sizeRef.Length][];
                for (int f = 0; f < sizeRef.Length; f++)
                {
                    exoTransforms[f] = new Transform[sizeRef[f].Length];
                    for (int j = 0; j < sizeRef[f].Length; j++)
                    {
                        GameObject obj = GameObject.Instantiate(exoJointTemplate, this.transform);
                        obj.name = $"Exo{f}-{j}";
                        exoTransforms[f][j] = obj.transform;
                    }
                    if (exoLines.Length > f)
                    {
                        exoLines[f].positionCount = sizeRef[f].Length;
                    }
                }
            }
        }

        public void UpdateExo()
        {

        }


        private void OnNewData()
        {
            if (this.deviceID < 0)
                return;

            if (R1_API.GetExoJoints_Pos_Rot(deviceID, out Vector3[][] positions, out Quaternion[][] rotations))
            {
                CheckCreateExo(positions);
                for (int f = 0; f < positions.Length; f++)
                {
                    for (int j = 0; j < positions[f].Length; j++)
                    {
                        exoTransforms[f][j].localRotation = rotations[f][j];
                        exoTransforms[f][j].localPosition = positions[f][j];
                    }
                }
            }
        }

        private void OnConnectionChange()
        {
            if (this.deviceID < 0)
            {
                this.deviceID = handSide == Hand.RIGHT ? R1_API.GetRightHandDeviceId() : R1_API.GetLeftHandDeviceId(); 
            }
        }


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void OnEnable()
        {
            R1_API.Init();
            OnConnectionChange();
            OnNewData();
            R1_API.OnConnectionChange.AddListener(OnConnectionChange);
            R1_API.OnNewData.AddListener(OnNewData);
        }

        // Update is called once per frame
        void OnDisable()
        {
            R1_API.OnConnectionChange.RemoveListener(OnConnectionChange);
            R1_API.OnNewData.RemoveListener(OnNewData);
        }

        private void Update()
        {
            if (exoTransforms != null)
            {
                if (drawDebugLines)
                {
                    for (int f = 0; f < exoTransforms.Length; f++)
                    {
                        Vector3 prev = this.transform.position;
                        for (int j = 0; j < exoTransforms[f].Length; j++)
                        {
                            Debug.DrawLine(prev, exoTransforms[f][j].position, Color.white);
                            prev = exoTransforms[f][j].position;
                        }
                    }
                }
                if (drawExoLines)
                {
                    for (int f = 0; f < exoTransforms.Length && f < exoLines.Length; f++)
                    {
                        Vector3[] pos = new Vector3[exoTransforms[f].Length];
                        for (int j = 0; j < exoTransforms[f].Length; j++)
                        {
                            pos[j] = exoTransforms[f][j].position;
                        }
                        exoLines[f].SetPositions(pos);
                    }
                }
            }
        }
    }
}