using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class povCameraBehavior : MonoBehaviour
{
  //distance between camera and player
  private Vector3 CamOffset = new Vector3(0f, 1f, -0.5f);
  //variable to store player transform info
  private Transform targetPlayer;

  private Vector3 angleCamera = new Vector3(-25f, 0f, 0f);
  public GameObject player;
  void Start()
  {
    this.transform.Rotate(angleCamera, Space.World);
  }

  
  void LateUpdate()
  {
    this.transform.position = player.transform.position + CamOffset;
  }
}
