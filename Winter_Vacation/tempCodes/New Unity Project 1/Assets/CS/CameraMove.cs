using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public float MoveSpeed = 2f;
    public float RotateSpeed = 0.1f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Y ) )
        {
            this.transform.Translate(Vector3.forward * MoveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.H) )
        {
            this.transform.Translate(Vector3.back * MoveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.Space) )
        {
            this.transform.Translate(Vector3.up * MoveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.LeftShift) )
        {
            this.transform.Translate(Vector3.down * MoveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.G))
        {
            this.transform.Rotate(Vector3.down * RotateSpeed);
        }
        if (Input.GetKey(KeyCode.J))
        {
            this.transform.Rotate(Vector3.up * RotateSpeed);
        }
    }
}
