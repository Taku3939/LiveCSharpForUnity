using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    private Rigidbody _rigidbody;

    public float speed = 0.3f;
    void Start()
    {
        _rigidbody = this.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        var h = Input.GetAxis("Horizontal");
        var v = Input.GetAxis("Vertical");
        _rigidbody.AddForce(new Vector3(h, 0, v) * speed);
    }
}