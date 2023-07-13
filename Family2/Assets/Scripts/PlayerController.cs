using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    private Rigidbody playerRb;
    public bool isGrounded;
    // Start is called before the first frame update
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // Move horizontally
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        /*Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput) * moveSpeed * Time.deltaTime;
        playerRb.velocity = new Vector3(moveDirection.x, playerRb.velocity.y, moveDirection.z);*/

        playerRb.AddForce(Vector3.forward * moveSpeed * verticalInput * Time.deltaTime);
        playerRb.AddForce(Vector3.right * moveSpeed * horizontalInput * Time.deltaTime);

    // Jump
    if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            playerRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        // Check if the character is touching the ground
        if (other.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
}
