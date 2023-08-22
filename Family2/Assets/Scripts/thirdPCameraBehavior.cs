using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEditor.ShaderData;

public class thirdPCameraBehavior : MonoBehaviour
{
  //distance between camera and player
  private Vector3 CamOffset = new Vector3(0f, 2.2f, -2.9f);
  //variable to store player transform info
  private Transform targetPlayer;

  private Vector3 angleCamera = new Vector3(15f, 0f, 0f);
  public GameObject player;

  // Start is called before the first frame update
  void Start()
  {
    //targetPlayer = GameObject.Find("Player").transform;

    //rotates camera
    this.transform.Rotate(angleCamera, Space.World);
  }

  /*PlayerController script moves the capsule in its Update
method, we want the code in CameraBehavior to run after the
movement happens; this guarantees that _target has the most up-todate
position to reference.
   */
  void LateUpdate()
  {
    //Sets the camera's position to _target.TransformPoint(CamOffset) for every frame
    //this.transform.position = targetPlayer.TransformPoint(CamOffset);
    this.transform.position = player.transform.position + CamOffset;
    //LookAt method updates the capsule's rotation every frame, focusing on the Transform parameter we pass in,
    //this.transform.LookAt(targetPlayer);
  }
}
