using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camaracontroller : MonoBehaviour
{
    public float moveSpeed;
    public float rotSpeed;
    public Transform[] positions;

    Transform currentView;

    void Start()
    {
        currentView = positions[0];
    }

    public void ChangeCameraPosition(int index)
    {
        currentView = positions[index];
    }

    void LateUpdate()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            currentView.position,
            Time.deltaTime * moveSpeed
        );

        Vector3 desiredAngle = Vector3.Lerp(
            transform.localEulerAngles,
            currentView.localEulerAngles,
            Time.deltaTime * rotSpeed
        );

        transform.localEulerAngles = desiredAngle;
    }
}