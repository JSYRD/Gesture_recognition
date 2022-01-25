using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CreateCubeAndBalls : MonoBehaviour
{
    public GameObject XingDi;
    void Start()
    {

    }
    void Update()
    {

    }
    void OnGUI()
    {
        if (GUILayout.Button("CreateCube", GUILayout.Height(50)))
        {
            GameObject m_cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_cube.AddComponent<Rigidbody>();
            m_cube.GetComponent<Renderer>().material.color = Color.blue;
            m_cube.transform.position = new Vector3(0, 10, 0);
        }
        if (GUILayout.Button("CreateSphere", GUILayout.Height(50)))
        {
            GameObject m_cube = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_cube.AddComponent<Rigidbody>();
            m_cube.GetComponent<Renderer>().material.color = Color.red;
            m_cube.transform.position = new Vector3(0, 10, 0);
        }
        if (GUILayout.Button("CreateXD", GUILayout.Height(50)))
        {
            GameObject m_XD = (GameObject)Instantiate(XingDi, transform.position, transform.rotation);
        }
    }
}