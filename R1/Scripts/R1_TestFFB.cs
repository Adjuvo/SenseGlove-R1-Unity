using R1;
using UnityEngine;
using UnityEngine.UI;

public class R1_TestFFB : MonoBehaviour
{
    public Button forcesOnButton;
    public Button forcesOffButton;

    private void OnEnable()
    {
        forcesOnButton.onClick.AddListener(ForcesOn);
        forcesOffButton.onClick.AddListener(ForcesOff);
    }


    public const int testForce = 4000;
    protected int[] testForces = new int[5] { testForce, testForce, testForce, testForce, 0 };


    public void ForcesOn()
    {
        Debug.Log("Forces ON");
        SendForceToAll(testForces);
    }

    public void ForcesOff()
    {
        Debug.Log("Forces OFF");
        SendForceToAll(new int[5]);
    }

    public static void SendForceToAll(int[] forces)
    {
        int[] devices = R1_API.GetDeviceIds();
        foreach (int id in devices)
        {
            R1_API.SetForceGoals(id, forces);
        }
    }

    private void OnDisable()
    {
        forcesOnButton.onClick.RemoveListener(ForcesOn);
        forcesOffButton.onClick.RemoveListener(ForcesOff);
    }
}
