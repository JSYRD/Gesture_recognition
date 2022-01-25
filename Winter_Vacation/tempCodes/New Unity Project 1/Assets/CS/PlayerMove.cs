using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float MoveSpeed = 2f;
    public float RotateSpeed = 1f;
    public int counter = 0;

    // Start is called before the first frame update
    void Start()
    {

    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            this.transform.Translate(Vector3.forward * MoveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            this.transform.Translate(Vector3.back * MoveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            this.transform.Rotate(Vector3.down * RotateSpeed);
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            this.transform.Rotate(Vector3.up * RotateSpeed);
        }
        System.Random rd = new System.Random();
        counter++;
        if(counter == 180){
            counter = 0;
            bool changeDirection = (rd.Next(0,100)>=50)?true:false;
            if(changeDirection) this.transform.Rotate(Vector3.up * 90);
        }
            this.transform.Translate(Vector3.forward * MoveSpeed * Time.deltaTime * 0.1f);
    }
}
