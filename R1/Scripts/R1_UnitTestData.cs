//#define R1_UNIT_TEST_ENABLED //comment this out to disable unit tests and therefore the dependency on Newron

using System;
using System.Collections.Generic;
#if R1_UNIT_TEST_ENABLED
using Newtonsoft.Json;
#endif
using UnityEngine;

namespace R1
{

    public static class R1_UnitTestData
    {
        
        private static List<RawTestCase> raw_tests = new List<RawTestCase>();
        private static List<TestCase> tests = new List<TestCase>();
        private static bool loadAttempt = false;

        private static int activeTestIndex = 0;

        public static int GetTestCount()
        {
            LoadTestCases();
            return tests.Count;
        }


        public static void NextActiveTest()
        {
            LoadTestCases();
            activeTestIndex++;
            if (activeTestIndex >= tests.Count)
                activeTestIndex = 0;

            if (activeTestIndex > -1 && activeTestIndex < tests.Count)
                Debug.Log("Active Test Set to " + tests[activeTestIndex].name);
        }


        public static void PreviousActiveTest()
        {
            LoadTestCases();
            activeTestIndex--;
            if (activeTestIndex < 0)
                activeTestIndex = tests.Count - 1;
            
            if (activeTestIndex > -1 && activeTestIndex < tests.Count)
                Debug.Log("Active Test Set to " + tests[activeTestIndex].name);
        }


        public static bool LoadCurrentTest(out TestCase test_case)
        {
            test_case = null;
            LoadTestCases();
            if (activeTestIndex > -1 && activeTestIndex < tests.Count)
            {
                test_case = tests[activeTestIndex];
            }
            return test_case != null;
        }

        public static bool LoadTestCase_ByName(string testCaseName, out TestCase test_case)
        {
            test_case = null;
            LoadTestCases();
            foreach (TestCase tcase in tests)
            {
                if (tcase.name.Equals(testCaseName, StringComparison.OrdinalIgnoreCase))
                {
                    test_case = tcase;
                    return true;
                }
            }
            return test_case != null;
        }

        public static bool LoadTestCase_ByIndex(int testCaseIndex, out TestCase test_case)
        {
            LoadTestCases();
            test_case = tests.Count > -1 && testCaseIndex < tests.Count ? tests[testCaseIndex] : null;
            return test_case != null;
        }

        public static void LoadTestCases()
        {
            if (loadAttempt)
                return;
            loadAttempt = true;

            try
            {
                raw_tests = R1_UnitTestData.LoadFromResources("test_cases");

                Debug.Log($"Loaded {raw_tests.Count} test cases.");
                tests = new List<TestCase>(raw_tests.Count);
                for (int i=0; i<raw_tests.Count; i++)
                {
                    tests.Add( TestCase.FromRaw(raw_tests[i]) );
                }
                if (activeTestIndex > -1 && activeTestIndex < tests.Count)
                    Debug.Log("Active Test Set to " + tests[activeTestIndex].name);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
                tests = new List<TestCase>(); //empty if anything goes wrong.
            }
        }

        /// <summary>
        /// Loads a JSON file from Resources.
        /// Example path: Resources/TestData/test_cases.json
        /// Pass "TestData/test_cases" (without extension).
        /// </summary>
        public static List<RawTestCase> LoadFromResources(string resourcePath)
        {
#if R1_UNIT_TEST_ENABLED
            TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);

            if (jsonFile == null)
            {
                Debug.LogError($"Could not find Resources/{resourcePath}.json");
                return null;
            }

            return JsonConvert.DeserializeObject<List<RawTestCase>>(jsonFile.text);
#else
            return new List<RawTestCase>();
#endif
        }

