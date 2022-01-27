using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    // private Rigidbody _rigidbody;

    public float speed = 0.3f;
    void Start()
    {
        //cd_rigidbody = this.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // var h = Input.GetAxis("Horizontal");
        // var v = Input.GetAxis("Vertical");
        // _rigidbody.AddForce(new Vector3(h, 0, v) * speed);
        
        //

        this.transform.rotation *= Quaternion.AngleAxis(1f * speed, Vector3.left + Vector3.forward);
    }
}