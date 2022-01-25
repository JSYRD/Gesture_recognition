using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform m_transform;

    Transform m_camTransform;
    Vector3 m_camRot;
    float m_camHeight = 1.4f;
    
    CharacterController m_ch;
    float m_speed = 3.0f;
    float m_gravity = 2.0f;

    void Start()
    {
        m_transform = this.transform;
        m_ch = this.GetComponent<CharacterController>();
        //Get camera
        m_camTransform = Camera.main.transform;
        
        Vector3 pos = m_transform.position;//获得玩家的pos
        pos.y += m_camHeight;
        m_camTransform.position = pos;//设置相机初始高度

        m_camTransform.rotation = m_transform.rotation;//获得玩家的方位
        m_camRot = m_camTransform.eulerAngles;//camRot用于调整玩家的初始方位
        Screen.lockCursor = true;//锁定光标
    }

    // Update is called once per frame
    void Update()
    ///
    {
        Control();
    }
    void Control()
    /*
    * 调整玩家位置和视角
    */
    {
        float rh = Input.GetAxis("Mouse X");
        float rv = Input.GetAxis("Mouse Y");
        m_camRot.x -= rv;
        m_camRot.y += rh;
        m_camTransform.eulerAngles = m_camRot;//调整cam视角

        Vector3 camrot = m_camTransform.eulerAngles;//camrot用于调整玩家的视角。
        camrot.x = 0;camrot.z = 0;
        m_transform.eulerAngles = camrot;

        Vector3 pos = m_transform.position;
        pos.y += m_camHeight;
        m_camTransform.position = pos;//调整cam的高度


        float xm = 0, ym = 0, zm = 0;//移动
        ym -= m_gravity * Time.deltaTime;
        if (Input.GetKey(KeyCode.W))
        {
            zm += m_speed * Time.deltaTime;
        }else if (Input.GetKey(KeyCode.S))
        {
            zm -= m_speed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            xm -= m_speed * Time.deltaTime;
        }else if (Input.GetKey(KeyCode.D))
        {
            xm += m_speed * Time.deltaTime;
        }
        m_ch.Move(m_transform.TransformDirection(new Vector3(xm, ym, zm)));
    }
}
/*Summary:
 * 对象的位置(position)和角度(eulerAngles)等都在Transform中。
 * 
 * position, eulerAngles是Vector3类型
 * 
 */
