using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class PlayerController : MonoBehaviour
{
  //to test thw two types of movement
  public bool movimiento = true;

  public Camera thirdPCamera; //variables for main camera in 3rd person
  public Camera povCamera; //variable for camera in first person
  public KeyCode switchKey; //variable for changing camera
  public GameObject VirtualCamera1;

  //make private when decided on final values
  public float moveSpeed = 30f;
  public float jumpForce = 5f;
  private float gravityModifier = 1f;
  private float horizontalInput;
  private float verticalInput;
  private Rigidbody playerRb;
  public bool isGrounded = true;
  void Start()
  {
    playerRb = GetComponent<Rigidbody>();
    Physics.gravity *= gravityModifier;
  }

  // Update is called once per frame
  void Update()
  {
    // Move in two axis
    horizontalInput = Input.GetAxis("Horizontal") * moveSpeed;
    verticalInput = Input.GetAxis("Vertical") * moveSpeed;

    if (movimiento) 
    {
      //doesn't slide
      Vector3 moveDirection = new Vector3(horizontalInput, 1f, verticalInput) * moveSpeed * Time.deltaTime;
      playerRb.velocity = new Vector3(moveDirection.x, playerRb.velocity.y, moveDirection.z);
    }
    if (!movimiento) 
    {
      //this slides like in ice
      playerRb.AddForce(Vector3.forward * moveSpeed * verticalInput * Time.deltaTime);
      //playerRb.AddForce(Vector3.back * moveSpeed * verticalInput * Time.deltaTime);
      playerRb.AddForce(Vector3.right * moveSpeed * horizontalInput * Time.deltaTime);
      //playerRb.AddForce(Vector3.left * moveSpeed * horizontalInput * Time.deltaTime);
    }
    

    // Jump
    if (isGrounded && Input.GetKeyDown(KeyCode.Space))
    {
      playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
      isGrounded = false;
    }
    //toggle which camera is enabled whenthe F key is pressed
    if (Input.GetKeyDown(switchKey))
    {
      ChangeStateOfGameObject();
      /*thirdPCamera.enabled = !thirdPCamera.enabled;
      povCamera.enabled = !povCamera.enabled;*/
    }

  }
  //Any physics- or Rigidbody-related code always goes inside the FixedUpdate method, rather than Update or the other MonoBehavior methods
  /*void FixedUpdate()
  {
    //Creates a new Vector3 variable to store our left and right rotation
    Vector3 rotation = Vector3.up * horizontalInput;
    //Quaternion.Euler takes a Vector3 parameter and returns a rotation value in Euler angles:
    Quaternion angleRot = Quaternion.Euler(rotation * Time.fixedDeltaTime);
    //Calls MovePosition on our _rb component, which takes in a Vector3 parameter and applies force accordingly
    playerRb.MovePosition(this.transform.position + this.transform.forward * verticalInput * Time.fixedDeltaTime);
    //Calls the MoveRotation method on the _rb component, which also takes in a Vector3 parameter and applies the corresponding forces under the hood
    playerRb.MoveRotation(playerRb.rotation * angleRot);
  }*/

  private void OnCollisionEnter(Collision collision)
  {
    // Check if the character is touching the ground
    if (collision.gameObject.CompareTag("Ground"))
    {
      isGrounded = true;
    }
  }

  void ChangeStateOfGameObject()
  {
    VirtualCamera1.SetActive(!VirtualCamera1.activeInHierarchy);
  }
}