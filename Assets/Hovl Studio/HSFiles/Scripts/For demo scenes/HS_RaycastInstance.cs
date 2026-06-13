using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HS_RaycastInstance : MonoBehaviour
{
    public Camera Cam;
    public GameObject[] Prefabs;
    private int Prefab;
    private Ray RayMouse;
    private GameObject Instance;
    private float windowDpi;

    //Double-click protection
    private float buttonSaver = 0f;

    void Start()
    {
        if (Screen.dpi < 1) windowDpi = 1;
        if (Screen.dpi < 200) windowDpi = 1;
        else windowDpi = Screen.dpi / 200f;
        Counter(0);
    }

    void Update()
    {
        bool fire1Pressed = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
        if (fire1Pressed)
        {
            if (Cam != null)
            {
                RaycastHit hit;
                var mousePos = UnityEngine.InputSystem.Mouse.current != null ? (Vector3)UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector3.zero;
                RayMouse = Cam.ScreenPointToRay(mousePos);
                if (Physics.Raycast(RayMouse.origin, RayMouse.direction, out hit, 40))
                {
                    Instance = Instantiate(Prefabs[Prefab]);
                    Instance.transform.position = hit.point + hit.normal * 0.01f;
                    Destroy(Instance, 1.5f);
                }
            }
            else
            {
                Debug.Log("No camera");
            }          
        }

        bool leftPressed = UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed);
        if (leftPressed && buttonSaver >= 0.4f)// left button
        {
            buttonSaver = 0f;
            Counter(-1);
        }
        bool rightPressed = UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed);
        if (rightPressed && buttonSaver >= 0.4f)// right button
        {
            buttonSaver = 0f;
            Counter(+1);
        }
        buttonSaver += Time.deltaTime;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10 * windowDpi, 5 * windowDpi, 400 * windowDpi, 20 * windowDpi), "Use the keyboard buttons A/<- and D/-> to change prefabs!");
        GUI.Label(new Rect(10 * windowDpi, 20 * windowDpi, 400 * windowDpi, 20 * windowDpi), "Use left mouse button for instancing!");
    }

    void Counter(int count)
    {
        Prefab += count;
        if (Prefab > Prefabs.Length - 1)
        {
            Prefab = 0;
        }
        else if (Prefab < 0)
        {
            Prefab = Prefabs.Length - 1;
        }
    }
}
