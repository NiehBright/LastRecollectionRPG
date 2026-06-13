using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System;
using UnityEngine;

public class HS_DemoShooting2D : MonoBehaviour
{
    public GameObject FirePoint;
    public Camera Cam;
    public float MaxLength;
    public GameObject[] Prefabs;

    private Ray RayMouse;
    private Vector3 direction;
    private Quaternion rotation;

    [Header("GUI")]
    private float windowDpi;
    private int Prefab;
    private GameObject Instance;
    private float hSliderValue = 0.1f;
    private float fireCountdown = 0f;

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
        //Single shoot
        bool fire1Pressed = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
        if (fire1Pressed)
        {
            Instantiate(Prefabs[Prefab], FirePoint.transform.position, FirePoint.transform.rotation);
        }

        //Fast shooting
        bool fire2Pressed = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.isPressed;
        if (fire2Pressed && fireCountdown <= 0f)
        {
            Instantiate(Prefabs[Prefab], FirePoint.transform.position, FirePoint.transform.rotation);
            fireCountdown = 0;
            fireCountdown += hSliderValue;
        }
        fireCountdown -= Time.deltaTime;

        //To change projectiles
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

        //To rotate fire point
        if (Cam != null)
        {
            var rawMousePos = UnityEngine.InputSystem.Mouse.current != null ? (Vector3)UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector3.zero;
            var mousePos = Camera.main.ScreenToWorldPoint(rawMousePos);
            transform.rotation = Quaternion.LookRotation(Vector3.forward, mousePos - transform.position);
        }
        else
        {
            Debug.Log("No camera");
        }
    }

    //GUI Text
    void OnGUI()
    {
        GUI.Label(new Rect(10 * windowDpi, 5 * windowDpi, 400 * windowDpi, 20 * windowDpi), "Use left mouse button to single shoot!");
        GUI.Label(new Rect(10 * windowDpi, 25 * windowDpi, 400 * windowDpi, 20 * windowDpi), "Use and hold the right mouse button for quick shooting!");
        GUI.Label(new Rect(10 * windowDpi, 45 * windowDpi, 400 * windowDpi, 20 * windowDpi), "Fire rate:");
        hSliderValue = GUI.HorizontalSlider(new Rect(70 * windowDpi, 50 * windowDpi, 100 * windowDpi, 20 * windowDpi), hSliderValue, 0.0f, 1.0f);
        GUI.Label(new Rect(10 * windowDpi, 65 * windowDpi, 400 * windowDpi, 20 * windowDpi), "Use the keyboard buttons A/<- and D/-> to change projectiles!");
    }

    // To change prefabs (count - prefab number)
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
