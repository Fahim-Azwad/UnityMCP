using UnityEngine;

public class SpinnerSetup : MonoBehaviour
{
    void Start()
    {
        // Find the Spinner object and add Rotator component if it doesn't have one
        GameObject spinner = GameObject.Find("Spinner");
        if (spinner != null && spinner.GetComponent<Rotator>() == null)
        {
            spinner.AddComponent<Rotator>();
            Debug.Log("Rotator component added to Spinner!");
        }
    }
}