        /// <summary>
        /// Loads a JSON string directly.
        /// </summary>
        public static List<RawTestCase> LoadFromJson(string json)
        {
#if R1_UNIT_TEST_ENABLED
            return JsonConvert.DeserializeObject<List<RawTestCase>>(json);
#else
            return new List<RawTestCase>();
#endif
        }
    }



    public class TestCase
    {
        public string name;

        //public List<List<float>> exo_angles;

        public int[] abd_perc_bents;
        public int[] flex_perc_bents;

        public float[] flex_perc_bent_angles;
        public float[] abd_perc_bent_angles;

        public Vector3[][] exo_poss;
        public Quaternion[][] exo_rots;

        public float[][] exo_angles_rad;
        public float[][] exo_angles_deg;

        public Vector3[] fingerip_poss;
        public Quaternion[] fingerip_rots;

        public float[] fingertip_distances;

        public TestCase()
        {

        }



        public static TestCase FromRaw(RawTestCase rCase)
        {
            TestCase res = new TestCase();

            res.name = rCase.name;
            res.abd_perc_bents = rCase.abd_perc_bents.ToArray();
            res.flex_perc_bents = rCase.flex_perc_bents.ToArray();
            res.flex_perc_bent_angles = rCase.flex_perc_bent_angles.ToArray();
            res.abd_perc_bent_angles = rCase.abd_perc_bent_angles.ToArray();

            Vector3[][] jPos = new Vector3[rCase.exo_poss.Count][];
            Quaternion[][] jRot = new Quaternion[rCase.exo_poss.Count][];
            for (int f = 0; f < rCase.exo_poss.Count; f++)
            {
                jPos[f] = new Vector3[rCase.exo_poss[f].Count];
                jRot[f] = new Quaternion[rCase.exo_rots[f].Count];
                for (int j = 0; j < rCase.exo_poss[f].Count; j++)
                {
                    jPos[f][j] = ToUnityVect(rCase.exo_poss[f][j]);
                    jRot[f][j] = ToUnityQuat(rCase.exo_rots[f][j]);
                }
            }
            res.exo_poss = jPos;
            res.exo_rots = jRot;

            float[][] angls = new float[rCase.exo_angles.Count][];
            float[][] angls_deg = new float[rCase.exo_angles.Count][];
            for (int f=0; f<angls.Length; f++)
            {
                angls[f] = rCase.exo_angles[f].ToArray();
                angls_deg[f] = new float[angls[f].Length];
                for (int j = 0; j < angls[f].Length; j++)
                {
                    angls_deg[f][j] = Mathf.Rad2Deg * angls[f][j];
                }
            }
            res.exo_angles_rad = angls;
            res.exo_angles_deg = angls_deg;

            Vector3[] fingerpos = new Vector3[rCase.fingertips_poss.Count];
            Quaternion[] fingerrot = new Quaternion[rCase.fingertips_poss.Count];
            for (int f=0; f<rCase.fingertips_poss.Count; f++)
            {
                fingerpos[f] = ToUnityVect(rCase.fingertips_poss[f]);
                fingerrot[f] = ToUnityQuat(rCase.fingertips_rots[f]);
            }
            res.fingerip_poss = fingerpos;
            res.fingerip_rots = fingerrot;

            float[] fingerDist = new float[res.fingerip_poss.Length - 1]; //using this for 'number of fingers'
            for (int i=1; i<fingerpos.Length; i++)
            {
                fingerDist[i - 1] = (fingerpos[i] - fingerpos[0]).magnitude;
            }
            res.fingertip_distances = fingerDist;


            return res;
        }


        private static Vector3 ToUnityVect(List<float> pythonPos)
        {
            return new Vector3(
                pythonPos[0] / 1000.0f, //X
                pythonPos[2] / 1000.0f, //Z
                pythonPos[1] / 1000.0f  //Y
                );
        }

        private static Quaternion ToUnityQuat(List<float> pythonQuat)
        {
            //python Quat [w x y z]
            //-quat.x, -quat.z, -quat.y, quat.w
            return new Quaternion(
                -pythonQuat[1], // -X
                -pythonQuat[3], // -Z
                -pythonQuat[2], // -Y
                pythonQuat[0] // W
            );
        }

    }



    //-------------------------------------------------------------
    // For JSOn stuff because I'm lazy

    [Serializable]
    public class RawTestCase
    {
        public string name;

        public List<List<float>> exo_angles;

        public List<List<List<float>>> exo_poss;
        public List<List<List<float>>> exo_rots;

        public List<List<float>> fingertips_poss;
        public List<List<float>> fingertips_rots;

        public List<float> flex_perc_bent_angles;
        public List<float> abd_perc_bent_angles;
        public List<int> flex_perc_bents;
        public List<int> abd_perc_bents;
    }


}