using UnityEditor.VisionOS;
using UnityEngine;

public class ComputerController : MonoBehaviour
{

public GameObject mouse;
public float sensitivity;

public GameObject icon;
public GameObject browser;

    void Update()
    {
        transform.localPosition = new Vector3(-mouse.transform.localPosition.z * sensitivity, -mouse.transform.localPosition.x * sensitivity, transform.localPosition.z);
    }

    public void OpenBrowser()
    {
        icon.SetActive(false);
        browser.SetActive(true);
    }
}

